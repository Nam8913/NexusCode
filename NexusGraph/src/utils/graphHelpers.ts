import Graph from 'graphology';
import forceAtlas2 from 'graphology-layout-forceatlas2';
import { GraphData } from '../types/graph';

export function buildGraph(data: GraphData): Graph {
  const graph = new Graph({ multi: true });

  for (const node of data.nodes) {
    const angle = Math.random() * Math.PI * 2;
    const radius = Math.sqrt(Math.random()) * 300;
    graph.addNode(node.id, {
      label: node.label,
      color: node.color,
      size: node.size,
      kind: node.kind,
      hidden: false,
      x: Math.cos(angle) * radius,
      y: Math.sin(angle) * radius
    });
  }

  for (const edge of data.edges) {
    if (!graph.hasNode(edge.source)) {
      const angle = Math.random() * Math.PI * 2;
      const radius = Math.sqrt(Math.random()) * 300;
      graph.addNode(edge.source, {
        label: edge.sourceLabel || 'Unknown',
        color: '#484f58',
        size: 3,
        kind: 'External',
        hidden: false,
        x: Math.cos(angle) * radius,
        y: Math.sin(angle) * radius
      });
    }
    if (!graph.hasNode(edge.target)) {
      const angle = Math.random() * Math.PI * 2;
      const radius = Math.sqrt(Math.random()) * 300;
      graph.addNode(edge.target, {
        label: edge.targetLabel || 'Unknown',
        color: '#484f58',
        size: 3,
        kind: 'External',
        hidden: false,
        x: Math.cos(angle) * radius,
        y: Math.sin(angle) * radius
      });
    }
    graph.addEdge(edge.source, edge.target, {
      color: edge.color,
      kind: edge.kind,
      size: 1,
      hidden: false
    });
  }

  return graph;
}

export function runForceLayoutAsync(
  graph: Graph,
  onProgress?: () => void,
  iterations = 100
): Promise<void> {
  return new Promise((resolve) => {
    if (graph.order === 0) { resolve(); return; }

    const settings = {
      gravity: 0.3,
      scalingRatio: 5,
      barnesHutOptimize: graph.order > 200,
      strongGravityMode: false,
      slowDown: 2,
      outboundAttractionDistribution: true,
      adjustSizes: true
    };

    let i = 0;
    const batchSize = 10;

    function step() {
      const end = Math.min(i + batchSize, iterations);
      forceAtlas2.assign(graph, { iterations: end - i, settings });
      i = end;
      onProgress?.();
      if (i < iterations) {
        requestAnimationFrame(step);
      } else {
        resolve();
      }
    }

    requestAnimationFrame(step);
  });
}

export function applyForceLayout(graph: Graph): void {
  if (graph.order === 0) return;

  forceAtlas2.assign(graph, {
    iterations: 80,
    settings: {
      gravity: 0.3,
      scalingRatio: 5,
      barnesHutOptimize: graph.order > 200,
      strongGravityMode: false,
      slowDown: 2,
      outboundAttractionDistribution: true,
      adjustSizes: true
    }
  });
}

export function toggleNodeKind(
  graph: Graph,
  kind: string,
  visible: boolean
): void {
  graph.forEachNode((nodeKey, attrs) => {
    if (attrs.kind === kind) {
      graph.setNodeAttribute(nodeKey, 'hidden', !visible);
    }
  });
}

export function toggleEdgeKind(
  graph: Graph,
  kind: string,
  visible: boolean
): void {
  graph.forEachEdge((edgeKey, attrs) => {
    if (attrs.kind === kind) {
      graph.setEdgeAttribute(edgeKey, 'hidden', !visible);
    }
  });
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
