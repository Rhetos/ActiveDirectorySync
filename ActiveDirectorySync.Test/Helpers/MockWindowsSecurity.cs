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

using Rhetos.Security;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace ActiveDirectorySync.Test.Helpers
{
    public class MockWindowsSecurity : IWindowsSecurity
    {
        readonly string _userGroupMembership;
        readonly string _testSuffix;

        public MockWindowsSecurity(string userGroupMembership, string testSuffix)
        {
            _userGroupMembership = userGroupMembership;
            _testSuffix = testSuffix;
        }

        public string GetClientWorkstation()
        {
            throw new NotImplementedException();
        }

        private MultiDictionary<string, string> _membership = null;

        public IEnumerable<string> GetIdentityMembership(string username)
        {
            if (_membership == null)
            {
                _membership = new MultiDictionary<string, string>();
                foreach (var pair in _userGroupMembership.Split(' '))
                    _membership.Add(Environment.UserDomainName + @"\" + pair.Split('-')[0] + _testSuffix, pair.Split('-')[1] + _testSuffix);
            }
            return _membership.Get(username).ToList();
        }

        public bool IsBuiltInAdministrator(WindowsIdentity userInfo)
        {
            throw new NotImplementedException();
        }
    }
}
