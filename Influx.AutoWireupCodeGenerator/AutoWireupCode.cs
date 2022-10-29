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

                    // get namespace, and class names, please!
                    var className = wireUp.Identifier.Text;
                    sb.Append($"class name:{className}\r\n");

                    // NOTE: If empty, then its PRIVATE!!  may contain "internal" or "public", etc.
                    var classModifiers = ""; wireUp.Modifiers.ToList().ForEach(m => classModifiers += $" {m.Text} ");
                    sb.Append($"class modifiers:{classModifiers}\r\n");

                    var namespaceName = "ARGH!";
                    var ns1 = wireUp.Parent as NamespaceDeclarationSyntax;
                    var ns2 = wireUp.Parent as FileScopedNamespaceDeclarationSyntax;
                    if (ns1 != null) namespaceName = ns1.Name.ToString();
                    if (ns2 != null) namespaceName = ns2.Name.ToString();
                    sb.Append($"class's namespace:{namespaceName}\r\n");

                    // iterate over each wireUp (class declaration) pulling out fields.
                    var fields = wireUp.ChildNodes()
                                       .Where(x => x.IsKind(SyntaxKind.FieldDeclaration))
                                       .Select(x => (FieldDeclarationSyntax)x)
                                       .ToList();



                    var fieldsAttributes = new Dictionary<string, List<string>>();
                    foreach(var field in fields)
                    {
                        var nameText = field.Declaration.Variables.FirstOrDefault()?.Identifier.Text.Trim();
                        fieldsAttributes.Add(nameText, 
                                             field.AttributeLists.Select(x => x.GetText()
                                                                               .ToString()
                                                                               .Trim())
                                                                 .ToList()
                                            );
                    }
                    // add to output:
                    foreach (var key in fieldsAttributes.Keys)
                        foreach (var value in fieldsAttributes[key])
                            sb.Append($"key:{key} value:{value} \r\n");
                    

                    // ignore the ignores!
                    fields = fields.Where(x => (x.AttributeLists.FirstOrDefault() == null 
                                             || x.AttributeLists.FirstOrDefault()
                                                               .GetText()
                                                               .ToString()
                                                               .Trim()
                                                               .StartsWith("[AutoWireupIgnore") == false))
                                   .ToList();

                    if(fields.Count >0)
                    {
                        // TODO: Add the EntityChanged, EntityDirty, and EntityValid events?
                        // TODO: Add a constructor that wires up to all property events, so it can fire off the EntityChanged etc.
                    }

                    foreach(var field in fields)
                    {
                        var typeText = field.Declaration.Type.GetText().ToString().Trim();
                        var nameText = field.Declaration.Variables.FirstOrDefault()?.Identifier.Text.Trim();

                        // TODO: Now we've type and name, we should be able to generate public properties,
                        // and wire up events?

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
