import React, { useState, useEffect } from 'react';
import { api } from '../utils/api';
import { IndexResult } from '../types/graph';

export default function Dashboard() {
  const [repoPath, setRepoPath] = useState('');
  const [isIndexing, setIsIndexing] = useState(false);
  const [result, setResult] = useState<IndexResult | null>(null);
  const [status, setStatus] = useState<any>(null);

  useEffect(() => { loadStatus(); }, []);

  async function loadStatus() {
    try { setStatus(await api.index.status()); } catch {}
  }

  async function handleIndex() {
    if (!repoPath.trim()) return;
    setIsIndexing(true);
    setResult(null);
    try {
      const res = await api.index.repository(repoPath);
      setResult(res);
      await loadStatus();
    } catch (err: any) {
      setResult({ success: false, error: err.message, filesIndexed: 0, symbolsExtracted: 0, graphNodesCreated: 0, graphEdgesCreated: 0, duration: '' });
    }
    setIsIndexing(false);
  }

  return (
    <>
      <h1>Dashboard</h1>
      <div className="card">
        <h2>Index Repository</h2>
        <div className="input-group">
          <input value={repoPath} onChange={e => setRepoPath(e.target.value)} placeholder="Enter repository path (e.g. D:\MyProject)" className="input" disabled={isIndexing} onKeyDown={e => e.key === 'Enter' && handleIndex()} />
          <button onClick={handleIndex} disabled={isIndexing} className="btn btn-primary">
            {isIndexing ? <><span className="loading-spinner"></span> Indexing...</> : 'Index'}
          </button>
        </div>
        {isIndexing && (
          <div className="loading-card">
            <div className="loading-spinner-large"></div>
            <p>Analyzing repository...</p>
          </div>
        )}
        {result && !isIndexing && (
          result.success ? (
            <div className="result-card success">
              <div className="result-header"><span className="icon-success">✓</span><h3>Indexing Complete</h3></div>
              <div className="result-grid">
                <div className="result-item"><span className="result-value">{result.filesIndexed}</span><span className="result-label">Files</span></div>
                <div className="result-item"><span className="result-value">{result.symbolsExtracted}</span><span className="result-label">Symbols</span></div>
                <div className="result-item"><span className="result-value">{result.graphNodesCreated}</span><span className="result-label">Nodes</span></div>
                <div className="result-item"><span className="result-value">{result.graphEdgesCreated}</span><span className="result-label">Edges</span></div>
              </div>
            </div>
          ) : (
            <div className="result-card error">
              <div className="result-header"><span className="icon-error">✗</span><h3>Indexing Failed</h3></div>
              <p>{result.error}</p>
            </div>
          )
        )}
      </div>

      {status && (
        <div className="stats-grid">
          <div className={`stat-card ${status.symbols > 0 ? 'active' : ''}`}><div className="stat-value">{status.symbols}</div><div className="stat-label">Symbols</div></div>
          <div className={`stat-card ${status.graphNodes > 0 ? 'active' : ''}`}><div className="stat-value">{status.graphNodes}</div><div className="stat-label">Graph Nodes</div></div>
          <div className={`stat-card ${status.graphEdges > 0 ? 'active' : ''}`}><div className="stat-value">{status.graphEdges}</div><div className="stat-label">Graph Edges</div></div>
          <div className={`stat-card ${status.indexed ? 'active' : ''}`}><div className="stat-value">{status.indexed ? 'Ready' : 'Not Indexed'}</div><div className="stat-label">Status</div></div>
        </div>
      )}
    </>
  );
}
