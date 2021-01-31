using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System;
using Xunit;
using Microsoft.CodeAnalysis;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Basic.Reference.Assemblies;

namespace Generators.UnitTests
{
    public abstract class GeneratorBaseUnitTests
    {
        public abstract IEnumerable<ISourceGenerator> SourceGenerators { get; }

        public Compilation GetCompilation(string sourceCode) =>
            GetCompilation(new[] { CSharpSyntaxTree.ParseText(sourceCode) });

        public Compilation GetCompilation(IEnumerable<SyntaxTree> syntaxTrees)
        {
            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

            var compilation = CSharpCompilation.Create(
                assemblyName: "TestAssembly",
                syntaxTrees: syntaxTrees,
                options: options).WithReferenceAssemblies(ReferenceAssemblyKind.NetCoreApp31);

            return compilation;
        }

        public GeneratorDriverRunResult GetRunResult(string sourceCode)
        {
            var compilation = GetCompilation(sourceCode);
            var driver = CSharpGeneratorDriver.Create(SourceGenerators);
            return driver
                .RunGenerators(compilation)
                .GetRunResult();
        }

        public void VerifyCompiles(string sourceCode)
        {
            var all = new SyntaxTree[] { CSharpSyntaxTree.ParseText(sourceCode) };
            var result = GetRunResult(sourceCode);
            var compilation = GetCompilation(all.Concat(result.GeneratedTrees));
            var diagnostics = compilation
                .GetDiagnostics()
                .Where(x => x.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning);
            Assert.Empty(diagnostics);
        }

        public void VerifyGeneratedCode(string expectedCode, SyntaxTree actualTree)
        {
            var actualCode = Trim(actualTree.ToString());

            Assert.Equal(Trim(expectedCode), actualCode);
            string Trim(string s) => s.Trim(' ', '\n', '\r');
        }
    }
}
