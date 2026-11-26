import React from 'react';
import { useQuery } from '@tanstack/react-query';
import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  Legend,
} from 'recharts';
import { Clock, AlertTriangle, CheckCircle2, FileClock } from 'lucide-react';
import { apiClient } from '../api/client';

export const DashboardPage: React.FC = () => {
  const { data, isLoading } = useQuery({
    queryKey: ['dashboard'],
    queryFn: async () => (await apiClient.get('/api/dashboard')).data,
  });

  if (isLoading) return <div className="text-sm text-slate-500">Loading dashboard metrics...</div>;

  const cards = [
    { label: 'Pending Approvals', value: data?.pendingApprovals ?? 0, icon: FileClock, color: 'text-amber-600 bg-amber-50' },
    { label: 'Expiring Within 7 Days', value: data?.expiringSoonCount ?? 0, icon: Clock, color: 'text-orange-600 bg-orange-50' },
    { label: 'Overdue Reviews', value: data?.overdueReviews ?? 0, icon: AlertTriangle, color: 'text-rose-600 bg-rose-50' },
    { label: 'My Active Access', value: data?.myActiveAccessCount ?? 0, icon: CheckCircle2, color: 'text-emerald-600 bg-emerald-50' },
  ];

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-slate-900">Dashboard</h1>
        <p className="text-sm text-slate-500">Overview of access requests, upcoming expirations, and governance metrics.</p>
      </div>

      {/* KPI Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        {cards.map((c) => {
          const Icon = c.icon;
          return (
            <div key={c.label} className="bg-white rounded-xl border border-slate-200 p-5 flex items-center gap-4">
              <div className={`p-3 rounded-xl ${c.color}`}>
                <Icon className="w-5 h-5" />
              </div>
              <div>
                <div className="text-2xl font-bold text-slate-900">{c.value}</div>
                <div className="text-xs font-medium text-slate-500">{c.label}</div>
              </div>
            </div>
          );
        })}
      </div>

      {/* Charts */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <div className="bg-white rounded-xl border border-slate-200 p-5">
          <h2 className="text-sm font-semibold text-slate-800 mb-4">Requests per Application</h2>
          <div className="h-64">
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={data?.requestsPerApp ?? []}>
                <CartesianGrid strokeDasharray="3 3" stroke="#e2e8f0" />
                <XAxis dataKey="applicationName" tick={{ fontSize: 11 }} />
                <YAxis allowDecimals={false} tick={{ fontSize: 11 }} />
                <Tooltip />
                <Legend />
                <Bar dataKey="approved" name="Approved" fill="#10b981" radius={[4, 4, 0, 0]} />
                <Bar dataKey="pending" name="Pending" fill="#f59e0b" radius={[4, 4, 0, 0]} />
                <Bar dataKey="rejected" name="Rejected" fill="#ef4444" radius={[4, 4, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          </div>
        </div>

        <div className="bg-white rounded-xl border border-slate-200 p-5">
          <h2 className="text-sm font-semibold text-slate-800 mb-4">Average Approval Time (Hours)</h2>
          <div className="h-64">
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={data?.approvalTimes ?? []}>
                <CartesianGrid strokeDasharray="3 3" stroke="#e2e8f0" />
                <XAxis dataKey="stage" tick={{ fontSize: 12 }} />
                <YAxis tick={{ fontSize: 11 }} />
                <Tooltip />
                <Bar dataKey="avgHours" name="Avg Hours" fill="#6366f1" radius={[4, 4, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          </div>
        </div>
      </div>

      {/* Expiring Soon Table */}
      <div className="bg-white rounded-xl border border-slate-200 p-5">
        <h2 className="text-sm font-semibold text-slate-800 mb-3">Access Expiring Within 7 Days</h2>
        {(!data?.expiringSoon || data.expiringSoon.length === 0) ? (
          <p className="text-sm text-slate-500">No access grants expiring in the next 7 days.</p>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm text-left">
              <thead className="text-xs text-slate-500 uppercase border-b border-slate-200">
                <tr>
                  <th className="py-2 pr-4">User</th>
                  <th className="py-2 pr-4">Application</th>
                  <th className="py-2 pr-4">Level</th>
                  <th className="py-2">Expires At</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {data.expiringSoon.map((item: any) => (
                  <tr key={item.id}>
                    <td className="py-2.5 pr-4 font-medium">{item.requesterName}</td>
                    <td className="py-2.5 pr-4">{item.applicationName}</td>
                    <td className="py-2.5 pr-4">{item.level}</td>
                    <td className="py-2.5 text-orange-600 font-medium">
                      {new Date(item.expiresAt).toLocaleDateString()}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
};
