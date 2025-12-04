import React from 'react';
import { NavLink, Outlet } from 'react-router-dom';
import { LayoutDashboard, Users, Mail, PlusCircle, Settings } from 'lucide-react';

const Layout: React.FC = () => {
  const navClass = ({ isActive }: { isActive: boolean }) =>
    `flex items-center gap-3 px-4 py-3 rounded-lg transition-colors ${
      isActive
        ? 'bg-blue-600 text-white'
        : 'text-gray-300 hover:bg-gray-800 hover:text-white'
    }`;

  return (
    <div className="flex h-screen bg-gray-50 text-gray-900 font-sans">
      {/* Sidebar */}
      <aside className="w-64 bg-gray-900 text-white flex flex-col flex-shrink-0">
        <div className="p-6 border-b border-gray-800">
          <div className="flex items-center gap-2 font-bold text-xl tracking-tight">
            <Mail className="w-6 h-6 text-blue-400" />
            <span>MailGo</span>
          </div>
          <p className="text-xs text-gray-500 mt-1">Self-hosted Campaigns</p>
        </div>

        <nav className="flex-1 p-4 space-y-2 overflow-y-auto">
          <NavLink to="/" className={navClass} end>
            <LayoutDashboard size={20} />
            <span>Dashboard</span>
          </NavLink>
          <NavLink to="/recipients" className={navClass}>
            <Users size={20} />
            <span>Recipients</span>
          </NavLink>
          <div className="pt-4 pb-2">
            <p className="px-4 text-xs font-semibold text-gray-500 uppercase tracking-wider">
              Actions
            </p>
          </div>
          <NavLink to="/campaigns/new" className={navClass}>
            <PlusCircle size={20} />
            <span>New Campaign</span>
          </NavLink>
          <NavLink to="/settings" className={navClass}>
            <Settings size={20} />
            <span>Sender Setup</span>
          </NavLink>
        </nav>

        <div className="p-4 border-t border-gray-800 text-xs text-gray-500 text-center">
          v1.0.0
        </div>
      </aside>

      {/* Main Content */}
      <main className="flex-1 overflow-auto">
        <div className="max-w-7xl mx-auto p-8">
          <Outlet />
        </div>
      </main>
    </div>
  );
};

export default Layout;
