﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2015 SonarSource
 * sonarqube@googlegroups.com
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
 * You should have received a copy of the GNU Lesser General Public
 * License along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02
 */

using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.Helpers;
using SonarLint.Rules;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;

namespace SonarLint.UnitTest.Helpers
{
    [TestClass]
    public class DiagnosticAnalyzerContextHelperTest
    {
        internal static void VerifyEmpty(string name, string content, DiagnosticAnalyzer diagnosticAnalyzer)
        {
            using (var workspace = new AdhocWorkspace())
            {
                var document = workspace.CurrentSolution.AddProject("foo", "foo.dll", LanguageNames.CSharp)
                    .AddMetadataReference(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
                    .AddMetadataReference(MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location))
                    .AddDocument(name, content);

                var compilation = document.Project.GetCompilationAsync().Result;

                var diagnostics = Verifier.GetDiagnostics(compilation, diagnosticAnalyzer);

                diagnostics.Should().HaveCount(0);
            }
        }


        [TestMethod]
        public void No_Issue_On_Generated_File_With_Generated_Name()
        {
            const string Source =
@"namespace Generated
{
    class MyClass
    {
        void M()
        {
            ;;;;
        }
    }
}";
            VerifyEmpty("test.g.cs", Source, new EmptyStatement());
        }
    }
}
