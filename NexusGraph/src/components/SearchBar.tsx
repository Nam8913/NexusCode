import React from 'react';

interface Props {
  value: string;
  onChange: (value: string) => void;
}

export default function SearchBar({ value, onChange }: Props) {
  return (
    <div style={{
      position: 'absolute',
      top: 12,
      left: '50%',
      transform: 'translateX(-50%)',
      zIndex: 10
    }}>
      <input
        type="text"
        value={value}
        onChange={(e) => onChange(e.target.value)}
        placeholder="Search nodes..."
        style={{
          width: 300,
          padding: '8px 14px',
          background: '#161b22',
          border: '1px solid #30363d',
          borderRadius: 6,
          color: '#c9d1d9',
          fontSize: 14,
          outline: 'none'
        }}
      />
    </div>
  );
}
