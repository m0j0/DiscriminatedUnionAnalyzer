using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace DiscriminatedUnionAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DiscriminatedUnionAnalyzerAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "DU001";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Naming";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            // TODO: Consider registering other actions that act on syntax instead of or in addition to symbols
            // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
            //context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
            context.RegisterSyntaxNodeAction(AnalyzeSyntaxNode, SyntaxKind.SwitchExpression);
        }

        private void AnalyzeSyntaxNode(SyntaxNodeAnalysisContext context)
        {
            var declaration = (SwitchExpressionSyntax)context.Node;

#pragma warning disable RS1030 // TODO
            var semanticModel = context.Compilation.GetSemanticModel(declaration.GoverningExpression.SyntaxTree);
#pragma warning restore RS1030

            var typeInfo = semanticModel.GetTypeInfo(declaration.GoverningExpression);

            if (typeInfo.Type is not INamedTypeSymbol namedTypeSymbol)
            {
                return;
            }
            
            if (!namedTypeSymbol.IsAbstract)
            {
                return;
            }

            if (namedTypeSymbol.Constructors.Length != 1)
            {
                return;
            }

            var constructor = namedTypeSymbol.Constructors[0];
            if (constructor.DeclaredAccessibility != Accessibility.Private)
            {
                return;
            }

            if (constructor.Parameters.Length != 0)
            {
                return;
            }

            var nestedTypes = namedTypeSymbol.GetMembers().OfType<INamedTypeSymbol>().ToArray();
            var subtypes = nestedTypes.Where(t => namedTypeSymbol.Equals(t.BaseType, SymbolEqualityComparer.Default)).ToArray();
            if (nestedTypes.Count() != subtypes.Count())
            {
                return;
            }

            var hasDiscard = declaration.Arms.Any(a => a.Pattern is DiscardPatternSyntax);
            if (!hasDiscard)
            {
                return;
            }


            var switchExpressionArmSyntaxes = declaration
                .Arms
                .Where(s => s.Pattern is ConstantPatternSyntax or DeclarationPatternSyntax)
                .Select(s =>
                {
                    if (s.Pattern is ConstantPatternSyntax cps)
                        return semanticModel.GetTypeInfo(cps.Expression).Type;

                    if (s.Pattern is DeclarationPatternSyntax dps)
                        return semanticModel.GetTypeInfo(dps.Type).Type;

                    throw new Exception();
                })
                .OfType<INamedTypeSymbol>()
                .ToArray();
            
            var notHandledArms = subtypes.Where(s => switchExpressionArmSyntaxes.All(arm => arm.ToString() != s.ToString())).ToArray();
            if (notHandledArms.Length == 0)
            {
                return;
            }

            var diagnostic = Diagnostic.Create(Rule, declaration.GetLocation(), string.Join(",", notHandledArms.Select(s => s.ToString())));

            context.ReportDiagnostic(diagnostic);
        }

        private static void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            // TODO: Replace the following code with your own analysis, generating Diagnostic objects for any issues you find
            var namedTypeSymbol = (INamedTypeSymbol)context.Symbol;

            // Find just those named type symbols with names containing lowercase letters.
            if (namedTypeSymbol.Name.ToCharArray().Any(char.IsLower))
            {
                // For all such symbols, produce a diagnostic.
                var diagnostic = Diagnostic.Create(Rule, namedTypeSymbol.Locations[0], namedTypeSymbol.Name);

                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
