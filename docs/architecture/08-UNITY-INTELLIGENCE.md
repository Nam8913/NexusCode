# Nexus Code Intelligence Platform - Unity Intelligence Layer

## Overview

The Unity Intelligence Layer extends the core Roslyn Analysis Engine with Unity-specific analysis capabilities, understanding MonoBehaviour lifecycles, serialization, component dependencies, and Unity-specific patterns.

---

## 1. Architecture

```
┌─────────────────────────────────────────────────────────┐
│              Unity Intelligence Layer                     │
├─────────────────────────────────────────────────────────┤
│                                                          │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  │
│  │ MonoBehaviour  │  │ ScriptableObject│  │ Editor      │  │
│  │ Analyzer       │  │ Analyzer       │  │ Analyzer    │  │
│  └──────────────┘  └──────────────┘  └──────────────┘  │
│         ↓                ↓                  ↓            │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  │
│  │ Serialization │  │ Assembly Def  │  │ Unity Graph  │  │
│  │ Analyzer      │  │ Analyzer      │  │ Builder      │  │
│  └──────────────┘  └──────────────┘  └──────────────┘  │
│                          ↓                  ↓            │
│                   ┌──────────────┐  ┌──────────────┐    │
│                   │ Scene/Prefab │  │ Addressables  │    │
│                   │ Analyzer     │  │ Analyzer      │    │
│                   └──────────────┘  └──────────────┘    │
│                                                          │
└─────────────────────────────────────────────────────────┘
```

---

## 2. MonoBehaviour Analysis

### 2.1 Detection

```
class MonoBehaviourAnalyzer
{
    AnalyzeType(typeSymbol: INamedTypeSymbol):
        result = new UnityTypeInfo()
        
        // Check if inherits from MonoBehaviour
        if InheritsFrom(typeSymbol, "UnityEngine.MonoBehaviour"):
            result.IsMonoBehaviour = true
            result.ComponentName = GetComponentName(typeSymbol)
            result.MenuPath = GetMenuPath(typeSymbol)
            
            // Analyze lifecycle methods
            result.LifecycleMethods = AnalyzeLifecycleMethods(typeSymbol)
            
            // Analyze serialized fields
            result.SerializedFields = AnalyzeSerializedFields(typeSymbol)
            
            // Analyze Unity events
            result.UnityEvents = AnalyzeUnityEvents(typeSymbol)
            
            // Analyze coroutines
            result.Coroutines = AnalyzeCoroutines(typeSymbol)
            
            // Analyze RequireComponent
            result.RequiredComponents = GetRequiredComponents(typeSymbol)
            
            // Analyze HideInInspector
            result.HiddenFields = GetHiddenFields(typeSymbol)
        
        return result
    
    InheritsFrom(typeSymbol: INamedTypeSymbol, baseTypeName: string):
        current = typeSymbol.BaseType
        while current != null:
            if current.ToDisplayString() == baseTypeName:
                return true
            current = current.BaseType
        return false
    
    GetComponentName(typeSymbol: INamedTypeSymbol):
        // Check AddComponentMenu attribute
        attribute = typeSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "AddComponentMenu")
        
        if attribute != null:
            return attribute.ConstructorArguments[0].Value as string
        
        // Default: type name
        return typeSymbol.Name
    
    GetMenuPath(typeSymbol: INamedTypeSymbol):
        attribute = typeSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "AddComponentMenu")
        
        if attribute != null && attribute.ConstructorArguments.Length > 1:
            return attribute.ConstructorArguments[1].Value as int
        
        return 0
}
```

### 2.2 Lifecycle Methods

