﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace Influx.CodeGenerators.AutoWireup
{
    [Generator]
    public class AutoWireupWithIntentIncrementalSourceCodeGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // looks like we have to identify the types instances that we consider needing source generation.
            // SO: should do something like the Syntax receiver.
            var stuffToProcess = context.SyntaxProvider.CreateSyntaxProvider(IsClassNeedingGeneratedCode, ()=>)

            // THEN register the collection (if any?) of objects that need processing with the provided code. (Action)
            // I'm guessing the compiler is therefore creating collections of collections, and parses the entire source tree,
            // before running all the generators.  But how and when it works, is not our concern.
            context.RegisterSourceOutput(,);

        }

        public bool IsClassNeedingGeneratedCode(SyntaxNode syntaxNode, CancellationToken cancellationToken)
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
                foreach (var attr in attribs)
                {
                    if (AttributeNamesConsidered.Contains(attr) == false)
                        AttributeNamesConsidered.Add(attr);
                }
            }
        }
    }

    [Generator]
    public class AutoWireupWithIntentSourceCodeGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            //// REMOVED!!!!  Causes debugger popups in Visual Studio, which annoyingly I didn't get when developing (would have been nice)
            ////              But you get it during package release.  I'm going crazy over new releases trying to get the deployed package to look
            ////              as it should -- because the source code, build and test etc - are not looking like the deployed package!!!
            ////              I've even pulled apart the NuGet package and confirmed the DLL appears to be the right one.  Still getting old behaviours???
            ////              I've noticed this re-enabled too.  Not sure how that happened, as I pulled in Git changes which should have remove it
            ////              when I committed from my other machine.  *sigh*.  *deep breath*.
#if DEBUG
            if (!Debugger.IsAttached)
            {
                Debugger.Launch();
            }
