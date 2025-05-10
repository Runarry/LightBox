import React from 'react';
import TopBar from './components/layout/TopBar';
import WorkspaceSwitcher from './components/layout/WorkspaceSwitcher';
import MainContent from './components/layout/MainContent';
import './App.css';

const App: React.FC = () => {
  return (
    <div className="app-container">
      <TopBar />
      <MainContent />
      <WorkspaceSwitcher />
    </div>
  );
};

export default App;