```
class UnityLifecycleAnalyzer
{
    static readonly string[] UnityMethods = [
        "Awake", "OnEnable", "Start", "FixedUpdate", 
        "Update", "LateUpdate", "OnDisable", "OnDestroy",
        "OnApplicationQuit", "OnApplicationPause", "OnApplicationFocus",
        "OnGUI", "OnRenderObject", "OnPostRender", "OnPreRender",
        "OnWillRenderObject", "OnBecameVisible", "OnBecameInvisible",
        "OnCollisionEnter", "OnCollisionStay", "OnCollisionExit",
        "OnTriggerEnter", "OnTriggerStay", "OnTriggerExit",
        "OnMouseDown", "OnMouseUp", "OnMouseDrag", "OnMouseEnter", "OnMouseExit",
        "OnParticleCollision", "OnParticleTrigger",
        "OnAnimatorIK", "OnStateMachineEnter", "OnStateMachineExit"
    ];
    
    AnalyzeLifecycleMethods(typeSymbol: INamedTypeSymbol):
        methods = new List<UnityMethodInfo>()
        
        foreach method in typeSymbol.GetMembers().OfType<IMethodSymbol>():
            if UnityMethods.Contains(method.Name):
                methods.Add(new UnityMethodInfo {
                    Name = method.Name,
                    IsOverride = method.IsOverride,
                    AccessModifier = method.DeclaredAccessibility,
                    IsAsync = method.IsAsync,
                    LineCount = CalculateLineCount(method),
                    CallsBase = CallsBaseMethod(method),
                    HasCoroutine = HasCoroutineReturnType(method),
                    SequenceNumber = GetExecutionOrder(method.Name)
                })
        
        return methods
    
    GetExecutionOrder(methodName: string):
        // Unity execution order
        return methodName switch {
            "Awake" => 0,
            "OnEnable" => 1,
            "Start" => 2,
            "FixedUpdate" => 3,
            "Update" => 4,
            "LateUpdate" => 5,
            "OnDisable" => 6,
            "OnDestroy" => 7,
            _ => 99
        }
    
    CallsBaseMethod(method: IMethodSymbol):
        // Check if method calls base.Awake(), base.Start(), etc.
        // This requires syntax analysis
        return false // placeholder - needs syntax tree inspection
    
    HasCoroutineReturnType(method: IMethodSymbol):
        return method.ReturnType.ToDisplayString() == "System.Collections.IEnumerator"
}
```

### 2.3 Serialized Fields

```
class UnitySerializationAnalyzer
{
    AnalyzeSerializedFields(typeSymbol: INamedTypeSymbol):
        fields = new List<SerializedFieldInfo>()
        
        foreach field in typeSymbol.GetMembers().OfType<IFieldSymbol>():
            // Check for [SerializeField]
            hasSerializeField = HasAttribute(field, "UnityEngine.SerializeField")
            
            // Check for [NonSerialized]
            hasNonSerialized = HasAttribute(field, "UnityEngine.NonSerialized")
            
            // Check for [HideInInspector]
            hasHideInInspector = HasAttribute(field, "UnityEngine.HideInInspector")
            
            // Check for [Header]
            headerAttr = GetAttribute(field, "UnityEngine.Header")
            
            // Check for [Tooltip]
            tooltipAttr = GetAttribute(field, "UnityEngine.Tooltip")
            
            // Check for [Range]
            rangeAttr = GetAttribute(field, "UnityEngine.Range")
            
            // Check for [Min]
            minAttr = GetAttribute(field, "UnityEngine.Min")
            
            // Check for [TextArea]
            textAreaAttr = GetAttribute(field, "UnityEngine.TextArea")
            
            // Determine if serialized
            isSerialized = DetermineIfSerialized(field, hasSerializeField, hasNonSerialized)
            
            if isSerialized:
                fields.Add(new SerializedFieldInfo {
                    Name = field.Name,
                    TypeName = field.Type.ToDisplayString(),
                    IsPrivate = field.DeclaredAccessibility == Accessibility.Private,
                    HasSerializeField = hasSerializeField,
                    HasHideInInspector = hasHideInInspector,
                    Header = headerAttr?.ConstructorArguments[0].Value as string,
                    Tooltip = tooltipAttr?.ConstructorArguments[0].Value as string,
                    RangeMin = rangeAttr?.ConstructorArguments[0].Value as double?,
                    RangeMax = rangeAttr?.ConstructorArguments[1].Value as double?,
                    DefaultValue = GetDefaultValue(field),
                    IsReadOnly = field.IsReadOnly,
                    IsConst = field.IsConst
                })
        
        return fields
    
    DetermineIfSerialized(field: IFieldSymbol, hasSerializeField: bool, hasNonSerialized: bool):
        // [NonSerialized] always means not serialized
        if hasNonSerialized:
            return false
        
        // Private fields need [SerializeField]
        if field.DeclaredAccessibility == Accessibility.Private:
            return hasSerializeField
        
        // Public fields are serialized by default
        if field.DeclaredAccessibility == Accessibility.Public:
            return true
        
        // Internal and protected need [SerializeField]
        return hasSerializeField
    
    GetDefaultValue(field: IFieldSymbol):
        if field.HasConstantValue:
            return field.ConstantValue?.ToString()
        
        // Check for field initializer in syntax tree
        // This requires syntax analysis
        return null
}
```

