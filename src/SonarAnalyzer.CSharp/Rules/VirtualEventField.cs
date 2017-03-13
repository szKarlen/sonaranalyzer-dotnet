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
    public class VirtualEventField : SonarDiagnosticAnalyzer
    {
        internal const string DiagnosticId = "S2290";
        private const string MessageFormat = "Remove this 'virtual' modifier of {0}.";
        private const IdeVisibility ideVisibility = IdeVisibility.Hidden;

        private static readonly DiagnosticDescriptor rule =
            DiagnosticDescriptorBuilder.GetDescriptor(DiagnosticId, MessageFormat, ideVisibility, RspecStrings.ResourceManager);

        protected sealed override DiagnosticDescriptor Rule => rule;

        protected override void Initialize(SonarAnalysisContext context)
        {
            context.RegisterSyntaxNodeActionInNonGenerated(
                c =>
                {
                    var eventField = (EventFieldDeclarationSyntax) c.Node;

                    if (eventField.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.VirtualKeyword)))
                    {
                        var virt = eventField.Modifiers.First(modifier => modifier.IsKind(SyntaxKind.VirtualKeyword));
                        var names = string.Join(", ", eventField.Declaration.Variables
                            .Select(syntax => $"'{syntax.Identifier.ValueText}'")
                            .OrderBy(s => s));
                        c.ReportDiagnostic(Diagnostic.Create(Rule, virt.GetLocation(), names));
                    }
                },
                SyntaxKind.EventFieldDeclaration);
        }
    }
}
