using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Analyzer1
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class Analyzer1Analyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "IsValidMethodAnalyzer";
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Code Structure Reformator";

        private static DiagnosticDescriptor CreateIsValidRule
            = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(CreateIsValidRule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.AttributeList);
        }

        private static DiagnosticDescriptor ErrorRule =
        new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);


        private static bool IsValidMethodContains(MethodDeclarationSyntax mds)
        {
            var sourceText = mds.ReturnType.GetText();
            var s = sourceText.ToString();
            var b = s.Contains("IEnumerable<ValidationResult>");
            var b1 = mds.Identifier.ValueText == "IsValid";
            var any = !mds.ParameterList.Parameters.Any();
            var isPublicStatic = new[] { "public", "static" }.All(x => mds.Modifiers.Any(mod => x.Contains(mod.Text)));
            return b && b1 && any && isPublicStatic;
        }

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            AttributeSyntax att = null;

            var als = context.Node as AttributeListSyntax;

            if (!als.Attributes.Any(xx => xx.Name.GetText().ToString() == "ValueObject"))
            {
                return;
            }

            if (!(als.Parent is ClassDeclarationSyntax cds))
            {
                return;
            }

            var maybeIsValidMethod = cds.DescendantNodes().OfType<MethodDeclarationSyntax>().Where(x => x.Identifier.ValueText == "IsValid");

            if (!maybeIsValidMethod.Any() || !maybeIsValidMethod.Any(x => IsValidMethodContains(x)))
            {
                context.ReportDiagnostic(Diagnostic.Create(ErrorRule, context.Node.Parent.GetLocation()));
            }
        }

        private static void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            // TODO: Replace the following code with your own analysis, generating Diagnostic objects for any issues you find
            var namedTypeSymbol = (INamedTypeSymbol)context.Symbol;

            // Find just those named type symbols with names containing lowercase letters.
            if (namedTypeSymbol.Name.ToCharArray().Any(char.IsLower))
            {
                // For all such symbols, produce a diagnostic.
                var diagnostic = Diagnostic.Create(CreateIsValidRule, namedTypeSymbol.Locations[0], namedTypeSymbol.Name);

                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
