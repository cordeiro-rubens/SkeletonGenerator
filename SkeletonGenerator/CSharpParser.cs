﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SkeletonGenerator.Domain;

namespace SkeletonGenerator
{
    internal class CSharpParser
    {
        public static SourceModel Parse(FileInfo info)
        {
            var content = File.ReadAllText(info.FullName);
            var tree = CSharpSyntaxTree.ParseText(content);
            var root = tree.GetRoot();

            var classes = GetClasses(root);
            var interfaces = GetInterfaces(root);
            var enums = GetEnums(root);

            var source = new SourceModel
            {
                Path = info.FullName,
                Classes = classes,
                Interfaces = interfaces,
                Enums = enums
            };
            return source;
        }

        private static List<ClassModel> GetClasses(SyntaxNode root)
        {
            var allClasses = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
            var result = new List<ClassModel>();

            foreach (var _class in allClasses)
            {
                var properties = GetProperties(_class);
                var methods = GetMethods(_class);
                var constructors  = GetConstructorsModel(_class);
                var modifier = GetModifier(_class.Modifiers);
                var classModel = new ClassModel
                {
                    Name = _class.Identifier.ValueText,
                    Properties = properties,
                    Methods = methods,
                    Constructors = constructors,
                    Modifier = modifier,
                    Language = Language.CSharp
                };
                result.Add(classModel);
            }
            return result;
        }

        public static List<InterfaceModel> GetInterfaces(SyntaxNode root)
        {

            var allInterfaces = root.DescendantNodes().OfType<InterfaceDeclarationSyntax>();
            var result = new List<InterfaceModel>();

            foreach (var _interface in allInterfaces)
            {
                var properties = GetProperties(_interface);
                var methods = GetMethods(_interface);
                var modifier = GetModifier(_interface.Modifiers);
                var interfaceModel = new InterfaceModel
                {
                    Name = _interface.Identifier.ValueText,
                    Properties = properties,
                    Methods = methods,
                    Modifier = modifier,
                    Language = Language.CSharp
                };
                result.Add(interfaceModel);
            }
            return result;
        }

        private static List<PropertyModel> GetProperties(SyntaxNode node)
        {
            var properties = node.DescendantNodes().OfType<PropertyDeclarationSyntax>()
                                                   .Where(x => x.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword) || m.IsKind(SyntaxKind.InternalKeyword) || m.IsKind(SyntaxKind.ProtectedKeyword)));

            var result = new List<PropertyModel>();
            foreach (var property in properties)
            {
                var modifier = GetModifier(property.Modifiers);
                var propertyModel = new PropertyModel
                {
                    Name = property.Identifier.ValueText,
                    Type = property.Type.ToString(),
                    Modifier = modifier
                };
                result.Add(propertyModel);
            }
            return result;
        }
         
        private static List<MethodModel> GetMethods(SyntaxNode node)
        {

            var methods = node.DescendantNodes().OfType<MethodDeclarationSyntax>()
                                                .Where(x => x.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)));

            var result = new List<MethodModel>();
            var parameters = GetParameters(node);
            foreach (var method in methods)
            {
                var modifier = GetModifier(method.Modifiers);
                var methodModel = new MethodModel
                {
                    Name = method.Identifier.ValueText,
                    ReturnType = method.ReturnType.ToString(),
                    Parameters = parameters,
                    Modifier = modifier
                };
                result.Add(methodModel);
            }
            return result;
        }

        private static List<ConstructorModel> GetConstructorsModel(ClassDeclarationSyntax classDeclaration)
        {

            var constructors = classDeclaration.DescendantNodes().OfType<ConstructorDeclarationSyntax>()
                                               .Where(x => x.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)));

            var result = new List<ConstructorModel>();

            foreach (var constructor in constructors)
            {
                var parameters = GetParameters(constructor);
                var modifier = GetModifier(constructor.Modifiers);
                var constructorModel = new ConstructorModel
                {
                    Name = constructor.Identifier.ValueText,
                    Parameters = parameters,
                    Modifier = modifier
                };
                result.Add(constructorModel);
            }
            return result;
        }

        private static List<ParameterModel> GetParameters(SyntaxNode node)
        {
            var parameters = node.DescendantNodes().OfType<ParameterSyntax>();
            var result = new List<ParameterModel>();
            foreach (var parameter in parameters)
            {
                var parameterModel = new ParameterModel
                {
                    Name = parameter.Identifier.ValueText,
                    Type = parameter.Type?.ToString() ?? "any"
                };
                result.Add(parameterModel);
            }
            return result;
        }

        private static List<EnumModel> GetEnums(SyntaxNode root)
        {
            var allEnums = root.DescendantNodes().OfType<EnumDeclarationSyntax>();
            var result = new List<EnumModel>();

            foreach (var _enum in allEnums)
            {
                var modifier = GetModifier(_enum.Modifiers);
                var enumModel = new EnumModel
                {
                    Name = _enum.Identifier.ValueText,
                    Values = GetEnumValues(_enum),
                    Modifier = modifier,
                    Language = Language.CSharp
                };
                result.Add(enumModel);
            }
            return result;
        }

        private static List<EnumValueModel> GetEnumValues(EnumDeclarationSyntax enumDeclaration)
        {
            var members = enumDeclaration.DescendantNodes().OfType<EnumMemberDeclarationSyntax>();
            var result = new List<EnumValueModel>();
            foreach (var member in members)
            {

                var enumValueModel = new EnumValueModel
                {
                    Name = member.Identifier.ValueText,
                    Value = GetEnumValue(member)
                };
                result.Add(enumValueModel);
            }
            return result;
        }

        private static string GetEnumValue(EnumMemberDeclarationSyntax member)
        {
            var value = member.DescendantNodes().OfType<EqualsValueClauseSyntax>().FirstOrDefault();
            if (value == null)
            {
                return null ?? "no-value";
            }
            return value.DescendantNodes().OfType<LiteralExpressionSyntax>().
                                           FirstOrDefault()?.Token.ValueText ?? "no-value";
        }

        private static ModifierKind GetModifier(SyntaxTokenList modifiers)
        {

            if (modifiers.Any(x => x.IsKind(SyntaxKind.StaticKeyword)))
            {
                return ModifierKind.Static;
            }

            if (modifiers.Any(x => x.IsKind(SyntaxKind.PublicKeyword)))
            {
                return ModifierKind.Public;
            }

            if (modifiers.Any(x => x.IsKind(SyntaxKind.InternalKeyword)))
            {
                return ModifierKind.Internal;
            }

            if (modifiers.Any(x => x.IsKind(SyntaxKind.ProtectedKeyword)))
            {
                return ModifierKind.Protected;
            }
            return ModifierKind.Private;
        }
    }
    
}
