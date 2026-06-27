import React, { useState, useEffect } from 'react';
import { api } from '../utils/api';
import { IndexResult, RepositoryInfo } from '../types/graph';

interface Props {
  disabledProjects: Set<string>;
  onProjectToggle: (projectName: string) => void;
}

export default function Dashboard({ disabledProjects, onProjectToggle }: Props) {
  const [repoPath, setRepoPath] = useState('');
  const [isIndexing, setIsIndexing] = useState(false);
  const [result, setResult] = useState<IndexResult | null>(null);
  const [repositories, setRepositories] = useState<RepositoryInfo[]>([]);
  const [reposLoading, setReposLoading] = useState(true);

  useEffect(() => { loadRepositories(); }, []);

  async function loadRepositories() {
    try {
      setReposLoading(true);
      const repos = await api.multiRepo.list();
      setRepositories(repos);
    } catch {
      setRepositories([]);
    }
    setReposLoading(false);
  }

  async function handleIndex() {
    if (!repoPath.trim()) return;
    setIsIndexing(true);
    setResult(null);
    try {
      const res = await api.index.repository(repoPath);
      setResult(res);
      await loadRepositories();
    } catch (err: any) {
      setResult({ success: false, error: err.message, filesIndexed: 0, symbolsExtracted: 0, graphNodesCreated: 0, graphEdgesCreated: 0, duration: '' });
    }
    setIsIndexing(false);
  }

  async function handleDelete(name: string) {
    if (!window.confirm(`Are you sure you want to remove repository "${name}"? This action cannot be undone.`)) {
      return;
    }
    try {
      await api.multiRepo.remove(name);
      setRepositories(prev => prev.filter(r => r.name !== name));
    } catch (err: any) {
      console.error('Failed to delete repository:', err);
    }
  }

  return (
    <>
      <h1>Dashboard</h1>
      <div className="card">
        <h2>Index Repository</h2>
        <div className="input-group">
          <input value={repoPath} onChange={e => setRepoPath(e.target.value)} placeholder="Enter repository path (e.g. D:\MyProject)" className="input" disabled={isIndexing} onKeyDown={e => e.key === 'Enter' && handleIndex()} aria-label="Repository path" />
          <button onClick={handleIndex} disabled={isIndexing} className="btn btn-primary" aria-label="Index repository">
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

      <div className="card">
        <h2>Analyzed Projects</h2>
        <p className="text-sm text-muted" style={{ marginBottom: 10 }}>
          Toggle projects on/off. Disabled projects are excluded from Graph and search.
        </p>
        {reposLoading && (
          <div className="loading-card"><div className="loading-spinner-large"></div><p>Loading projects...</p></div>
        )}
        {!reposLoading && repositories.length === 0 && (
          <p className="text-muted text-sm">No projects indexed yet. Index a repository above to get started.</p>
        )}
        {!reposLoading && repositories.length > 0 && (
          <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
            {repositories.map(repo => {
              const isActive = !disabledProjects.has(repo.name);
              return (
                <div key={repo.name} style={{
                  display: 'flex', alignItems: 'center', padding: '10px 14px',
                  background: '#21262d', borderRadius: 6, fontSize: 13,
                  borderLeft: `3px solid ${isActive ? '#238636' : '#30363d'}`
                }}>
                  <div style={{ flex: 1, minWidth: 0 }}>
                    <div style={{ fontWeight: 600, color: isActive ? '#f0f6fc' : '#484f58', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{repo.name}</div>
                    <div style={{ fontSize: 11, color: '#484f58', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{repo.path}</div>
                  </div>
                  <button
                    onClick={() => onProjectToggle(repo.name)}
                    title={isActive ? 'Disable' : 'Enable'}
                    style={{
                      marginLeft: 8, width: 44, height: 24, borderRadius: 12,
                      border: 'none', cursor: 'pointer', position: 'relative',
                      background: isActive ? '#238636' : '#30363d',
                      transition: 'background 0.15s', flexShrink: 0
                    }}
                  >
                    <div style={{
                      width: 18, height: 18, borderRadius: '50%', background: '#fff',
                      position: 'absolute', top: 3, left: isActive ? 23 : 3,
                      transition: 'left 0.15s', boxShadow: '0 1px 3px rgba(0,0,0,0.3)'
                    }} />
                  </button>
                  <button
                    onClick={() => handleDelete(repo.name)}
                    title="Delete project"
                    style={{
                      marginLeft: 6, width: 24, height: 24, borderRadius: 4,
                      border: '1px solid #30363d', cursor: 'pointer',
                      background: 'transparent', color: '#8b949e',
                      fontSize: 14, display: 'flex', alignItems: 'center', justifyContent: 'center',
                      flexShrink: 0, transition: 'all 0.15s'
                    }}
                    onMouseEnter={e => { e.currentTarget.style.borderColor = '#f85149'; e.currentTarget.style.color = '#f85149'; }}
                    onMouseLeave={e => { e.currentTarget.style.borderColor = '#30363d'; e.currentTarget.style.color = '#8b949e'; }}
                  >
                    ×
                  </button>
                </div>
              );
            })}
          </div>
        )}
      </div>
    </>
  );
}
