import React from 'react';
import './MainContent.css';

const MainContent: React.FC = () => {
  return (
    <div className="main-content-root" style={{ backgroundColor: '#F8F8F8' }}> {/* Renamed from main-content-container */}
      <div className="main-content-inner-wrapper"> {/* New inner wrapper */}
        <p style={{ color: '#4A4A4A' }}>主内容区占位符</p>
      </div>
    </div>
  );
};

export default MainContent;