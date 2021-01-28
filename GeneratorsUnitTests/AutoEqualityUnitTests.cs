using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System;
using Xunit;
// TODO: delete and put the logic for embedding here.
using Roslyn.CodeDom;
using Microsoft.CodeAnalysis;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace Generators.UnitTests
{
    public sealed class AutoEqualityTests
    {
        private Compilation GetCompilation(string sourceCode) =>
            GetCompilation(new[] { CSharpSyntaxTree.ParseText(sourceCode) });

        private Compilation GetCompilation(IEnumerable<SyntaxTree> syntaxTrees)
        {
            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

            var compilation = CSharpCompilation.Create(
                assemblyName: "TestAssembly",
                syntaxTrees: syntaxTrees,
                options: options).WithFrameworkReferences(TargetFramework.NetCoreApp31);

            return compilation;
        }

        public GeneratorDriverRunResult GetRunResult(string sourceCode)
        {
            var compilation = GetCompilation(sourceCode);
            var driver = CSharpGeneratorDriver.Create(new AutoEqualityGenerator());
            return driver
                .RunGenerators(compilation)
                .GetRunResult();
        }

        public SyntaxTree GetGeneratedTree(string sourceCode)
        {
            var result = GetRunResult(sourceCode);
            return result.GeneratedTrees.Single(x => x.FilePath.Contains("GeneratedEquality"));
        }

        private void VerifyCompiles(string sourceCode)
        {
            var all = new SyntaxTree[] { CSharpSyntaxTree.ParseText(sourceCode) };
            var result = GetRunResult(sourceCode);
            var compilation = GetCompilation(all.Concat(result.GeneratedTrees));
            var diagnostics = compilation
                .GetDiagnostics()
                .Where(x => x.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning);
            Assert.Empty(diagnostics);
        }

        private void VerifyGeneratedCode(string expectedCode, SyntaxTree actualTree)
        {
            var actualCode = Trim(actualTree.ToString());

            //Assert.Equal(Trim(expectedCode), actualCode);
            string Trim(string s) => s.Trim(' ', '\n', '\r');
        }

        [Fact]
        public void Simple()
        {
            var source = @"
using System;
#pragma warning disable 649

[AutoEquality]
partial class C
{
    int Field;
}

";

            VerifyGeneratedCode("", GetGeneratedTree(source));
            VerifyCompiles(source);


        }
    }
}
