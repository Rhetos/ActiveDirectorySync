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

using Rhetos.Compiler;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.Dsl;
using Rhetos.Extensibility;
using System.ComponentModel.Composition;

namespace Rhetos.ActiveDirectorySync
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(InitializationConcept))]
    [ExportMetadata(MefProvider.DependsOn, typeof(DomInitializationCodeGenerator))]
    public class ContainsIdsInterceptorCodeGenerator : IConceptCodeGenerator
    {
        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            codeBuilder.InsertCode(@"protected Lazy<Rhetos.ActiveDirectorySync.IWindowsSecurity> _windowsSecurity;
        public Rhetos.ActiveDirectorySync.IWindowsSecurity WindowsSecurity { get { return _windowsSecurity.Value; } }
        
        ", ModuleCodeGenerator.ExecutionContextMemberTag);
            codeBuilder.InsertCode(@",
            Lazy<Rhetos.ActiveDirectorySync.IWindowsSecurity> windowsSecurity
            ", ModuleCodeGenerator.ExecutionContextConstructorArgumentTag);
            codeBuilder.InsertCode(@"_windowsSecurity = windowsSecurity;
            ", ModuleCodeGenerator.ExecutionContextConstructorAssignmentTag);
            codeBuilder.InsertCode(@"builder.RegisterType<Rhetos.ActiveDirectorySync.WindowsSecurity>().As<Rhetos.ActiveDirectorySync.IWindowsSecurity>().SingleInstance();
            ", ModuleCodeGenerator.CommonAutofacConfigurationMembersTag);
        }
    }
}
