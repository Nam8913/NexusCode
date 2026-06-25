import React, { useState } from 'react';
import { api } from '../utils/api';
import { RagResult } from '../types/graph';

export default function RagPage() {
  const [question, setQuestion] = useState('');
  const [result, setResult] = useState<RagResult | null>(null);
  const [loading, setLoading] = useState(false);

  async function ask() {
    if (!question.trim()) return;
    setLoading(true);
    try {
      const res = await api.rag.ask(question);
      setResult(res);
    } catch (err: any) {
      setResult(null);
      alert(err.message);
    }
    setLoading(false);
  }

  return (
    <>
      <h1>Graph RAG</h1>
      <div className="card">
        <div className="input-group">
          <input value={question} onChange={e => setQuestion(e.target.value)} placeholder="Ask a question about the code..." className="input" onKeyDown={e => e.key === 'Enter' && ask()} />
          <button onClick={ask} disabled={loading} className="btn btn-primary">{loading ? '...' : 'Ask'}</button>
        </div>
      </div>

      {loading && <div className="loading-card"><div className="loading-spinner-large"></div><p>Analyzing...</p></div>}

      {result && !loading && (
        <>
          <div className="card">
            <h2>Context ({result.evidenceCount} symbols found, ~{result.tokenCount} tokens)</h2>
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
          <div className="card">
            <h2>Generated Prompt</h2>
            <pre>{result.prompt}</pre>
          </div>
        </>
      )}
    </>
  );
}
