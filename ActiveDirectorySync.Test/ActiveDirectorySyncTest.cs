/*
    Copyright (C) 2014 Omega software d.o.o.

    This file is part of Rhetos.

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU Affero General Public License as
    published by the Free Software Foundation, either version 3 of the
    License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Affero General Public License for more details.

    You should have received a copy of the GNU Affero General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using ActiveDirectorySync.Test.Helpers;
using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.Persistence;
using Rhetos.TestCommon;
using Rhetos.Utilities;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ActiveDirectorySync.Test
{
    [TestClass]
    public class ActiveDirectorySyncTest
    {
        [TestMethod]
        public void TestMock()
        {
            using (var container = new MockWindowsSecurityRhetosContainer("u1-r1 u1-r12 u2-r12 u2-r2", commitChanges: false))
            {
                var u1 = container.NewPrincipal("u1");
                Console.WriteLine(u1.Name);

                var ws = container.Resolve<Rhetos.Security.IWindowsSecurity>();
                Assert.AreEqual("r1, r12", TestUtility.DumpSorted(ws.GetIdentityMembership(u1.Name), container.Shorten), "u1 active directory groups");
            }
        }

        [TestMethod]
        public void ComputeOnInsertPrincipal()
        {
            using (var container = new MockWindowsSecurityRhetosContainer("u1-r1 u1-r12 u2-r12 u2-r2", commitChanges: false))
            {
                // Insert test data:

                var u1 = container.NewPrincipal("u1");
                var u2 = container.NewPrincipal("u2");
                var u3 = container.NewPrincipal("u3");
                var u5 = container.NewPrincipal("u5", domain: false);
                var r1 = container.NewRole("r1");
                var r2 = container.NewRole("r2");
                var r25 = container.NewRole("r25", domain: false);

                var roles = container.Resolve<GenericRepository<Common.Role>>();
                var principals = container.Resolve<GenericRepository<IPrincipal>>();
                var membership = container.Resolve<GenericRepository<Common.PrincipalHasRole>>();

                roles.Insert(r1, r2, r25);
                Assert.AreEqual(@"\r1, \r2, r25", container.ReportRoles(roles.Load()), "roles created");

                principals.Insert(u1, u2, u3, u5);
                Assert.AreEqual(@"\u1, \u2, \u3, u5", container.ReportPrincipals(principals.Load()), "principals created");

                // Recompute membership on insert domain users:

                Assert.AreEqual(@"\u1-\r1, \u2-\r2", container.ReportMembership(), "auto-membership on insert");

                // Inserting non-domain users and roles:

                membership.Insert(new[] {
                    new Common.PrincipalHasRole { PrincipalID = u2.ID, RoleID = r25.ID },
                    new Common.PrincipalHasRole { PrincipalID = u5.ID, RoleID = r25.ID } },
                    checkUserPermissions: true);
                Assert.AreEqual(@"\u1-\r1, \u2-\r2, \u2-r25, u5-r25", container.ReportMembership(), "non-domain users and roles");
            }
        }

        [TestMethod]
        public void ComputeOnUpdatePrincipal()
        {
            using (var container = new MockWindowsSecurityRhetosContainer("u1-r1 u1-r12 u2-r12 u2-r2", commitChanges: false))
            {
                // Insert test data:

                var u1 = container.NewPrincipal("u1");
                var u2 = container.NewPrincipal("u2");
                var u3 = container.NewPrincipal("u3");
                var u5 = container.NewPrincipal("u5", domain: false);
                var r1 = container.NewRole("r1");
                var r2 = container.NewRole("r2");
                var r25 = container.NewRole("r25", domain: false);

                var roles = container.Resolve<GenericRepository<Common.Role>>();
                var principals = container.Resolve<GenericRepository<IPrincipal>>();
                var membership = container.Resolve<GenericRepository<Common.PrincipalHasRole>>();

                roles.Insert(r1, r2, r25);
                principals.Insert(u1, u2, u3, u5);
                Assert.AreEqual(@"\u1-\r1, \u2-\r2", container.ReportMembership(), "auto-membership on insert");

                membership.Insert(new[] { // Non-domain users and roles.
                    new Common.PrincipalHasRole { PrincipalID = u2.ID, RoleID = r25.ID },
                    new Common.PrincipalHasRole { PrincipalID = u5.ID, RoleID = r25.ID } });
                Assert.AreEqual(@"\u1-\r1, \u2-\r2, \u2-r25, u5-r25", container.ReportMembership(), "manual membership for non-domain roles and users");

                // Recompute membership on update principal (domain users only):

                u2 = principals.Load(new[] { u2.ID }).Single(); // Refresh before modification.
                u2.Name = container.NewName("u2x", domain: false);
                principals.Update(u2);
                Assert.AreEqual(@"\u1-\r1, u2x-\r2, u2x-r25, u5-r25", container.ReportMembership(), "auto-membership on update ignore non-domain users");
                u2.Name = container.NewName("u2x");
                principals.Update(u2);
                Assert.AreEqual(@"\u1-\r1, \u2x-r25, u5-r25", container.ReportMembership(), "auto-membership on update domain users");
                u2.Name = container.NewName("u2");
                principals.Update(u2);
                Assert.AreEqual(@"\u1-\r1, \u2-\r2, \u2-r25, u5-r25", container.ReportMembership(), "auto-membership on update domain users 2");
            }
        }

        [TestMethod]
        public void CommonFilters()
        {
            using (var container = new MockWindowsSecurityRhetosContainer("u1-r1 u1-r12 u2-r12 u2-r2", commitChanges: false))
            {
                // Insert test data:

                var u1 = container.NewPrincipal("u1");
                var u2 = container.NewPrincipal("u2");
                var u3 = container.NewPrincipal("u3");
                var u5 = container.NewPrincipal("u5", domain: false);
                var r1 = container.NewRole("r1");
                var r2 = container.NewRole("r2");
                var r25 = container.NewRole("r25", domain: false);

                var repository = container.Resolve<Common.DomRepository>();
                var roles = container.Resolve<GenericRepository<Common.Role>>();
                var principals = container.Resolve<GenericRepository<IPrincipal>>();
                var membership = repository.Common.PrincipalHasRole;

                roles.Insert(r1, r2, r25);
                principals.Insert(u1, u2, u3, u5);
                membership.Insert(new[] { // Non-domain users and roles.
                    new Common.PrincipalHasRole { PrincipalID = u2.ID, RoleID = r25.ID },
                    new Common.PrincipalHasRole { PrincipalID = u5.ID, RoleID = r25.ID } });

                // Common filters:

                var filter1 = new Common.ActiveDirectoryAllUsersParameter();
                Assert.AreEqual(@"\u1-\r1, \u2-\r2",
                    container.ReportMembership(filter1),
                    "filter ActiveDirectoryAllUsersParameter");

                var filter2 = new[] { u1.Name, u2.Name }.Select(name => new Common.ActiveDirectoryUserParameter { UserName = name }).ToArray();
                Assert.AreEqual(@"\u1-\r1, \u2-\r2",
                    container.ReportMembership(filter2),
                    "filter ActiveDirectoryUserParameter");
            }
        }

        [TestMethod]
        public void UserShouldNotUpdateDomainMembership()
        {
            using (var container = new MockWindowsSecurityRhetosContainer("u1-r1 u1-r12 u2-r12 u2-r2", commitChanges: false))
            {
                // Insert test data:

                var u1 = container.NewPrincipal("u1");
                var u2 = container.NewPrincipal("u2");
                var r1 = container.NewRole("r1");
                var r2 = container.NewRole("r2");

                var repository = container.Resolve<Common.DomRepository>();
                var roles = container.Resolve<GenericRepository<Common.Role>>();
                var principals = container.Resolve<GenericRepository<IPrincipal>>();
                var membership = repository.Common.PrincipalHasRole;

                roles.Insert(r1, r2);
                principals.Insert(u1, u2);

                // The user should not update domain users/groups membership:

                Assert.AreEqual(@"\u1-\r1, \u2-\r2", container.ReportMembership());

                var u2r2 = membership.Query(m => m.Principal.Name.Contains(@"\u2")).Single();
                membership.Delete(new[] { u2r2 }, checkUserPermissions: false);
                Assert.AreEqual(@"\u1-\r1", container.ReportMembership());

                var u1r1 = membership.Query(m => m.Principal.Name.Contains(@"\u1")).Single();
                TestUtility.ShouldFail(
                    () => membership.Delete(new[] { u1r1 }, checkUserPermissions: true),
                    $"It is not allowed to remove the user membership here, because role {r1.Name} is synchronized with an Active Directory group");
            }
        }

        [TestMethod]
        public void RecomputeMembership()
        {
            using (var container = new MockWindowsSecurityRhetosContainer("u1-r1 u1-r12 u2-r12 u2-r2", commitChanges: false))
            {
                // Insert test data:

                var u1 = container.NewPrincipal("u1");
                var u2 = container.NewPrincipal("u2");
                var u3 = container.NewPrincipal("u3");
                var u5 = container.NewPrincipal("u5", domain: false);
                var r1 = container.NewRole("r1");
                var r2 = container.NewRole("r2");
                var r25 = container.NewRole("r25", domain: false);

                var repository = container.Resolve<Common.DomRepository>();
                var roles = container.Resolve<GenericRepository<Common.Role>>();
                var principals = container.Resolve<GenericRepository<IPrincipal>>();
                var membership = repository.Common.PrincipalHasRole;

                roles.Insert(r1, r2, r25);
                principals.Insert(u1, u2, u3, u5);
                membership.Insert(new[] { // Non-domain users and roles.
                    new Common.PrincipalHasRole { PrincipalID = u2.ID, RoleID = r25.ID },
                    new Common.PrincipalHasRole { PrincipalID = u5.ID, RoleID = r25.ID } });

                // Recompute membership relations:

                var u1r1 = membership.Query(m => m.Principal.Name.Contains(@"\u1")).Single();
                membership.Delete(new[] { u1r1 }, checkUserPermissions: false);
                Assert.AreEqual(@"\u2-\r2, \u2-r25, u5-r25", container.ReportMembership(), "modified membership");

                repository.Common.PrincipalHasRole.RecomputeFromActiveDirectoryPrincipalHasRole();
                Assert.AreEqual(@"\u1-\r1, \u2-\r2, \u2-r25, u5-r25", container.ReportMembership(), "recomputed membership");
            }
        }

        [TestMethod]
        public void ComputeOnUpdateRole()
        {
            using (var container = new MockWindowsSecurityRhetosContainer("u1-r1 u1-r12 u2-r12 u2-r2", commitChanges: false))
            {
                // Insert test data:

                var u1 = container.NewPrincipal("u1");
                var u2 = container.NewPrincipal("u2");
                var u3 = container.NewPrincipal("u3");
                var u5 = container.NewPrincipal("u5", domain: false);
                var r1 = container.NewRole("r1");
                var r2 = container.NewRole("r2");
                var r25 = container.NewRole("r25", domain: false);

                var roles = container.Resolve<GenericRepository<Common.Role>>();
                var principals = container.Resolve<GenericRepository<IPrincipal>>();
                var membership = container.Resolve<GenericRepository<Common.PrincipalHasRole>>();

                roles.Insert(r1, r2, r25);
                principals.Insert(u1, u2, u3, u5);
                membership.Insert(new[] { // Non-domain users and roles.
                    new Common.PrincipalHasRole { PrincipalID = u2.ID, RoleID = r25.ID },
                    new Common.PrincipalHasRole { PrincipalID = u5.ID, RoleID = r25.ID } });

                // Recompute membership on modified role should remove obsolete memebers:

                Assert.AreEqual(@"\u1-\r1, \u2-\r2, \u2-r25, u5-r25", container.ReportMembership(), "initial membership");

                r2.Name = container.NewName("r2x");
                roles.Update(r2);
                Assert.AreEqual(@"\u1-\r1, \u2-r25, u5-r25", container.ReportMembership(), "recomputed membership after rename role");

                // New role members will not be added automatically, to avoid performance penalty:
                // (the membership will be added on the principal's authorization check)

                r2.Name = container.NewName("r2");
                roles.Update(r2);
                // This is not reqested feature, this assert simply describes currently implemented behaviour:
                Assert.AreEqual(@"\u1-\r1, \u2-r25, u5-r25", container.ReportMembership());
            }
        }

        [TestMethod]
        public void RoleInheritance()
        {
            using (var container = new MockWindowsSecurityRhetosContainer("u1-r1 u1-r12 u2-r12 u2-r2", commitChanges: false))
            {
                // Insert test data:

                var u1 = container.NewPrincipal("u1");
                var u2 = container.NewPrincipal("u2");
                var u3 = container.NewPrincipal("u3");
                var u5 = container.NewPrincipal("u5", domain: false);
                var r1 = container.NewRole("r1");
                var r2 = container.NewRole("r2");
                var r25 = container.NewRole("r25", domain: false);

                var repository = container.Resolve<Common.DomRepository>();
                var roles = container.Resolve<GenericRepository<Common.Role>>();
                var principals = container.Resolve<GenericRepository<IPrincipal>>();
                var membership = container.Resolve<GenericRepository<Common.PrincipalHasRole>>();

                roles.Insert(r1, r2, r25);
                principals.Insert(u1, u2, u3, u5);
                membership.Insert(new[] { // Non-domain users and roles.
                    new Common.PrincipalHasRole { PrincipalID = u2.ID, RoleID = r25.ID },
                    new Common.PrincipalHasRole { PrincipalID = u5.ID, RoleID = r25.ID } });

                // Modify role inheritance:

                repository.Common.RoleInheritsRole.Insert(new[] { new Common.RoleInheritsRole {
                    UsersFromID = r1.ID, PermissionsFromID = r25.ID } });

                TestUtility.ShouldFail(() => repository.Common.RoleInheritsRole.Insert(new[] { new Common.RoleInheritsRole {
                    UsersFromID = r25.ID, PermissionsFromID = r2.ID } }), "UserException",
                    "It is not allowed to add users or user groups here because this role is synchronized with an Active Directory group.",
                    "Please change the user membership on Active Directory instead.");
            }
        }

        [TestMethod]
        public void ComputeOnAuthorization()
        {
            using (var container = new MockWindowsSecurityRhetosContainer("u1-r1 u1-r12 u2-r12 u2-r2", commitChanges: false))
            {
                // Insert test data:

                var u1 = container.NewPrincipal("u1");
                var u2 = container.NewPrincipal("u2");
                var u3 = container.NewPrincipal("u3");
                var u5 = container.NewPrincipal("u5", domain: false);
                var r1 = container.NewRole("r1");
                var r2 = container.NewRole("r2");
                var r25 = container.NewRole("r25", domain: false);

                var roles = container.Resolve<GenericRepository<Common.Role>>();
                var principals = container.Resolve<GenericRepository<IPrincipal>>();
                var membership = container.Resolve<GenericRepository<Common.PrincipalHasRole>>();

                roles.Insert(r1, r2, r25);
                principals.Insert(u1, u2, u3, u5);
                membership.Delete(membership.Load());
                membership.Insert(new[] { // Non-domain users and roles.
                    new Common.PrincipalHasRole { PrincipalID = u2.ID, RoleID = r25.ID },
                    new Common.PrincipalHasRole { PrincipalID = u5.ID, RoleID = r25.ID } });

                // Recompute membership on authorization:

                var authorizationProvider = container.Resolve<CommonAuthorizationProvider>();

                Assert.AreEqual(@"\u2-r25, u5-r25", container.ReportMembership());

                {
                    var userRoles = authorizationProvider.GetUsersRoles(u1);
                    Assert.AreEqual(@"\r1", container.ReportRoles(userRoles));
                    Assert.AreEqual(@"\u1-\r1, \u2-r25, u5-r25", container.ReportMembership(), "membership recomputed on authorization u1");
                }

                {
                    var userRoles = authorizationProvider.GetUsersRoles(u2);
                    Assert.AreEqual(@"\r2, r25", container.ReportRoles(userRoles), "mixed membership");
                    Assert.AreEqual(@"\u1-\r1, \u2-\r2, \u2-r25, u5-r25", container.ReportMembership(), "membership recomputed on authorization u2");
                }

                AuthorizationDataCache.ClearCache();
                membership.Delete(membership.Load());
                Assert.AreEqual(@"", container.ReportMembership(), "membership deleted");

                {
                    var userRoles = authorizationProvider.GetUsersRoles(u1);
                    Assert.AreEqual(@"\r1", container.ReportRoles(userRoles));
                    Assert.AreEqual(@"\u1-\r1", container.ReportMembership(), "membership recomputed on authorization u1");
                }

                {
                    var userRoles = authorizationProvider.GetUsersRoles(u2);
                    Assert.AreEqual(@"\r2", container.ReportRoles(userRoles));
                    Assert.AreEqual(@"\u1-\r1, \u2-\r2", container.ReportMembership(), "membership recomputed on authorization u2");
                }
            }
        }

        [TestMethod]
        public void ParallelRequests()
        {
            string u1Name = TestName + "U";
            string r1Name = TestName + "P";
            string commonTestSuffix = Guid.NewGuid().ToString().Replace("-", "");

            IPrincipal u1;
            Common.Role r1;

            using (var container = new MockWindowsSecurityRhetosContainer($"{u1Name}-{r1Name}", commitChanges: true, commonTestSuffix))
            {
                var principals = container.Resolve<GenericRepository<IPrincipal>>();
                var roles = container.Resolve<GenericRepository<Common.Role>>();

                u1 = container.NewPrincipal(u1Name);
                principals.Insert(u1);

                r1 = container.NewRole(r1Name);
                roles.Insert(r1);
            }

            for (int test = 0; test < 5; test++)
            {
                Console.WriteLine("Test: " + test);

                // Test setup: PrincipalHasRole is deleted to make sure it is not up-to-date.
                // PrincipalHasRole will be recomputed when reading PrincipalHasRole.
                using (var container = new MockWindowsSecurityRhetosContainer($"{u1Name}-{r1Name}", commitChanges: true, commonTestSuffix))
                {
                    var membership = container.Resolve<GenericRepository<Common.PrincipalHasRole>>();
                    membership.Delete(membership.Load());
                    Assert.AreEqual(@"", container.ReportMembership(), "Initial empty membership.");
                    AuthorizationDataCache.ClearCache();
                }

                // Recompute membership on authorization with multiple parallel requests:
                Parallel.For(0, 4, thread =>
                {
                    using (var container = new MockWindowsSecurityRhetosContainer($"{u1Name}-{r1Name}", commitChanges: true, commonTestSuffix))
                    {
                        var authorizationProvider = container.Resolve<CommonAuthorizationProvider>();
                        var userRoles = authorizationProvider.GetUsersRoles(u1);
                        Assert.AreEqual($@"\{r1Name}", container.ReportRoles(userRoles), "User's roles should be recomputed.");
                        Assert.AreEqual($@"\{u1Name}-\{r1Name}", container.ReportMembership(), "Updated role membership");
                    }
                });
            }
        }

        [TestMethod]
        public void ParallelRequestsTryCreatePrincipal()
        {
            string u1Name = TestName + "U";
            string r1Name = TestName + "P";
            string commonTestSuffix = Guid.NewGuid().ToString().Replace("-", "");

            IPrincipal u1Prototype;
            Common.Role r1;
            RhetosAppOptions rhetosAppOptions;

            using (var container = new MockWindowsSecurityRhetosContainer($"{u1Name}-{r1Name}", commitChanges: true, commonTestSuffix))
            {
                rhetosAppOptions = container.Resolve<RhetosAppOptions>();

                u1Prototype = container.NewPrincipal(u1Name);

                var roles = container.Resolve<GenericRepository<Common.Role>>();
                r1 = container.NewRole(r1Name);
                roles.Insert(r1);
            }

            rhetosAppOptions.AuthorizationAddUnregisteredPrincipals = true;

            for (int test = 0; test < 5; test++)
            {
                Console.WriteLine("Test: " + test);

                // Test setup: PrincipalHasRole is deleted to make sure it is not up-to-date.
                // PrincipalHasRole will be recomputed when reading PrincipalHasRole.
                using (var container = new MockWindowsSecurityRhetosContainer($"{u1Name}-{r1Name}", commitChanges: true, commonTestSuffix))
                {
                    var principals = container.Resolve<GenericRepository<IPrincipal>>();
                    principals.Delete(principals.Load(p => p.Name.Contains(TestName)));

                    var membership = container.Resolve<GenericRepository<Common.PrincipalHasRole>>();
                    membership.Delete(membership.Load());
                    Assert.AreEqual(@"", container.ReportMembership(), "Initial empty membership.");
                    AuthorizationDataCache.ClearCache();
                }

                // Recompute membership on authorization with multiple parallel requests:
                Parallel.For(0, 4, thread =>
                {
                    using (var container = new MockWindowsSecurityRhetosContainer($"{u1Name}-{r1Name}", commitChanges: true, commonTestSuffix))
                    {
                        try
                        {
                            container.InitializeSession += builder => builder.RegisterInstance(rhetosAppOptions).ExternallyOwned();

                            var authorizationData = container.Resolve<IAuthorizationData>();
                            var authorizationProvider = container.Resolve<CommonAuthorizationProvider>();

                            PrincipalInfo u1 = authorizationData.GetPrincipal(u1Prototype.Name); // First call will automatically create a new principal, see AuthorizationAddUnregisteredPrincipals above.
                            var userRoles = authorizationProvider.GetUsersRoles(u1);
                            Assert.AreEqual($@"\{r1Name}", container.ReportRoles(userRoles), "User's roles should be recomputed.");
                            Assert.AreEqual($@"\{u1Name}-\{r1Name}", container.ReportMembership(), "Updated role membership");
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            container.Resolve<IPersistenceTransaction>().DiscardChanges();
                            throw;
                        }
                    }
                });
            }
        }

        private const string TestName = "ActiveDirectorySyncTest";

        [ClassCleanup]
        public static void ClassCleanup()
        {
            using (var container = new MockWindowsSecurityRhetosContainer("u1-r1", commitChanges: true))
            {
                var principals = container.Resolve<GenericRepository<IPrincipal>>();
                var roles = container.Resolve<GenericRepository<Common.Role>>();

                principals.Delete(principals.Load(p => p.Name.Contains(TestName)));
                roles.Delete(roles.Load(r => r.Name.Contains(TestName)));
            }
        }
    }
}
