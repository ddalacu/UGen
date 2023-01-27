using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace UGen;

public static class Constants
{
    public static string AttributeName => "UGen.Runtime.GetComponentAttribute";
}

[Generator]
public class LazyGetSourceGenerator : ISourceGenerator
{
    public void Execute(GeneratorExecutionContext context)
    {
        //#if DEBUG
        //        if (!Debugger.IsAttached)
        //        {
        //            Debugger.Launch();
        //        }
        //#endif

        if (!(context.SyntaxContextReceiver is SyntaxReceiver receiver))
            return;

        var attributeSymbol = context.Compilation.GetTypeByMetadataName(Constants.AttributeName);

        foreach (var group in receiver.Fields
                     .GroupBy<IFieldSymbol, INamedTypeSymbol>(f => f.ContainingType,
                         SymbolEqualityComparer.Default))
        {
            var key = group.Key;

            var method = GenerateMethod(group, attributeSymbol);

            var cls = GenerateClass(key.Name, method);


            var attributes = new List<AttributeSyntax>();

            foreach (var fieldSymbol in group)
            {
                if (IsRequired(fieldSymbol) == false)
                    continue;

                var fieldType = fieldSymbol.Type;

                var typeIdentifierName = IdentifierName(fieldType.ToDisplayString());

                var identifierNameSyntax = IdentifierName("UnityEngine.RequireComponent");

                var attribute = Attribute(
                        identifierNameSyntax)
                    .WithArgumentList(
                        AttributeArgumentList(
                            SingletonSeparatedList<AttributeArgumentSyntax>(
                                AttributeArgument(
                                    TypeOfExpression(typeIdentifierName)))));
                attributes.Add(attribute);
            }

            foreach (var attribute in attributes)
            {
                cls = cls.AddAttributeLists(new AttributeListSyntax[]
                {
                    AttributeList(SingletonSeparatedList<AttributeSyntax>(attribute))
                });
            }


            var unitSyntax = GenerateCode(key.ContainingNamespace?.Name, cls);

            var fileName = $"{key.Name}_InitializeComponents.g.cs";
            context.AddSource(fileName, unitSyntax.ToFullString());
        }
    }

    private List<(int, string)> GetEnumValues(ITypeSymbol symbol)
    {
        if (symbol is not INamedTypeSymbol namedTypeSymbol)
            throw new ArgumentException("Symbol is not an enum", nameof(symbol));

        if (namedTypeSymbol.TypeKind != TypeKind.Enum)
            throw new ArgumentException("Symbol is not an enum", nameof(symbol));

        var values = new List<(int, string)>();

        foreach (var member in symbol.GetMembers().OfType<IFieldSymbol>())
        {
            if (member.HasConstantValue == false)
                continue;

            values.Add(((int)member.ConstantValue!, member.Name));
        }

        return values;
    }

    private string GetEnumName(ITypeSymbol symbol, int value)
    {
        if (symbol is not INamedTypeSymbol namedTypeSymbol)
            throw new ArgumentException("Symbol is not an enum", nameof(symbol));

        if (namedTypeSymbol.TypeKind != TypeKind.Enum)
            throw new ArgumentException("Symbol is not an enum", nameof(symbol));

        foreach (var member in symbol.GetMembers().OfType<IFieldSymbol>())
        {
            if (member.HasConstantValue == false)
                continue;

            if ((int)member.ConstantValue == value)
                return member.Name;
        }

        return null;
    }

    private string GetMethod(IFieldSymbol fieldSymbol, ISymbol attributeSymbol)
    {
        var attributeData = fieldSymbol.GetAttributes().Single(ad =>
            ad.AttributeClass.Equals(attributeSymbol, SymbolEqualityComparer.Default));

        if (attributeData.ConstructorArguments.Length == 0)
            return "GetComponent";

        var firstArg = attributeData.ConstructorArguments[0];

        var val = GetEnumName(firstArg.Type, (int)firstArg.Value);

        switch (val)
        {
            case "This":
                return "GetComponent";
            case "Parent":
                return "GetComponentInParent";
            case "Child":
                return "GetComponentInChildren";
        }

        throw new NotImplementedException();
    }

    private MemberDeclarationSyntax GenerateMethod(IEnumerable<IFieldSymbol> fields,
        ISymbol attributeSymbol)
    {
        var statementList = new List<StatementSyntax>();

        foreach (var fieldSymbol in fields)
        {
            var fieldType = fieldSymbol.Type;

            var typeIdentifierName = IdentifierName(fieldType.ToDisplayString());

            var methodName = GetMethod(fieldSymbol, attributeSymbol);

            var getComponent = InvocationExpression(
                    IdentifierName(methodName))
                .WithArgumentList(
                    ArgumentList(
                        SingletonSeparatedList<ArgumentSyntax>(
                            Argument(
                                TypeOfExpression(
                                    typeIdentifierName)))));

            var cast = BinaryExpression(SyntaxKind.AsExpression,
                getComponent,
                typeIdentifierName);

            var expression = ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(fieldSymbol.Name),
                    cast));