---

## 3. ScriptableObject Analysis

```
class ScriptableObjectAnalyzer
{
    AnalyzeType(typeSymbol: INamedTypeSymbol):
        result = new ScriptableObjectInfo()
        
        if InheritsFrom(typeSymbol, "UnityEngine.ScriptableObject"):
            result.IsScriptableObject = true
            
            // Check for [CreateAssetMenu]
            createMenu = GetAttribute(typeSymbol, "UnityEngine.CreateAssetMenu")
            if createMenu != null:
                result.HasCreateAssetMenu = true
                result.MenuName = createMenu.NamedArguments
                    .FirstOrDefault(a => a.Key == "menuName").Value.Value as string
                result.FileName = createMenu.NamedArguments
                    .FirstOrDefault(a => a.Key == "fileName").Value.Value as string
                result.Order = createMenu.NamedArguments
                    .FirstOrDefault(a => a.Key == "order").Value.Value as int?
            
            // Analyze serialized fields (same as MonoBehaviour)
            result.SerializedFields = AnalyzeSerializedFields(typeSymbol)
            
            // Analyze methods
            result.Methods = AnalyzeMethods(typeSymbol)
        
        return result
    
    InheritsFrom(typeSymbol: INamedTypeSymbol, baseTypeName: string):
        current = typeSymbol.BaseType
        while current != null:
            if current.ToDisplayString() == baseTypeName:
                return true
            current = current.BaseType
        return false
}
```

---

## 4. Assembly Definition Analysis

```
class AssemblyDefinitionAnalyzer
{
    AnalyzeAssemblyDefinitions(repositoryPath: string):
        asmdefFiles = Directory.GetFiles(repositoryPath, "*.asmdef", SearchOption.AllDirectories)
        
        definitions = new List<AssemblyDefinitionInfo>()
        
        foreach file in asmdefFiles:
            content = File.ReadAllText(file)
            asmdef = JsonSerializer.Deserialize<AsmDefJson>(content)
            
            definition = new AssemblyDefinitionInfo {
                Name = asmdef.name,
                RootNamespace = asmdef.rootNamespace,
                References = asmdef.references ?? [],
                IncludePlatforms = asmdef.includePlatforms ?? [],
                ExcludePlatforms = asmdef.excludePlatforms ?? [],
                AllowUnsafeCode = asmdef.allowUnsafeCode,
                OverrideReferences = asmdef.overrideReferences,
                PrecompiledReferences = asmdef.precompiledReferences ?? [],
                AutoReferenced = asmdef.autoReferenced,
                DefineConstraints = asmdef.defineConstraints ?? [],
                VersionDefines = asmdef.versionDefines ?? [],
                NoEngineReferences = asmdef.noEngineReferences,
                FilePath = file,
                Directory = Path.GetDirectoryName(file)
            }
            
            definitions.Add(definition)
        
        return definitions
    
    BuildAssemblyDependencyGraph(definitions: List<AssemblyDefinitionInfo>):
        graph = new GraphBuilder()
        
        foreach def in definitions:
            graph.AddNode(new GraphNode {
                Id = GraphNodeId.FromName(def.Name),
                Label = def.Name,
                Kind = NodeKind.Assembly,
                Metadata = new Dictionary<string, string> {
                    ["FilePath"] = def.FilePath,
                    ["References"] = string.Join(",", def.References)
                }
            })
        
        foreach def in definitions:
            foreach reference in def.References:
                graph.AddEdge(new GraphEdge {
                    SourceId = GraphNodeId.FromName(def.Name),
                    TargetId = GraphNodeId.FromName(reference),
                    Kind = EdgeKind.DEPENDS_ON
                })
        
        return graph
    }
}

class AsmDefJson
{
    public string name { get; set; }
    public string rootNamespace { get; set; }
    public List<string> references { get; set; }
    public List<string> includePlatforms { get; set; }
    public List<string> excludePlatforms { get; set; }
    public bool allowUnsafeCode { get; set; }
    public bool overrideReferences { get; set; }
    public List<string> precompiledReferences { get; set; }
    public bool autoReferenced { get; set; }
    public List<string> defineConstraints { get; set; }
    public List<VersionDefine> versionDefines { get; set; }
    public bool noEngineReferences { get; set; }
}

class VersionDefine
{
    public string name { get; set; }
    public string expression { get; set; }
    public string define { get; set; }
}
```

