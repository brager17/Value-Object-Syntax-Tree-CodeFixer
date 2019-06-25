using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Analyzer1
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CreateIsValidCodeFix)), Shared]
    public class CreateIsValidCodeFix : CodeFixProvider
    {
        private const string title = "Create IsValid method";

        private const string DataAnnotationsUsingString = @"using System.ComponentModel.DataAnnotations;";
        private UsingDirectiveSyntax DataAnnotationUsing => SyntaxFactory.ParseSyntaxTree(DataAnnotationsUsingString)
            .GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>().Single();

        private static MethodDeclarationSyntax IsValidMethodTemplate => SyntaxFactory.ParseSyntaxTree(
      @"
      public static IEnumerable<ValidationResult> IsValid()
      {
           throw new NotImplementException();
      }
      ").GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single();

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(Analyzer1Analyzer.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var classDeclaration = root.FindNode(diagnosticSpan) as ClassDeclarationSyntax;

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: title,
                    createChangedDocument: c => MakeSolution(context.Document, classDeclaration, c),
                    equivalenceKey: title),
                diagnostic);
        }

        private async Task<Document> MakeSolution(Document document, ClassDeclarationSyntax cds, CancellationToken cancellationToken)
        {
            var oldTree = await document.GetSyntaxRootAsync();
            var newCds = cds.WithMembers(new SyntaxList<MemberDeclarationSyntax>(IsValidMethodTemplate));
            var dataAnnotationUsingExists = oldTree.DescendantNodes().OfType<UsingDirectiveSyntax>().Any(x => x.Language == DataAnnotationsUsingString);

            var newTree = oldTree;
            if (!dataAnnotationUsingExists)
            {
                var compilationUnit = SyntaxFactory.ParseCompilationUnit(oldTree.ToString());
                compilationUnit = compilationUnit.AddUsings(DataAnnotationUsing);

                newTree = await compilationUnit.SyntaxTree.GetRootAsync(cancellationToken);
            }

            newTree = newTree.ReplaceNode(cds, newCds);
            return document.WithSyntaxRoot(newTree);

        }
    }
}
