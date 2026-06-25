import React from 'react';
import { GraphData } from '../types/graph';

interface Props {
  nodeId: string | null;
  data: GraphData;
  onClose: () => void;
}

export default function NodeDetails({ nodeId, data, onClose }: Props) {
  if (!nodeId) return null;

  const node = data.nodes.find(n => n.id === nodeId);
  if (!node) return null;

  const connectedEdges = data.edges.filter(e => e.source === nodeId || e.target === nodeId);
  const incomingEdges = connectedEdges.filter(e => e.target === nodeId);
  const outgoingEdges = connectedEdges.filter(e => e.source === nodeId);

  return (
    <div style={{
      position: 'absolute',
      top: 12,
      right: 12,
      width: 300,
      background: '#161b22',
      border: '1px solid #30363d',
      borderRadius: 8,
      padding: 16,
      zIndex: 10,
      color: '#c9d1d9',
      fontSize: 13
    }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 12 }}>
        <div style={{ fontWeight: 700, color: '#f0f6fc', fontSize: 14 }}>{node.label}</div>
        <button onClick={onClose} style={{ background: 'none', border: 'none', color: '#8b949e', cursor: 'pointer', fontSize: 18 }}>×</button>
      </div>

      <div style={{ marginBottom: 8 }}>
        <span style={{ color: '#8b949e' }}>Kind: </span>
        <span style={{ color: node.color }}>{node.kind}</span>
      </div>

      {Object.entries(node.metadata || {}).map(([key, value]) => (
        <div key={key} style={{ marginBottom: 4 }}>
          <span style={{ color: '#8b949e' }}>{key}: </span>
          <span style={{ wordBreak: 'break-all' }}>{value}</span>
        </div>
      ))}

      {incomingEdges.length > 0 && (
        <div style={{ marginTop: 12 }}>
          <div style={{ fontWeight: 600, color: '#8b949e', marginBottom: 6 }}>Incoming ({incomingEdges.length})</div>
          {incomingEdges.slice(0, 5).map(e => {
            const sourceNode = data.nodes.find(n => n.id === e.source);
            return (
              <div key={e.id} style={{ marginBottom: 4, fontSize: 12 }}>
                <span style={{ color: '#58a6ff' }}>{sourceNode?.label || e.source}</span>
                <span style={{ color: '#8b949e' }}> → </span>
                <span style={{ color: e.color }}>{e.kind}</span>
              </div>
            );
          })}
        </div>
      )}

      {outgoingEdges.length > 0 && (
        <div style={{ marginTop: 12 }}>
          <div style={{ fontWeight: 600, color: '#8b949e', marginBottom: 6 }}>Outgoing ({outgoingEdges.length})</div>
          {outgoingEdges.slice(0, 5).map(e => {
            const targetNode = data.nodes.find(n => n.id === e.target);
            return (
              <div key={e.id} style={{ marginBottom: 4, fontSize: 12 }}>
                <span style={{ color: e.color }}>{e.kind}</span>
                <span style={{ color: '#8b949e' }}> → </span>
                <span style={{ color: '#58a6ff' }}>{targetNode?.label || e.target}</span>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}
