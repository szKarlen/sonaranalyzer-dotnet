﻿/*
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
    public class ConstructorOverridableCall : SonarDiagnosticAnalyzer
    {
        internal const string DiagnosticId = "S1699";
        private const string MessageFormat = "Remove this call from a constructor to the overridable '{0}' method.";

        private static readonly DiagnosticDescriptor rule =
            DiagnosticDescriptorBuilder.GetDescriptor(DiagnosticId, MessageFormat, RspecStrings.ResourceManager);

        protected sealed override DiagnosticDescriptor Rule => rule;

        protected override void Initialize(SonarAnalysisContext context)
        {
            context.RegisterSyntaxNodeActionInNonGenerated(
                c => CheckOverridableCallInConstructor(c),
                SyntaxKind.InvocationExpression);
        }

        private static void CheckOverridableCallInConstructor(SyntaxNodeAnalysisContext context)
        {
            var invocationExpression = (InvocationExpressionSyntax)context.Node;

            var calledOn = (invocationExpression.Expression as MemberAccessExpressionSyntax)?.Expression;
            var isCalledOnThis = calledOn == null || calledOn is ThisExpressionSyntax;
            if (!isCalledOnThis)
            {
                return;
            }

            var enclosingSymbol = context.SemanticModel.GetEnclosingSymbol(invocationExpression.SpanStart) as IMethodSymbol;
            if (!IsMethodConstructor(enclosingSymbol))
            {
                return;
            }

            var methodSymbol = context.SemanticModel.GetSymbolInfo(invocationExpression.Expression).Symbol as IMethodSymbol;

            if (methodSymbol != null &&
                IsMethodOverridable(methodSymbol) &&
                enclosingSymbol.IsInType(methodSymbol.ContainingType))
            {
                context.ReportDiagnostic(Diagnostic.Create(rule, invocationExpression.Expression.GetLocation(),
                    methodSymbol.Name));
            }
        }

        private static bool IsMethodOverridable(IMethodSymbol methodSymbol)
        {
            return methodSymbol.IsVirtual || methodSymbol.IsAbstract;
        }

        private static bool IsMethodConstructor(IMethodSymbol methodSymbol)
        {
            return methodSymbol != null &&
                methodSymbol.MethodKind == MethodKind.Constructor;
        }
    }
}