---

## 5. Unity Graph

### 5.1 Graph Structure

```csharp
class UnityGraph
{
    // Component dependency graph
    Dictionary<GraphNodeId, List<GraphNodeId>> _componentDependencies;
    
    // Scene hierarchy
    Dictionary<GraphNodeId, GraphNodeId> _parentChild;
    
    // Prefab connections
    Dictionary<GraphNodeId, List<GraphNodeId>> _prefabInstances;
    
    // Addressable references
    Dictionary<GraphNodeId, List<GraphNodeId>> _addressableRefs;
}
```

### 5.2 Unity-Specific Edges

| Edge Type | Source → Target | Description |
|-----------|-----------------|-------------|
| COMPONENT_OF | MonoBehaviour → GameObject | Component attached to GameObject |
| REQUIRES | MonoBehaviour → MonoBehaviour | RequireComponent dependency |
| REFERENCES | MonoBehaviour → ScriptableObject | ScriptableObject field reference |
| INSTANCE_OF | GameObject → Prefab | Prefab instance |
| CHILD_OF | GameObject → GameObject | Parent-child hierarchy |
| ADDRESSABLE_REF | Any → Asset | Addressable asset reference |
| UNITY_EVENT | MonoBehaviour → MonoBehaviour | UnityEvent connection |
| COROUTINE | MonoBehaviour → IEnumerator | Coroutine method |
| ASSEMBLY_DEP | Assembly → Assembly | Assembly definition dependency |

### 5.3 Graph Builder

