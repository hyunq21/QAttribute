using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Text;

//Debug하려면 System.Diagnostics.Debugger.Launch()

namespace QAttribute
{
    [Generator]
    public class BindingGenerator : IIncrementalGenerator //[QBinding]
    {
        private const string Indent = $@"    ";
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            //context.RegisterPostInitializationOutput((ctx) =>
            //{
            //    var sb = new StringBuilder();// Auto-generated code
            //    sb.AppendLine(@$"//SHK auto-generated code :)");
            //    sb.AppendLine(@$"namespace System");
            //    sb.AppendLine(@$"{{");
            //    sb.AppendLine(@$"{Indent}[System.AttributeUsage(System.AttributeTargets.Class)]");
            //    sb.AppendLine(@$"{Indent}public class QBindingAttribute : System.Attribute");
            //    sb.AppendLine(@$"{Indent}{{");
            //    sb.AppendLine(@$"{Indent}}}");
            //    sb.AppendLine(@$"");
            //    sb.AppendLine(@$"{Indent}[System.AttributeUsage(System.AttributeTargets.Field)]");
            //    sb.AppendLine(@$"{Indent}public class QNoBindingAttribute : System.Attribute");
            //    sb.AppendLine(@$"{Indent}{{");
            //    sb.AppendLine(@$"{Indent}}}");
            //    sb.AppendLine(@$"");
            //    sb.AppendLine(@$"{Indent}[System.AttributeUsage(System.AttributeTargets.Field)]");
            //    sb.AppendLine(@$"{Indent}public class QVirtualAttribute : System.Attribute");
            //    sb.AppendLine(@$"{Indent}{{");
            //    sb.AppendLine(@$"{Indent}}}");
            //    sb.AppendLine(@$"");
            //    sb.AppendLine(@$"{Indent}[System.AttributeUsage(System.AttributeTargets.Field)]");
            //    sb.AppendLine(@$"{Indent}public class QEventAttribute : System.Attribute");
            //    sb.AppendLine(@$"{Indent}{{");
            //    sb.AppendLine(@$"{Indent}{Indent}public string PropertyChanging;");
            //    sb.AppendLine(@$"{Indent}{Indent}public string PropertyChanged;");
            //    sb.AppendLine(@$"{Indent}}}");
            //    sb.AppendLine(@$"");
            //    sb.AppendLine(@$"{Indent}[System.AttributeUsage(System.AttributeTargets.Field)]");
            //    sb.AppendLine(@$"{Indent}public class QJsonNameAttribute : System.Attribute");
            //    sb.AppendLine(@$"{Indent}{{");
            //    sb.AppendLine(@$"{Indent}{Indent}private string jsonName;");
            //    sb.AppendLine(@$"{Indent}{Indent}public QJsonNameAttribute(string name)");
            //    sb.AppendLine(@$"{Indent}{Indent}{{");
            //    sb.AppendLine(@$"{Indent}{Indent}{Indent}this.jsonName = name;");
            //    sb.AppendLine(@$"{Indent}{Indent}}}");
            //    sb.AppendLine(@$"{Indent}}}");
            //    sb.AppendLine(@$"");
            //    sb.AppendLine(@$"{Indent}[System.AttributeUsage(System.AttributeTargets.Method)]");
            //    sb.AppendLine(@$"{Indent}public class QCommandAttribute : System.Attribute");
            //    sb.AppendLine(@$"{Indent}{{");
            //    sb.AppendLine(@$"{Indent}}}");
            //    sb.AppendLine(@$"}}");

            //    ctx.AddSource("QBindingAttribute.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
            //});

