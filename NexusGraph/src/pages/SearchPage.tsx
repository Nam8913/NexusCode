import React, { useState } from 'react';
import { api } from '../utils/api';
import { SymbolEntity } from '../types/graph';

interface Props {
  disabledProjects: Set<string>;
}

export default function SearchPage({ disabledProjects }: Props) {
  const [query, setQuery] = useState('');
  const [results, setResults] = useState<SymbolEntity[]>([]);
  const [loading, setLoading] = useState(false);
  const [selected, setSelected] = useState<SymbolEntity | null>(null);
  const [details, setDetails] = useState<any>(null);
  const [detailsLoading, setDetailsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  function filterByProject(items: SymbolEntity[]): SymbolEntity[] {
    if (disabledProjects.size === 0) return items;
    return items.filter(s => {
      const fp = (s.filePath || '').toLowerCase();
      return !Array.from(disabledProjects).some(name => fp.includes(name.toLowerCase()));
    });
  }

  async function search() {
    if (!query.trim()) return;
    setLoading(true);
    setError(null);
    try {
      const data = await api.search.symbol(query);
      setResults(filterByProject(data));
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
      setDetails({ callers: c1, callees: c2 });
    } catch { setDetails(null); }
    setDetailsLoading(false);
  }

  return (
    <>
      <h1>Search</h1>
      <div className="card">
        <div className="input-group">
          <input value={query} onChange={e => setQuery(e.target.value)} placeholder="Search for symbols..." className="input" onKeyDown={e => e.key === 'Enter' && search()} aria-label="Search symbols" />
          <button onClick={search} disabled={loading} className="btn btn-primary">{loading ? <><span className="loading-spinner"></span> Searching...</> : 'Search'}</button>
        </div>
      </div>

      {error && <div className="result-card error"><div className="result-header"><span className="icon-error">✗</span><h3>Error</h3></div><p>{error}</p></div>}

      {loading && <div className="loading-card"><div className="loading-spinner-large"></div><p>Searching...</p></div>}

      {!loading && results.length > 0 && (
        <div className="card">
          <h2>Results ({results.length})</h2>
          <div style={{ overflowX: 'auto' }}>
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
        </div>
      )}

      {!loading && results.length === 0 && query && !error && <p className="text-muted">No results found.</p>}

      {selected && (
        <div className="card">
          <h2>{selected.fullName}</h2>
          <p className="text-sm"><strong>Kind:</strong> {selected.kind} | <strong>Access:</strong> {selected.accessModifier}</p>
          {detailsLoading && <div className="loading-card"><div className="loading-spinner-large"></div><p>Loading details...</p></div>}
          {!detailsLoading && details?.callers?.length > 0 && <><h3>Callers</h3><ul>{details.callers.slice(0, 10).map((c: any, i: number) => <li key={i} className="text-sm">{c.symbol?.fullName || c.name}</li>)}</ul></>}
          {!detailsLoading && details?.callees?.length > 0 && <><h3>Callees</h3><ul>{details.callees.slice(0, 10).map((c: any, i: number) => <li key={i} className="text-sm">{c.symbol?.fullName || c.name}</li>)}</ul></>}
        </div>
      )}
    </>
  );
}
