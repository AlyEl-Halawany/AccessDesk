import React, { useState } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import {
  LayoutDashboard,
  PlusCircle,
  FileText,
  KeyRound,
  Inbox,
  AppWindow,
  ArrowRightLeft,
  ClipboardCheck,
  ShieldAlert,
  Bell,
  LogOut,
  Menu,
  X,
  Shield,
} from 'lucide-react';
import { useAuth } from '../auth/AuthContext';
import { apiClient } from '../api/client';

export const Layout: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const { user, logout, hasRole } = useAuth();
  const location = useLocation();
  const navigate = useNavigate();
  const [mobileOpen, setMobileOpen] = useState(false);

  const { data: unreadData } = useQuery({
    queryKey: ['unread-count'],
    queryFn: async () => (await apiClient.get('/api/notifications/unread-count')).data,
    refetchInterval: 30000,
    enabled: !!user,
  });

  const unreadCount = unreadData?.unreadCount ?? 0;

  const navItems = [
    { to: '/', label: 'Dashboard', icon: LayoutDashboard, show: true },
    { to: '/requests/new', label: 'New Request', icon: PlusCircle, show: true },
    { to: '/requests', label: 'My Requests', icon: FileText, show: true },
    { to: '/my-access', label: 'My Access', icon: KeyRound, show: true },
    { to: '/approvals', label: 'Approval Inbox', icon: Inbox, show: hasRole('Manager', 'AppOwner') },
    { to: '/reviews', label: 'Access Reviews', icon: ClipboardCheck, show: hasRole('Manager', 'SecurityAdmin') },
    { to: '/applications', label: 'Applications', icon: AppWindow, show: hasRole('SecurityAdmin') },
    { to: '/jml', label: 'JML Console', icon: ArrowRightLeft, show: hasRole('SecurityAdmin') },
    { to: '/audit-logs', label: 'Audit Log', icon: ShieldAlert, show: hasRole('SecurityAdmin') },
    { to: '/notifications', label: 'Notifications', icon: Bell, badge: unreadCount, show: true },
  ].filter((item) => item.show);

  const handleLogout = async () => {
    await logout();
    navigate('/login');
  };

  return (
    <div className="min-h-screen flex bg-slate-50">
      {/* Mobile backdrop */}
      {mobileOpen && (
        <div
          className="fixed inset-0 bg-slate-900/50 z-20 lg:hidden"
          onClick={() => setMobileOpen(false)}
        />
      )}

      {/* Sidebar */}
      <aside
        className={`fixed inset-y-0 left-0 z-30 w-64 bg-slate-900 text-slate-100 flex flex-col transition-transform lg:static lg:translate-x-0 ${
          mobileOpen ? 'translate-x-0' : '-translate-x-full'
        }`}
      >
        <div className="h-16 px-5 flex items-center justify-between border-b border-slate-800">
          <div className="flex items-center gap-2.5">
            <Shield className="w-6 h-6 text-indigo-400" />
            <span className="font-bold text-lg tracking-tight">AccessDesk</span>
          </div>
          <button className="lg:hidden text-slate-400" onClick={() => setMobileOpen(false)}>
            <X className="w-5 h-5" />
          </button>
        </div>

        <nav className="flex-1 p-3 space-y-1 overflow-y-auto">
          {navItems.map((item) => {
            const Icon = item.icon;
            const active = location.pathname === item.to;
            return (
              <Link
                key={item.to}
                to={item.to}
                onClick={() => setMobileOpen(false)}
                className={`flex items-center justify-between px-3 py-2.5 rounded-lg text-sm font-medium transition-colors ${
                  active
                    ? 'bg-indigo-600 text-white'
                    : 'text-slate-300 hover:bg-slate-800 hover:text-white'
                }`}
              >
                <span className="flex items-center gap-3">
                  <Icon className="w-4 h-4" />
                  {item.label}
                </span>
                {item.badge !== undefined && item.badge > 0 && (
                  <span className="px-2 py-0.5 text-xs rounded-full bg-rose-500 text-white font-semibold">
                    {item.badge}
                  </span>
                )}
              </Link>
            );
          })}
        </nav>

        <div className="p-4 border-t border-slate-800">
          <div className="text-sm font-medium text-white truncate">{user?.fullName}</div>
          <div className="text-xs text-slate-400 truncate">{user?.email}</div>
          <div className="mt-1.5 flex flex-wrap gap-1">
            {user?.roles.map((r) => (
              <span key={r} className="px-1.5 py-0.5 text-[10px] rounded bg-slate-800 text-indigo-300">
                {r}
              </span>
            ))}
          </div>
          <button
            onClick={handleLogout}
            className="mt-3 w-full flex items-center justify-center gap-2 px-3 py-2 rounded-lg text-xs font-medium bg-slate-800 hover:bg-slate-700 text-slate-200 transition-colors"
          >
            <LogOut className="w-3.5 h-3.5" />
            Sign Out
          </button>
        </div>
      </aside>

      {/* Main Content */}
      <div className="flex-1 flex flex-col min-w-0">
        <header className="h-16 bg-white border-b border-slate-200 px-4 lg:px-8 flex items-center justify-between">
          <button className="lg:hidden p-2 text-slate-600" onClick={() => setMobileOpen(true)}>
            <Menu className="w-5 h-5" />
          </button>
          <div className="text-sm text-slate-500 hidden lg:block">
            Enterprise Access Request &amp; Lifecycle Governance
          </div>
          <Link
            to="/notifications"
            className="relative p-2 text-slate-600 hover:text-slate-900 rounded-lg hover:bg-slate-100"
          >
            <Bell className="w-5 h-5" />
            {unreadCount > 0 && (
              <span className="absolute top-1 right-1 w-2 h-2 rounded-full bg-rose-500" />
            )}
          </Link>
        </header>

        <main className="flex-1 p-4 lg:p-8 max-w-7xl w-full mx-auto">{children}</main>
      </div>
    </div>
  );
};
