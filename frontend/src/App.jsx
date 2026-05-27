import React from 'react';
import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import AuditoriaExecutionScreen from './AuditoriaExecutionScreen';
import AuditoriaDashboardScreen from './AuditoriaDashboardScreen';

function App() {
  return (
    <Router>
      <Routes>
        <Route path="/" element={<AuditoriaExecutionScreen />} />
        <Route path="/dashboard/:id" element={<AuditoriaDashboardScreen />} />
      </Routes>
    </Router>
  );
}

export default App;