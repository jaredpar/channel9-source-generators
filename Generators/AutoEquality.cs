using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Text;

namespace Generators
{
    [Generator]
    public class AutoEqualityGenerator : ISourceGenerator
    {
        private const string attributeText = @"
using System;
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
internal sealed class AutoEqualityAttribute : Attribute
{
    public AutoEqualityAttribute()
    {
    }
}
";

        public void Initialize(GeneratorInitializationContext context)
        {
            // Register a syntax receiver that will be created for each generation pass
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            // add the attribute text
            context.AddSource("AutoEqualityAttribute", SourceText.From(attributeText, Encoding.UTF8));

            if (!(context.SyntaxReceiver is SyntaxReceiver receiver))
                return;

            // TODO: should verify the name
            var list = new List<(INamedTypeSymbol NamedTypeSymbol, bool IsAnnotated)>();
            foreach (var group in receiver.TypeDeclarationSyntaxList.GroupBy(x => x.SyntaxTree))
            {
                var semanticModel = context.Compilation.GetSemanticModel(group.Key, ignoreAccessibility: true);
                foreach (var decl in group)
                {
                    if (semanticModel.GetDeclaredSymbol(decl) is { } namedTypeSymbol)
                    {
                        var isAnnotated = semanticModel.GetNullableContext(decl.SpanStart) == NullableContext.Enabled;
                        list.Add((namedTypeSymbol, isAnnotated));
                    }
                }
            }

            var builder = new StringBuilder();
            AddTypeGeneration(builder, list);
            context.AddSource("GeneratedEquality", SourceText.From(builder.ToString(), Encoding.UTF8));
        }