            statementList.Add(expression);
            statementList.Add(NullCheck(fieldSymbol));
        }

        return MethodDeclaration(
                PredefinedType(
                    Token(SyntaxKind.VoidKeyword)),
                Identifier("InitializeComponent"))
            .WithModifiers(
                TokenList(
                    Token(SyntaxKind.ProtectedKeyword)))
            .WithBody(
                Block(statementList));
    }

    private static IfStatementSyntax NullCheck(IFieldSymbol fieldSymbol)
    {
        var message = $"Could not get component of type {fieldSymbol.Type.ToDisplayString()}";
        
        return IfStatement(
            BinaryExpression(
                SyntaxKind.EqualsExpression,
                IdentifierName(fieldSymbol.Name),
                LiteralExpression(
                    SyntaxKind.NullLiteralExpression)),
            ExpressionStatement(
                InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName("UnityEngine"),
                                IdentifierName("Debug")),
                            IdentifierName("LogError")))
                    .WithArgumentList(
                        ArgumentList(
                            SeparatedList<ArgumentSyntax>(
                                new SyntaxNodeOrToken[]{
                                    Argument(
                                        LiteralExpression(
                                            SyntaxKind.StringLiteralExpression,
                                            Literal(message))),
                                    Token(SyntaxKind.CommaToken),
                                    Argument(
                                        ThisExpression())})))));
    }

    private static MemberDeclarationSyntax GenerateClass(string className, MemberDeclarationSyntax members)
    {

        return ClassDeclaration(className)
            .WithModifiers(
                TokenList(
                    new[]
                    {
                        Token(SyntaxKind.PartialKeyword)
                    }))
            .WithMembers(SingletonList<MemberDeclarationSyntax>(members));
    }

    private static CompilationUnitSyntax GenerateCode(string ns, MemberDeclarationSyntax member)
    {
        if (string.IsNullOrWhiteSpace(ns) == false)
        {
            member = NamespaceDeclaration(IdentifierName(ns)).AddMembers(member);
        }


        member = member.NormalizeWhitespace();

        return CompilationUnit()
            .WithMembers(
                SingletonList<MemberDeclarationSyntax>(member));
    }

    private static ClassDeclarationSyntax GenerateClass(string className, List<MemberDeclarationSyntax> member)
    {
        return ClassDeclaration(className)
            .WithModifiers(
                TokenList(
                    new[]
                    {
                        //Token(SyntaxKind.ProtectedKeyword),
                        Token(SyntaxKind.PartialKeyword)
                    }))
            .WithMembers(new SyntaxList<MemberDeclarationSyntax>(member));
    }

    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
    }

    private static bool IsRequired(IFieldSymbol field)
    {
        var attributes = field.GetAttributes();

        var attribute =
            attributes.FirstOrDefault(ad => ad.AttributeClass.ToDisplayString() == Constants.AttributeName);


        if (attribute == null)
            return false;


        if (attribute.ConstructorArguments.Length > 0)
        {
            var get = attribute.ConstructorArguments[1];

            if ((bool)get.Value == true)
            {
                return true;
            }
        }

        return false;
    }
}

internal class SyntaxReceiver : ISyntaxContextReceiver
{
    public List<IFieldSymbol> Fields { get; } = new List<IFieldSymbol>();


    public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
    {
        var fieldDeclarationSyntax = context.Node as FieldDeclarationSyntax;

        if (fieldDeclarationSyntax == null)
            return;

        if (fieldDeclarationSyntax.AttributeLists.Count == 0)
            return;

        var typeKind = context.SemanticModel.GetTypeInfo(fieldDeclarationSyntax.Declaration.Type).Type;

        foreach (VariableDeclaratorSyntax variable in fieldDeclarationSyntax.Declaration.Variables)
        {
            IFieldSymbol fieldSymbol =
                ModelExtensions.GetDeclaredSymbol(context.SemanticModel, variable) as IFieldSymbol;

            if (fieldSymbol == null)
                continue;

            var container = fieldSymbol.ContainingType.BaseType;

            if (IsDerivedFrom(container, ComponentType) == false)
                continue;

            var fieldBase = fieldSymbol.Type;

            if (IsDerivedFrom(fieldBase, ComponentType) == false &&
                typeKind.TypeKind != TypeKind.Interface)
                continue;

            var attributes = fieldSymbol.GetAttributes();

            var isMarked =
                attributes.Any(ad => ad.AttributeClass.ToDisplayString() == Constants.AttributeName);

            if (isMarked == false)
                continue;

            Fields.Add(fieldSymbol);
        }
    }

    public const string ComponentType = "UnityEngine.Component";


    private bool IsDerivedFrom(ITypeSymbol baseType, string targetType)
    {
        while (baseType != null)
        {
            var baseTypeName = baseType.ToDisplayString();

            if (baseTypeName == targetType)
                return true;

            baseType = baseType.BaseType;
        }

        return false;
    }
}