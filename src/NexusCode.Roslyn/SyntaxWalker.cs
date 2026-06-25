using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NexusCode.Domain;
using DomainSymbolKind = NexusCode.Domain.SymbolKind;
using DomainTypeKind = NexusCode.Domain.TypeKind;

namespace NexusCode.Roslyn;

internal sealed class SyntaxWalker : CSharpSyntaxWalker
{
    private readonly SemanticModel _semanticModel;
    private readonly string _filePath;
    private readonly Dictionary<string, SymbolEntity> _symbolMap = new();
    private readonly Dictionary<string, Guid> _typeIdMap = new();

    public List<SymbolEntity> Symbols { get; } = [];
    public List<ReferenceEntity> References { get; } = [];
    public List<GraphNodeEntity> GraphNodes { get; } = [];
    public List<GraphEdgeEntity> GraphEdges { get; } = [];

    public SyntaxWalker(SemanticModel semanticModel, string filePath)
    {
        _semanticModel = semanticModel;
        _filePath = filePath;
    }

    public override void VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
    {
        var symbol = _semanticModel.GetDeclaredSymbol(node);
        if (symbol != null)
        {
            var entity = CreateSymbolFromNamedType(symbol, DomainSymbolKind.Namespace);
            AddSymbol(entity);
        }
        base.VisitNamespaceDeclaration(node);
    }

    public override void VisitFileScopedNamespaceDeclaration(FileScopedNamespaceDeclarationSyntax node)
    {
        var symbol = _semanticModel.GetDeclaredSymbol(node);
        if (symbol != null)
        {
            var entity = CreateSymbolFromNamedType(symbol, DomainSymbolKind.Namespace);
            AddSymbol(entity);
        }
        base.VisitFileScopedNamespaceDeclaration(node);
    }

