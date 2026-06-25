export interface GraphNode {
  id: string;
  label: string;
  kind: string;
  color: string;
  size: number;
  metadata: Record<string, string>;
}

export interface GraphEdge {
  id: string;
  source: string;
  target: string;
  kind: string;
  color: string;
}

export interface GraphData {
  nodes: GraphNode[];
  edges: GraphEdge[];
}

export interface GraphStats {
  nodes: number;
  edges: number;
  symbols: number;
  nodeKinds: { kind: string; count: number }[];
}

export interface IndexResult {
  success: boolean;
  filesIndexed: number;
  symbolsExtracted: number;
  graphNodesCreated: number;
  graphEdgesCreated: number;
  duration: string;
  error?: string;
}

export interface SymbolEntity {
  id: string;
  name: string;
  fullName: string;
  kind: string;
  typeName?: string;
  filePath?: string;
  startLine: number;
  endLine: number;
  accessModifier: string;
  returnType?: string;
}

export interface RagResult {
  question: string;
  evidenceCount: number;
  evidence: { symbol: SymbolEntity; score: number; source: string }[];
  tokenCount: number;
  prompt: string;
}

export type NodeKindFilter = Record<string, boolean>;
export type EdgeKindFilter = Record<string, boolean>;
export type Page = 'dashboard' | 'graph' | 'symbols' | 'search' | 'rag' | 'about';
