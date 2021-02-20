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

using Autofac;
using Rhetos;
using Rhetos.ActiveDirectorySync;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.TestCommon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ActiveDirectorySync.Test.Helpers
{
    /// <summary>
    /// Helper class that manages Dependency Injection container for unit tests.
    /// The container can be customized for each unit test scope.
    /// </summary>
    public class TestScope : UnitOfWorkScope
    {
        /// <summary>
        /// Creates a thread-safe lifetime scope DI container (service provider)
        /// to isolate unit of work with a <b>separate database transaction</b>.
        /// It registers the <see cref="MockWindowsSecurity"/> as <see cref="IWindowsSecurity"/>
        /// class with the provided user group membership.
        /// It registers the CommonAuthorizationProvider to test it even if another security package was deployed.
        /// </summary>
        public static TestScope Create(string userGroupMembership, string testSuffix = null, Action<ContainerBuilder> registerCustomComponents = null)
        {
            testSuffix = testSuffix ?? "_" + Guid.NewGuid().ToString().Replace("-", "");
            Console.WriteLine($"TestSuffix: {testSuffix}");
            return new TestScope(_rhetosHost.GetRootContainer(), builder =>
            {
                builder.RegisterInstance(new MockWindowsSecurity(userGroupMembership, testSuffix)).As<IWindowsSecurity>();
                builder.RegisterType<CommonAuthorizationProvider>();
                registerCustomComponents?.Invoke(builder);

            }, testSuffix);
        }

        public readonly string TestSuffix;

        public readonly string DomainPrefix = Environment.UserDomainName + @"\";

        public TestScope(IContainer container, Action<ContainerBuilder> registerCustomComponents, string testSuffix) : base(container, registerCustomComponents)
        {
            TestSuffix = testSuffix;
        }

        /// <summary>
        /// Creates a new principal with the current windows domain prefix (by default) and a random test context suffix.
        /// </summary>
        public IPrincipal NewPrincipal(string namePrefix, bool domain = true)
        {
            var principal = Resolve<Common.ExecutionContext>().GenericRepository<IPrincipal>("Common.Principal").CreateInstance();
            principal.ID = Guid.NewGuid();
            principal.Name = NewName(namePrefix, domain);
            return principal;
        }

        /// <summary>
        /// Creates a new role with the current windows domain prefix (by default) and a random test context suffix.
        /// </summary>
        public Common.Role NewRole(string namePrefix, bool domain = true)
        {
            return new Common.Role
            {
                ID = Guid.NewGuid(),
                Name = NewName(namePrefix, domain)
            };
        }

        public string NewName(string namePrefix, bool domain = true)
        {
            return (domain ? DomainPrefix : "") + namePrefix + TestSuffix;
        }

        /// <summary>Shortens and sorts the names.</summary>
        public string ReportMembership(object filter = null)
        {
            var context = this.Resolve<Common.ExecutionContext>();
            var membership = context.Repository.Common.PrincipalHasRole;
            var query = filter == null ? membership.Query() : membership.Query(membership.Load(filter).Select(item => item.ID));
            var reportData = query
                .Where(phr => phr.Principal.Name.EndsWith(TestSuffix) && phr.Role.Name.EndsWith(TestSuffix))
                .Select(phr => new { PrincipalName = phr.Principal.Name, RoleName = phr.Role.Name }).ToList();
            string report = TestUtility.DumpSorted(reportData, phr => Shorten(phr.PrincipalName) + "-" + Shorten(phr.RoleName));
            Console.WriteLine("[ReportMembership] " + report);
            return report;
        }

        public string Shorten(string name)
        {
            name = name.StartsWith(DomainPrefix) ? name.Substring(DomainPrefix.Length - 1) : name;
            return name.Replace(TestSuffix, "");
        }

        /// <summary>Shortens and sorts the names.</summary>
        public string ReportPrincipals(IEnumerable<IPrincipal> principals)
        {
            return ReportPrincipals(principals
                .Where(p => p.Name != null && p.Name.EndsWith(TestSuffix))
                .Select(p => p.Name));
        }

        /// <summary>Shortens and sorts the names.</summary>
        private string ReportPrincipals(IEnumerable<string> domainPrincipalNames)
        {
            string report = string.Join(", ", domainPrincipalNames.Select(Shorten).OrderBy(name => name));
            Console.WriteLine("[ReportPrincipals] " + report);
            return report;
        }

        /// <summary>Shortens and sorts the names. Removes system roles.</summary>
        public string ReportRoles(IEnumerable<Common.Role> roles)
        {
            return ReportRoles(roles
                .Where(r => r.Name.EndsWith(TestSuffix))
                .Select(r => r.Name));
        }

        /// <summary>Shortens and sorts the names. Removes system roles.</summary>
        private string ReportRoles(IEnumerable<string> roleNames)
        {
            var simplifiedRoleNames = roleNames
                .Except(Enum.GetNames(typeof(SystemRole)))
                .Select(Shorten)
                .OrderBy(name => name)
                .ToList();

            string report = string.Join(", ", simplifiedRoleNames);
            Console.WriteLine("[ReportRoles] " + report);
            return report;
        }

        /// <summary>Shortens and sorts the names. Removes system roles.</summary>
        public string ReportRoles(IEnumerable<Guid> roles)
        {
            var roleNames = this.Resolve<GenericRepository<Common.Role>>().Load().ToDictionary(r => r.ID, r => r.Name);
            return ReportRoles(roles.Select(id => roleNames[id]));
        }

        /// <summary>
        /// Reusing a single shared static DI container between tests, to reduce initialization time for each test.
        /// Each test should create a child scope with <see cref="TestScope.Create(string, string, Action{ContainerBuilder})"/> methods to start a 'using' block.
        /// </summary>
        private static readonly RhetosHost _rhetosHost = RhetosHost.FindBuilder(Path.GetFullPath(@"..\..\..\..\..\test\Rhetos.ActiveDirectorySync.TestApp\bin\Debug\net5.0\Rhetos.ActiveDirectorySync.TestApp.dll")).Build();
    }
}
