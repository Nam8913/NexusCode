import React, { useState } from 'react';
import { api } from '../utils/api';
import { SymbolEntity } from '../types/graph';

export default function SearchPage() {
  const [query, setQuery] = useState('');
  const [results, setResults] = useState<SymbolEntity[]>([]);
  const [loading, setLoading] = useState(false);
  const [selected, setSelected] = useState<SymbolEntity | null>(null);
  const [details, setDetails] = useState<any>(null);

  async function search() {
    if (!query.trim()) return;
    setLoading(true);
    try {
      const data = await api.search.symbol(query);
      setResults(data);
    } catch {}
    setLoading(false);
  }

  async function selectSymbol(s: SymbolEntity) {
    setSelected(s);
    try {
      const [c1, c2] = await Promise.all([api.search.callers(s.fullName), api.search.callees(s.fullName)]);
      setDetails({ callers: c1, callees: c2 });
    } catch { setDetails(null); }
  }

  return (
    <>
      <h1>Search</h1>
      <div className="card">
        <div className="input-group">
          <input value={query} onChange={e => setQuery(e.target.value)} placeholder="Search for symbols..." className="input" onKeyDown={e => e.key === 'Enter' && search()} />
          <button onClick={search} disabled={loading} className="btn btn-primary">{loading ? '...' : 'Search'}</button>
        </div>
      </div>

      {results.length > 0 && (
        <div className="card">
          <h2>Results ({results.length})</h2>
          <table className="table">
            <thead><tr><th>Name</th><th>Kind</th><th>File</th><th>Line</th></tr></thead>
            <tbody>
              {results.map(s => (
                <tr key={s.fullName} className="clickable" onClick={() => selectSymbol(s)}>
                  <td><code>{s.fullName}</code></td><td>{s.kind}</td><td>{s.filePath?.split(/[/\\]/).pop()}</td><td>{s.startLine}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {selected && details && (
        <div className="card">
          <h2>{selected.fullName}</h2>
          <p className="text-sm"><strong>Kind:</strong> {selected.kind} | <strong>Access:</strong> {selected.accessModifier}</p>
          {details.callers?.length > 0 && <><h3>Callers</h3><ul>{details.callers.slice(0, 10).map((c: any, i: number) => <li key={i} className="text-sm">{c.symbol?.fullName || c.name}</li>)}</ul></>}
          {details.callees?.length > 0 && <><h3>Callees</h3><ul>{details.callees.slice(0, 10).map((c: any, i: number) => <li key={i} className="text-sm">{c.symbol?.fullName || c.name}</li>)}</ul></>}
        </div>
      )}
    </>
  );
}
