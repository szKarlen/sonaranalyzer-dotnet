/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2015-2017 SonarSource SA
 * mailto: contact AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarAnalyzer.Common;
using SonarAnalyzer.Helpers;

namespace SonarAnalyzer.Rules.CSharp
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    [Rule(DiagnosticId)]
    public class SecurityPInvokeMethodShouldNotBeCalled : SonarDiagnosticAnalyzer
    {
        internal const string DiagnosticId = "S3884";
        internal const string MessageFormat = "Refactor the code to remove this use of '{0}'.";
        private const string InteropDllName = "ole32.dll";

        private static readonly ISet<string> InvalidMethods = new HashSet<string>
        {
            "CoSetProxyBlanket",
            "CoInitializeSecurity"
        };

        private static readonly DiagnosticDescriptor rule =
            DiagnosticDescriptorBuilder.GetDescriptor(DiagnosticId, MessageFormat, RspecStrings.ResourceManager);

        protected sealed override DiagnosticDescriptor Rule => rule;

        protected override void Initialize(SonarAnalysisContext context)
        {
            context.RegisterSyntaxNodeActionInNonGenerated(CheckForIssue, SyntaxKind.InvocationExpression);
        }
        private void CheckForIssue(SyntaxNodeAnalysisContext analysisContext)
        {
            var invocation = (InvocationExpressionSyntax)analysisContext.Node;
            var directMethodCall = invocation.Expression as IdentifierNameSyntax;
            if (directMethodCall == null)
            {
                return;
            }

            if (!InvalidMethods.Contains(directMethodCall.Identifier.ValueText))
            {
                return;
            }

            var methodCallSymbol = analysisContext.SemanticModel.GetSymbolInfo(directMethodCall);
            if (methodCallSymbol.Symbol == null)
            {
                return;
            }

            if (!methodCallSymbol.Symbol.IsExtern || !methodCallSymbol.Symbol.IsStatic)
            {
                return;
            }

            var dllImportAttribute = methodCallSymbol.Symbol.GetAttributes()
                .FirstOrDefault(attribute => attribute.AttributeClass.Is(KnownType.System_Runtime_InteropServices_DllImportAttribute));
            if (dllImportAttribute == null)
            {
                return;
            }

            if (dllImportAttribute.ConstructorArguments.Any(x => x.Value.Equals(InteropDllName)))
            {
                analysisContext.ReportDiagnostic(Diagnostic.Create(Rule, directMethodCall.Identifier.GetLocation(),
                    directMethodCall.Identifier.ValueText));
            }
        }
    }
}