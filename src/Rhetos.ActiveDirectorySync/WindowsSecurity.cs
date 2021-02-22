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

using System;
using Rhetos.Logging;
using System.Security.Principal;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices;
using System.Runtime.Versioning;

namespace Rhetos.ActiveDirectorySync
{
    /// <summary>
    /// A utility class for Active directory operations.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class WindowsSecurity : IWindowsSecurity
    {
        private readonly ILogger _logger;
        private readonly ILogger _performanceLogger;

        public WindowsSecurity(ILogProvider logProvider)
        {
            _logger = logProvider.GetLogger(GetType().Name);
            _performanceLogger = logProvider.GetLogger("Performance." + GetType().Name);
        }

        /// <summary>
        /// Queries Active Directory server to retrieve the user's Windows domain groups.
        /// Throws an exception if the username does not have the current domain prefix.
        /// Returns null if the user is not found on Active Directory (returns empty list is the user exists, but has no membership records).
        /// </summary>
        public IEnumerable<string> GetIdentityMembership(string username)
        {
            var stopwatch = Stopwatch.StartNew();

            var accountName = RemoveDomainPrefix(username);

            // Search user's domain groups:

            var userNestedMembership = new List<string>();

            var domainConnection = new DirectoryEntry("LDAP://" + Environment.UserDomainName);
            var searcher = new DirectorySearcher(domainConnection);
            searcher.Filter = "(samAccountName=" + accountName + ")";
            searcher.PropertiesToLoad.Add("name");

            SearchResult searchResult = null;
            try
            {
                searchResult = searcher.FindOne();
            }
            catch (Exception ex)
            {
                throw new FrameworkException("Active Directory server is not available. To run Rhetos under IISExpress without AD: a) IISExpress must be run as administrator, b) user connecting to Rhetos service must be local administrator, c) 'BuiltinAdminOverride' must be set to 'True' in config file.", ex);
            }

            if (searchResult != null)
            {
                _logger.Trace("Found Active Directory entry: " + searchResult.Path);

                userNestedMembership.Add(accountName);

                var theUser = searchResult.GetDirectoryEntry();
                theUser.RefreshCache(new[] { "tokenGroups" });

                foreach (byte[] resultBytes in theUser.Properties["tokenGroups"])
                {
                    // Search domain group's name and displayName:

                    var mySID = new SecurityIdentifier(resultBytes, 0);

                    _logger.Trace(() => string.Format("User '{0}' is a member of group with objectSid '{1}'.", accountName, mySID.Value));

                    var sidSearcher = new DirectorySearcher(domainConnection);
                    sidSearcher.Filter = "(objectSid=" + mySID.Value + ")";
                    sidSearcher.PropertiesToLoad.Add("name");
                    sidSearcher.PropertiesToLoad.Add("displayname");

                    var sidResult = sidSearcher.FindOne();
                    if (sidResult != null)
                    {
                        var name = sidResult.Properties["name"][0].ToString();
                        userNestedMembership.Add(name);
                        _logger.Trace(() => string.Format("Added membership to group with name '{0}' for user '{1}'.", name, accountName));

                        var displayNameProperty = sidResult.Properties["displayname"];
                        if (displayNameProperty.Count > 0)
                        {
                            var displayName = displayNameProperty[0].ToString();
                            if (!string.Equals(name, displayName))
                            {
                                userNestedMembership.Add(displayName);
                                _logger.Trace(() => string.Format("Added membership to group with display name '{0}' for user '{1}'.", displayName, accountName));
                            }
                        }
                    }
                    else
                        _logger.Trace(() => string.Format("Cannot find the active directory entry for user's '{0}' parent group with objectSid '{1}'.", accountName, mySID.Value));
                }
            }
            else
                _logger.Trace(() => string.Format("Account name '{0}' not found on Active Directory for domain '{1}'.", accountName, Environment.UserDomainName));

            _performanceLogger.Write(stopwatch, "GetIdentityMembership() done.");
            return userNestedMembership;
        }

        /// <summary>
        /// Throws an exception if the username does not have the current domain prefix.
        /// </summary>
        private string RemoveDomainPrefix(string username)
        {
            _logger.Trace(() => "Domain: " + Environment.UserDomainName);

            var domainPrefix = Environment.UserDomainName + "\\";

            if (!username.StartsWith(domainPrefix, StringComparison.OrdinalIgnoreCase))
            {
                const string msg = "The user is not authenticated in current domain.";
                _logger.Trace(() => msg + " Identity: '" + username + "', domain: '" + Environment.UserDomainName + "'.");
                throw new ClientException(msg);
            }

            var usernameWithoutDomain = username.Substring(domainPrefix.Length);
            _logger.Trace(() => "Identity without domain: " + usernameWithoutDomain);
            return usernameWithoutDomain;
        }
    }
}