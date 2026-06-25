import React, { useState, useEffect, useRef } from 'react';
import Sigma from 'sigma';
import Graph from 'graphology';
import { api } from '../utils/api';
import { GraphData, NodeKindFilter, EdgeKindFilter } from '../types/graph';
import { NODE_COLORS, EDGE_COLORS } from '../utils/colors';

interface Props {
  nodeKinds: NodeKindFilter;
  edgeKinds: EdgeKindFilter;
  onNodeKindToggle: (kind: string) => void;
  onEdgeKindToggle: (kind: string) => void;
}

export default function GraphPage({ nodeKinds, edgeKinds, onNodeKindToggle, onEdgeKindToggle }: Props) {
  const containerRef = useRef<HTMLDivElement>(null);
  const sigmaRef = useRef<Sigma | null>(null);
  const graphRef = useRef<Graph | null>(null);
  const [data, setData] = useState<GraphData | null>(null);
  const [loading, setLoading] = useState(true);
  const [selectedNode, setSelectedNode] = useState<any>(null);
  const [searchQuery, setSearchQuery] = useState('');
  const [stats, setStats] = useState<any>(null);

  useEffect(() => { loadData(); }, []);

  useEffect(() => {
    if (!data || !containerRef.current) return;
    renderGraph();
    return () => { sigmaRef.current?.kill(); };
  }, [data, nodeKinds, edgeKinds]);

  async function loadData() {
    try {
      const [graphData, graphStats] = await Promise.all([api.graph.export(), api.graph.stats()]);
      setData(graphData);
      setStats(graphStats);
    } catch {}
    setLoading(false);
  }

  function renderGraph() {
    if (!data || !containerRef.current) return;
    sigmaRef.current?.kill();

    const graph = new Graph({ multi: true });
    const addedNodes = new Set<string>();

    for (const node of data.nodes) {
      if (nodeKinds[node.kind] === false) continue;
      if (addedNodes.has(node.id)) continue;
      addedNodes.add(node.id);
      graph.addNode(node.id, { label: node.label, color: node.color, size: node.size, kind: node.kind, x: Math.random() * 1000, y: Math.random() * 1000 });
    }

    for (const edge of data.edges) {
      if (edgeKinds[edge.kind] === false) continue;
      if (!graph.hasNode(edge.source) || !graph.hasNode(edge.target)) continue;
      if (graph.hasEdge(edge.source, edge.target)) continue;
      graph.addEdge(edge.source, edge.target, { color: edge.color, kind: edge.kind, size: 1 });
    }

    graphRef.current = graph;

    const sigma = new Sigma(graph, containerRef.current, {
      renderLabels: true, renderEdgeLabels: false, defaultNodeColor: '#8b949e', defaultEdgeColor: '#30363d',
      labelFont: 'Segoe UI, system-ui, sans-serif', labelSize: 11, labelColor: { color: '#c9d1d9' },
      minCameraRatio: 0.1, maxCameraRatio: 10
    });

    sigma.on('clickNode', ({ node }) => {
      const n = data.nodes.find(x => x.id === node);
      if (n) setSelectedNode(n);
    });
    sigma.on('clickStage', () => setSelectedNode(null));
    sigmaRef.current = sigma;
  }

  function handleSearch(query: string) {
    setSearchQuery(query);
    if (!graphRef.current) return;
    const lower = query.toLowerCase();
    graphRef.current.forEachNode((key, attrs) => {
      const match = !query || (attrs.label as string).toLowerCase().includes(lower);
      graphRef.current!.setNodeAttribute(key, 'highlighted', match);
      graphRef.current!.setNodeAttribute(key, 'color', match ? (data?.nodes.find(n => n.id === key)?.color || '#8b949e') : '#21262d');
    });
    sigmaRef.current?.refresh();
  }

  if (loading) return <div className="loading-card"><div className="loading-spinner-large"></div><p>Loading graph...</p></div>;
  if (!data) return <p className="text-muted">No graph data. Index a repository first from Dashboard.</p>;

  return (
    <div style={{ display: 'flex', height: '100%', gap: 0 }}>
      <div style={{ width: 180, background: '#161b22', border: '1px solid #30363d', borderRadius: 8, padding: 10, overflowY: 'auto', fontSize: 11, flexShrink: 0 }}>
        <div style={{ fontWeight: 700, color: '#58a6ff', marginBottom: 6 }}>NODE TYPES</div>
        {Object.entries(NODE_COLORS).map(([k, c]) => (
          <label key={k} style={{ display: 'flex', alignItems: 'center', gap: 5, marginBottom: 3, cursor: 'pointer' }}>
            <input type="checkbox" checked={nodeKinds[k] !== false} onChange={() => onNodeKindToggle(k)} style={{ width: 12, height: 12 }} />
            <span style={{ width: 7, height: 7, borderRadius: '50%', background: c, display: 'inline-block' }} />{k}
          </label>
        ))}
        <div style={{ fontWeight: 700, color: '#58a6ff', margin: '10px 0 6px' }}>EDGE TYPES</div>
        {Object.entries(EDGE_COLORS).map(([k, c]) => (
          <label key={k} style={{ display: 'flex', alignItems: 'center', gap: 5, marginBottom: 3, cursor: 'pointer' }}>
            <input type="checkbox" checked={edgeKinds[k] !== false} onChange={() => onEdgeKindToggle(k)} style={{ width: 12, height: 12 }} />
            <span style={{ width: 14, height: 2, background: c, display: 'inline-block' }} />{k}
          </label>
        ))}
        {stats && <div style={{ marginTop: 10, color: '#8b949e' }}>{stats.nodes} nodes · {stats.edges} edges</div>}
      </div>

      <div style={{ flex: 1, position: 'relative', minWidth: 0 }}>
        <div style={{ position: 'absolute', top: 8, left: 8, zIndex: 10 }}>
          <input value={searchQuery} onChange={e => handleSearch(e.target.value)} placeholder="Search nodes..." style={{ width: 220, padding: '5px 10px', background: '#161b22', border: '1px solid #30363d', borderRadius: 6, color: '#c9d1d9', fontSize: 12 }} />
        </div>
        <div ref={containerRef} style={{ width: '100%', height: '100%', background: '#0d1117' }} />
        {selectedNode && (
          <div style={{ position: 'absolute', top: 8, right: 8, width: 260, background: '#161b22', border: '1px solid #30363d', borderRadius: 8, padding: 12, zIndex: 10, fontSize: 12 }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 6 }}>
              <span style={{ fontWeight: 700, color: '#f0f6fc' }}>{selectedNode.label}</span>
              <span style={{ cursor: 'pointer', color: '#8b949e' }} onClick={() => setSelectedNode(null)}>×</span>
            </div>
            <div style={{ marginBottom: 4 }}><span style={{ color: '#8b949e' }}>Kind: </span><span style={{ color: selectedNode.color }}>{selectedNode.kind}</span></div>
            {Object.entries(selectedNode.metadata || {}).map(([k, v]) => (
              <div key={k} style={{ marginBottom: 2 }}><span style={{ color: '#8b949e' }}>{k}: </span><span style={{ wordBreak: 'break-all' }}>{String(v)}</span></div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
