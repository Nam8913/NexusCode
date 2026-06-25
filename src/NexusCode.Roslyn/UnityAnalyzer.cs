using Microsoft.CodeAnalysis;
using NexusCode.Domain;

namespace NexusCode.Roslyn;

public sealed class UnityAnalyzer
{
    private static readonly HashSet<string> MonoBehaviourMethods =
    [
        "Awake", "Start", "Update", "FixedUpdate", "LateUpdate",
        "OnEnable", "OnDisable", "OnDestroy",
        "OnCollisionEnter", "OnCollisionExit", "OnCollisionStay",
        "OnTriggerEnter", "OnTriggerExit", "OnTriggerStay",
        "OnMouseDown", "OnMouseUp", "OnMouseDrag",
        "OnGUI", "OnApplicationQuit", "OnApplicationPause", "OnApplicationFocus"
    ];

    public UnityTypeInfo AnalyzeType(INamedTypeSymbol symbol)
    {
        var result = new UnityTypeInfo
        {
            TypeName = symbol.Name,
            FullName = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            IsMonoBehaviour = InheritsFrom(symbol, "UnityEngine.MonoBehaviour"),
            IsScriptableObject = InheritsFrom(symbol, "UnityEngine.ScriptableObject"),
            IsEditor = InheritsFrom(symbol, "UnityEditor.Editor"),
            IsEditorWindow = InheritsFrom(symbol, "UnityEditor.EditorWindow")
        };

        if (result.IsMonoBehaviour || result.IsScriptableObject)
        {
            result.SerializedFields = FindSerializedFields(symbol);
            result.LifecycleMethods = FindLifecycleMethods(symbol);
            result.RequiredComponents = GetRequiredComponents(symbol);
            result.ComponentMenu = GetComponentMenu(symbol);
        }

        return result;
    }

    public bool InheritsFrom(INamedTypeSymbol symbol, string baseTypeName)
    {
        var current = symbol.BaseType;
        while (current != null)
        {
            if (current.ToDisplayString() == baseTypeName)
                return true;
            current = current.BaseType;
        }
        return false;
    }

    private List<SerializedFieldInfo> FindSerializedFields(INamedTypeSymbol symbol)
    {
        var fields = new List<SerializedFieldInfo>();

        foreach (var field in symbol.GetMembers().OfType<IFieldSymbol>())
        {
            var hasSerializeField = HasAttribute(field, "UnityEngine.SerializeField");
            var hasNonSerialized = HasAttribute(field, "UnityEngine.NonSerialized");
            var hasHideInInspector = HasAttribute(field, "UnityEngine.HideInInspector");

            bool isSerialized;
            if (hasNonSerialized)
                isSerialized = false;
            else if (field.DeclaredAccessibility == Accessibility.Public)
                isSerialized = true;
            else
                isSerialized = hasSerializeField;

            if (isSerialized)
            {
                fields.Add(new SerializedFieldInfo
                {
                    Name = field.Name,
                    TypeName = field.Type.ToDisplayString(),
                    HasSerializeField = hasSerializeField,
                    HasHideInInspector = hasHideInInspector,
                    IsPrivate = field.DeclaredAccessibility == Accessibility.Private
                });
            }
        }

        return fields;
    }

    private List<string> FindLifecycleMethods(INamedTypeSymbol symbol)
    {
        return symbol.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(m => MonoBehaviourMethods.Contains(m.Name))
            .Select(m => m.Name)
            .ToList();
    }

    private List<string> GetRequiredComponents(INamedTypeSymbol symbol)
    {
        var required = new List<string>();

        foreach (var attr in symbol.GetAttributes())
        {
            if (attr.AttributeClass?.Name == "RequireComponent" && attr.ConstructorArguments.Length > 0)
            {
                var arg = attr.ConstructorArguments[0];
                if (arg.Value is INamedTypeSymbol type)
                {
                    required.Add(type.ToDisplayString());
                }
            }
        }

        return required;
    }

    private string? GetComponentMenu(INamedTypeSymbol symbol)
    {
        foreach (var attr in symbol.GetAttributes())
        {
            if (attr.AttributeClass?.Name == "AddComponentMenu" && attr.ConstructorArguments.Length > 0)
            {
                return attr.ConstructorArguments[0].Value?.ToString();
            }
        }
        return null;
    }

    private bool HasAttribute(ISymbol symbol, string attributeName)
    {
        return symbol.GetAttributes().Any(a =>
            a.AttributeClass?.ToDisplayString() == attributeName);
    }
}

public class UnityTypeInfo
{
    public string TypeName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public bool IsMonoBehaviour { get; set; }
    public bool IsScriptableObject { get; set; }
    public bool IsEditor { get; set; }
    public bool IsEditorWindow { get; set; }
    public List<SerializedFieldInfo> SerializedFields { get; set; } = [];
    public List<string> LifecycleMethods { get; set; } = [];
    public List<string> RequiredComponents { get; set; } = [];
    public string? ComponentMenu { get; set; }
}

public class SerializedFieldInfo
{
    public string Name { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
    public bool HasSerializeField { get; set; }
    public bool HasHideInInspector { get; set; }
    public bool IsPrivate { get; set; }
}