#endif
            context.RegisterForSyntaxNotifications(() => new AutoWireupWithIntentReceiver());
        }

        // TEMP
        private List<string> attributeNamesConsidered = null!; 

        public void Execute(GeneratorExecutionContext context)
        {
            // Get our SyntaxReceiver back
            if ((context.SyntaxReceiver is AutoWireupWithIntentReceiver receiver))
            {
                foreach (var wireUp in receiver.WireUpsList)
                {
                    // TEMP
                    attributeNamesConsidered = receiver.AttributeNamesConsidered; // DELETE ME

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

            // DELETE ME
            foreach (string attribute in attributeNamesConsidered)
                sb.Append($"// {attribute}\r\n");


            renderUsings(wireUp, sb);

            // get namespace, and class names, please!
            var className = getClassName(wireUp);
            var classModifiers = getClassModifiers(wireUp);
            var namespaceName = getNamespaceName(wireUp);

            // render namespace
            sb.Append($"\r\nnamespace {namespaceName} \r\n{{\r\n");

            // render class declaration
            sb.Append($"\t{classModifiers.Trim()} class {className}\r\n\t{{\r\n");

            var fields = getAutoWireupFields(wireUp);
            var fieldsAttributes = getFieldsAttributes(fields);

            renderConstructor(wireUp, sb, className, fields);

            // render the OnEntityChanged event.
            sb.Append(@"
        public readonly IntentProcessor IntentProcessor;
");

            renderResetToPristine(sb, fields);
            renderModelTouched(sb, fields);
            renderModelDirty(sb, fields);

            sb.Append(@"
        // PROPERTY
        // --------
");

            renderPropertiesWithAttributes(sb, fields, fieldsAttributes, className);

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
            sb.Append("using System.Diagnostics;\r\n");

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
            this.IntentProcessor = intentProcessor;
            var factory = new InsightsFactory(intentProcessor);");

            foreach (var field in fields)
            {
                var fieldName = field.Declaration.Variables.FirstOrDefault()?.Identifier.Text.Trim();
                var propertyName = capitaliseFirstLetter(fieldName);
                if (propertyName == fieldName)
                    propertyName += "_";
                var propertyIndirectName = $"{propertyName}Insights";

                sb.Append(
$@"
            var {fieldName}Resources = factory.Make({fieldName});
            {propertyName}Insights = {fieldName}Resources.insight;
            { propertyName}InsightsManager = {fieldName}Resources.manager;
                ");

            } // end of foreach

            // render end of constructor
            sb.Append(
@"
        }
            ");
        }

        private void renderResetToPristine(StringBuilder sb, IEnumerable<FieldDeclarationSyntax> fields)
        {
            sb.Append(@"
        public void ResetToPristine()
        {
            ");

            foreach (var field in fields)
            {
                var fieldName = field.Declaration.Variables.FirstOrDefault()?.Identifier.Text.Trim();
                var propertyName = capitaliseFirstLetter(fieldName);
                if (propertyName == fieldName)
                    propertyName += "_";
                var propertyIndirectName = $"{propertyName}Insights";

                sb.Append($@"{propertyIndirectName}.ResetToPristine();
            ");

            } // end of foreach

            sb.Append(@"
        }
");

        }   // end of renderResetToPristine

        private void renderModelTouched(StringBuilder sb, IEnumerable<FieldDeclarationSyntax> fields)
        {
            sb.Append(@"
        public bool ModelTouched =>
            ");

            var allFields = fields.ToList();
            for(int i=0; i< fields.Count(); i++)
            {
                var field = allFields[i];

                var fieldName = field.Declaration.Variables.FirstOrDefault()?.Identifier.Text.Trim();
                var propertyName = capitaliseFirstLetter(fieldName);
                if (propertyName == fieldName)
                    propertyName += "_";
                var propertyIndirectName = $"{propertyName}Insights";

                if (i > 0)
                    sb.Append(" && ");

                sb.Append($"{propertyIndirectName}.IsTouched");
            }

            sb.Append(@";
");
        } // end of renderModelTouched

        private void renderModelDirty(StringBuilder sb, IEnumerable<FieldDeclarationSyntax> fields)
        {
            sb.Append(@"
        public bool ModelDirty =>
            ");

            var allFields = fields.ToList();
            for (int i = 0; i < fields.Count(); i++)
            {
                var field = allFields[i];

                var fieldName = field.Declaration.Variables.FirstOrDefault()?.Identifier.Text.Trim();
                var propertyName = capitaliseFirstLetter(fieldName);
                if (propertyName == fieldName)
                    propertyName += "_";
                var propertyIndirectName = $"{propertyName}Insights";

                if (i > 0)
                    sb.Append(" && ");

                sb.Append($"{propertyIndirectName}.IsDirty");
            }

            sb.Append(@";
");
        } // end of renderModelTouched

        private void renderPropertiesWithAttributes(StringBuilder sb, IEnumerable<FieldDeclarationSyntax> fields, Dictionary<string, List<string>> fieldsAttributes, string className)
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
                //sb.Append($"\t\tpublic {typeText} {propertyName} {{get => {propertyIndirectName}.Value; set => {propertyIndirectName}.Value = value; }}\r\n");

                sb.Append(
$@"        public {typeText} {propertyName} => {fieldName};
        public readonly Insights<{typeText}> {propertyName}Insights;
        private readonly IOwnInsight {propertyName}InsightsManager;

        //[DebuggerStepThrough]
        public void TrySet{propertyName}({typeText} newValue, Action? codeIfAllowed = null, Action? codeIfNotAllowed = null) =>
            IntentHelper.TrySet<{typeText}>(IntentProcessor, ""{className}"", ""{propertyName}"",  () => {fieldName}, x => {fieldName} = x, newValue,
                {propertyName}Insights, {propertyName}InsightsManager, codeIfAllowed, codeIfNotAllowed);
        ");

            } // end of foreach
        } // end of renderPropertiesWithAttributes

        private string capitaliseFirstLetter(string text)
        {
            char[] chars = text.ToCharArray();
            chars[0] = char.ToUpper(chars[0]);

            return new string(chars);
        }
    }
}
