import React from 'react';
import './TopBar.css';

const TopBar: React.FC = () => {
  return (
    <div className="top-bar-container" style={{ backgroundColor: '#FDFDFD' }}>
      {/* 第一行 */}
      <div className="top-bar-row top-bar-row-1">
        <div className="app-title" style={{ color: '#4A4A4A' }}>LightBox</div>
        {/* 全局设置按钮占位符 */}
        <i className="fas fa-cog global-settings-icon"></i>
      </div>
      {/* 第二行 */}
      <div className="top-bar-row top-bar-row-2">
        {/* 工作区占位图标 */}
        <i className="fas fa-folder workspace-icon"></i>
        <span className="workspace-name" style={{ color: '#4A4A4A' }}>当前工作区</span>
        {/* 工作区设置按钮占位符 */}
        <i className="fas fa-ellipsis-h workspace-settings-icon"></i>
      </div>
    </div>
  );
};

export default TopBar;