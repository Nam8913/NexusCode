import React, { useState, useCallback } from 'react';
import Layout from './components/Layout';
import Dashboard from './pages/Dashboard';
import GraphPage from './pages/GraphPage';
import Symbols from './pages/Symbols';
import SearchPage from './pages/SearchPage';
import RagPage from './pages/RagPage';
import AboutPage from './pages/AboutPage';
import { Page, NodeKindFilter, EdgeKindFilter } from './types/graph';
import { NODE_COLORS, EDGE_COLORS } from './utils/colors';

export default function App() {
  const [page, setPage] = useState<Page>('dashboard');
  const [nodeKinds, setNodeKinds] = useState<NodeKindFilter>(
    Object.fromEntries(Object.keys(NODE_COLORS).map(k => [k, true]))
  );
  const [edgeKinds, setEdgeKinds] = useState<EdgeKindFilter>(
    Object.fromEntries(Object.keys(EDGE_COLORS).map(k => [k, true]))
  );
  const [disabledProjects, setDisabledProjects] = useState<Set<string>>(new Set());

  const handleNodeKindToggle = useCallback((kind: string) => {
    setNodeKinds(prev => ({ ...prev, [kind]: !prev[kind] }));
  }, []);

  const handleEdgeKindToggle = useCallback((kind: string) => {
    setEdgeKinds(prev => ({ ...prev, [kind]: !prev[kind] }));
  }, []);

  const handleProjectToggle = useCallback((projectName: string) => {
    setDisabledProjects(prev => {
      const next = new Set(prev);
      if (next.has(projectName)) {
        next.delete(projectName);
      } else {
        next.add(projectName);
      }
      return next;
    });
  }, []);

  const renderPage = () => {
    switch (page) {
      case 'dashboard': return <Dashboard disabledProjects={disabledProjects} onProjectToggle={handleProjectToggle} />;
      case 'graph': return <GraphPage nodeKinds={nodeKinds} edgeKinds={edgeKinds} onNodeKindToggle={handleNodeKindToggle} onEdgeKindToggle={handleEdgeKindToggle} disabledProjects={disabledProjects} />;
      case 'symbols': return <Symbols disabledProjects={disabledProjects} />;
      case 'search': return <SearchPage disabledProjects={disabledProjects} />;
      case 'rag': return <RagPage />;
      case 'about': return <AboutPage />;
      default: return <Dashboard disabledProjects={disabledProjects} onProjectToggle={handleProjectToggle} />;
    }
  };

  return (
    <Layout currentPage={page} onNavigate={setPage}>
      {renderPage()}
    </Layout>
  );
}
