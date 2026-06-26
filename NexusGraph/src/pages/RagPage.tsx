import React, { useState } from 'react';
import { api } from '../utils/api';
import { RagResult } from '../types/graph';

export default function RagPage() {
  const [question, setQuestion] = useState('');
  const [result, setResult] = useState<RagResult | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function ask() {
    if (!question.trim()) return;
    setLoading(true);
    setError(null);
    setResult(null);
    try {
      const res = await api.rag.ask(question);
      setResult(res);
    } catch (err: any) {
      setError(err.message || 'Failed to get answer');
    }
    setLoading(false);
  }

  return (
    <>
      <h1>Graph RAG</h1>
      <div className="card">
        <div className="input-group">
          <input value={question} onChange={e => setQuestion(e.target.value)} placeholder="Ask a question about the code..." className="input" onKeyDown={e => e.key === 'Enter' && ask()} aria-label="Ask question" />
          <button onClick={ask} disabled={loading} className="btn btn-primary">{loading ? <><span className="loading-spinner"></span> Analyzing...</> : 'Ask'}</button>
        </div>
      </div>

      {loading && <div className="loading-card"><div className="loading-spinner-large"></div><p>Analyzing codebase...</p></div>}

      {error && <div className="result-card error"><div className="result-header"><span className="icon-error">✗</span><h3>Error</h3></div><p>{error}</p></div>}

      {result && !loading && !error && (
        <>
          <div className="card">
            <h2>Context ({result.evidenceCount} symbols found, ~{result.tokenCount} tokens)</h2>
            <div style={{ overflowX: 'auto' }}>
              <table className="table">
                <thead><tr><th>Symbol</th><th>Kind</th><th>Source</th><th>Score</th></tr></thead>
                <tbody>
                  {result.evidence.slice(0, 20).map((e, i) => (
                    <tr key={i}>
                      <td><code>{e.symbol.fullName}</code></td><td>{e.symbol.kind}</td><td>{e.source}</td><td>{e.score.toFixed(2)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
          <div className="card">
            <h2>Generated Prompt</h2>
            <pre>{result.prompt}</pre>
          </div>
        </>
      )}
    </>
  );
}
