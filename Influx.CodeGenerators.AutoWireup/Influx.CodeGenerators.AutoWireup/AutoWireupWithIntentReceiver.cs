using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace Influx.CodeGenerators.AutoWireup
{
    internal class AutoWireupWithIntentReceiver : ISyntaxReceiver
    {
        public List<ClassDeclarationSyntax> WireUpsList = new List<ClassDeclarationSyntax>();

        public List<string> AttributeNamesConsidered = new List<string>();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is ClassDeclarationSyntax cds)
            {
                if (cds.AttributeLists
                        .Select(x => x.Attributes)
                        .SelectMany(x => x)
                        .Select(x => x.Name).OfType<IdentifierNameSyntax>()
                        .Any(x => x.Identifier.ValueText.StartsWith("AutoWireupWithIntent")))
                {
                    WireUpsList.Add(cds);
                }

                // TEMP
                AttributeNamesConsidered.Clear();
                var attribs =
                    cds.AttributeLists.Select(x => x.Attributes)
                                      .SelectMany(x => x)
                                      .Select(x => x.Name).OfType<IdentifierNameSyntax>()
                                      .Select(x => x.Identifier.ValueText);
                foreach(var attr in attribs)
                {
                    if(AttributeNamesConsidered.Contains(attr) == false)
                        AttributeNamesConsidered.Add(attr);
                }
            }
        }
    }
}
