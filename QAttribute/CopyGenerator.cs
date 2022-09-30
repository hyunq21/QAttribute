using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Text;

//Debug하려면 System.Diagnostics.Debugger.Launch()

namespace QAttribute
{
    [Generator]
    public class CopyGenerator : IIncrementalGenerator //[QCopy]
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
            //    sb.AppendLine(@$"{Indent}public class QCopyAttribute : System.Attribute");
            //    sb.AppendLine(@$"{Indent}{{");
            //    sb.AppendLine(@$"{Indent}}}");

            //    sb.AppendLine(@$"{Indent}public interface ICopyable");
            //    sb.AppendLine(@$"{Indent}{{");
            //    sb.AppendLine(@$"{Indent}{Indent}public void Copy(object value);");
            //    sb.AppendLine(@$"{Indent}}}");

            //    sb.AppendLine(@$"}}");

            //    ctx.AddSource("QCopyAttribute.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
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

                    if (fullName == "System.QCopyAttribute")
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

        private static string GenerateSource(HashSet<string> usingNamespaces, string clsNamespace, string className, List<FieldInfo> fieldList)
        {
            var sb = new StringBuilder();
            var namespaceIndent = string.Empty;

            var hasNamespace = string.IsNullOrEmpty(clsNamespace) == false;
            if (hasNamespace)
            {
                namespaceIndent = Indent;

                sb.AppendLine(@$"//SHK auto-generated code :)");
                usingNamespaces.Add("System");
                usingNamespaces.Add("System.Linq");
                foreach (var usingNamespace in usingNamespaces)
                    sb.AppendLine(@$"using {usingNamespace};");
                sb.AppendLine(@$"");

                sb.AppendLine(@$"namespace {clsNamespace}");
                sb.AppendLine(@$"{{");
            }
            
            sb.AppendLine(@$"{namespaceIndent}partial class {className}: ICopyable");
            sb.AppendLine(@$"{namespaceIndent}{{");
            sb.AppendLine(@$"{namespaceIndent}");
            sb.AppendLine(@$"{namespaceIndent}{Indent}public void Copy(object value)");
            sb.AppendLine(@$"{namespaceIndent}{Indent}{{");
            sb.AppendLine(@$"{namespaceIndent}{Indent}{Indent}var refValue = value as {className};");
            sb.AppendLine(@$"{namespaceIndent}{Indent}");

            foreach (var field in fieldList)
            {
                if (field.typeName is "int" or "double" or "bool" or "char" or "string" or "byte" or "decimal" or "long" or "float" or "short")
                    sb.AppendLine(@$"{namespaceIndent}{Indent}{Indent}{field.identifier} = refValue.{field.identifier};");


                //Match m = new Regex(@"^List<\w*>$").Match(field.TypeName);
                //if (m.Success)
                //{
                //    Debug.WriteLine("{0}:{1}", m.Index, m.Value);
                //}
                else if (field.typeKind == TypeKind.Array)
                {
                    sb.AppendLine(@$"{namespaceIndent}{Indent}{Indent}{field.identifier} = new {field.typeName.Replace("]",@$"refValue.{field.identifier}.Count()]")};");
                    sb.AppendLine(@$"{namespaceIndent}{Indent}{Indent}for(int i = 0; i < refValue.{field.identifier}.Count(); i++)");
                    sb.AppendLine(@$"{namespaceIndent}{Indent}{Indent}{{");
                    sb.AppendLine(@$"{namespaceIndent}{Indent}{Indent}{Indent}{field.identifier}[i] = refValue.{field.identifier}[i];");
                    sb.AppendLine(@$"{namespaceIndent}{Indent}{Indent}}}");

                }
                else if (field.typeName.Contains("List<") || field.typeName.Contains("ObservableCollection<"))
                {
                    sb.AppendLine(@$"{namespaceIndent}{Indent}{Indent}{field.identifier} = new {field.typeName}();");
                    sb.AppendLine(@$"{namespaceIndent}{Indent}{Indent}foreach(var item in refValue.{field.identifier})");
                    sb.AppendLine(@$"{namespaceIndent}{Indent}{Indent}{{");
                    sb.AppendLine(@$"{namespaceIndent}{Indent}{Indent}{Indent}{field.identifier}.Add(item);");
                    sb.AppendLine(@$"{namespaceIndent}{Indent}{Indent}}}");
                }
                else if (field.typeName.Contains("IEnumerable<"))
                {
                    sb.AppendLine(@$"{namespaceIndent}{Indent}{Indent}var list_{field.identifier} = new {field.typeName.Replace("IEnumerable", "List")}();");
                    sb.AppendLine(@$"{namespaceIndent}{Indent}{Indent}foreach(var item in refValue.{field.identifier})");
                    sb.AppendLine(@$"{namespaceIndent}{Indent}{Indent}{{");
                    sb.AppendLine(@$"{namespaceIndent}{Indent}{Indent}{Indent}list_{field.identifier}.Add(item);");
                    sb.AppendLine(@$"{namespaceIndent}{Indent}{Indent}}}");
                    sb.AppendLine(@$"{namespaceIndent}{Indent}{Indent}{field.identifier} = list;");
                }
                else
                {
                    sb.AppendLine(@$"{namespaceIndent}{Indent}{Indent}{field.identifier}.Copy(refValue.{field.identifier});");


                }
            }
            sb.AppendLine(@$"{namespaceIndent}{Indent}}}");

            sb.AppendLine(@$"{namespaceIndent}}}");

            if (string.IsNullOrEmpty(clsNamespace) == false)
                sb.AppendLine("}");

            return sb.ToString();
        }

        private static string GetSafeFieldName(string identifier) //Getter, Setter 이름 생성
        {
            if (identifier[0] == '_')
                return identifier.Substring(0);

            if (char.IsLower(identifier[0]))
                return identifier[0].ToString().ToUpper() + identifier.Substring(1);

            return identifier.ToUpper();
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
                        typeKind = model.GetTypeInfo(field.Declaration.Type).Type.TypeKind,
                        typeName = field.Declaration.Type.ToString(),
                        attributes = field.AttributeLists.SelectMany(x => x.Attributes.Select(y => new AttributeInfo() { name = y.Name.ToString() }))
                    };

                    fieldList.Add(info);
                }
            }

            return fieldList;
        }
    }
}