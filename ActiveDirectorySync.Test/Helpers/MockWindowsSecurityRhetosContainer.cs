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
using Rhetos.Configuration.Autofac;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.Security;
using Rhetos.TestCommon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ActiveDirectorySync.Test.Helpers
{
    public class MockWindowsSecurityRhetosContainer : RhetosTestContainer
    {
        public MockWindowsSecurityRhetosContainer(string userGroupMembership, bool commitChanges = false)
            : base(commitChanges, GetRhetosServerFolder())
        {
            Console.WriteLine($"TestSuffix: {TestSuffix}");
            InitializeSession += builder =>
            {
                builder.RegisterInstance(new MockWindowsSecurity(userGroupMembership, TestSuffix)).As<IWindowsSecurity>();
                // Test the CommonAuthorizationProvider even if another security package was deployed:
                builder.RegisterType<CommonAuthorizationProvider>();
            };
        }

        private static string GetRhetosServerFolder()
        {
            string start = Directory.GetCurrentDirectory();
            string root = start;
            while (Path.GetFileName(root) != "ActiveDirectorySync")
            {
                root = Path.GetDirectoryName(root);
                if (root == null)
                    throw new ApplicationException($"Cannot find the root 'ActiveDirectorySync' source folder starting from '{start}'.");
            }
            return Path.Combine(root, @"..\..\Rhetos\Source\Rhetos");
        }

        public readonly string TestSuffix = "_" + Guid.NewGuid().ToString().Replace("-", "");

        public readonly string DomainPrefix = Environment.UserDomainName + @"\";

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
    }
}
