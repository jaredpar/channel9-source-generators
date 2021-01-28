using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Generators
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class EqualityAnalyzer : DiagnosticAnalyzer
    {
        public static DiagnosticDescriptor DiagnosticNeedOperatorEqauls = new(
            "JP001",
            title: "Add equality operators",
            messageFormat: "Type {0} needs both == and != operators",
            category: "Equality",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor DiagnosticNeedImplementIEquatable = new(
            "JP002",
            title: "Implement IEquatable<T>",
            messageFormat: "Type {0} to implement IEquatable<T>",
            category: "Equality",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor DiagnosticNeedStrongEquals = new(
            "JP002",
            title: "Need strongly typed Equals",
            messageFormat: "Type {0} needs a strongly typed Equals method",
            category: "Equality",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            DiagnosticNeedOperatorEqauls,
            DiagnosticNeedImplementIEquatable,
            DiagnosticNeedStrongEquals);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(context =>
            {
                var compilation = context.Compilation;
                var typeIEquatableT = compilation.GetTypeByMetadataName(typeof(IEquatable<>).FullName!) as INamedTypeSymbol;
                if (typeIEquatableT is object)
                {
                    var analyzer = new Analyzer(compilation, typeIEquatableT);
                    context.RegisterSymbolAction(analyzer.OnSymbol, SymbolKind.NamedType);
                }
            });
        }

        private sealed class Analyzer
        {
            public Compilation Compilation { get; }
            public INamedTypeSymbol TypeIEquatableT { get; }

            public Analyzer(Compilation compilation, INamedTypeSymbol typeIEquatableT)
            {
                Compilation = compilation;
                TypeIEquatableT = typeIEquatableT;
            }

            public void OnSymbol(SymbolAnalysisContext context)
            {
                if (!(context.Symbol is INamedTypeSymbol namedTypeSymbol &&
                    namedTypeSymbol.TypeKind is TypeKind.Class or TypeKind.Struct))
                {
                    return;
                }

                if (HasEqualsQualities(namedTypeSymbol))
                {
                    if (!ImplementsIEquatableT(namedTypeSymbol))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticNeedImplementIEquatable,
                            namedTypeSymbol.Locations[0],
                            namedTypeSymbol.Name));
                    }

                    if (!HasEqualityOperators(namedTypeSymbol))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticNeedOperatorEqauls,
                            namedTypeSymbol.Locations[0],
                            namedTypeSymbol.Name));
                    }

                    if (!HasStronglyTypedEquals(namedTypeSymbol))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticNeedStrongEquals,
                            namedTypeSymbol.Locations[0],
                            namedTypeSymbol.Name));
                    }
                }
            }

            private bool ImplementsIEquatableT(INamedTypeSymbol namedTypeSymbol)
            {
                if (namedTypeSymbol.Interfaces.Length > 0)
                {
                    foreach (var i in namedTypeSymbol.Interfaces)
                    {
                        if (i.OriginalDefinition.Equals(TypeIEquatableT, SymbolEqualityComparer.Default))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }

            private bool HasStronglyTypedEquals(INamedTypeSymbol namedTypeSymbol)
            {
                foreach (var methodSymbol in namedTypeSymbol.GetMembers("Equals").OfType<IMethodSymbol>())
                {
                    if (methodSymbol.Parameters.Length == 1 &&
                        namedTypeSymbol.Equals(methodSymbol.Parameters[0].Type, SymbolEqualityComparer.Default) &&
                        methodSymbol.ReturnType?.SpecialType == SpecialType.System_Boolean)
                    {
                        return true;
                    }
                }

                return false;
            }

            private bool HasEqualityOperators(INamedTypeSymbol namedTypeSymbol) =>
                namedTypeSymbol.GetMembers("op_Equality").Length > 0 &&
                namedTypeSymbol.GetMembers("op_Inequality").Length > 0;

            private bool HasEqualsQualities(INamedTypeSymbol namedTypeSymbol)
            {
                if (ImplementsIEquatableT(namedTypeSymbol))
                {
                    return true;
                }

                if (HasEqualityOperators(namedTypeSymbol) ||
                    namedTypeSymbol.GetMembers("Equals").Length > 0 ||
                    namedTypeSymbol.GetMembers("GetHashCode").Length > 0)
                {
                    return true;
                }

                return false;
            }
        }
    }
}
