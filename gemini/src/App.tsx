import React from 'react';
import { Routes, Route, HashRouter } from 'react-router-dom';
import Layout from './components/Layout';
import Dashboard from './pages/Dashboard';
import RecipientsPage from './pages/Recipients';
import CampaignEditor from './pages/CampaignEditor';
import CampaignDetail from './pages/CampaignDetail';

function App() {
  return (
    <HashRouter>
      <Routes>
        <Route path="/" element={<Layout />}>
          <Route index element={<Dashboard />} />
          <Route path="recipients" element={<RecipientsPage />} />
          
          <Route path="campaigns">
             <Route path="new" element={<CampaignEditor />} />
             <Route path=":id" element={<CampaignDetail />} />
             <Route path=":id/edit" element={<CampaignEditor />} />
          </Route>
        </Route>
      </Routes>
    </HashRouter>
  );
}

export default App;
