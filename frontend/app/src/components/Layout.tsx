import React from 'react';
import { NavLink, Outlet } from 'react-router-dom';
import { LayoutDashboard, Users, PlusCircle, Settings } from 'lucide-react';

const Layout: React.FC = () => {
  const logoSrc = `${process.env.PUBLIC_URL || ''}/brand/mailgo.png`;
  const navClass = ({ isActive }: { isActive: boolean }) =>
    `flex items-center gap-3 px-4 py-3 rounded-lg border transition-colors ${
      isActive
        ? 'bg-white text-brand-navy shadow-sm border-brand-blue/30'
        : 'text-brand-navy/70 border-transparent hover:bg-white/70 hover:text-brand-navy'
    }`;

  return (
    <div className="flex h-screen bg-brand-soft text-brand-navy font-sans">
      {/* Sidebar */}
      <aside className="w-64 bg-brand-surface text-brand-navy flex flex-col flex-shrink-0 border-r border-gray-200">
        <div className="p-6 border-b border-gray-200">
          <div className="flex items-center gap-3 font-bold text-xl tracking-tight text-brand-navy">
            <div className="w-12 h-12 rounded-xl bg-white shadow-sm border border-gray-200 flex items-center justify-center p-2">
              <img
                src={logoSrc}
                alt="Mailgo logo"
                className="w-full h-full object-contain"
              />
            </div>
            <div className="leading-tight">
              <span className="block">MailGo</span>
              <span className="text-xs font-semibold text-brand-blue">Campaign Manager</span>
            </div>
          </div>
          <p className="text-xs text-brand-navy/60 mt-3">Self-hosted Campaigns</p>
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
            <p className="px-4 text-xs font-semibold text-brand-navy/60 uppercase tracking-wider">
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

        <div className="p-4 border-t border-gray-200 text-xs text-brand-navy/60 text-center">
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
