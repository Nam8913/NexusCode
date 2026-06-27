import { GraphData, GraphStats, IndexResult, SymbolEntity, RagResult, RepositoryInfo } from '../types/graph';

const API_BASE = '/api';

async function fetchJson<T>(url: string, options?: RequestInit): Promise<T> {
  const response = await fetch(url, options);
  if (!response.ok) {
    const err = await response.json().catch(() => ({ error: response.statusText }));
    throw new Error(err.error || 'Request failed');
  }
  return response.json();
}

export const api = {
  index: {
    status: () => fetchJson<{ indexed: boolean; symbols: number; graphNodes: number; graphEdges: number }>(`${API_BASE}/index/status`),
    repository: (path: string) => fetchJson<IndexResult>(`${API_BASE}/index/repository`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ path })
    })
  },
  graph: {
    export: () => fetchJson<GraphData>(`${API_BASE}/graph/export`),
    stats: () => fetchJson<GraphStats>(`${API_BASE}/graph/stats`)
  },
  search: {
    symbol: (query: string, kind?: string) => {
      const params = new URLSearchParams({ query, maxResults: '50' });
      if (kind) params.set('kind', kind);
      return fetchJson<SymbolEntity[]>(`${API_BASE}/search/symbol?${params}`);
    },
    callers: (name: string) => fetchJson<any[]>(`${API_BASE}/search/callers/${encodeURIComponent(name)}`),
    callees: (name: string) => fetchJson<any[]>(`${API_BASE}/search/callees/${encodeURIComponent(name)}`)
  },
  rag: {
    ask: (question: string) => fetchJson<RagResult>(`${API_BASE}/rag/ask`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ question })
    })
  },
  multiRepo: {
    list: () => fetchJson<RepositoryInfo[]>(`${API_BASE}/multirepo/list`),
    remove: (repoName: string) => fetchJson<{ message: string }>(`${API_BASE}/multirepo/${encodeURIComponent(repoName)}`, { method: 'DELETE' })
  }
};
