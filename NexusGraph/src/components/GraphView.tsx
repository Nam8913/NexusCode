import React, { useEffect, useRef, useState, useCallback } from 'react';
import Graph from 'graphology';
import Sigma from 'sigma';
import { GraphData, NodeKindFilter, EdgeKindFilter } from '../types/graph';
import { buildGraph, filterGraph, focusOnNode, searchNodes } from '../utils/graphHelpers';

interface Props {
  data: GraphData;
  nodeKinds: NodeKindFilter;
  edgeKinds: EdgeKindFilter;
  focusDepth: number;
  searchQuery: string;
  onNodeSelect: (nodeId: string | null) => void;
}

export default function GraphView({ data, nodeKinds, edgeKinds, focusDepth, searchQuery, onNodeSelect }: Props) {
  const containerRef = useRef<HTMLDivElement>(null);
  const sigmaRef = useRef<Sigma | null>(null);
  const graphRef = useRef<Graph | null>(null);
  const [selectedNode, setSelectedNode] = useState<string | null>(null);

  useEffect(() => {
    if (!containerRef.current || !data) return;

    const graph = buildGraph(data);
    graphRef.current = graph;

    if (sigmaRef.current) {
      sigmaRef.current.kill();
    }

    const sigma = new Sigma(graph, containerRef.current, {
      renderLabels: true,
      renderEdgeLabels: false,
      defaultNodeColor: '#8b949e',
      defaultEdgeColor: '#30363d',
      labelFont: 'Segoe UI, system-ui, sans-serif',
      labelSize: 12,
      labelColor: { color: '#c9d1d9' },
      minCameraRatio: 0.1,
      maxCameraRatio: 10
    });

    sigma.on('clickNode', ({ node }) => {
      setSelectedNode(node);
      onNodeSelect(node);
    });

    sigma.on('clickStage', () => {
      setSelectedNode(null);
      onNodeSelect(null);
    });

    sigmaRef.current = sigma;

    return () => {
      sigma.kill();
      sigmaRef.current = null;
    };
  }, [data]);

  useEffect(() => {
    if (!graphRef.current) return;
    const filtered = filterGraph(graphRef.current, nodeKinds, edgeKinds);
    if (sigmaRef.current) {
      sigmaRef.current.kill();
    }

    const sigma = new Sigma(filtered, containerRef.current!, {
      renderLabels: true,
      renderEdgeLabels: false,
      defaultNodeColor: '#8b949e',
      defaultEdgeColor: '#30363d',
      labelFont: 'Segoe UI, system-ui, sans-serif',
      labelSize: 12,
      labelColor: { color: '#c9d1d9' },
      minCameraRatio: 0.1,
      maxCameraRatio: 10
    });

    sigma.on('clickNode', ({ node }) => {
      setSelectedNode(node);
      onNodeSelect(node);
    });

    sigma.on('clickStage', () => {
      setSelectedNode(null);
      onNodeSelect(null);
    });

    sigmaRef.current = sigma;
  }, [nodeKinds, edgeKinds]);

  useEffect(() => {
    if (!graphRef.current || !sigmaRef.current || !searchQuery) return;

    const matches = searchNodes(graphRef.current, searchQuery);

    graphRef.current.forEachNode((nodeKey) => {
      const isMatch = matches.includes(nodeKey);
      graphRef.current!.setNodeAttribute(nodeKey, 'highlighted', isMatch);
    });

    sigmaRef.current.refresh();

    return () => {
      if (graphRef.current) {
        graphRef.current.forEachNode((nodeKey) => {
          graphRef.current!.removeNodeAttribute(nodeKey, 'highlighted');
        });
      }
    };
  }, [searchQuery]);

  return (
    <div
      ref={containerRef}
      style={{ width: '100%', height: '100%', background: '#0d1117' }}
    />
  );
}
