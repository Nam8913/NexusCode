import React from 'react';

export default function AboutPage() {
  return (
    <>
      <h1>About NexusGraph</h1>
      <div className="card">
        <h2>Overview</h2>
        <p>NexusGraph is an interactive graph visualization tool for the Nexus Code Intelligence Platform. It provides deep code understanding through knowledge graphs, symbol search, and AI-powered context generation.</p>
      </div>
      <div className="card">
        <h2>Technology Stack</h2>
        <table className="table">
          <thead><tr><th>Component</th><th>Technology</th></tr></thead>
          <tbody>
            <tr><td>Frontend</td><td>React + TypeScript + Vite</td></tr>
            <tr><td>Graph Visualization</td><td>Sigma.js + Graphology</td></tr>
            <tr><td>Backend</td><td>.NET 10 + ASP.NET Core</td></tr>
            <tr><td>Analysis</td><td>Roslyn</td></tr>
            <tr><td>AI</td><td>Ollama</td></tr>
          </tbody>
        </table>
      </div>
      <div className="card">
        <h2>Features</h2>
        <ul style={{ listStyle: 'none', padding: 0 }}>
          {['Interactive force-directed graph visualization', 'Node/edge type filtering', 'Symbol search with fuzzy matching', 'Call graph analysis', 'Graph RAG for AI context', 'Repository health metrics', 'Multi-repository support'].map(f => (
            <li key={f} style={{ padding: '6px 0', borderBottom: '1px solid #21262d' }}>• {f}</li>
          ))}
        </ul>
      </div>
    </>
  );
}