            IncrementalValuesProvider<ClassDeclarationSyntax> classDeclarations = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (s, _) => IsSyntaxTargetForGeneration(s), // select class with attributes
                    transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx)) // select the class with the [AutoProp] attribute
                .Where(static m => m is not null)!; // filter out attributed classes that we don't care about

            // 선택한 class를 `Compilation`과 결합
            IncrementalValueProvider<(Compilation, ImmutableArray<ClassDeclarationSyntax>)> compilationAndClass
                = context.CompilationProvider.Combine(classDeclarations.Collect());

            // Compilation 및 class를 사용하여 소스 생성
            context.RegisterSourceOutput(compilationAndClass,
                static (spc, source) => Execute(source.Item1, source.Item2, spc));
        }

        private static void Execute(Compilation compilation, ImmutableArray<ClassDeclarationSyntax> classes, SourceProductionContext context)
        {
            if (classes.IsDefaultOrEmpty)
                return;

            foreach (var cls in classes.Distinct())
            {
                var fieldList = GetFieldList(compilation, cls);
                if (fieldList.Count == 0)
                    continue;


                var usingNamespace = GetUsingNamespaces(compilation, cls);

                var clsNamespace = GetNamespace(compilation, cls);

                var src = GenerateSource(usingNamespace, clsNamespace, cls.Identifier.ValueText, fieldList);
                context.AddSource($"{cls.Identifier.ValueText}.g.cs", SourceText.From(src, Encoding.UTF8));
            }
        }

        private static bool IsSyntaxTargetForGeneration(SyntaxNode node)
        {
            return node is ClassDeclarationSyntax m && m.AttributeLists.Count > 0;
        }

        //private const string EnumExtensionsAttribute = "NetEscapades.EnumGenerators.EnumExtensionsAttribute";

        private static ClassDeclarationSyntax? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
        {
            var classDeclarationSyntax = (ClassDeclarationSyntax)context.Node;

            foreach (var attributeListSyntax in classDeclarationSyntax.AttributeLists)
            {
                foreach (var attributeSyntax in attributeListSyntax.Attributes)
                {
                    if (context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol is not IMethodSymbol attributeSymbol)
                        continue;

                    var fullName = attributeSymbol.ContainingType.ToDisplayString();

                    if (fullName == "System.QBindingAttribute")
                    {
                        return classDeclarationSyntax;
                    }
                }
            }

            return null;
        }


        private static HashSet<string> GetUsingNamespaces(Compilation compilation, ClassDeclarationSyntax cls)
        {
            var model = compilation.GetSemanticModel(cls.SyntaxTree);

            HashSet<string> namespaces = new HashSet<string>();
            foreach (var usingDirective in cls.SyntaxTree.GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>())
            {
                var symbol = model.GetSymbolInfo(usingDirective.Name).Symbol;
                namespaces.Add(symbol.ToDisplayString(/*SymbolDisplayFormat.FullyQualifiedFormat*/));
            }

            return namespaces;
        }

        private static string GetNamespace(Compilation compilation, ClassDeclarationSyntax cls)
        {
            var model = compilation.GetSemanticModel(cls.SyntaxTree);

            foreach (var ns in cls.Ancestors().OfType<NamespaceDeclarationSyntax>())
                return ns.Name.ToString();

            return "";
        }

        private static string GenerateSource(HashSet<string> usingNamespaces, string clsNamespace, string className, List<FieldInfo> fields)
        {
            var sb = new StringBuilder();
            var namespaceIndent = string.Empty;

            var hasNamespace = string.IsNullOrEmpty(clsNamespace) == false;
            if (hasNamespace)
            {
                namespaceIndent = Indent;

                sb.AppendLine(@$"//SHK auto-generated code :)");
                usingNamespaces.Add("System.ComponentModel");
                usingNamespaces.Add("System.Runtime.CompilerServices");
                //usingNamespaces.Add("System.Text.Json");
                //usingNamespaces.Add("System.Text.Json.Serialization");
                foreach (var usingNamespace in usingNamespaces)
                    sb.AppendLine(@$"using {usingNamespace};");
                sb.AppendLine(@$"");

                sb.AppendLine(@$"namespace {clsNamespace}");
                sb.AppendLine(@$"{{");
            }

            sb.AppendLine(@$"{namespaceIndent}partial class {className} : INotifyPropertyChanged");
            sb.AppendLine(@$"{namespaceIndent}{{");

            foreach (var field in fields)
            {
                //check Binding exception field
                if (field.attributes.Any(x => x.name is "QNoBinding" or "QNoBindingAttribute"))
                    continue;

                string qVirtual = "";
                if (field.attributes.Any(x => x.name is "QVirtual" or "QVirtualAttribute"))
                    qVirtual = "virtual ";

                var propertyName = GetSafeFieldName(field.identifier);
                if(propertyName is null)
                    continue;
                

                var jsonAttribute = field.attributes?.FirstOrDefault(x => x.name is "QJsonName" or "QJsonNameAttribute");
                if (jsonAttribute is not null)
                {
                    var jsonName = jsonAttribute.commands.FirstOrDefault();
                    if (jsonName is not null)
                        sb.AppendLine(@$"{namespaceIndent}{Indent}[System.Text.Json.Serialization.JsonPropertyName(""{jsonName.command}"")]");
                }

                sb.AppendLine(@$"{namespaceIndent}{Indent}public {qVirtual}{field.typeName} {propertyName}");
                sb.AppendLine(@$"{namespaceIndent}{Indent}{{");
                sb.AppendLine(@$"{namespaceIndent}{Indent}{Indent}get => {field.identifier};");
                sb.AppendLine(@$"{namespaceIndent}{Indent}{Indent}set");
                sb.AppendLine(@$"{namespaceIndent}{Indent}{Indent}{{");

                var attribute = field.attributes?.FirstOrDefault(x => x.name is "QEvent" or "QEventAttribute");

                //PropertyChanging Event
                if (attribute is not null)
                {
                    var propertyChangingCommand = attribute.commands.FirstOrDefault(x => x.name.Contains("PropertyChanging"));
                    if (propertyChangingCommand is not null)
                        sb.AppendLine(@$"{namespaceIndent}{Indent}{Indent}{Indent}{propertyChangingCommand.command}();");
                }

                sb.AppendLine(@$"{namespaceIndent}{Indent}{Indent}{Indent}{field.identifier} = value;");
                sb.AppendLine(@$"{namespaceIndent}{Indent}{Indent}{Indent}NotifyPropertyChanged();");

                //PropertyChanged Event
                if (attribute is not null)
                {
                    var propertyChangedCommand = attribute.commands.FirstOrDefault(x => x.name.Contains("PropertyChanged"));
                    if (propertyChangedCommand is not null)
                        sb.AppendLine(@$"{namespaceIndent}{Indent}{Indent}{Indent}{propertyChangedCommand.command}();");
                }

                sb.AppendLine(@$"{namespaceIndent}{Indent}{Indent}}}");
                sb.AppendLine(@$"{namespaceIndent}{Indent}}}");

                sb.AppendLine(@$"{namespaceIndent}");
            }

            sb.AppendLine(@$"{namespaceIndent}");
            sb.AppendLine(@$"{namespaceIndent}#region INotifyPropertyChanged");
            sb.AppendLine(@$"{namespaceIndent}{Indent}public event PropertyChangedEventHandler PropertyChanged;");
            sb.AppendLine(@$"{namespaceIndent}{Indent}// This method is called by the Set accessor of each property.");
            sb.AppendLine(@$"{namespaceIndent}{Indent}// The CallerMemberName attribute that is applied to the optional propertyName");
            sb.AppendLine(@$"{namespaceIndent}{Indent}// parameter causes the property name of the caller to be substituted as an argument.");
            sb.AppendLine(@$"{namespaceIndent}{Indent}private void NotifyPropertyChanged([CallerMemberName] string propertyName = """")");
            sb.AppendLine(@$"{namespaceIndent}{Indent}{{");
            sb.AppendLine(@$"{namespaceIndent}{Indent}{Indent}PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));");
            sb.AppendLine(@$"{namespaceIndent}{Indent}}}");
            sb.AppendLine(@$"{namespaceIndent}#endregion");

            sb.AppendLine(@$"{namespaceIndent}}}");

            if (string.IsNullOrEmpty(clsNamespace) == false)
                sb.AppendLine("}");

            return sb.ToString();
        }

        private static string? GetSafeFieldName(string? identifier) //Getter, Setter 이름 생성
        {
            if(string.IsNullOrEmpty(identifier))
                return null;

            string name = identifier;
            if (identifier.Length > 0)
                if (identifier[0] is '_')
                    name = identifier.Substring(1, identifier.Length - 1);

            if (char.IsLower(name[0]))
                return name[0].ToString().ToUpper() + name.Substring(1);

            return name.ToUpper();
        }

        private static List<FieldInfo> GetFieldList(Compilation compilation, ClassDeclarationSyntax cls)
        {
            var fieldList = new List<FieldInfo>();

            var model = compilation.GetSemanticModel(cls.SyntaxTree);
            foreach (var field in cls.DescendantNodes().OfType<FieldDeclarationSyntax>())
            {
                foreach (var item in field.Declaration.Variables)
                {
                    var info = new FieldInfo
                    {
                        identifier = item.Identifier.ValueText,
                        typeName = field.Declaration.Type.ToString(),
                        attributes = field.AttributeLists.SelectMany(x => x.Attributes.Select(y => new AttributeInfo()
                        {
                            name = y.Name.ToString(),
                            commands = y.ArgumentList?.Arguments.Select(z => new CommandInfo()
                            {
                                name = z.NameEquals?.ToString(),
                                command = z.Expression.ToString().Replace("\"", "").Replace("()", "")
                            }) ?? null
                        }))
                    };

                    fieldList.Add(info);
                }
            }

            return fieldList;
        }
    }
}