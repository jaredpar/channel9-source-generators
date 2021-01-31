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
    public sealed class AutoEqualityTests : GeneratorBaseUnitTests
    {
        public override IEnumerable<ISourceGenerator> SourceGenerators => new[] { new AutoEqualityGenerator() };

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
            other is object &&
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
        public void SimpleNullable()
        {
            var source = @"
using System;
#pragma warning disable 649
#nullable enable

[AutoEquality]
partial class C
{
    Exception Field = new();
}

";

            VerifyGeneratedCode(@"
using System;
using System.Collections.Generic;
#nullable enable

partial class C : IEquatable<C>
{
    public override bool Equals(object? obj) => obj is C other && Equals(other);
    public static bool operator==(C? left, C? right) => left is object && left.Equals(right);
    public static bool operator!=(C? left, C? right) => !(left == right);

    public bool Equals(C? other)
    {
        return
            other is object &&
            EqualityComparer<global::System.Exception>.Default.Equals(Field, other.Field);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            Field);
    }
}
#nullable disable
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
                other is object &&
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

        [Fact]
        public void NamespaceMultipleProperties()
        {
            var source = @"
using System;
#pragma warning disable 649

namespace N
{
    [AutoEquality]
    partial class C
    {
        Exception Field1 { get; }
        Exception Field2 { get; }
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
                other is object &&
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
        public void NamespaceMultiplePropertiesNullable()
        {
            var source = @"
using System;
#pragma warning disable 649

#nullable enable

namespace N
{
    [AutoEquality]
    partial class C
    {
        Exception? Field1 { get; }
        Exception? Field2 { get; }
    }
}
";

            VerifyGeneratedCode(@"
using System;
using System.Collections.Generic;
namespace N
{
#nullable enable

    partial class C : IEquatable<C>
    {
        public override bool Equals(object? obj) => obj is C other && Equals(other);
        public static bool operator==(C? left, C? right) => left is object && left.Equals(right);
        public static bool operator!=(C? left, C? right) => !(left == right);

        public bool Equals(C? other)
        {
            return
                other is object &&
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
#nullable disable
}
", GetGeneratedTree(source));

            VerifyCompiles(source);


        }

        [Fact]
        public void NamespaceMultiplePropertiesNullableOnStruct()
        {
            var source = @"
using System;
#pragma warning disable 649

#nullable enable

namespace N
{
    [AutoEquality]
    partial struct S
    {
        Exception? Field1 { get; }
        Exception? Field2 { get; }
    }
}
";

            VerifyGeneratedCode(@"
using System;
using System.Collections.Generic;
namespace N
{
#nullable enable

    partial struct S : IEquatable<S>
    {
        public override bool Equals(object? obj) => obj is S other && Equals(other);
        public static bool operator==(S left, S right) => left.Equals(right);
        public static bool operator!=(S left, S right) => !(left == right);

        public bool Equals(S other)
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
#nullable disable
}
", GetGeneratedTree(source));

            VerifyCompiles(source);


        }
    }
}
