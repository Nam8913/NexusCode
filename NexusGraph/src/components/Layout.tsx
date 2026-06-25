import React from 'react';
import { Page } from '../types/graph';

interface Props {
  currentPage: Page;
  onNavigate: (page: Page) => void;
  children: React.ReactNode;
}

const NAV_ITEMS: { page: Page; label: string; icon: string }[] = [
  { page: 'dashboard', label: 'Dashboard', icon: '⊞' },
  { page: 'graph', label: 'Graph', icon: '◎' },
  { page: 'symbols', label: 'Symbols', icon: '∈' },
  { page: 'search', label: 'Search', icon: '⌕' },
  { page: 'rag', label: 'Graph RAG', icon: '✦' },
  { page: 'about', label: 'About', icon: 'ℹ' },
];

export default function Layout({ currentPage, onNavigate, children }: Props) {
  return (
    <div className="layout">
      <div className="sidebar">
        <div className="brand">Nexus Graph</div>
        <nav>
          {NAV_ITEMS.map(item => (
            <div
              key={item.page}
              className={`nav-link ${currentPage === item.page ? 'active' : ''}`}
              onClick={() => onNavigate(item.page)}
            >
              <span className="nav-icon">{item.icon}</span>
              {item.label}
            </div>
          ))}
        </nav>
      </div>
      <div className="main">
        <div className="main-content" style={currentPage === 'graph' ? { padding: 0, overflow: 'hidden' } : {}}>
          {children}
        </div>
      </div>
    </div>
  );
}
