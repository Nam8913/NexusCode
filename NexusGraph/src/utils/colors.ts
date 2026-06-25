export const NODE_COLORS: Record<string, string> = {
  Class: '#58a6ff', Interface: '#a371f7', Method: '#3fb950',
  Property: '#d29922', Field: '#f85149', Enum: '#f0883e',
  Namespace: '#8b949e', File: '#58a6ff', Struct: '#79c0ff',
  Record: '#d2a8ff', Event: '#ffa657', Repository: '#f0883e',
  Project: '#79c0ff', Package: '#8b949e', MonoBehaviour: '#3fb950',
  ScriptableObject: '#a371f7', Editor: '#d29922', GameObject: '#58a6ff',
  Prefab: '#79c0ff', Component: '#f85149'
};

export const EDGE_COLORS: Record<string, string> = {
  Contains: '#30363d', Calls: '#3fb950', Inherits: '#58a6ff',
  Implements: '#a371f7', Overrides: '#d29922', Declares: '#8b949e',
  Uses: '#f0883e', References: '#f85149', DependsOn: '#8b949e',
  AssemblyDep: '#8b949e', Component: '#f85149'
};
