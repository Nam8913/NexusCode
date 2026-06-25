import React from 'react';
import { NodeKindFilter, EdgeKindFilter } from '../types/graph';
import { NODE_COLORS, EDGE_COLORS } from '../utils/colors';

interface Props {
  nodeKinds: NodeKindFilter;
  edgeKinds: EdgeKindFilter;
  onNodeKindToggle: (kind: string) => void;
  onEdgeKindToggle: (kind: string) => void;
  focusDepth: number;
  onFocusDepthChange: (depth: number) => void;
}

export default function Sidebar({
  nodeKinds, edgeKinds,
  onNodeKindToggle, onEdgeKindToggle,
  focusDepth, onFocusDepthChange
}: Props) {
  return (
    <div style={{
      width: 220,
      background: '#161b22',
      borderRight: '1px solid #30363d',
      padding: 16,
      overflowY: 'auto',
      color: '#c9d1d9',
      fontSize: 13
    }}>
      <div style={{ marginBottom: 20 }}>
        <div style={{ fontWeight: 700, color: '#58a6ff', marginBottom: 12, fontSize: 14 }}>
          NODE TYPES
        </div>
        {Object.entries(NODE_COLORS).map(([kind, color]) => (
          <label key={kind} style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 6, cursor: 'pointer' }}>
            <input
              type="checkbox"
              checked={nodeKinds[kind] !== false}
              onChange={() => onNodeKindToggle(kind)}
            />
            <span style={{ width: 10, height: 10, borderRadius: '50%', background: color, display: 'inline-block' }} />
            <span>{kind}</span>
          </label>
        ))}
      </div>

      <div style={{ marginBottom: 20 }}>
        <div style={{ fontWeight: 700, color: '#58a6ff', marginBottom: 12, fontSize: 14 }}>
          EDGE TYPES
        </div>
        {Object.entries(EDGE_COLORS).map(([kind, color]) => (
          <label key={kind} style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 6, cursor: 'pointer' }}>
            <input
              type="checkbox"
              checked={edgeKinds[kind] !== false}
              onChange={() => onEdgeKindToggle(kind)}
            />
            <span style={{ width: 20, height: 3, background: color, display: 'inline-block' }} />
            <span>{kind}</span>
          </label>
        ))}
      </div>

      <div style={{ marginBottom: 20 }}>
        <div style={{ fontWeight: 700, color: '#58a6ff', marginBottom: 12, fontSize: 14 }}>
          FOCUS DEPTH
        </div>
        <div style={{ display: 'flex', gap: 4 }}>
          {[0, 1, 2, 3, 5].map(d => (
            <button
              key={d}
              onClick={() => onFocusDepthChange(d)}
              style={{
                padding: '4px 10px',
                background: focusDepth === d ? '#58a6ff' : '#21262d',
                color: focusDepth === d ? '#fff' : '#c9d1d9',
                border: '1px solid #30363d',
                borderRadius: 4,
                cursor: 'pointer',
                fontSize: 12
              }}
            >
              {d === 0 ? 'All' : `${d} hop`}
            </button>
          ))}
        </div>
      </div>

      <div>
        <div style={{ fontWeight: 700, color: '#58a6ff', marginBottom: 12, fontSize: 14 }}>
          COLOR LEGEND
        </div>
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 4 }}>
          {Object.entries(NODE_COLORS).slice(0, 8).map(([kind, color]) => (
            <div key={kind} style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
              <span style={{ width: 8, height: 8, borderRadius: '50%', background: color, display: 'inline-block' }} />
              <span style={{ fontSize: 11, color: '#8b949e' }}>{kind}</span>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
