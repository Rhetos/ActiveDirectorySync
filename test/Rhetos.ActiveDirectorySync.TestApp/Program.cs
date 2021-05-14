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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Rhetos.Security;
using Rhetos.Utilities;

namespace Rhetos.ActiveDirectorySync.TestApp
{
    public static class Program
    {
        public static void Main()
        {
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices(ConfigureRhetos);

        /// <summary>
        /// Provides basic runtime infrastructure for Rhetos framework: configuration settings and system components registration.
        /// </summary>
        private static void ConfigureRhetos(HostBuilderContext hostContext, IServiceCollection services)
        {
            services.AddRhetosHost((serviceProvider, rhetosHostBuilder) =>
            {
                rhetosHostBuilder.ConfigureRhetosAppDefaults()
                    .ConfigureConfiguration(configurationBuilder => configurationBuilder
                        .AddJsonFile("rhetos-app.local.settings.json")
                    )
                    .ConfigureContainer(rhetosContainerBuilder =>
                    {
                        rhetosContainerBuilder.RegisterType<ProcessUserInfo>().As<IUserInfo>();
                    });
            });
        }
    }
}
