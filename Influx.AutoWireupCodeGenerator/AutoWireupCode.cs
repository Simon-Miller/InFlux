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
                    sb.Append($"// ** AUTO GENERATED CODE: Last generated: {DateTime.Now} **\r\n");
                    sb.Append($"// ***********************\r\n\r\n");

                    // get namespace, and class names, please!
                    var className = wireUp.Identifier.Text.Trim();

                    // NOTE: If empty, then its PRIVATE!!  may contain "internal" or "public", etc.
                    var classModifiers = ""; wireUp.Modifiers.ToList().ForEach(m => classModifiers += $" {m.Text} ");

                    var namespaceName = "ARGH!";
                    var ns1 = wireUp.Parent as NamespaceDeclarationSyntax;
                    var ns2 = wireUp.Parent as FileScopedNamespaceDeclarationSyntax;
                    if (ns1 != null) namespaceName = ns1.Name.ToString().Trim();
                    if (ns2 != null) namespaceName = ns2.Name.ToString().Trim();


                    // render usings
                    sb.Append("using InFlux;\r\n");

                    var syntaxRoot = GetCompilationUnit(wireUp);
                    if(syntaxRoot != null)
                    {
                        foreach (var usingSyntax in syntaxRoot.Usings)
                        {
                            var textOfUsing = usingSyntax.GetText().ToString().Trim();

                            sb.Append($"{textOfUsing}\r\n");
                        }
                    }

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

                    var fieldsAttributes = new Dictionary<string, List<string>>();
                    foreach (var field in fields)
                    {
                        var nameText = field.Declaration.Variables.FirstOrDefault()?.Identifier.Text.Trim();
                        fieldsAttributes.Add(nameText,
                                             field.AttributeLists.Select(x => x.GetText()
                                                                               .ToString()
                                                                               .Trim())
                                                                 .ToList()
                                            );
                    }

                    // render namespace
                    sb.Append($"\r\nnamespace {namespaceName} \r\n{{\r\n");

                    // render class declaration
                    sb.Append($"\t{classModifiers.Trim()} class {className}\r\n\t{{\r\n");

                    // render class constructor declaration
                    sb.Append($"\t\tpublic {className}()\r\n\t\t{{\r\n");

                    foreach (var field in fields)
                    {
                        var fieldName = field.Declaration.Variables.FirstOrDefault()?.Identifier.Text.Trim();
                        var propertyName = capitaliseFirstLetter(fieldName);
                        if (propertyName == fieldName)
                            propertyName += "_";
                        var propertyIndirectName = $"{propertyName}Indirect";

                        sb.Append($"\t\t\t{propertyIndirectName} = new(() => {fieldName}, value => {fieldName} = value);\r\n");
                        sb.Append($"\t\t\t{propertyIndirectName}.ValueChangedNotification.Subscribe(() => OnEntityChanged.FireEvent());\r\n");
                    }

                    // render end of constructor
                    sb.Append("\t\t}\r\n");

                    // render the OnEntityChanged event.
                    sb.Append("\t\tpublic readonly QueuedEvent OnEntityChanged = new();\r\n");

                    foreach (var field in fields)
                    {
                        var fieldName = field.Declaration.Variables.FirstOrDefault()?.Identifier.Text.Trim();
                        var typeText = field.Declaration.Type.GetText().ToString().Trim();

                        var propertyName = capitaliseFirstLetter(fieldName);
                        if (propertyName == fieldName)
                            propertyName += "_";
                        var propertyIndirectName = $"{propertyName}Indirect";

                        // render property attributes
                        if (fieldsAttributes.TryGetValue(fieldName, out var attributes))
                        {
                            sb.Append("\t\t");
                            foreach (var attribute in attributes)
                                sb.Append($"{attribute} ");
                            sb.Append("\r\n");
                        }
                        // render property
                        sb.Append($"\t\tpublic int {propertyName} {{get => {propertyIndirectName}.Value; set => {propertyIndirectName}.Value = value; }}\r\n");

                        // render property indirection accessor
                        sb.Append($"\t\tpublic readonly QueuedEventPropertyIndirect<{typeText}> {propertyIndirectName};");
                    }

                    // render end of class
                    sb.Append("\r\n\t}");

                    // render end of namespace
                    sb.Append("\r\n}");

                    context.AddSource($"{wireUp.Identifier.ValueText}.g.cs", SourceText.From(encoding: Encoding.UTF8, text: sb.ToString()));
                }
            }
        }

        /// <summary>
        /// see: https://github.com/dominikjeske/Samples/blob/main/SourceGenerators/HomeCenter.SourceGenerators/Extensions/RoslynExtensions.cs
        /// </summary>
        public CompilationUnitSyntax GetCompilationUnit(SyntaxNode syntaxNode)
        {
            return syntaxNode.Ancestors().OfType<CompilationUnitSyntax>().FirstOrDefault();
        }

        private string capitaliseFirstLetter(string text)
        {
            char[] chars = text.ToCharArray();
            chars[0] = char.ToUpper(chars[0]);

            return new string(chars);
        }

        public void Initialize(GeneratorInitializationContext context)
        {
//#if DEBUG
//            if (!Debugger.IsAttached)
//            {
//                Debugger.Launch();
//            }
//#endif 

            context.RegisterForSyntaxNotifications(() => new MySyntaxReceiver());
        }
    }

    internal class MySyntaxReceiver : ISyntaxReceiver
    {
        public List<ClassDeclarationSyntax> WireUpsList = new List<ClassDeclarationSyntax>();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is ClassDeclarationSyntax cds)
            {
                if (cds.AttributeLists
                        .Select(x => x.Attributes)
                        .SelectMany(x => x)
                        .Select(x => x.Name).OfType<IdentifierNameSyntax>()
                        .Any(x => x.Identifier.ValueText.StartsWith("AutoWireup")))
                {
                    WireUpsList.Add(cds);
                }
            }
        }
    }
}
