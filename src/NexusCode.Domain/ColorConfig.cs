namespace NexusCode.Domain;

public static class ColorConfig
{
    public static readonly Dictionary<string, string> NodeColors = new()
    {
        ["Repository"] = "#f0883e",
        ["Project"] = "#79c0ff",
        ["Namespace"] = "#8b949e",
        ["Class"] = "#58a6ff",
        ["Interface"] = "#a371f7",
        ["Struct"] = "#79c0ff",
        ["Enum"] = "#f0883e",
        ["Record"] = "#d2a8ff",
        ["Method"] = "#3fb950",
        ["Property"] = "#d29922",
        ["Field"] = "#f85149",
        ["Event"] = "#ffa657",
        ["File"] = "#58a6ff",
        ["Assembly"] = "#8b949e",
        ["Package"] = "#8b949e",
        ["MonoBehaviour"] = "#3fb950",
        ["ScriptableObject"] = "#a371f7",
        ["Editor"] = "#d29922",
        ["GameObject"] = "#58a6ff",
        ["Prefab"] = "#79c0ff",
        ["Component"] = "#f85149"
    };

    public static readonly Dictionary<string, string> EdgeColors = new()
    {
        ["Contains"] = "#30363d",
        ["Calls"] = "#3fb950",
        ["Inherits"] = "#58a6ff",
        ["Implements"] = "#a371f7",
        ["Overrides"] = "#d29922",
        ["Declares"] = "#8b949e",
        ["Uses"] = "#f0883e",
        ["References"] = "#f85149",
        ["DependsOn"] = "#8b949e",
        ["Reads"] = "#79c0ff",
        ["Writes"] = "#d29922",
        ["Requires"] = "#f0883e",
        ["Attribute"] = "#a371f7",
        ["Returns"] = "#58a6ff",
        ["Parameter"] = "#ffa657",
        ["FieldType"] = "#f85149",
        ["PropertyType"] = "#d29922",
        ["EventHandler"] = "#3fb950",
        ["ImplicitlyImplements"] = "#a371f7",
        ["ChildOf"] = "#30363d",
        ["InstanceOf"] = "#58a6ff",
        ["AddressableRef"] = "#79c0ff",
        ["UnityEvent"] = "#ffa657",
        ["Coroutine"] = "#3fb950",
        ["AssemblyDep"] = "#8b949e",
        ["Component"] = "#f85149"
    };

    public static string GetNodeColor(string kind) =>
        NodeColors.GetValueOrDefault(kind, "#8b949e");

    public static string GetEdgeColor(string kind) =>
        EdgeColors.GetValueOrDefault(kind, "#30363d");
}
