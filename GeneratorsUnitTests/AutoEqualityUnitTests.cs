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

            Assert.Equal(Trim(expectedCode), actualCode);
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
    Exception Field;
}

";

            VerifyGeneratedCode(@"
using System;
using System.Collections.Generic;

partial class C : IEquatable<C>
{
    public override bool Equals(object obj) => obj is C other && Equals(other);
    public static bool operator==(C left, C right) => left is object && left.Equals(right);
    public static bool operator!=(C left, C right) => !(left == right);

    public bool Equals(C other)
    {
        return
            EqualityComparer<global::System.Exception>.Default.Equals(Field, other.Field);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            Field);
    }
}
", GetGeneratedTree(source));

            VerifyCompiles(source);


        }

        [Fact]
        public void NamespaceMultipleFields()
        {
            var source = @"
using System;
#pragma warning disable 649

namespace N
{
    [AutoEquality]
    partial class C
    {
        Exception Field1;
        Exception Field2;
    }
}
";

            VerifyGeneratedCode(@"
using System;
using System.Collections.Generic;
namespace N
{

    partial class C : IEquatable<C>
    {
        public override bool Equals(object obj) => obj is C other && Equals(other);
        public static bool operator==(C left, C right) => left is object && left.Equals(right);
        public static bool operator!=(C left, C right) => !(left == right);

        public bool Equals(C other)
        {
            return
                EqualityComparer<global::System.Exception>.Default.Equals(Field1, other.Field1) &&
                EqualityComparer<global::System.Exception>.Default.Equals(Field2, other.Field2);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                Field1,
                Field2);
        }
    }
}
", GetGeneratedTree(source));

            VerifyCompiles(source);


        }

        [Fact]
        public void NamespaceMultipleFieldsMixed()
        {
            var source = @"
using System;
#pragma warning disable 649

namespace N
{
    [AutoEquality]
    partial struct S
    {
        int Field1;
        string Field2;
        Exception Field3;
    }
}
";

            VerifyGeneratedCode(@"
using System;
using System.Collections.Generic;
namespace N
{

    partial struct S : IEquatable<S>
    {
        public override bool Equals(object obj) => obj is S other && Equals(other);
        public static bool operator==(S left, S right) => left.Equals(right);
        public static bool operator!=(S left, S right) => !(left == right);

        public bool Equals(S other)
        {
            return
                Field1 == other.Field1 &&
                Field2 == other.Field2 &&
                EqualityComparer<global::System.Exception>.Default.Equals(Field3, other.Field3);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                Field1,
                Field2,
                Field3);
        }
    }
}
", GetGeneratedTree(source));

            VerifyCompiles(source);


        }
    }
}
