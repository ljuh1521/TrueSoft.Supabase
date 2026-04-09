using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Truesoft.Supabase.RemoteConfig.Generator
{
    [Generator]
    public sealed class RemoteConfigSourceGenerator : ISourceGenerator
    {
        private const string RemoteConfigAttributeFullName = "Truesoft.Supabase.Unity.RemoteConfig.RemoteConfigAttribute";
        private const string RemoteConfigKeyAttributeFullName = "Truesoft.Supabase.Unity.RemoteConfig.RemoteConfigKeyAttribute";
        private const string RemoteConfigEntryTypeName = "Truesoft.Supabase.Unity.RemoteConfig.RemoteConfigEntry`1";

        public void Initialize(GeneratorInitializationContext context)
        {
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var compilation = context.Compilation;
            var remoteConfigAttr = compilation.GetTypeByMetadataName(RemoteConfigAttributeFullName);
            var keyAttr = compilation.GetTypeByMetadataName(RemoteConfigKeyAttributeFullName);
            var entryOpen = compilation.GetTypeByMetadataName(RemoteConfigEntryTypeName);
            if (remoteConfigAttr == null || keyAttr == null || entryOpen == null)
                return;

            foreach (var syntaxTree in compilation.SyntaxTrees)
            {
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var root = syntaxTree.GetRoot(context.CancellationToken);
                foreach (var classDecl in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
                {
                    if (!classDecl.Modifiers.Any(SyntaxKind.StaticKeyword) ||
                        !classDecl.Modifiers.Any(SyntaxKind.PartialKeyword))
                        continue;

                    if (semanticModel.GetDeclaredSymbol(classDecl, context.CancellationToken) is not INamedTypeSymbol classSymbol)
                        continue;

                    if (!HasAttribute(classSymbol, remoteConfigAttr))
                        continue;

                    var entries = CollectEntries(classDecl, semanticModel, keyAttr, entryOpen, context);
                    if (entries.Count == 0)
                        continue;

                    var source = BuildSource(classSymbol, entries);
                    var hint = $"{HintPrefix(classSymbol)}.RemoteConfig.g.cs";
                    context.AddSource(hint, SourceText.From(source, Encoding.UTF8));
                }
            }
        }

        private static List<EntryInfo> CollectEntries(
            ClassDeclarationSyntax classDecl,
            SemanticModel semanticModel,
            INamedTypeSymbol keyAttr,
            INamedTypeSymbol entryOpen,
            GeneratorExecutionContext context)
        {
            var list = new List<EntryInfo>();
            foreach (var methodDecl in classDecl.Members.OfType<MethodDeclarationSyntax>())
            {
                if (!methodDecl.Modifiers.Any(SyntaxKind.StaticKeyword) ||
                    !methodDecl.Modifiers.Any(SyntaxKind.PartialKeyword))
                    continue;

                if (semanticModel.GetDeclaredSymbol(methodDecl, context.CancellationToken) is not IMethodSymbol methodSymbol)
                    continue;

                string? key = null;
                foreach (var a in methodSymbol.GetAttributes())
                {
                    if (!SymbolEqualityComparer.Default.Equals(a.AttributeClass, keyAttr))
                        continue;
                    if (a.ConstructorArguments.Length > 0 && a.ConstructorArguments[0].Value is string s)
                        key = s;
                    break;
                }

                if (string.IsNullOrWhiteSpace(key))
                    continue;

                if (methodSymbol.ReturnType is not INamedTypeSymbol ret ||
                    !SymbolEqualityComparer.Default.Equals(ret.OriginalDefinition, entryOpen) ||
                    ret.TypeArguments.Length != 1)
                {
                    var loc = methodDecl.Identifier.GetLocation();
                    context.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor(
                            "TSRC001",
                            "RemoteConfig 반환 형식 오류",
                            "메서드 '{0}'은 RemoteConfigEntry<T>를 반환하는 partial static 메서드여야 합니다.",
                            "Truesoft.Supabase",
                            DiagnosticSeverity.Error,
                            true),
                        loc,
                        methodSymbol.Name));
                    continue;
                }

                var typeArg = ret.TypeArguments[0]!;
                list.Add(new EntryInfo(methodSymbol.Name, key.Trim(), typeArg));
            }

            return list;
        }

        private static string HintPrefix(INamedTypeSymbol classSymbol)
        {
            if (classSymbol.ContainingNamespace.IsGlobalNamespace)
                return classSymbol.Name;
            return classSymbol.ContainingNamespace.ToDisplayString().Replace('.', '_') + "_" + classSymbol.Name;
        }

        private static string BuildSource(INamedTypeSymbol classSymbol, List<EntryInfo> entries)
        {
            var ns = classSymbol.ContainingNamespace.IsGlobalNamespace
                ? null
                : classSymbol.ContainingNamespace.ToDisplayString();

            var sb = new StringBuilder();
            sb.AppendLine("// <auto-generated />");
            sb.AppendLine("#nullable enable");
            if (!string.IsNullOrEmpty(ns))
            {
                sb.AppendLine($"namespace {ns}");
                sb.AppendLine("{");
            }

            var indent = string.IsNullOrEmpty(ns) ? "" : "    ";
            sb.AppendLine($"{indent}partial class {classSymbol.Name}");
            sb.AppendLine($"{indent}{{");

            for (var i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                var typeName = e.TypeArg.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var field = $"_truesoftRemoteConfig_{Sanitize(e.MethodName)}_{i}";
                var escapedKey = e.Key.Replace("\\", "\\\\").Replace("\"", "\\\"");
                sb.AppendLine(
                    $"{indent}    private static readonly global::Truesoft.Supabase.Unity.RemoteConfig.RemoteConfigEntry<{typeName}> {field} = new global::Truesoft.Supabase.Unity.RemoteConfig.RemoteConfigEntry<{typeName}>(\"{escapedKey}\");");
                sb.AppendLine(
                    $"{indent}    public static partial global::Truesoft.Supabase.Unity.RemoteConfig.RemoteConfigEntry<{typeName}> {e.MethodName}() => {field};");
            }

            sb.AppendLine($"{indent}}}");

            if (!string.IsNullOrEmpty(ns))
                sb.AppendLine("}");

            return sb.ToString();
        }

        private static string Sanitize(string name)
        {
            var sb = new StringBuilder(name.Length);
            foreach (var c in name)
            {
                if (char.IsLetterOrDigit(c))
                    sb.Append(c);
                else
                    sb.Append('_');
            }

            return sb.Length > 0 ? sb.ToString() : "M";
        }

        private static bool HasAttribute(INamedTypeSymbol symbol, INamedTypeSymbol attrClass)
        {
            foreach (var a in symbol.GetAttributes())
            {
                if (SymbolEqualityComparer.Default.Equals(a.AttributeClass, attrClass))
                    return true;
            }

            return false;
        }

        private readonly struct EntryInfo
        {
            public readonly string MethodName;
            public readonly string Key;
            public readonly ITypeSymbol TypeArg;

            public EntryInfo(string methodName, string key, ITypeSymbol typeArg)
            {
                MethodName = methodName;
                Key = key;
                TypeArg = typeArg;
            }
        }
    }
}
