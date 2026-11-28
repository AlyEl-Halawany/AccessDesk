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
} from 'lucide-react';
import type { LucideIcon } from 'lucide-react';
import { useAuth } from '../auth/AuthContext';
import { apiClient } from '../api/client';

type NavItem = {
  to: string;
  label: string;
  icon: LucideIcon;
  show: boolean;
  badge?: number;
};

function initials(name?: string) {
  if (!name) return 'AD';
  return name
    .split(' ')
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase())
    .join('');
}

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

  const navGroups: { label: string; items: NavItem[] }[] = [
    {
      label: 'Workspace',
      items: [
        { to: '/', label: 'Dashboard', icon: LayoutDashboard, show: true },
        { to: '/requests/new', label: 'New request', icon: PlusCircle, show: true },
        { to: '/requests', label: 'My requests', icon: FileText, show: true },
        { to: '/my-access', label: 'My access', icon: KeyRound, show: true },
        { to: '/notifications', label: 'Notifications', icon: Bell, badge: unreadCount, show: true },
      ],
    },
    {
      label: 'Governance',
      items: [
        { to: '/approvals', label: 'Approvals', icon: Inbox, show: hasRole('Manager', 'AppOwner') },
        { to: '/reviews', label: 'Access reviews', icon: ClipboardCheck, show: hasRole('Manager', 'SecurityAdmin') },
      ],
    },
    {
      label: 'Administration',
      items: [
        { to: '/applications', label: 'Applications', icon: AppWindow, show: hasRole('SecurityAdmin') },
        { to: '/jml', label: 'Lifecycle', icon: ArrowRightLeft, show: hasRole('SecurityAdmin') },
        { to: '/audit-logs', label: 'Audit log', icon: ShieldAlert, show: hasRole('SecurityAdmin') },
      ],
    },
  ]
    .map((group) => ({ ...group, items: group.items.filter((item) => item.show) }))
    .filter((group) => group.items.length > 0);
  const currentPage = navGroups.flatMap((group) => group.items).find((item) => item.to === location.pathname);

  const handleLogout = async () => {
    await logout();
    navigate('/login');
  };

  return (
    <div className="min-h-screen flex bg-paper">
      {mobileOpen && (
        <div className="fixed inset-0 bg-ink/40 z-20 lg:hidden" onClick={() => setMobileOpen(false)} />
      )}

      <aside
        className={`fixed inset-y-0 left-0 z-30 w-[15.5rem] shrink-0 bg-sidebar text-[#d8d5cc] flex flex-col transition-transform lg:static lg:translate-x-0 ${
          mobileOpen ? 'translate-x-0' : '-translate-x-full'
        }`}
      >
        <div className="h-[4.25rem] px-5 flex items-center justify-between border-b border-sidebar-border">
          <Link to="/" className="flex items-baseline gap-2" onClick={() => setMobileOpen(false)}>
            <span className="font-serif text-[1.35rem] text-white tracking-tight">AccessDesk</span>
            <span className="text-[10px] tracking-[0.16em] uppercase text-[#8d9286]">IGA</span>
          </Link>
          <button className="lg:hidden text-[#8d9286]" onClick={() => setMobileOpen(false)} aria-label="Close menu">
            <X className="w-5 h-5" />
          </button>
        </div>

        <nav className="flex-1 overflow-y-auto px-3 py-5">
          <div className="space-y-5">
            {navGroups.map((group) => (
              <section key={group.label}>
                <p className="px-3 mb-1.5 text-[9px] font-medium tracking-[0.15em] uppercase text-[#777c71]">
                  {group.label}
                </p>
                <div className="space-y-0.5">
                  {group.items.map((item) => {
                    const Icon = item.icon;
                    const active = location.pathname === item.to;
                    return (
                      <Link
                        key={item.to}
                        to={item.to}
                        onClick={() => setMobileOpen(false)}
                        aria-current={active ? 'page' : undefined}
                        className={`group flex items-center justify-between px-3 py-2 rounded-md text-[13px] transition-colors ${
                          active ? 'bg-white/10 text-white' : 'text-[#b7b3a8] hover:bg-sidebar-hover hover:text-white'
                        }`}
                      >
                        <span className="flex items-center gap-2.5">
                          <span className={`h-4 w-0.5 rounded-full ${active ? 'bg-accent' : 'bg-transparent'}`} />
                          <Icon className={`w-4 h-4 ${active ? 'text-[#7dcdc4]' : 'text-[#8d9286]'}`} />
                          {item.label}
                        </span>
                        {item.badge !== undefined && item.badge > 0 && (
                          <span className="min-w-5 h-5 px-1.5 flex items-center justify-center text-[10px] rounded-full bg-accent text-white font-medium">
                            {item.badge}
                          </span>
                        )}
                      </Link>
                    );
                  })}
                </div>
              </section>
            ))}
          </div>
        </nav>

        <div className="p-4 border-t border-sidebar-border">
          <div className="flex items-center gap-3">
            <div className="h-9 w-9 rounded-full bg-[#2a2d27] text-[#cde8e4] text-xs font-medium flex items-center justify-center">
              {initials(user?.fullName)}
            </div>
            <div className="min-w-0">
              <div className="text-sm text-white truncate">{user?.fullName}</div>
              <div className="text-[11px] text-[#8d9286] truncate">{user?.email}</div>
            </div>
          </div>
          <div className="mt-2.5 flex flex-wrap gap-1">
            {user?.roles.map((role) => (
              <span key={role} className="px-1.5 py-0.5 text-[10px] rounded bg-[#2a2d27] text-[#b7b3a8]">
                {role}
              </span>
            ))}
          </div>
          <button
            onClick={handleLogout}
            className="mt-3 w-full flex items-center justify-center gap-2 h-9 rounded-md text-xs font-medium bg-[#1e211c] hover:bg-[#2a2d27] text-[#d8d5cc] transition-colors"
          >
            <LogOut className="w-3.5 h-3.5" />
            Sign out
          </button>
        </div>
      </aside>

      <div className="flex-1 flex flex-col min-w-0">
        <header className="h-[4.25rem] bg-surface/80 backdrop-blur border-b border-line px-4 lg:px-8 flex items-center justify-between">
          <div className="flex items-center gap-3">
            <button className="lg:hidden p-2 text-ink-soft" onClick={() => setMobileOpen(true)} aria-label="Open menu">
              <Menu className="w-5 h-5" />
            </button>
            <div className="hidden lg:block">
              <div className="text-[10px] tracking-[0.14em] uppercase text-muted">Access governance</div>
              <div className="text-sm text-ink-soft">{currentPage?.label ?? 'Workspace'}</div>
            </div>
          </div>
          <Link
            to="/notifications"
            className="relative h-9 w-9 flex items-center justify-center text-ink-soft hover:text-ink rounded-md hover:bg-paper"
            aria-label="Notifications"
          >
            <Bell className="w-4 h-4" />
            {unreadCount > 0 && <span className="absolute top-1.5 right-1.5 w-1.5 h-1.5 rounded-full bg-accent" />}
          </Link>
        </header>

        <main className="flex-1 p-5 lg:p-8 max-w-6xl w-full mx-auto">{children}</main>
      </div>
    </div>
  );
};