    public override void VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        var symbol = _semanticModel.GetDeclaredSymbol(node);
        if (symbol != null)
        {
            var entity = CreateTypeSymbol(symbol, DomainTypeKind.Class);
            AddSymbol(entity);
            AddTypeNode(entity, symbol);
            AddTypeEdges(entity, symbol);
        }
        base.VisitClassDeclaration(node);
    }

    public override void VisitStructDeclaration(StructDeclarationSyntax node)
    {
        var symbol = _semanticModel.GetDeclaredSymbol(node);
        if (symbol != null)
        {
            var entity = CreateTypeSymbol(symbol, DomainTypeKind.Struct);
            AddSymbol(entity);
            AddTypeNode(entity, symbol);
            AddTypeEdges(entity, symbol);
        }
        base.VisitStructDeclaration(node);
    }

    public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
    {
        var symbol = _semanticModel.GetDeclaredSymbol(node);
        if (symbol != null)
        {
            var entity = CreateTypeSymbol(symbol, DomainTypeKind.Interface);
            AddSymbol(entity);
            AddTypeNode(entity, symbol);
            AddTypeEdges(entity, symbol);
        }
        base.VisitInterfaceDeclaration(node);
    }

    public override void VisitEnumDeclaration(EnumDeclarationSyntax node)
    {
        var symbol = _semanticModel.GetDeclaredSymbol(node);
        if (symbol != null)
        {
            var entity = CreateTypeSymbol(symbol, DomainTypeKind.Enum);
            AddSymbol(entity);
            AddTypeNode(entity, symbol);
        }
        base.VisitEnumDeclaration(node);
    }

    public override void VisitRecordDeclaration(RecordDeclarationSyntax node)
    {
        var symbol = _semanticModel.GetDeclaredSymbol(node);
        if (symbol != null)
        {
            var entity = CreateTypeSymbol(symbol, DomainTypeKind.Record);
            AddSymbol(entity);
            AddTypeNode(entity, symbol);
            AddTypeEdges(entity, symbol);
        }
        base.VisitRecordDeclaration(node);
    }

    public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        var symbol = _semanticModel.GetDeclaredSymbol(node);
        if (symbol != null)
        {
            var entity = CreateMethodSymbol(symbol);
            AddSymbol(entity);
            AddMethodNode(entity, symbol);
            AddMethodEdges(entity, symbol);
        }
        base.VisitMethodDeclaration(node);
    }

    public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
    {
        var symbol = _semanticModel.GetDeclaredSymbol(node);
        if (symbol != null)
        {
            var entity = CreatePropertySymbol(symbol);
            AddSymbol(entity);
            AddPropertyNode(entity, symbol);
        }
        base.VisitPropertyDeclaration(node);
    }

    public override void VisitFieldDeclaration(FieldDeclarationSyntax node)
    {
        foreach (var variable in node.Declaration.Variables)
        {
            var variableSymbol = _semanticModel.GetDeclaredSymbol(variable) as IFieldSymbol;
            if (variableSymbol != null)
            {
                var entity = CreateFieldSymbol(variableSymbol);
                AddSymbol(entity);
                AddFieldNode(entity, variableSymbol);
            }
        }
        base.VisitFieldDeclaration(node);
    }

    public override void VisitEventDeclaration(EventDeclarationSyntax node)
    {
        var symbol = _semanticModel.GetDeclaredSymbol(node);
        if (symbol != null)
        {
            var entity = CreateEventSymbol(symbol);
            AddSymbol(entity);
        }
        base.VisitEventDeclaration(node);
    }

    public override void VisitInvocationExpression(InvocationExpressionSyntax node)
    {
        var symbolInfo = _semanticModel.GetSymbolInfo(node);
        if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
        {
            var containingSymbol = FindContainingSymbol(node);
            if (containingSymbol != null)
            {
                var sourceId = GraphNodeId.FromFullName(containingSymbol.FullName);
                var targetId = GraphNodeId.FromFullName(methodSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));

                GraphEdges.Add(new GraphEdgeEntity
                {
                    Id = ComputeEdgeId(sourceId.Hash, targetId.Hash, EdgeKind.Calls),
                    SourceId = sourceId.Hash,
                    TargetId = targetId.Hash,
                    Kind = EdgeKind.Calls
                });

                References.Add(new ReferenceEntity
                {
                    SymbolId = GetSymbolId(methodSymbol),
                    SourceFileId = Guid.Empty,
                    SourceSymbolId = containingSymbol.Id,
                    Line = node.GetLocation().GetLineSpan().StartLinePosition.Line,
                    Column = node.GetLocation().GetLineSpan().StartLinePosition.Character,
                    Kind = ReferenceKind.Call
                });
            }
        }
        base.VisitInvocationExpression(node);
    }

    public override void VisitIdentifierName(IdentifierNameSyntax node)
    {
        var symbolInfo = _semanticModel.GetSymbolInfo(node);
        if (symbolInfo.Symbol != null && symbolInfo.Symbol.Kind != Microsoft.CodeAnalysis.SymbolKind.Local)
        {
            var containingSymbol = FindContainingSymbol(node);
            if (containingSymbol != null)
            {
                References.Add(new ReferenceEntity
                {
                    SymbolId = GetSymbolId(symbolInfo.Symbol),
                    SourceFileId = Guid.Empty,
                    SourceSymbolId = containingSymbol.Id,
                    Line = node.GetLocation().GetLineSpan().StartLinePosition.Line,
                    Column = node.GetLocation().GetLineSpan().StartLinePosition.Character,
                    Kind = DetermineReferenceKind(symbolInfo.Symbol)
                });
            }
        }
        base.VisitIdentifierName(node);
    }

    private SymbolEntity CreateTypeSymbol(INamedTypeSymbol symbol, DomainTypeKind typeKind)
    {
        var location = symbol.Locations.FirstOrDefault()?.GetLineSpan();
        var fullName = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        var entity = new SymbolEntity
        {
            Id = GetSymbolId(symbol),
            Name = symbol.Name,
            FullName = fullName,
            Kind = DomainSymbolKind.Type,
            TypeName = typeKind.ToString(),
            FilePath = _filePath,
            StartLine = location?.StartLinePosition.Line ?? 0,
            EndLine = location?.EndLinePosition.Line ?? 0,
            AccessModifier = MapAccessModifier(symbol.DeclaredAccessibility),
            IsStatic = symbol.IsStatic,
            IsAbstract = symbol.IsAbstract,
            IsSealed = symbol.IsSealed,
            IsGeneric = symbol.IsGenericType,
            ContainerId = symbol.ContainingType != null ? GetSymbolId(symbol.ContainingType) : null
        };

        entity.Metadata["TypeKind"] = typeKind.ToString();
        entity.Metadata["Namespace"] = symbol.ContainingNamespace?.ToDisplayString() ?? "";

        if (symbol.BaseType != null)
            entity.Metadata["BaseType"] = symbol.BaseType.ToDisplayString();

        if (symbol.Interfaces.Length > 0)
            entity.Metadata["Interfaces"] = string.Join(",", symbol.Interfaces.Select(i => i.ToDisplayString()));

        return entity;
    }

    private SymbolEntity CreateMethodSymbol(IMethodSymbol symbol)
    {
        var location = symbol.Locations.FirstOrDefault()?.GetLineSpan();
        var fullName = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        var entity = new SymbolEntity
        {
            Id = GetSymbolId(symbol),
            Name = symbol.Name,
            FullName = fullName,
            Kind = DomainSymbolKind.Method,
            TypeName = symbol.ReturnType.ToDisplayString(),
            FilePath = _filePath,
            StartLine = location?.StartLinePosition.Line ?? 0,
            EndLine = location?.EndLinePosition.Line ?? 0,
            AccessModifier = MapAccessModifier(symbol.DeclaredAccessibility),
            IsStatic = symbol.IsStatic,
            IsAbstract = symbol.IsAbstract,
            IsVirtual = symbol.IsVirtual,
            IsOverride = symbol.IsOverride,
            IsAsync = symbol.IsAsync,
            IsGeneric = symbol.IsGenericMethod,
            ReturnType = symbol.ReturnType.ToDisplayString(),
            DeclaringTypeId = symbol.ContainingType != null ? GetSymbolId(symbol.ContainingType) : null,
            Parameters = symbol.Parameters.Select(p => new ParameterEntity
            {
                Name = p.Name,
                TypeName = p.Type.ToDisplayString(),
                HasDefault = p.HasExplicitDefaultValue,
                DefaultValue = p.HasExplicitDefaultValue ? p.ExplicitDefaultValue?.ToString() : null,
                IsParams = p.IsParams,
                IsThis = p.IsThis,
                IsRef = p.RefKind == RefKind.Ref,
                IsOut = p.RefKind == RefKind.Out,
                IsIn = p.RefKind == RefKind.In
            }).ToList()
        };

        entity.Metadata["MethodKind"] = symbol.MethodKind.ToString();
        entity.Metadata["ReturnType"] = symbol.ReturnType.ToDisplayString();
        entity.Metadata["ParameterCount"] = symbol.Parameters.Length.ToString();

        if (symbol.OverriddenMethod != null)
            entity.Metadata["OverriddenMethod"] = symbol.OverriddenMethod.ToDisplayString();

        return entity;
    }

    private SymbolEntity CreatePropertySymbol(IPropertySymbol symbol)
    {
        var location = symbol.Locations.FirstOrDefault()?.GetLineSpan();
        var fullName = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        var entity = new SymbolEntity
        {
            Id = GetSymbolId(symbol),
            Name = symbol.Name,
            FullName = fullName,
            Kind = DomainSymbolKind.Property,
            TypeName = symbol.Type.ToDisplayString(),
            FilePath = _filePath,
            StartLine = location?.StartLinePosition.Line ?? 0,
            EndLine = location?.EndLinePosition.Line ?? 0,
            AccessModifier = MapAccessModifier(symbol.DeclaredAccessibility),
            IsStatic = symbol.IsStatic,
            IsVirtual = symbol.IsVirtual,
            IsOverride = symbol.IsOverride,
            DeclaringTypeId = symbol.ContainingType != null ? GetSymbolId(symbol.ContainingType) : null
        };

        entity.Metadata["Type"] = symbol.Type.ToDisplayString();
        entity.Metadata["IsReadOnly"] = symbol.IsReadOnly.ToString();
        entity.Metadata["IsWriteOnly"] = (symbol.GetMethod == null && symbol.SetMethod != null).ToString();

        return entity;
    }

    private SymbolEntity CreateFieldSymbol(IFieldSymbol symbol)
    {
        var location = symbol.Locations.FirstOrDefault()?.GetLineSpan();
        var fullName = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        var entity = new SymbolEntity
        {
            Id = GetSymbolId(symbol),
            Name = symbol.Name,
            FullName = fullName,
            Kind = DomainSymbolKind.Field,
            TypeName = symbol.Type.ToDisplayString(),
            FilePath = _filePath,
            StartLine = location?.StartLinePosition.Line ?? 0,
            EndLine = location?.EndLinePosition.Line ?? 0,
            AccessModifier = MapAccessModifier(symbol.DeclaredAccessibility),
            IsStatic = symbol.IsStatic,
            IsAbstract = symbol.IsAbstract,
            IsSealed = symbol.IsReadOnly,
            DeclaringTypeId = symbol.ContainingType != null ? GetSymbolId(symbol.ContainingType) : null
        };

        entity.Metadata["Type"] = symbol.Type.ToDisplayString();
        entity.Metadata["IsReadOnly"] = symbol.IsReadOnly.ToString();
        entity.Metadata["IsConst"] = symbol.IsConst.ToString();
        entity.Metadata["IsVolatile"] = symbol.IsVolatile.ToString();

        if (symbol.HasConstantValue)
            entity.Metadata["ConstantValue"] = symbol.ConstantValue?.ToString() ?? "";

        return entity;
    }

    private SymbolEntity CreateEventSymbol(IEventSymbol symbol)
    {
        var location = symbol.Locations.FirstOrDefault()?.GetLineSpan();
        var fullName = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        var entity = new SymbolEntity
        {
            Id = GetSymbolId(symbol),
            Name = symbol.Name,
            FullName = fullName,
            Kind = DomainSymbolKind.Event,
            TypeName = symbol.Type.ToDisplayString(),
            FilePath = _filePath,
            StartLine = location?.StartLinePosition.Line ?? 0,
            EndLine = location?.EndLinePosition.Line ?? 0,
            AccessModifier = MapAccessModifier(symbol.DeclaredAccessibility),
            IsStatic = symbol.IsStatic,
            IsVirtual = symbol.IsVirtual,
            IsOverride = symbol.IsOverride,
            DeclaringTypeId = symbol.ContainingType != null ? GetSymbolId(symbol.ContainingType) : null
        };

        entity.Metadata["HandlerType"] = symbol.Type.ToDisplayString();

        return entity;
    }

    private SymbolEntity CreateSymbolFromNamedType(INamespaceOrTypeSymbol symbol, DomainSymbolKind kind)
    {
        var location = symbol.Locations.FirstOrDefault()?.GetLineSpan();
        var fullName = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        return new SymbolEntity
        {
            Id = GetSymbolId(symbol),
            Name = symbol.Name,
            FullName = fullName,
            Kind = kind,
            FilePath = _filePath,
            StartLine = location?.StartLinePosition.Line ?? 0,
            EndLine = location?.EndLinePosition.Line ?? 0
        };
    }

    private void AddSymbol(SymbolEntity entity)
    {
        Symbols.Add(entity);
        _symbolMap[entity.FullName] = entity;
    }

    private void AddTypeNode(SymbolEntity entity, INamedTypeSymbol symbol)
    {
        var nodeKind = entity.TypeName switch
        {
            "Class" => NodeKind.Class,
            "Struct" => NodeKind.Struct,
            "Interface" => NodeKind.Interface,
            "Enum" => NodeKind.Enum,
            "Record" => NodeKind.Record,
            _ => NodeKind.Class
        };

        GraphNodes.Add(new GraphNodeEntity
        {
            Id = entity.Id.ToByteArray(),
            FullName = entity.FullName,
            Label = entity.Name,
            Kind = nodeKind,
            Metadata = new Dictionary<string, string>(entity.Metadata)
        });

        _typeIdMap[entity.FullName] = entity.Id;
    }

    private void AddTypeEdges(SymbolEntity entity, INamedTypeSymbol symbol)
    {
        var sourceId = entity.Id.ToByteArray();

        if (symbol.BaseType != null && symbol.BaseType.SpecialType != SpecialType.System_Object)
        {
            var targetId = GetSymbolId(symbol.BaseType).ToByteArray();
            GraphEdges.Add(new GraphEdgeEntity
            {
                Id = ComputeEdgeId(sourceId, targetId, EdgeKind.Inherits),
                SourceId = sourceId,
                TargetId = targetId,
                Kind = EdgeKind.Inherits
            });
        }

        foreach (var iface in symbol.AllInterfaces)
        {
            var targetId = GetSymbolId(iface).ToByteArray();
            GraphEdges.Add(new GraphEdgeEntity
            {
                Id = ComputeEdgeId(sourceId, targetId, EdgeKind.Implements),
                SourceId = sourceId,
                TargetId = targetId,
                Kind = EdgeKind.Implements
            });
        }
    }

    private void AddMethodNode(SymbolEntity entity, IMethodSymbol symbol)
    {
        GraphNodes.Add(new GraphNodeEntity
        {
            Id = entity.Id.ToByteArray(),
            FullName = entity.FullName,
            Label = entity.Name,
            Kind = NodeKind.Method,
            Metadata = new Dictionary<string, string>(entity.Metadata)
        });

        if (symbol.ContainingType != null)
        {
            var typeId = GetSymbolId(symbol.ContainingType).ToByteArray();
            GraphEdges.Add(new GraphEdgeEntity
            {
                Id = ComputeEdgeId(typeId, entity.Id.ToByteArray(), EdgeKind.Declares),
                SourceId = typeId,
                TargetId = entity.Id.ToByteArray(),
                Kind = EdgeKind.Declares
            });
        }
    }

    private void AddMethodEdges(SymbolEntity entity, IMethodSymbol symbol)
    {
        if (symbol.OverriddenMethod != null)
        {
            var targetId = GetSymbolId(symbol.OverriddenMethod).ToByteArray();
            GraphEdges.Add(new GraphEdgeEntity
            {
                Id = ComputeEdgeId(entity.Id.ToByteArray(), targetId, EdgeKind.Overrides),
                SourceId = entity.Id.ToByteArray(),
                TargetId = targetId,
                Kind = EdgeKind.Overrides
            });
        }
    }

    private void AddPropertyNode(SymbolEntity entity, IPropertySymbol symbol)
    {
        GraphNodes.Add(new GraphNodeEntity
        {
            Id = entity.Id.ToByteArray(),
            FullName = entity.FullName,
            Label = entity.Name,
            Kind = NodeKind.Property,
            Metadata = new Dictionary<string, string>(entity.Metadata)
        });

        if (symbol.ContainingType != null)
        {
            var typeId = GetSymbolId(symbol.ContainingType).ToByteArray();
            GraphEdges.Add(new GraphEdgeEntity
            {
                Id = ComputeEdgeId(typeId, entity.Id.ToByteArray(), EdgeKind.Declares),
                SourceId = typeId,
                TargetId = entity.Id.ToByteArray(),
                Kind = EdgeKind.Declares
            });
        }
    }

    private void AddFieldNode(SymbolEntity entity, IFieldSymbol symbol)
    {
        GraphNodes.Add(new GraphNodeEntity
        {
            Id = entity.Id.ToByteArray(),
            FullName = entity.FullName,
            Label = entity.Name,
            Kind = NodeKind.Field,
            Metadata = new Dictionary<string, string>(entity.Metadata)
        });

        if (symbol.ContainingType != null)
        {
            var typeId = GetSymbolId(symbol.ContainingType).ToByteArray();
            GraphEdges.Add(new GraphEdgeEntity
            {
                Id = ComputeEdgeId(typeId, entity.Id.ToByteArray(), EdgeKind.Declares),
                SourceId = typeId,
                TargetId = entity.Id.ToByteArray(),
                Kind = EdgeKind.Declares
            });
        }
    }

    private SymbolEntity? FindContainingSymbol(SyntaxNode node)
    {
        var current = node.Parent;
        while (current != null)
        {
            if (current is MemberDeclarationSyntax member)
            {
                var symbol = _semanticModel.GetDeclaredSymbol(member);
                if (symbol != null)
                {
                    var fullName = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    if (_symbolMap.TryGetValue(fullName, out var entity))
                        return entity;
                }
            }
            current = current.Parent;
        }
        return null;
    }

    private Guid GetSymbolId(ISymbol symbol)
    {
        var fullName = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        return CreateDeterministicGuid(fullName);
    }

    private static Guid CreateDeterministicGuid(string input)
    {
        var hash = System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(input));
        return new Guid(hash[..16]);
    }

    private static byte[] ComputeEdgeId(byte[] sourceId, byte[] targetId, EdgeKind kind)
    {
        var combined = new byte[sourceId.Length + targetId.Length + 4];
        sourceId.CopyTo(combined, 0);
        targetId.CopyTo(combined, sourceId.Length);
        BitConverter.GetBytes((int)kind).CopyTo(combined, sourceId.Length + targetId.Length);
        return System.Security.Cryptography.MD5.HashData(combined);
    }

    private static AccessModifier MapAccessModifier(Accessibility accessibility)
    {
        return accessibility switch
        {
            Accessibility.Public => AccessModifier.Public,
            Accessibility.Internal => AccessModifier.Internal,
            Accessibility.Protected => AccessModifier.Protected,
            Accessibility.Private => AccessModifier.Private,
            Accessibility.ProtectedOrInternal => AccessModifier.ProtectedInternal,
            Accessibility.ProtectedAndInternal => AccessModifier.PrivateProtected,
            _ => AccessModifier.NotApplicable
        };
    }

    private static ReferenceKind DetermineReferenceKind(ISymbol symbol)
    {
        return symbol.Kind switch
        {
            Microsoft.CodeAnalysis.SymbolKind.Method => ReferenceKind.Call,
            Microsoft.CodeAnalysis.SymbolKind.Property => ReferenceKind.Read,
            Microsoft.CodeAnalysis.SymbolKind.Field => ReferenceKind.Read,
            Microsoft.CodeAnalysis.SymbolKind.Event => ReferenceKind.Read,
            Microsoft.CodeAnalysis.SymbolKind.NamedType => ReferenceKind.Type,
            _ => ReferenceKind.Read
        };
    }
}
