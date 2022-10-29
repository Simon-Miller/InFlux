using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Influx.AutoWireupCodeGenerator
{
    [Generator]
    public class AutoWireupCode : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            // Get our SyntaxReceiver back
            if ((context.SyntaxReceiver is MySyntaxReceiver receiver))
            {
                

                foreach (var wireUp in receiver.WireUpsList)
                {
                    var sb = new StringBuilder();
                    sb.Append("/* WOW!  IT Worked! \r\n");

                    // iterate over each wireUp (class declaration) pulling out fields.
                    var fields = wireUp.ChildNodes()
                                       .Where(x => x.IsKind(SyntaxKind.FieldDeclaration))
                                       .Select(x => (FieldDeclarationSyntax)x)
                                       .ToList();

                    // ignore the ignores!
                    fields = fields.Where(x => (x.AttributeLists.FirstOrDefault() == null 
                                             || x.AttributeLists.FirstOrDefault()
                                                               .GetText()
                                                               .ToString()
                                                               .Trim()
                                                               .StartsWith("[AutoWireupIgnore") == false))
                                   .ToList();

                    foreach(var field in fields)
                    {
                        var typeText = field.Declaration.Type.GetText().ToString().Trim();
                        var nameText = field.Declaration.Variables.FirstOrDefault()?.Identifier.Text.Trim();

                        //var hasIgnore = field.GetAnnotations("AutoWireupIgnore").Count() > 0;
                        //var hasIgnore2 = field.GetAnnotations("AutoWireupIgnoreAttribute").Count() > 0;
                        //var hasIgnore = field.AttributeLists.FirstOrDefault()?.GetText().ToString().Trim().StartsWith("[AutoWireupIgnore");


                        sb.Append($"type: {typeText} name: {nameText}\r\n");
                    }

                    

                    //foreach(var field in fields) 
                    //    sb.Append(field.GetText().ToString());

                    sb.Append("*/");

                    context.AddSource($"{wireUp.Identifier.ValueText}.g.cs", SourceText.From(encoding: Encoding.UTF8, text:sb.ToString()));
                }
            }
        }

        public void Initialize(GeneratorInitializationContext context)
        {
#if DEBUG
            if (!Debugger.IsAttached)
            {
                Debugger.Launch();
            }
#endif 

            context.RegisterForSyntaxNotifications(() => new MySyntaxReceiver());
        }
    }

    class MySyntaxReceiver : ISyntaxReceiver
    {
        public List<ClassDeclarationSyntax> WireUpsList = new List<ClassDeclarationSyntax>();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if(syntaxNode is ClassDeclarationSyntax cds)
            {
                if(cds.AttributeLists
                        .Select(x=> x.Attributes)
                        .SelectMany(x=>x)
                        .Select(x=>x.Name).OfType<IdentifierNameSyntax>()
                        .Any(x=>x.Identifier.ValueText.StartsWith("AutoWireup")))
                {
                    WireUpsList.Add(cds);
                }
            }
        }
    }
}
