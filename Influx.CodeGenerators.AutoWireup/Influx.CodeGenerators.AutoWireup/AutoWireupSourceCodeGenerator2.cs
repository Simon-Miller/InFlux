using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Influx.CodeGenerators.AutoWireup
{
    [Generator]
    public class AutoWireupSourceCodeGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            //// REMOVED!!!!  Causes debugger popups in Visual Studio, which annoyingly I didn't get when developing (would have been nice)
            ////              But you get it during package release.  I'm going crazy over new releases trying to get the deployed package to look
            ////              as it should -- because the source code, build and test etc - are not looking like the deployed package!!!
            ////              I've even pulled apart the NuGet package and confirmed the DLL appears to be the right one.  Still getting old behaviours???
            ////              I've noticed this re-enabled too.  Not sure how that happened, as I pulled in Git changes which should have remove it
            ////              when I committed from my other machine.  *sigh*.  *deep breath*.
            //#if DEBUG
            //            if (!Debugger.IsAttached)
            //            {
            //                Debugger.Launch();
            //            }
            //#endif
            context.RegisterForSyntaxNotifications(() => new MySyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            // Get our SyntaxReceiver back
            if ((context.SyntaxReceiver is MySyntaxReceiver receiver))
            {
                foreach (var wireUp in receiver.WireUpsList)
                {
                    var generatedCode = beginWireupOfCodeFile(wireUp);

                    context.AddSource($"{wireUp.Identifier.ValueText}.g.cs", SourceText.From(encoding: Encoding.UTF8, text: generatedCode));
                }
            }
        }

        private string beginWireupOfCodeFile(ClassDeclarationSyntax wireUp)
        {
            var sb = new StringBuilder();
            sb.Append($"// ** AUTO GENERATED CODE: Last generated: {DateTime.Now} **\r\n");
            sb.Append($"// ***********************\r\n\r\n");

            renderUsings(wireUp, sb);

            // get namespace, and class names, please!
            var className = getClassName(wireUp);
            var classModifiers = getClassModifiers(wireUp);
            var namespaceName = getNamespaceName(wireUp);

            // render namespace
            sb.Append($"\r\nnamespace {namespaceName} \r\n{{\r\n");

            // render class declaration
            sb.Append($"\t{classModifiers.Trim()} class {className} : IAutoWireup\r\n\t{{\r\n");

            var fields = getAutoWireupFields(wireUp);
            var fieldsAttributes = getFieldsAttributes(fields);

            renderConstructor(wireUp, sb, className, fields);

            // render the OnEntityChanged event.
            sb.Append("\t\tpublic QueuedEvent OnEntityChanged { get; init; } = new();\r\n\r\n");

            renderPropertiesWithAttributes(sb, fields, fieldsAttributes);

            // render end of class
            sb.Append("\r\n\t}");

            // render end of namespace
            sb.Append("\r\n}");

            return sb.ToString();
        }

        private string getClassName(ClassDeclarationSyntax wireUp) =>
            wireUp.Identifier.Text.Trim();

        private string getClassModifiers(ClassDeclarationSyntax wireUp)
        {
            // NOTE: If empty, then its PRIVATE!!  may contain "internal" or "public", etc.
            var classModifiers = string.Empty;
            wireUp.Modifiers.ToList().ForEach(m => classModifiers += $" {m.Text} ");

            return classModifiers;
        }

        private string getNamespaceName(ClassDeclarationSyntax wireUp)
        {
            var namespaceName = "ARGH!";

            SyntaxNode parent = wireUp;
            var done = false;

            while (parent.Parent != null || !done)
            {
                parent = parent.Parent;

                var ns3 = parent as NamespaceDeclarationSyntax;
                if (ns3 != null)
                {
                    namespaceName = ns3.Name.ToString().Trim();
                    done = true;
                }

                var ns4 = parent as FileScopedNamespaceDeclarationSyntax;
                if (ns4 != null)
                {
                    namespaceName = ns4.Name.ToString().Trim();
                    done = true;
                }
            }

            return namespaceName;
        }

        private void renderUsings(ClassDeclarationSyntax wireUp, StringBuilder sb)
        {
            // render usings
            sb.Append("using InFlux;\r\n");

            var syntaxRoot = getCompilationUnit(wireUp);
            if (syntaxRoot != null)
            {
                foreach (var usingSyntax in syntaxRoot.Usings)
                {
                    var textOfUsing = usingSyntax.GetText().ToString().Trim();

                    sb.Append($"{textOfUsing}\r\n");
                }
            }
        }

        /// <summary>
        /// Returns an element that represent the code file root, and therefore giving access to 'using' statements.
        /// see: https://github.com/dominikjeske/Samples/blob/main/SourceGenerators/HomeCenter.SourceGenerators/Extensions/RoslynExtensions.cs
        /// </summary>
        private CompilationUnitSyntax getCompilationUnit(SyntaxNode syntaxNode) =>
            syntaxNode.Ancestors().OfType<CompilationUnitSyntax>().FirstOrDefault();

        private IEnumerable<FieldDeclarationSyntax> getAutoWireupFields(ClassDeclarationSyntax wireUp)
        {
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

            return fields;
        }

        private Dictionary<string, List<string>> getFieldsAttributes(IEnumerable<FieldDeclarationSyntax> fields)
        {
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

            return fieldsAttributes;
        }

        private void renderConstructor(ClassDeclarationSyntax wireUp, StringBuilder sb, string className, IEnumerable<FieldDeclarationSyntax> fields)
        {
            // render class constructor declaration
            sb.Append($"\t\tpublic {className}(IntentProcessor intentProcessor)\r\n\t\t{{");

            sb.Append(@"
            IntentProcessor = intentProcessor;
            var factory = new InsightsFactory(IntentProcessor);

            // CONSTRUCUTOR
            // ------------");

            foreach (var field in fields)
            {
                var fieldName = field.Declaration.Variables.FirstOrDefault()?.Identifier.Text.Trim();
                var propertyName = capitaliseFirstLetter(fieldName);
                if (propertyName == fieldName)
                    propertyName += "_";
                var propertyIndirectName = $"{propertyName}Insights";

                sb.Append($@"
            var ");


                sb.Append($"\t\t\t{propertyIndirectName} = new(() => {fieldName}, value => {fieldName} = value);\r\n");
                sb.Append($"\t\t\t{propertyIndirectName}.ValueChangedNotification.Subscribe(() => OnEntityChanged.FireEvent());\r\n\r\n");
            }

            // render end of constructor
            sb.Append("\t\t}\r\n\r\n");
        }

        

        

        private void renderPropertiesWithAttributes(StringBuilder sb, IEnumerable<FieldDeclarationSyntax> fields, Dictionary<string, List<string>> fieldsAttributes)
        {
            foreach (var field in fields)
            {
                var fieldName = field.Declaration.Variables.FirstOrDefault()?.Identifier.Text.Trim();
                var typeText = field.Declaration.Type.GetText().ToString().Trim();

                var propertyName = capitaliseFirstLetter(fieldName);
                if (propertyName == fieldName)
                    propertyName += "_";
                var propertyIndirectName = $"{propertyName}Insight";

                // render property attributes
                if (fieldsAttributes.TryGetValue(fieldName, out var attributes))
                {
                    if (attributes.Count > 0)
                    {
                        foreach (var attribute in attributes)
                        {
                            sb.Append($"\t\t{attribute}\r\n");
                        }
                    }
                }

                // render property
                sb.Append($"\t\tpublic {typeText} {propertyName} {{get => {propertyIndirectName}.Value; set => {propertyIndirectName}.Value = value; }}\r\n");

                // render property indirection accessor
                sb.Append($"\t\tpublic readonly QueuedEventPropertyIndirect<{typeText}> {propertyIndirectName};\r\n\r\n");
            }
        }

        private string capitaliseFirstLetter(string text)
        {
            char[] chars = text.ToCharArray();
            chars[0] = char.ToUpper(chars[0]);

            return new string(chars);
        }
    }
}
