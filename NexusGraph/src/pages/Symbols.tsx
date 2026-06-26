import React, { useState } from 'react';
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
  const [detailsLoading, setDetailsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function search() {
    if (!query.trim()) return;
    setLoading(true);
    setError(null);
    try {
      const data = await api.search.symbol(query, kind || undefined);
      setResults(data);
    } catch (err: any) {
      setError(err.message || 'Search failed');
      setResults([]);
    }
    setLoading(false);
  }

  async function selectSymbol(s: SymbolEntity) {
    setSelected(s);
    setDetailsLoading(true);
    try {
      const [c1, c2] = await Promise.all([api.search.callers(s.fullName), api.search.callees(s.fullName)]);
      setCallers(c1);
      setCallees(c2);
    } catch {}
    setDetailsLoading(false);
  }

  return (
    <>
      <h1>Symbols</h1>
      <div className="card">
        <div className="input-group">
          <input value={query} onChange={e => setQuery(e.target.value)} placeholder="Search symbols..." className="input" onKeyDown={e => e.key === 'Enter' && search()} aria-label="Search symbols" />
          <select value={kind} onChange={e => setKind(e.target.value)} className="select" aria-label="Filter by kind">
            <option value="">All Types</option>
            <option value="Type">Classes</option>
            <option value="Method">Methods</option>
            <option value="Property">Properties</option>
            <option value="Field">Fields</option>
          </select>
          <button onClick={search} disabled={loading} className="btn btn-primary">{loading ? <><span className="loading-spinner"></span> Searching...</> : 'Search'}</button>
        </div>
      </div>

      {error && <div className="result-card error"><div className="result-header"><span className="icon-error">✗</span><h3>Error</h3></div><p>{error}</p></div>}

      {loading && <div className="loading-card"><div className="loading-spinner-large"></div><p>Searching symbols...</p></div>}

      {!loading && results.length > 0 && (
        <div className="card">
          <h2>Results ({results.length})</h2>
          <div style={{ overflowX: 'auto' }}>
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
        </div>
      )}

      {!loading && results.length === 0 && query && !error && <p className="text-muted">No symbols found.</p>}

      {selected && (
        <div className="card">
          <h2>{selected.fullName}</h2>
          <p className="text-sm"><strong>Kind:</strong> {selected.kind} | <strong>File:</strong> {selected.filePath}:{selected.startLine}</p>
          {detailsLoading && <div className="loading-card"><div className="loading-spinner-large"></div><p>Loading details...</p></div>}
          {!detailsLoading && callers.length > 0 && <><h3>Callers ({callers.length})</h3><ul>{callers.slice(0, 10).map((c, i) => <li key={i} className="text-sm">{c.symbol?.fullName || c.name}</li>)}</ul></>}
          {!detailsLoading && callees.length > 0 && <><h3>Callees ({callees.length})</h3><ul>{callees.slice(0, 10).map((c, i) => <li key={i} className="text-sm">{c.symbol?.fullName || c.name}</li>)}</ul></>}
        </div>
      )}
    </>
  );
}
