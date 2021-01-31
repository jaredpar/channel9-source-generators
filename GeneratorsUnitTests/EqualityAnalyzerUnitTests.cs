using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Generators.UnitTests
{
    public sealed class EqualityAnalyzerUnitTests
    {
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

        public async Task<List<Diagnostic>> GetDiagnosticsAsync(string sourceCode)
        {
            var compilation = new CompilationWithAnalyzers(
                GetCompilation(sourceCode),
                ImmutableArray.Create<DiagnosticAnalyzer>(new EqualityAnalyzer()),
                new CompilationWithAnalyzersOptions(
                    new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty),
                    (ex, _, _) => Assert.True(false, ex.Message),
                    concurrentAnalysis: false,
                    logAnalyzerExecutionTime: false));
            var diagnostics = await compilation
                .GetAnalyzerDiagnosticsAsync()
                .ConfigureAwait(false);
            return diagnostics
                .OrderBy(x => x.Id)
                .ToList();
        }

        [Fact]
        public async Task NeedsOperators()
        {
            var source = @"
using System;
using System.Collections.Generic;

class C : IEquatable<C>
{
    public bool Equals(C other) => true;
}
";

            var diagnostics = await GetDiagnosticsAsync(source).ConfigureAwait(false);
            Assert.Single(diagnostics);
            Assert.Equal(EqualityAnalyzer.DiagnosticNeedOperatorEqauls, diagnostics[0].Descriptor);
        }

        [Fact]
        public async Task NeedsStronglyTypedEquals()
        {
            var source = @"
using System;
using System.Collections.Generic;

class C : IEquatable<C>
{
    public static bool operator==(C left, C right) => true;
    public static bool operator!=(C left, C right) => true;
}
";

            var diagnostics = await GetDiagnosticsAsync(source).ConfigureAwait(false);
            Assert.Single(diagnostics);
            Assert.Equal(EqualityAnalyzer.DiagnosticNeedStrongEquals, diagnostics[0].Descriptor);
        }

        [Fact]
        public async Task NeedsInterface()
        {
            var source = @"
using System;
using System.Collections.Generic;

class C
{
    public bool Equals(C other) => true;
    public static bool operator==(C left, C right) => true;
    public static bool operator!=(C left, C right) => true;
}
";

            var diagnostics = await GetDiagnosticsAsync(source).ConfigureAwait(false);
            Assert.Single(diagnostics);
            Assert.Equal(EqualityAnalyzer.DiagnosticNeedImplementIEquatable, diagnostics[0].Descriptor);
        }

        [Fact]
        public async Task NeedsNothing()
        {
            var source = @"
using System;
using System.Collections.Generic;

class C : IEquatable<C>
{
    public bool Equals(C other) => true;
    public static bool operator==(C left, C right) => true;
    public static bool operator!=(C left, C right) => true;
}
";

            var diagnostics = await GetDiagnosticsAsync(source).ConfigureAwait(false);
            Assert.Empty(diagnostics);
        }
    }
}
