import React from 'react';
import './WorkspaceSwitcher.css';

const WorkspaceSwitcher: React.FC = () => {
  return (
    <div className="workspace-switcher-container" style={{ backgroundColor: '#E4E4E4' }}>
      {/* 工作区图标占位符 */}
      <i className="far fa-circle workspace-switcher-icon active"></i>
      <i className="far fa-circle workspace-switcher-icon"></i>
      <i className="far fa-circle workspace-switcher-icon"></i>
      <i className="far fa-circle workspace-switcher-icon"></i>
    </div>
  );
};

export default WorkspaceSwitcher;