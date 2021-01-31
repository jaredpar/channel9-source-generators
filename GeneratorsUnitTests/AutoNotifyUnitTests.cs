using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System;
using Xunit;
using Microsoft.CodeAnalysis;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace Generators.UnitTests
{
    public sealed class AutoNotifyTests : GeneratorBaseUnitTests
    {
        public override IEnumerable<ISourceGenerator> SourceGenerators => new[] { new AutoNotifyGenerator() };

        public SyntaxTree GetGeneratedTree(string sourceCode)
        {
            var result = GetRunResult(sourceCode);
            return result.GeneratedTrees.Single(x => x.FilePath.Contains("GeneratedNotify"));
        }

        [Fact]
        public void Simple()
        {
            var source = @"
using System;
using System.ComponentModel;
#pragma warning disable 649

partial class C
{
    [AutoNotify]
    int _field;
}

";

            VerifyCompiles(source);
            VerifyGeneratedCode(@"

public partial class C : System.ComponentModel.INotifyPropertyChanged
{
    public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
    public int Field 
    {
        get 
        {
            return this._field;
        }

        set
        {
            this._field = value;
            this.PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(Field)));
        }
    }
}", GetGeneratedTree(source));

        }
    }
}