```
class UnityGraphBuilder
{
    knowledgeGraph: KnowledgeGraph
    unityAnalyzer: UnityAnalyzer
    
    BuildUnityGraph(repositoryPath: string):
        // Analyze all MonoBehaviour types
        monoBehaviours = unityAnalyzer.FindAllMonoBehaviours(repositoryPath)
        
        foreach mb in monoBehaviours:
            // Add MonoBehaviour node
            AddMonoBehaviourNode(mb)
            
            // Add RequireComponent edges
            foreach required in mb.RequiredComponents:
                AddRequireComponentEdge(mb, required)
            
            // Add serialized field references
            foreach field in mb.SerializedFields:
                if IsUnityType(field.TypeName):
                    AddFieldReferenceEdge(mb, field)
        
        // Analyze all ScriptableObjects
        scriptableObjects = unityAnalyzer.FindAllScriptableObjects(repositoryPath)
        
        foreach so in scriptableObjects:
            AddScriptableObjectNode(so)
        
        // Analyze assembly definitions
        assemblies = unityAnalyzer.AnalyzeAssemblyDefinitions(repositoryPath)
        BuildAssemblyDependencyGraph(assemblies)
        
        // Analyze scenes and prefabs if available
        scenes = unityAnalyzer.FindScenes(repositoryPath)
        foreach scene in scenes:
            AnalyzeScene(scene)
        
        prefabs = unityAnalyzer.FindPrefabs(repositoryPath)
        foreach prefab in prefabs:
            AnalyzePrefab(prefab)
    
    AddMonoBehaviourNode(mb: UnityTypeInfo):
        node = new GraphNode {
            Id = GraphNodeId.FromFullName(mb.FullName),
            Label = mb.ComponentName,
            Kind = NodeKind.MonoBehaviour,
            FullName = mb.FullName,
            Metadata = new Dictionary<string, string> {
                ["IsMonoBehaviour"] = "true",
                ["LifecycleMethods"] = string.Join(",", mb.LifecycleMethods.Select(m => m.Name)),
                ["SerializedFieldCount"] = mb.SerializedFields.Count.ToString(),
                ["RequiredComponents"] = string.Join(",", mb.RequiredComponents)
            }
        }
        knowledgeGraph.AddNode(node)
    
    AddRequireComponentEdge(mb: UnityTypeInfo, requiredType: string):
        sourceId = GraphNodeId.FromFullName(mb.FullName)
        targetId = GraphNodeId.FromFullName(requiredType)
        
        // Ensure target node exists
        if !knowledgeGraph.HasNode(targetId):
            knowledgeGraph.AddNode(new GraphNode {
                Id = targetId,
                Label = requiredType.Split('.').Last(),
                Kind = NodeKind.Component,
                FullName = requiredType
            })
        
        knowledgeGraph.AddEdge(new GraphEdge {
            SourceId = sourceId,
            TargetId = targetId,
            Kind = EdgeKind.REQUIRES
        })
    
    AnalyzeScene(scenePath: string):
        // Parse Unity scene file (YAML format)
        content = File.ReadAllText(scenePath)
        
        // Extract GameObjects and Components
        gameObjects = ParseSceneYaml(content)
        
        foreach go in gameObjects:
            // Add GameObject node
            goNode = new GraphNode {
                Id = GraphNodeId.FromName($"{scenePath}:{go.Name}"),
                Label = go.Name,
                Kind = NodeKind.GameObject,
                Metadata = new Dictionary<string, string> {
                    ["Scene"] = scenePath,
                    ["Components"] = string.Join(",", go.Components.Select(c => c.TypeName))
                }
            }
            knowledgeGraph.AddNode(goNode)
            
            // Add component edges
            foreach component in go.Components:
                AddComponentEdge(goNode, component)
            
            // Add parent-child edges
            if go.ParentId != null:
                knowledgeGraph.AddEdge(new GraphEdge {
                    SourceId = GraphNodeId.FromName($"{scenePath}:{go.ParentId}"),
                    TargetId = goNode.Id,
                    Kind = EdgeKind.CHILD_OF
                })
    
    AnalyzePrefab(prefabPath: string):
        // Similar to scene analysis
        // Track prefab instances
        content = File.ReadAllText(prefabPath)
        prefabData = ParsePrefabYaml(content)
        
        prefabNode = new GraphNode {
            Id = GraphNodeId.FromName(prefabPath),
            Label = Path.GetFileNameWithoutExtension(prefabPath),
            Kind = NodeKind.Prefab,
            Metadata = new Dictionary<string, string> {
                ["FilePath"] = prefabPath,
                ["ComponentCount"] = prefabData.Components.Count.ToString()
            }
        }
        knowledgeGraph.AddNode(prefabNode)
```

---

## 6. Unity-Specific Queries

### 6.1 Find All MonoBehaviours with Specific Pattern

```
FindMonoBehavioursWithPattern(pattern: string):
    // Find all MonoBehaviours matching a pattern
    // e.g., "Singleton", "Manager", "Controller"
    
    monoBehaviours = knowledgeGraph.GetNodesByKind(NodeKind.MonoBehaviour)
    
    return monoBehaviours
        .Where(mb => mb.Label.Contains(pattern, StringComparison.OrdinalIgnoreCase))
        .Select(mb => new UnitySearchResult {
            Node = mb,
            Metadata = mb.Metadata,
            Score = CalculatePatternScore(mb, pattern)
        })
```

### 6.2 Find Component Dependencies

```
FindComponentDependencies(typeName: string):
    // Find all components that depend on this component
    
    typeId = GraphNodeId.FromFullName(typeName)
    
    // Find RequireComponent edges
    dependents = knowledgeGraph.GetIncomingEdges(typeId)
        .Where(e => e.Kind == EdgeKind.REQUIRES)
        .Select(e => knowledgeGraph.GetNode(e.SourceId))
    
    // Find serialized field references
    fieldDependents = knowledgeGraph.GetIncomingEdges(typeId)
        .Where(e => e.Kind == EdgeKind.REFERENCES)
        .Select(e => knowledgeGraph.GetNode(e.SourceId))
    
    return dependents.Concat(fieldDependents).Distinct()
```

### 6.3 Find Lifecycle Flow

