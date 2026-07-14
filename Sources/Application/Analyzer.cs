using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Mmu.Mlra.ReferenceBuddy
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class Analyzer : DiagnosticAnalyzer
    {
        private const string DiagnosticId = "ROL001";
        private static readonly DiagnosticDescriptor _rule = new DiagnosticDescriptor(
            DiagnosticId,
            "Unused MediatR Request",
            "Request '{0}' is only used in unit tests and/or handlers and is otherwise unused",
            "Usage",
            DiagnosticSeverity.Warning,
            true,
            null,
            null,
            WellKnownDiagnosticTags.CompilationEnd);

        private static readonly ImmutableHashSet<string> _testAttributeNames =
            ImmutableHashSet.Create(StringComparer.OrdinalIgnoreCase, "Fact", "Theory", "Test", "TestMethod", "TestClass", "TestFixture");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(compilationContext =>
            {
                var iRequest = compilationContext.Compilation.GetTypeByMetadataName("MediatR.IRequest");
                var iRequestOfT = compilationContext.Compilation.GetTypeByMetadataName("MediatR.IRequest`1");
                var iRequestHandler = compilationContext.Compilation.GetTypeByMetadataName("MediatR.IRequestHandler`1");
                var iRequestHandlerOfT = compilationContext.Compilation.GetTypeByMetadataName("MediatR.IRequestHandler`2");

                var trackedRequests = new ConcurrentDictionary<INamedTypeSymbol, RequestUsageData>(SymbolEqualityComparer.Default);

                compilationContext.RegisterSymbolAction(symbolContext =>
                {
                    var typeSymbol = (INamedTypeSymbol)symbolContext.Symbol;

                    if (!IsRequestType(typeSymbol, iRequest, iRequestOfT) && !HasRequestBaseTypeSyntax(typeSymbol))
                    {
                        return;
                    }

                    trackedRequests.TryAdd(typeSymbol, new RequestUsageData(typeSymbol));
                }, SymbolKind.NamedType);

                compilationContext.RegisterSyntaxNodeAction(syntaxContext =>
                {
                    var classDeclaration = (ClassDeclarationSyntax)syntaxContext.Node;
                    var classSymbol = syntaxContext.SemanticModel.GetDeclaredSymbol(classDeclaration, syntaxContext.CancellationToken);

                    if (classSymbol == null)
                    {
                        return;
                    }

                    if (!IsRequestType(classSymbol, iRequest, iRequestOfT) && !HasRequestBaseTypeSyntax(classDeclaration))
                    {
                        return;
                    }

                    trackedRequests.TryAdd(classSymbol, new RequestUsageData(classSymbol));
                }, SyntaxKind.ClassDeclaration);

                compilationContext.RegisterSyntaxNodeAction(syntaxContext =>
                    {
                        var typeSyntax = (TypeSyntax)syntaxContext.Node;
                        var typeSymbol = ResolveType(typeSyntax, syntaxContext.SemanticModel, syntaxContext.CancellationToken);

                        if (!IsRequestType(typeSymbol, iRequest, iRequestOfT) && !HasRequestBaseTypeSyntax(typeSymbol))
                        {
                            return;
                        }

                        var requestUsage = trackedRequests.GetOrAdd(typeSymbol, _ => new RequestUsageData(typeSymbol));

                        if (IsWithinRequest(typeSyntax, typeSymbol, syntaxContext.SemanticModel, syntaxContext.CancellationToken))
                        {
                            return;
                        }

                        var ownerSymbol = syntaxContext.SemanticModel.GetEnclosingSymbol(typeSyntax.SpanStart, syntaxContext.CancellationToken);

                        if (ownerSymbol != null)
                        {
                            requestUsage.UsageOwners.Add(ownerSymbol);
                        }
                    },
                    SyntaxKind.IdentifierName,
                    SyntaxKind.GenericName,
                    SyntaxKind.QualifiedName,
                    SyntaxKind.AliasQualifiedName);

                compilationContext.RegisterCompilationEndAction(endContext =>
                {
                    foreach (var requestUsage in trackedRequests.Values)
                    {
                        if (requestUsage.RequestType.IsGenericType)
                        {
                            continue;
                        }

                        if (!IsUsedOnlyInTestsOrHandlers(requestUsage, iRequestHandler, iRequestHandlerOfT))
                        {
                            continue;
                        }

                        var location = requestUsage.RequestType.Locations.FirstOrDefault(loc => loc.IsInSource);

                        if (location == null)
                        {
                            continue;
                        }

                        endContext.ReportDiagnostic(Diagnostic.Create(_rule, location, requestUsage.RequestType.Name));
                    }
                });
            });
        }

        private static bool HasRequestBaseTypeSyntax(ClassDeclarationSyntax classDeclaration)
        {
            if (classDeclaration.BaseList == null)
            {
                return false;
            }

            foreach (var baseType in classDeclaration.BaseList.Types)
            {
                if (IsRequestTypeSyntax(baseType.Type))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasRequestBaseTypeSyntax(INamedTypeSymbol typeSymbol)
        {
            if (typeSymbol == null)
            {
                return false;
            }

            foreach (var syntaxReference in typeSymbol.DeclaringSyntaxReferences)
            {
                if (syntaxReference.GetSyntax() is ClassDeclarationSyntax classDeclaration && HasRequestBaseTypeSyntax(classDeclaration))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsAssociatedHandlerType(
            INamedTypeSymbol handlerType,
            INamedTypeSymbol requestType,
            INamedTypeSymbol iRequestHandler,
            INamedTypeSymbol iRequestHandlerOfT)
        {
            foreach (var implementedInterface in handlerType.AllInterfaces)
            {
                if (!IsRequestHandlerInterface(implementedInterface, iRequestHandler, iRequestHandlerOfT))
                {
                    continue;
                }

                if (implementedInterface.TypeArguments.Length < 1)
                {
                    continue;
                }

                if (implementedInterface.TypeArguments[0] is INamedTypeSymbol handledRequest &&
                    SymbolEqualityComparer.Default.Equals(handledRequest, requestType))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsInsideHandler(
            ISymbol symbol,
            INamedTypeSymbol requestType,
            INamedTypeSymbol iRequestHandler,
            INamedTypeSymbol iRequestHandlerOfT)
        {
            var current = symbol;

            while (current != null)
            {
                if (current is INamedTypeSymbol namedType &&
                    IsAssociatedHandlerType(namedType, requestType, iRequestHandler, iRequestHandlerOfT))
                {
                    return true;
                }

                current = current.ContainingSymbol;
            }

            return false;
        }

        private static bool IsInsideUnitTest(ISymbol symbol)
        {
            var current = symbol;

            while (current != null)
            {
                if (IsLikelyUnitTestSymbol(current))
                {
                    return true;
                }

                current = current.ContainingSymbol;
            }

            return false;
        }

        private static bool IsLikelyUnitTestSymbol(ISymbol symbol)
        {
            if (symbol is IAssemblySymbol assemblySymbol &&
                (assemblySymbol.Name.EndsWith("Tests", StringComparison.OrdinalIgnoreCase) ||
                 assemblySymbol.Name.EndsWith("Test", StringComparison.OrdinalIgnoreCase) ||
                 assemblySymbol.Name.EndsWith("UnitTests", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            if (symbol.Name.EndsWith("Tests", StringComparison.OrdinalIgnoreCase) ||
                symbol.Name.EndsWith("Test", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            foreach (var attribute in symbol.GetAttributes())
            {
                var attributeName = attribute.AttributeClass?.Name;

                if (attributeName == null)
                {
                    continue;
                }

                if (attributeName.EndsWith("Attribute", StringComparison.OrdinalIgnoreCase))
                {
                    attributeName = attributeName.Substring(0, attributeName.Length - "Attribute".Length);
                }

                if (_testAttributeNames.Contains(attributeName))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsMediatRInterface(INamedTypeSymbol symbol, string interfaceName, int arity)
        {
            return symbol != null &&
                   symbol.TypeKind == TypeKind.Interface &&
                   symbol.Arity == arity &&
                   symbol.Name == interfaceName &&
                   symbol.ContainingNamespace?.ToDisplayString() == "MediatR";
        }

        private static bool IsRequestHandlerInterface(
            INamedTypeSymbol implementedInterface,
            INamedTypeSymbol iRequestHandler,
            INamedTypeSymbol iRequestHandlerOfT)
        {
            return SymbolEqualityComparer.Default.Equals(implementedInterface.OriginalDefinition, iRequestHandler) ||
                   SymbolEqualityComparer.Default.Equals(implementedInterface.OriginalDefinition, iRequestHandlerOfT) ||
                   IsMediatRInterface(implementedInterface, "IRequestHandler", 1) ||
                   IsMediatRInterface(implementedInterface, "IRequestHandler", 2) ||
                   IsMediatRInterface(implementedInterface.OriginalDefinition, "IRequestHandler", 1) ||
                   IsMediatRInterface(implementedInterface.OriginalDefinition, "IRequestHandler", 2);
        }

        private static bool IsRequestType(INamedTypeSymbol typeSymbol, INamedTypeSymbol iRequest, INamedTypeSymbol iRequestOfT)
        {
            if (typeSymbol == null || typeSymbol.TypeKind != TypeKind.Class)
            {
                return false;
            }

            foreach (var implementedInterface in typeSymbol.AllInterfaces)
            {
                if (SymbolEqualityComparer.Default.Equals(implementedInterface, iRequest) ||
                    SymbolEqualityComparer.Default.Equals(implementedInterface.OriginalDefinition, iRequestOfT) ||
                    IsMediatRInterface(implementedInterface, "IRequest", 0) ||
                    IsMediatRInterface(implementedInterface, "IRequest", 1) ||
                    IsMediatRInterface(implementedInterface.OriginalDefinition, "IRequest", 1))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsRequestTypeSyntax(TypeSyntax typeSyntax)
        {
            if (typeSyntax is IdentifierNameSyntax identifier)
            {
                return identifier.Identifier.ValueText == "IRequest";
            }

            if (typeSyntax is GenericNameSyntax genericName)
            {
                return genericName.Identifier.ValueText == "IRequest";
            }

            if (typeSyntax is QualifiedNameSyntax qualifiedName)
            {
                return IsRequestTypeSyntax(qualifiedName.Right);
            }

            if (typeSyntax is AliasQualifiedNameSyntax aliasQualifiedName)
            {
                return IsRequestTypeSyntax(aliasQualifiedName.Name);
            }

            return false;
        }

        private static bool IsUsedOnlyInTestsOrHandlers(
            RequestUsageData requestUsage,
            INamedTypeSymbol iRequestHandler,
            INamedTypeSymbol iRequestHandlerOfT)
        {
            if (requestUsage.UsageOwners.IsEmpty)
            {
                return true;
            }

            foreach (var usageOwner in requestUsage.UsageOwners)
            {
                if (usageOwner == null)
                {
                    continue;
                }

                if (SymbolEqualityComparer.Default.Equals(usageOwner, requestUsage.RequestType) ||
                    SymbolEqualityComparer.Default.Equals(usageOwner.ContainingType, requestUsage.RequestType))
                {
                    continue;
                }

                if (IsInsideUnitTest(usageOwner))
                {
                    continue;
                }

                if (IsInsideHandler(usageOwner, requestUsage.RequestType, iRequestHandler, iRequestHandlerOfT))
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        private static bool IsWithinRequest(
            TypeSyntax typeSyntax,
            INamedTypeSymbol requestType,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            var typeDeclaration = typeSyntax.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();

            if (typeDeclaration == null)
            {
                return false;
            }

            var declarationSymbol = semanticModel.GetDeclaredSymbol(typeDeclaration, cancellationToken);

            return declarationSymbol != null && SymbolEqualityComparer.Default.Equals(declarationSymbol, requestType);
        }

        private static INamedTypeSymbol ResolveType(
            TypeSyntax typeSyntax,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            if (semanticModel.GetSymbolInfo(typeSyntax, cancellationToken).Symbol is INamedTypeSymbol symbol)
            {
                return symbol;
            }

            return semanticModel.GetTypeInfo(typeSyntax, cancellationToken).Type as INamedTypeSymbol;
        }

        private sealed class RequestUsageData
        {
            public INamedTypeSymbol RequestType { get; }
            public ConcurrentBag<ISymbol> UsageOwners { get; }

            public RequestUsageData(INamedTypeSymbol requestType)
            {
                RequestType = requestType;
                UsageOwners = new ConcurrentBag<ISymbol>();
            }
        }
    }
}