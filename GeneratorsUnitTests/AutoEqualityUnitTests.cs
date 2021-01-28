using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System;
using Xunit;
// TODO: delete and put the logic for embedding here.
using Roslyn.CodeDom;
using Microsoft.CodeAnalysis;
using System.Linq;

namespace Generators.UnitTests
{
    public sealed class AutoEqualityTests
    {
        public GeneratorDriverRunResult GetRunResult(string sourceCode)
        {
            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

            var compilation = CSharpCompilation.Create(
                assemblyName: "TestAssembly",
                syntaxTrees: new[] { CSharpSyntaxTree.ParseText(sourceCode) },
                options: options).WithFrameworkReferences(TargetFramework.NetCoreApp31);

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

        [Fact]
        public void Simple()
        {
            var source = @"
using System;

[AutoEquality]
class C
{
    int Field;
}

";

            var syntaxTree = GetGeneratedTree(source);
            Assert.Equal("", syntaxTree.ToString());



        }
    }
}