```
FindLifecycleFlow(typeName: string):
    // Find the execution flow of Unity lifecycle methods
    
    typeId = GraphNodeId.FromFullName(typeName)
    typeNode = knowledgeGraph.GetNode(typeId)
    
    // Get lifecycle methods
    lifecycleMethods = GetLifecycleMethods(typeId)
    
    // Build execution flow
    flow = new LifecycleFlow()
    
    foreach method in lifecycleMethods.OrderBy(m => GetExecutionOrder(m.Name)):
        // Find methods called by this lifecycle method
        callees = FindCallees(method.Id)
        
        flow.AddStep(new LifecycleStep {
            Method = method,
            Callees = callees,
            Order = GetExecutionOrder(method.Name)
        })
    
    return flow
```

### 6.4 Find Event Connections

```
FindEventConnections(typeName: string):
    // Find all UnityEvent connections for a type
    
    typeId = GraphNodeId.FromFullName(typeName)
    
    // Find UnityEvent fields
    eventFields = knowledgeGraph.GetOutgoingEdges(typeId)
        .Where(e => e.Kind == EdgeKind.DECLARES)
        .Select(e => knowledgeGraph.GetNode(e.TargetId))
        .Where(n => n.Kind == NodeKind.Event || IsUnityEventType(n))
    
    connections = new List<UnityEventConnection>()
    
    foreach eventField in eventFields:
        // Find listeners
        listeners = GetEventListeners(typeId, eventField.Id)
        connections.AddRange(listeners)
    
    return connections
```

---

## 7. Unity Type Detection

```csharp
class UnityTypeDetector
{
    static readonly Dictionary<string, NodeKind> UnityTypeMap = new()
    {
        ["MonoBehaviour"] = NodeKind.MonoBehaviour,
        ["ScriptableObject"] = NodeKind.ScriptableObject,
        ["Editor"] = NodeKind.Editor,
        ["EditorWindow"] = NodeKind.EditorWindow,
        ["PropertyDrawer"] = NodeKind.PropertyDrawer,
        ["CustomEditor"] = NodeKind.CustomEditor,
        ["ScriptedImporter"] = NodeKind.ScriptedImporter
    };
    
    static readonly HashSet<string> UnityComponentTypes = new()
    {
        "Transform", "Rigidbody", "Rigidbody2D",
        "Collider", "Collider2D",
        "Renderer", "MeshRenderer", "SkinnedMeshRenderer",
        "AudioSource", "AudioListener",
        "Camera", "Light",
        "Animator", "Animation",
        "ParticleSystem",
        "Canvas", "CanvasGroup",
        "RectTransform",
        "LayoutGroup", "HorizontalLayoutGroup", "VerticalLayoutGroup",
        "GridLayoutGroup",
        "ScrollRect", "Mask", "Image", "RawImage",
        "Text", "TextMeshPro", "TextMeshProUGUI",
        "Button", "Toggle", "Slider", "Scrollbar",
        "InputField", "Dropdown"
    };
    
    DetectUnityType(typeSymbol: INamedTypeSymbol):
        // Check base type chain
        current = typeSymbol.BaseType
        while current != null:
            baseName = current.ToDisplayString()
            
            if UnityTypeMap.ContainsKey(baseName):
                return UnityTypeMap[baseName]
            
            current = current.BaseType
        
        // Check interfaces
        foreach iface in typeSymbol.AllInterfaces:
            if iface.ToDisplayString() == "UnityEditor.AssetPostprocessor":
                return NodeKind.AssetPostprocessor
        
        return NodeKind.Class
    }
    
    IsUnityComponent(typeName: string):
        return UnityComponentTypes.Contains(typeName)
    }
}
```

---

## 8. Complexity Analysis

| Operation | Time Complexity | Space Complexity |
|-----------|----------------|-----------------|
| MonoBehaviour Analysis | O(M * F) where M = methods, F = fields | O(M + F) |
| Serialized Field Analysis | O(F) where F = fields in type | O(F) |
| Assembly Definition Parse | O(A) where A = asmdef files | O(A) |
| Unity Graph Build | O(T * M) where T = types, M = avg members | O(T * E) where E = edges |
| Scene Analysis | O(G * C) where G = GameObjects, C = components | O(G * C) |
| Find Dependencies | O(V + E) BFS | O(V) |
| Find Lifecycle Flow | O(M) where M = lifecycle methods | O(M) |
