using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LightRules.Generator
{
    [Generator]
    public class RuleAdapterGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var compilation = context.Compilation;
            // find the RuleAttribute symbol
            // Try to resolve attribute symbols; if not available, the generator will fall back to name-based matching.
            var ruleAttribute = compilation.GetTypeByMetadataName("LightRules.Attributes.RuleAttribute");
            var conditionAttr = compilation.GetTypeByMetadataName("LightRules.Attributes.ConditionAttribute");
            var actionAttr = compilation.GetTypeByMetadataName("LightRules.Attributes.ActionAttribute");
            var factAttr = compilation.GetTypeByMetadataName("LightRules.Attributes.FactAttribute");
            var priorityAttr = compilation.GetTypeByMetadataName("LightRules.Attributes.PriorityAttribute");

            var registryEntries = new List<string>();

            // iterate all syntax trees and find classes with [Rule]
            foreach (var syntaxTree in compilation.SyntaxTrees)
            {
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var root = syntaxTree.GetRoot();
                var classDecls = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
                foreach (var cls in classDecls)
                {
                    var symbol = semanticModel.GetDeclaredSymbol(cls) as INamedTypeSymbol;
                    if (symbol == null) continue;
                    var attrs = symbol.GetAttributes();
                    // match rule attribute either by resolved symbol or by name
                    bool hasRuleAttr = attrs.Any(a => (a.AttributeClass != null && (a.AttributeClass.Name == "RuleAttribute" || a.AttributeClass.Name == "Rule")) || (ruleAttribute != null && SymbolEqualityComparer.Default.Equals(a.AttributeClass, ruleAttribute)));
                    if (!hasRuleAttr) continue;

                    // generate adapter source
                    string generated = null;
                    try
                    {
                        generated = GenerateAdapterFor(symbol, conditionAttr, actionAttr, factAttr, priorityAttr, context);
                        if (string.IsNullOrWhiteSpace(generated))
                        {
                            // nothing to generate for this type (e.g., no condition found)
                            continue;
                        }
                        var fileName = (symbol.ContainingNamespace.IsGlobalNamespace ? "" : symbol.ContainingNamespace.ToDisplayString() + ".") + symbol.Name + "_RuleAdapter.g.cs";
                        // replace invalid chars for file name
                        fileName = fileName.Replace('<', '_').Replace('>', '_').Replace('`', '_').Replace(' ', '_');
                        context.AddSource(fileName, generated);
                    }
                    catch (Exception ex)
                    {
                        var diag = Diagnostic.Create(new DiagnosticDescriptor("LRGGEN001", "Generator error", $"RuleAdapterGenerator failed for '{symbol.Name}': {ex.Message}", "LightRules.Generator", DiagnosticSeverity.Error, true), Location.None);
                        context.ReportDiagnostic(diag);
                    }

                    // produce a RuleMetadata entry using the generated adapter type
                    var adapterFullName = (symbol.ContainingNamespace.IsGlobalNamespace ? "" : symbol.ContainingNamespace.ToDisplayString() + ".") + symbol.Name + "_RuleAdapter";
                    // determine name/description/priority for registry (reuse logic from generator)
                    var ruleAttr = symbol.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name == "RuleAttribute");
                    string nameLiteral = null;
                    string descriptionLiteral = null;
                    int priorityLiteral = int.MinValue;
                    if (ruleAttr != null)
                    {
                        foreach (var na in ruleAttr.NamedArguments)
                        {
                            if (na.Key == "Name" && na.Value.Value is string s && !string.IsNullOrEmpty(s)) nameLiteral = s;
                            if (na.Key == "Description" && na.Value.Value is string d && !string.IsNullOrEmpty(d)) descriptionLiteral = d;
                            if (na.Key == "Priority" && na.Value.Value is int p) priorityLiteral = p;
                        }
                        if (nameLiteral == null && ruleAttr.ConstructorArguments.Length > 0 && ruleAttr.ConstructorArguments[0].Value is string cs && !string.IsNullOrEmpty(cs)) nameLiteral = cs;
                        if (descriptionLiteral == null && ruleAttr.ConstructorArguments.Length > 1 && ruleAttr.ConstructorArguments[1].Value is string cds && !string.IsNullOrEmpty(cds)) descriptionLiteral = cds;
                        if (priorityLiteral == int.MinValue && ruleAttr.ConstructorArguments.Length > 2 && ruleAttr.ConstructorArguments[2].Value is int cp) priorityLiteral = cp;
                    }
                    var nameExpr = nameLiteral != null ? "\"" + nameLiteral.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"" : "null";
                    var descExpr = descriptionLiteral != null ? "\"" + descriptionLiteral.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"" : "null";
                    var prioExpr = priorityLiteral != int.MinValue ? priorityLiteral.ToString() : "IRule.DefaultPriority";
                    registryEntries.Add($"new LightRules.Discovery.RuleMetadata(typeof(global::{adapterFullName}), {nameExpr}, {descExpr}, {prioExpr}, true, new string[0])");
                }
            }

            // generate registry
            var registrySource = GenerateRegistrySource(registryEntries);
            context.AddSource("RuleRegistry.g.cs", registrySource);
        }

        private string GenerateAdapterFor(INamedTypeSymbol symbol, INamedTypeSymbol? conditionAttr, INamedTypeSymbol? actionAttr, INamedTypeSymbol? factAttr, INamedTypeSymbol? priorityAttr, GeneratorExecutionContext context)
        {
            var ns = symbol.ContainingNamespace.IsGlobalNamespace ? "LightRules.Generated" : symbol.ContainingNamespace.ToDisplayString();
            var typeName = symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
            var simpleTypeName = symbol.Name;
            var adapterName = simpleTypeName + "_RuleAdapter";

            // find condition method
            var methods = symbol.GetMembers().OfType<IMethodSymbol>().Where(m => m.MethodKind == MethodKind.Ordinary).ToList();
            // find condition method (match by symbol when available, else by attribute name)
            var condition = methods.FirstOrDefault(m => m.GetAttributes().Any(a => (conditionAttr != null && SymbolEqualityComparer.Default.Equals(a.AttributeClass, conditionAttr)) || (a.AttributeClass != null && (a.AttributeClass.Name == "ConditionAttribute" || a.AttributeClass.Name == "Condition"))));
            if (condition == null)
            {
                // generator cannot produce adapter without condition
                return string.Empty;
            }

            var actionMethods = methods.Where(m => m.GetAttributes().Any(a => (actionAttr != null && SymbolEqualityComparer.Default.Equals(a.AttributeClass, actionAttr)) || (a.AttributeClass != null && (a.AttributeClass.Name == "ActionAttribute" || a.AttributeClass.Name == "Action")))).ToList();
            // order by ActionAttribute.Order where possible
            var orderedActions = actionMethods.Select(m => new
            {
                Method = m,
                Order = GetActionOrder(m)
            }).OrderBy(x => x.Order).Select(x => x.Method).ToList();

            var priorityMethod = methods.FirstOrDefault(m => m.GetAttributes().Any(a => (priorityAttr != null && SymbolEqualityComparer.Default.Equals(a.AttributeClass, priorityAttr)) || (a.AttributeClass != null && (a.AttributeClass.Name == "PriorityAttribute" || a.AttributeClass.Name == "Priority"))));

            // extract RuleAttribute name/description/priority if present
            var ruleAttr = symbol.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name == "RuleAttribute");
            string nameLiteral = null;
            string descriptionLiteral = null;
            int? attributePriority = null;
            if (ruleAttr != null)
            {
                // named arguments first
                foreach (var na in ruleAttr.NamedArguments)
                {
                    if (na.Key == "Name" && na.Value.Value is string s && !string.IsNullOrEmpty(s)) nameLiteral = s;
                    if (na.Key == "Description" && na.Value.Value is string d && !string.IsNullOrEmpty(d)) descriptionLiteral = d;
                    if (na.Key == "Priority" && na.Value.Value is int p) attributePriority = p;
                }
                // constructor args fallback (if any)
                if (nameLiteral == null && ruleAttr.ConstructorArguments.Length > 0 && ruleAttr.ConstructorArguments[0].Value is string cs && !string.IsNullOrEmpty(cs)) nameLiteral = cs;
                if (descriptionLiteral == null && ruleAttr.ConstructorArguments.Length > 1 && ruleAttr.ConstructorArguments[1].Value is string cds && !string.IsNullOrEmpty(cds)) descriptionLiteral = cds;
                if (attributePriority == null && ruleAttr.ConstructorArguments.Length > 2 && ruleAttr.ConstructorArguments[2].Value is int cp) attributePriority = cp;
            }

            string nameExpression = nameLiteral != null ? "\"" + nameLiteral.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"" : "_target.GetType().Name";
            string descriptionExpression = descriptionLiteral != null ? "\"" + descriptionLiteral.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"" : "_target.GetType().Name";
            string priorityExpression;
            if (priorityMethod != null)
            {
                priorityExpression = $"_target.{priorityMethod.Name}()";
            }
            else if (attributePriority != null)
            {
                priorityExpression = attributePriority.Value.ToString();
            }
            else
            {
                priorityExpression = "IRule.DefaultPriority";
            }

            // generate parameter binding code for a method
            string BuildParameterBindings(IMethodSymbol method)
            {
                var lines = new List<string>();
                for (int i = 0; i < method.Parameters.Length; i++)
                {
                    var p = method.Parameters[i];
                    var pName = p.Name ?? "arg" + i;
                    var hasFact = p.GetAttributes().Any(a => (factAttr != null && SymbolEqualityComparer.Default.Equals(a.AttributeClass, factAttr)) || (a.AttributeClass != null && (a.AttributeClass.Name == "FactAttribute" || a.AttributeClass.Name == "Fact")));
                    if (hasFact)
                    {
                        // find attribute value (match by symbol when possible, else by attribute name)
                        AttributeData attr = null;
                        foreach (var a in p.GetAttributes())
                        {
                            if (factAttr != null && SymbolEqualityComparer.Default.Equals(a.AttributeClass, factAttr)) { attr = a; break; }
                            if (a.AttributeClass != null && (a.AttributeClass.Name == "FactAttribute" || a.AttributeClass.Name == "Fact")) { attr = a; break; }
                        }
                        string factNameLiteral = pName;
                        if (attr != null && attr.ConstructorArguments.Length == 1 && attr.ConstructorArguments[0].Value is string sval && !string.IsNullOrEmpty(sval))
                        {
                            factNameLiteral = sval;
                        }

                        var paramType = p.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
                        // generate TryGetValue<T>("name", out var var)
                        lines.Add($"if (!facts.TryGetValue<{paramType}>(\"{factNameLiteral}\", out var {pName})) throw new global::LightRules.Core.NoSuchFactException(\"No fact named '{factNameLiteral}' found\", \"{factNameLiteral}\");");
                    }
                    else
                    {
                        // expect Facts
                        var paramType = p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        if (paramType != "LightRules.Core.Facts")
                        {
                            // cannot bind non-Facts non-FactAnnotated param
                            context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("LRG001", "Unsupported parameter", $"Parameter '{p.Name}' of method '{method.Name}' in '{symbol.Name}' is not annotated with [Fact] and is not Facts type.", "LightRules.Generator", DiagnosticSeverity.Warning, true), Location.None));
                        }
                    }
                }

                var pre = string.Join("\n            ", lines);
                return pre.Length > 0 ? pre + "\n            " : string.Empty;
            }

            string BuildArgumentList(IMethodSymbol method)
            {
                var argsList = method.Parameters.Select((p, idx) =>
                {
                    var has = p.GetAttributes().Any(a => (factAttr != null && SymbolEqualityComparer.Default.Equals(a.AttributeClass, factAttr)) || (a.AttributeClass != null && (a.AttributeClass.Name == "FactAttribute" || a.AttributeClass.Name == "Fact")));
                    return has ? (p.Name ?? "arg" + idx) : "facts";
                }).ToArray();
                return string.Join(", ", argsList);
            }

            // Build source
            var source = $"// <auto-generated/>\n#nullable enable\nusing System;\nusing LightRules.Core;\n\nnamespace {ns}\n{{\n    public sealed class {adapterName} : IRule\n    {{\n        private readonly {typeName} _target;\n        public {adapterName}({typeName} target) => _target = target ?? throw new ArgumentNullException(nameof(target));\n\n        public string Name => {nameExpression};\n        public string Description => {descriptionExpression};\n        public int Priority => {priorityExpression};\n\n        public bool Evaluate(Facts facts)\n        {{\n            // parameter binding\n            {BuildParameterBindings(condition)}\n            return _target.{condition.Name}({BuildArgumentList(condition)});\n        }}\n\n        public void Execute(Facts facts)\n        {{\n";

            foreach (var act in orderedActions)
            {
                var preAndArgs = BuildParameterBindings(act);
                var argList = BuildArgumentList(act);
                source += $"            {preAndArgs}\n            _target.{act.Name}({argList});\n";
            }

            // add CompareTo implementation
            source += "        }\n\n        public int CompareTo(IRule? other)\n        {\n            if (other == null) return 1;\n            if (Priority < other.Priority) return -1;\n            if (Priority > other.Priority) return 1;\n            return string.Compare(Name, other.Name, StringComparison.Ordinal);\n        }\n\n    }\n}\n";

            return source;
        }

        private int GetActionOrder(IMethodSymbol m)
        {
            var attr = m.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name == "ActionAttribute");
            if (attr == null) return 0;
            if (attr.ConstructorArguments.Length > 0 && attr.ConstructorArguments[0].Value is int i) return i;
            var named = attr.NamedArguments.FirstOrDefault(kv => kv.Key == "Order");
            if (named.Value.Value is int j) return j;
            return 0;
        }

        private string GenerateRegistrySource(List<string> entries)
        {
            var body = string.Join(",\n            ", entries);
            var source = $"// <auto-generated/>\nusing System;\nusing LightRules.Discovery;\n\nnamespace LightRules.Generated\n{{\n    public static class RuleRegistry\n    {{\n        public static readonly RuleMetadata[] All = new RuleMetadata[]\n        {{\n            {body}\n        }};\n    }}\n}}";
            return source;
        }
    }
}
