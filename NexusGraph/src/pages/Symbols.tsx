import React, { useState, useEffect } from 'react';
import { api } from '../utils/api';
import { SymbolEntity } from '../types/graph';

export default function Symbols() {
  const [query, setQuery] = useState('');
  const [kind, setKind] = useState('');
  const [results, setResults] = useState<SymbolEntity[]>([]);
  const [selected, setSelected] = useState<SymbolEntity | null>(null);
  const [callers, setCallers] = useState<any[]>([]);
  const [callees, setCallees] = useState<any[]>([]);
  const [loading, setLoading] = useState(false);

  async function search() {
    if (!query.trim()) return;
    setLoading(true);
    try {
      const data = await api.search.symbol(query, kind || undefined);
      setResults(data);
    } catch {}
    setLoading(false);
  }

  async function selectSymbol(s: SymbolEntity) {
    setSelected(s);
    try {
      const [c1, c2] = await Promise.all([api.search.callers(s.fullName), api.search.callees(s.fullName)]);
      setCallers(c1);
      setCallees(c2);
    } catch {}
  }

  return (
    <>
      <h1>Symbols</h1>
      <div className="card">
        <div className="input-group">
          <input value={query} onChange={e => setQuery(e.target.value)} placeholder="Search symbols..." className="input" onKeyDown={e => e.key === 'Enter' && search()} />
          <select value={kind} onChange={e => setKind(e.target.value)} className="select">
            <option value="">All Types</option>
            <option value="Type">Classes</option>
            <option value="Method">Methods</option>
            <option value="Property">Properties</option>
            <option value="Field">Fields</option>
          </select>
          <button onClick={search} disabled={loading} className="btn btn-primary">{loading ? '...' : 'Search'}</button>
        </div>
      </div>

      {results.length > 0 && (
        <div className="card">
          <h2>Results ({results.length})</h2>
          <table className="table">
            <thead><tr><th>Name</th><th>Kind</th><th>File</th><th>Line</th></tr></thead>
            <tbody>
              {results.slice(0, 50).map(s => (
                <tr key={s.fullName} className="clickable" onClick={() => selectSymbol(s)}>
                  <td><code>{s.fullName}</code></td><td>{s.kind}</td><td>{s.filePath?.split(/[/\\]/).pop()}</td><td>{s.startLine}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {selected && (
        <div className="card">
          <h2>{selected.fullName}</h2>
          <p className="text-sm"><strong>Kind:</strong> {selected.kind} | <strong>File:</strong> {selected.filePath}:{selected.startLine}</p>
          {callers.length > 0 && <><h3>Callers ({callers.length})</h3><ul>{callers.slice(0, 10).map((c, i) => <li key={i} className="text-sm">{c.symbol?.fullName || c.name}</li>)}</ul></>}
          {callees.length > 0 && <><h3>Callees ({callees.length})</h3><ul>{callees.slice(0, 10).map((c, i) => <li key={i} className="text-sm">{c.symbol?.fullName || c.name}</li>)}</ul></>}
        </div>
      )}
    </>
  );
}