        private void AddTypeGeneration(StringBuilder builder, IEnumerable<(INamedTypeSymbol NamedTypeSymbol, bool IsAnnotated)> typeSymbols)
        {
            if (!typeSymbols.Any())
            {
                return;
            }

            var indent = new IndentUtil();
            builder.AppendLine($@"
using System;
using System.Collections.Generic;");

            // TODO: can't assume they all have the same namespace 
            var namespaceSymbol = typeSymbols.First().NamedTypeSymbol.ContainingNamespace;
            if (!namespaceSymbol.IsGlobalNamespace)
            {
                builder.AppendLine($@"namespace {namespaceSymbol.Name}
{{");
                indent.IncreaseSimple();
            }

            foreach (var tuple in typeSymbols)
            {
                AddTypeGeneration(builder, indent, tuple.NamedTypeSymbol, tuple.IsAnnotated);
            }

            if (!namespaceSymbol.IsGlobalNamespace)
            {
                indent.Decrease();
                builder.AppendLine("}");
            }
        }

        private void AddTypeGeneration(StringBuilder builder, IndentUtil indent, INamedTypeSymbol typeSymbol, bool isAnnotated)
        {
            var kind = typeSymbol.TypeKind == TypeKind.Class ? "class" : "struct";

            var refAnnotation = "";
            var typeAnnotation = "";
            if (isAnnotated)
            {
                builder.AppendLine("#nullable enable");
                refAnnotation = "?";
                typeAnnotation = typeSymbol.TypeKind == TypeKind.Class ? "?" : "";
            }

            builder.AppendLine($@"
{indent.Value}partial {kind} {typeSymbol.Name} : IEquatable<{typeSymbol.Name}>
{indent.Value}{{
{indent.Value2}public override bool Equals(object{refAnnotation} obj) => obj is {typeSymbol.Name} other && Equals(other);");

            AddOperatorEquals();

            var memberInfoList = GetMemberInfo();
            using var marker = indent.Increase();

            AddEquals();
            AddGetHashCode();

            marker.Revert();
            builder.AppendLine($@"{indent.Value}}}");

            if (isAnnotated)
            {
                builder.AppendLine("#nullable disable");
            }

            void AddOperatorEquals()
            {
                using var _ = indent.Increase();

                var prefix = "";
                if (!typeSymbol.IsValueType)
                {
                    prefix = "left is object && ";
                }

                builder.AppendLine($"{indent.Value}public static bool operator==({typeSymbol.Name}{typeAnnotation} left, {typeSymbol.Name}{typeAnnotation} right) => {prefix}left.Equals(right);");
                builder.AppendLine($"{indent.Value}public static bool operator!=({typeSymbol.Name}{typeAnnotation} left, {typeSymbol.Name}{typeAnnotation} right) => !(left == right);");
            }

            void AddEquals()
            {
                builder.AppendLine($@"
{indent.Value}public bool Equals({typeSymbol.Name}{typeAnnotation} other)
{indent.Value}{{
{indent.Value2}return");

                using var marker = indent.Increase(2);

                if (typeSymbol.TypeKind == TypeKind.Class)
                {
                    builder.AppendLine($"{indent.Value}other is object &&");
                }

                for (var i = 0; i < memberInfoList.Count; i++)
                {
                    var current = memberInfoList[i];
                    if (current.UseOperator)
                    {
                        builder.Append($"{indent.Value}{current.Name} == other.{current.Name}");
                    }
                    else
                    {
                        builder.Append($"{indent.Value}EqualityComparer<{current.TypeName}>.Default.Equals({current.Name}, other.{current.Name})");
                    }

                    if (i + 1 < memberInfoList.Count)
                    {
                        builder.Append(" &&");
                    }
                    else
                    {
                        builder.Append(";");
                    }
                    builder.AppendLine();
                }

                marker.Revert();
                builder.AppendLine($"{indent.Value}}}");
            }

            void AddGetHashCode()
            {
                builder.AppendLine($@"
{indent.Value}public override int GetHashCode()
{indent.Value}{{
{indent.Value2}return HashCode.Combine(");

                using var marker = indent.Increase(2);

                // TODO: handle more than eight fields
                for (var i = 0; i < memberInfoList.Count; i++)
                {
                    var current = memberInfoList[i];
                    builder.Append($"{indent.Value}{current.Name}");
                    if (i + 1 < memberInfoList.Count)
                    {
                        builder.AppendLine(",");
                    }
                    else
                    {
                        builder.AppendLine(");");
                    }
                }

                marker.Revert();
                builder.AppendLine($"{indent.Value}}}");
            }

            List<MemberInfo> GetMemberInfo()
            {
                var list = new List<MemberInfo>();
                foreach (var symbol in typeSymbol.GetMembers())
                {
                    switch (symbol)
                    {
                        case IFieldSymbol { Type: { }, IsImplicitlyDeclared: false } fieldSymbol:
                            list.Add(new MemberInfo(fieldSymbol.Name, fieldSymbol.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), UseOperator(fieldSymbol.Type)));
                            break;
                        case IPropertySymbol { IsIndexer: false, GetMethod: { } } propertySymbol:
                            list.Add(new MemberInfo(propertySymbol.Name, propertySymbol.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), UseOperator(propertySymbol.Type)));
                            break;
                        default:
                            break;
                    }
                }

                return list;

                bool UseOperator(ITypeSymbol? type) =>
                    type is { } &&
                    type.SpecialType is
                        SpecialType.System_Int16 or
                        SpecialType.System_Int32 or
                        SpecialType.System_Int64 or
                        SpecialType.System_UInt16 or
                        SpecialType.System_UInt32 or
                        SpecialType.System_UInt64 or
                        SpecialType.System_String or
                        SpecialType.System_IntPtr or
                        SpecialType.System_UIntPtr;
            }
        }

        private record MemberInfo(string Name, string TypeName, bool UseOperator);

        /// <summary>
        /// Created on demand before each generation pass
        /// </summary>
        internal sealed class SyntaxReceiver : ISyntaxReceiver
        {
            internal List<TypeDeclarationSyntax> TypeDeclarationSyntaxList { get; } = new();

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                // TODO: could do a quick filter on whether the attribute has the right name
                if (syntaxNode is TypeDeclarationSyntax typeDeclarationSyntax
                    && typeDeclarationSyntax.AttributeLists.Count > 0)
                {
                    TypeDeclarationSyntaxList.Add(typeDeclarationSyntax);
                }
            }
        }
    }
}
