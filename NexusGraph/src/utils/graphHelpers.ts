import Graph from 'graphology';
import { GraphData, GraphNode, GraphEdge } from '../types/graph';

export function buildGraph(data: GraphData): Graph {
  const graph = new Graph({ multi: true });

  for (const node of data.nodes) {
    graph.addNode(node.id, {
      label: node.label,
      color: node.color,
      size: node.size,
      kind: node.kind,
      x: Math.random() * 1000,
      y: Math.random() * 1000
    });
  }

  for (const edge of data.edges) {
    if (graph.hasNode(edge.source) && graph.hasNode(edge.target)) {
      graph.addEdge(edge.source, edge.target, {
        color: edge.color,
        kind: edge.kind,
        size: 1
      });
    }
  }

  return graph;
}

export function filterGraph(
  graph: Graph,
  nodeKinds: Record<string, boolean>,
  edgeKinds: Record<string, boolean>
): Graph {
  const filtered = new Graph({ multi: true });

  graph.forEachNode((nodeKey, attributes) => {
    const kind = attributes.kind as string;
    if (nodeKinds[kind] !== false) {
      filtered.addNode(nodeKey, attributes);
    }
  });

  graph.forEachEdge((edgeKey, attributes, source, target) => {
    const kind = attributes.kind as string;
    if (edgeKinds[kind] !== false && filtered.hasNode(source) && filtered.hasNode(target)) {
      filtered.addEdge(source, target, attributes);
    }
  });

  return filtered;
}

export function focusOnNode(graph: Graph, nodeId: string, depth: number): string[] {
  const focused = new Set<string>();
  const queue: [string, number][] = [[nodeId, 0]];

  while (queue.length > 0) {
    const [current, currentDepth] = queue.shift()!;
    if (focused.has(current) || currentDepth > depth) continue;

    focused.add(current);

    if (currentDepth < depth) {
      graph.forEachNeighbor(current, (neighbor) => {
        if (!focused.has(neighbor)) {
          queue.push([neighbor, currentDepth + 1]);
        }
      });
    }
  }

  return Array.from(focused);
}

export function searchNodes(graph: Graph, query: string): string[] {
  const lowerQuery = query.toLowerCase();
  const matches: string[] = [];

  graph.forEachNode((nodeKey, attributes) => {
    const label = (attributes.label as string)?.toLowerCase() || '';
    const kind = (attributes.kind as string)?.toLowerCase() || '';
    if (label.includes(lowerQuery) || kind.includes(lowerQuery)) {
      matches.push(nodeKey);
    }
  });

  return matches;
}
