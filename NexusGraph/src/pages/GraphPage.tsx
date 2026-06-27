import React, { useState, useEffect, useRef, useMemo } from 'react';
import Sigma from 'sigma';
import Graph from 'graphology';
import { api } from '../utils/api';
import { GraphData, NodeKindFilter, EdgeKindFilter } from '../types/graph';
import { NODE_COLORS, EDGE_COLORS } from '../utils/colors';
import { buildGraph, runForceLayoutAsync, toggleNodeKind, toggleEdgeKind } from '../utils/graphHelpers';

interface Props {
  nodeKinds: NodeKindFilter;
  edgeKinds: EdgeKindFilter;
  onNodeKindToggle: (kind: string) => void;
  onEdgeKindToggle: (kind: string) => void;
  disabledProjects?: Set<string>;
}

export default function GraphPage({ nodeKinds, edgeKinds, onNodeKindToggle, onEdgeKindToggle, disabledProjects }: Props) {
  const containerRef = useRef<HTMLDivElement>(null);
  const sigmaRef = useRef<Sigma | null>(null);
  const graphRef = useRef<Graph | null>(null);
  const dataRef = useRef<GraphData | null>(null);
  const abortRef = useRef<AbortController | null>(null);
  const [loading, setLoading] = useState(true);
  const [layoutDone, setLayoutDone] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [selectedNode, setSelectedNode] = useState<any>(null);
  const [searchQuery, setSearchQuery] = useState('');
  const [stats, setStats] = useState<any>(null);
  const originalColorsRef = useRef<Map<string, string>>(new Map());

  const nodeCounts = useMemo(() => {
    if (!dataRef.current) return {};
    const counts: Record<string, number> = {};
    for (const node of dataRef.current.nodes) {
      counts[node.kind] = (counts[node.kind] || 0) + 1;
    }
    return counts;
  }, [stats]);

  const edgeCounts = useMemo(() => {
    if (!dataRef.current) return {};
    const counts: Record<string, number> = {};
    for (const edge of dataRef.current.edges) {
      counts[edge.kind] = (counts[edge.kind] || 0) + 1;
    }
    return counts;
  }, [stats]);

  useEffect(() => {
    loadData();
    return () => {
      abortRef.current?.abort();
      sigmaRef.current?.kill();
    };
  }, []);

  useEffect(() => {
    if (!graphRef.current || !sigmaRef.current) return;

    Object.entries(nodeKinds).forEach(([kind, visible]) => {
      toggleNodeKind(graphRef.current!, kind, visible);
    });

    Object.entries(edgeKinds).forEach(([kind, visible]) => {
      toggleEdgeKind(graphRef.current!, kind, visible);
    });

    sigmaRef.current.refresh();
  }, [nodeKinds, edgeKinds]);

  useEffect(() => {
    if (!graphRef.current || !sigmaRef.current) return;
    if (!dataRef.current || !disabledProjects) return;

    const hiddenNodes = new Set<string>();

    graphRef.current.forEachNode((nodeKey) => {
      const nodeData = dataRef.current!.nodes.find(n => n.id === nodeKey);
      if (nodeData) {
        const meta = nodeData.metadata || {};
        const allValues = Object.values(meta).join(' ').toLowerCase();
        const disabled = Array.from(disabledProjects).some(name => allValues.includes(name.toLowerCase()));
        graphRef.current!.setNodeAttribute(nodeKey, 'hidden', disabled);
        if (disabled) hiddenNodes.add(nodeKey);
      }
    });

    graphRef.current.forEachEdge((edgeKey, attrs, source, target) => {
      const shouldHide = hiddenNodes.has(source) || hiddenNodes.has(target);
      graphRef.current!.setEdgeAttribute(edgeKey, 'hidden', shouldHide);
    });

    sigmaRef.current.refresh();
  }, [disabledProjects]);

  async function loadData() {
    try {
      setLoading(true);
      setError(null);
      const [graphData, graphStats] = await Promise.all([api.graph.export(), api.graph.stats()]);
      dataRef.current = graphData;
      setStats(graphStats);
      initGraph(graphData);
    } catch (err: any) {
      setError(err.message || 'Failed to load graph data');
    }
    setLoading(false);
  }

  async function initGraph(data: GraphData) {
    if (!containerRef.current) return;

    abortRef.current?.abort();
    abortRef.current = new AbortController();
    const signal = abortRef.current.signal;

    const graph = buildGraph(data);

    const currentKinds = { ...nodeKinds };
    const currentEdges = { ...edgeKinds };
    const currentDisabled = disabledProjects ? new Set(disabledProjects) : new Set<string>();

    Object.entries(currentKinds).forEach(([kind, visible]) => {
      if (!visible) toggleNodeKind(graph, kind, false);
    });

    Object.entries(currentEdges).forEach(([kind, visible]) => {
      if (!visible) toggleEdgeKind(graph, kind, false);
    });

    if (currentDisabled.size > 0) {
      const hiddenByProject = new Set<string>();
      graph.forEachNode((nodeKey) => {
        const nodeData = data.nodes.find(n => n.id === nodeKey);
        if (nodeData) {
          const allValues = Object.values(nodeData.metadata || {}).join(' ').toLowerCase();
          const disabled = Array.from(currentDisabled).some(name => allValues.includes(name.toLowerCase()));
          graph.setNodeAttribute(nodeKey, 'hidden', disabled);
          if (disabled) hiddenByProject.add(nodeKey);
        }
      });
      graph.forEachEdge((edgeKey, attrs, source, target) => {
        if (hiddenByProject.has(source) || hiddenByProject.has(target)) {
          graph.setEdgeAttribute(edgeKey, 'hidden', true);
        }
      });
    }

    graph.forEachNode((nodeKey, attrs) => {
      originalColorsRef.current.set(nodeKey, (attrs.color as string) || '#8b949e');
    });

    graphRef.current = graph;

    sigmaRef.current?.kill();

    const sigma = new Sigma(graph, containerRef.current, {
      renderLabels: true,
      renderEdgeLabels: false,
      defaultNodeColor: '#8b949e',
      defaultEdgeColor: '#30363d',
      labelFont: 'Segoe UI, system-ui, sans-serif',
      labelSize: 11,
      labelColor: { color: '#c9d1d9' },
      minCameraRatio: 0.1,
      maxCameraRatio: 10,
      labelRenderedSizeThreshold: 6
    });

    sigma.on('clickNode', ({ node }) => {
      const n = data.nodes.find(x => x.id === node);
      if (n) setSelectedNode(n);
    });
    sigma.on('clickStage', () => setSelectedNode(null));
    sigmaRef.current = sigma;

    setLayoutDone(false);
    try {
      await runForceLayoutAsync(graph, () => {
        if (!signal.aborted) sigma.refresh();
      }, 80);
    } catch {}
    if (!signal.aborted) setLayoutDone(true);
  }

  function fitToScreen() {
    sigmaRef.current?.getCamera().animatedReset({ duration: 500 });
  }

  function handleSearch(query: string) {
    setSearchQuery(query);
    if (!graphRef.current) return;
    const lower = query.toLowerCase();

    graphRef.current.forEachNode((key) => {
      const originalColor = originalColorsRef.current.get(key) || '#8b949e';
      const isHidden = graphRef.current!.getNodeAttribute(key, 'hidden');

      if (!query) {
        if (!isHidden) {
          graphRef.current!.setNodeAttribute(key, 'color', originalColor);
        }
        graphRef.current!.removeNodeAttribute(key, 'highlighted');
      } else {
        if (isHidden) {
          graphRef.current!.removeNodeAttribute(key, 'highlighted');
          return;
        }
        const attrs = graphRef.current!.getNodeAttributes(key);
        const label = (attrs.label as string || '').toLowerCase();
        const kind = (attrs.kind as string || '').toLowerCase();
        const match = label.includes(lower) || kind.includes(lower);
        graphRef.current!.setNodeAttribute(key, 'highlighted', match);
        graphRef.current!.setNodeAttribute(key, 'color', match ? originalColor : '#21262d');
      }
    });
    sigmaRef.current?.refresh();
  }

  if (loading) return <div className="loading-card"><div className="loading-spinner-large"></div><p>Loading graph...</p></div>;
  if (error) return <div className="result-card error"><div className="result-header"><span className="icon-error">✗</span><h3>Failed to Load Graph</h3></div><p>{error}</p><button className="btn" onClick={loadData} style={{ marginTop: 8 }}>Retry</button></div>;

  const kindEntries = Object.entries(NODE_COLORS).filter(([k]) => nodeCounts[k]);
  const edgeEntries = Object.entries(EDGE_COLORS).filter(([k]) => edgeCounts[k]);

  return (
    <div style={{ display: 'flex', height: '100%', gap: 0 }}>
      <div style={{ width: 200, background: '#161b22', border: '1px solid #30363d', borderRadius: 8, padding: 10, overflowY: 'auto', fontSize: 11, flexShrink: 0 }}>
        <div style={{ fontWeight: 700, color: '#58a6ff', marginBottom: 6 }}>NODE TYPES</div>
        {kindEntries.map(([k, c]) => (
          <label key={k} style={{ display: 'flex', alignItems: 'center', gap: 5, marginBottom: 3, cursor: 'pointer' }}>
            <input type="checkbox" checked={nodeKinds[k] !== false} onChange={() => onNodeKindToggle(k)} style={{ width: 12, height: 12 }} />
            <span style={{ width: 7, height: 7, borderRadius: '50%', background: c, display: 'inline-block' }} />
            <span style={{ flex: 1 }}>{k}</span>
            <span style={{ color: '#484f58', fontSize: 10 }}>{nodeCounts[k] || 0}</span>
          </label>
        ))}
        <div style={{ fontWeight: 700, color: '#58a6ff', margin: '10px 0 6px' }}>EDGE TYPES</div>
        {edgeEntries.map(([k, c]) => (
          <label key={k} style={{ display: 'flex', alignItems: 'center', gap: 5, marginBottom: 3, cursor: 'pointer' }}>
            <input type="checkbox" checked={edgeKinds[k] !== false} onChange={() => onEdgeKindToggle(k)} style={{ width: 12, height: 12 }} />
            <span style={{ width: 14, height: 2, background: c, display: 'inline-block' }} />
            <span style={{ flex: 1 }}>{k}</span>
            <span style={{ color: '#484f58', fontSize: 10 }}>{edgeCounts[k] || 0}</span>
          </label>
        ))}
        <div style={{ marginTop: 10, color: '#8b949e' }}>
          {stats?.nodes || 0} nodes · {stats?.edges || 0} edges
        </div>
        {!layoutDone && <div style={{ marginTop: 4, color: '#d29922', fontSize: 10 }}>Layouting...</div>}
      </div>

      <div style={{ flex: 1, position: 'relative', minWidth: 0 }}>
        <div style={{ position: 'absolute', top: 8, left: 8, zIndex: 10, display: 'flex', gap: 6 }}>
          <input value={searchQuery} onChange={e => handleSearch(e.target.value)} placeholder="Search nodes..." style={{ width: 200, padding: '5px 10px', background: '#161b22', border: '1px solid #30363d', borderRadius: 6, color: '#c9d1d9', fontSize: 12 }} aria-label="Search nodes" />
          <button onClick={fitToScreen} className="btn btn-sm" title="Fit to screen">⊡</button>
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
