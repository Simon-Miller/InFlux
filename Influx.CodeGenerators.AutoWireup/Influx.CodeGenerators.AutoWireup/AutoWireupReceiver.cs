using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace Influx.CodeGenerators.AutoWireup
{
    internal class AutoWireupReceiver : ISyntaxReceiver
    {
        public List<ClassDeclarationSyntax> WireUpsList = new List<ClassDeclarationSyntax>();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            //if (syntaxNode is ClassDeclarationSyntax cds)
            //{
            //    if (cds.AttributeLists
            //            .Select(x => x.Attributes)
            //            .SelectMany(x => x)
            //            .Select(x => x.Name).OfType<IdentifierNameSyntax>()
            //            .Any(x => x.Identifier.ValueText == "AutoWireup")) // works!!!  YAY!

            //            //.Any(x => x.Identifier.ValueText.StartsWith("AutoWireup"))) // works..
            //            //.Any(x => x.Identifier.ValueText.StartsWith("AutoWireupAttribute"))) // doesn't work.
            //    {
            //        WireUpsList.Add(cds);
            //    }
            //}
        }
    }
}
