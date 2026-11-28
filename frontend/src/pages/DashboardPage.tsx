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
} from 'recharts';
import { Clock, AlertTriangle, CheckCircle2, FileClock } from 'lucide-react';
import { apiClient } from '../api/client';
import { PageHeader } from '../components/PageHeader';
import { LoadingState } from '../components/EmptyState';
import { cardClass, tableHead, thClass, tdClass } from '../components/ui';

const tooltipStyle = {
  background: '#fffcf7',
  border: '1px solid #e3dfd6',
  borderRadius: 8,
  fontSize: 12,
  color: '#161815',
};

export const DashboardPage: React.FC = () => {
  const { data, isLoading } = useQuery({
    queryKey: ['dashboard'],
    queryFn: async () => (await apiClient.get('/api/dashboard')).data,
  });

  if (isLoading) return <LoadingState label="Loading dashboard metrics…" />;

  const cards = [
    { label: 'Pending approvals', value: data?.pendingApprovals ?? 0, icon: FileClock, tone: 'text-warn bg-amber-50' },
    { label: 'Expiring in 7 days', value: data?.expiringSoonCount ?? 0, icon: Clock, tone: 'text-orange-700 bg-orange-50' },
    { label: 'Overdue reviews', value: data?.overdueReviews ?? 0, icon: AlertTriangle, tone: 'text-danger bg-rose-50' },
    { label: 'My active access', value: data?.myActiveAccessCount ?? 0, icon: CheckCircle2, tone: 'text-ok bg-emerald-50' },
  ];

  return (
    <div className="space-y-8">
      <PageHeader
        eyebrow="Overview"
        title="Dashboard"
        description="Access requests, upcoming expirations, and governance metrics for your organization."
      />

      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3">
        {cards.map((c) => {
          const Icon = c.icon;
          return (
            <div key={c.label} className={`${cardClass} p-4 flex items-start gap-3`}>
              <div className={`p-2 rounded-md ${c.tone}`}>
                <Icon className="w-4 h-4" />
              </div>
              <div>
                <div className="font-serif text-2xl text-ink leading-none">{c.value}</div>
                <div className="mt-1.5 text-xs text-muted">{c.label}</div>
              </div>
            </div>
          );
        })}
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
        <div className={`${cardClass} p-5`}>
          <h2 className="text-sm font-medium text-ink">Requests by application</h2>
          <p className="text-xs text-muted mt-0.5 mb-4">Approved, pending, and rejected counts</p>
          <div className="h-64">
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={data?.requestsPerApp ?? []} barGap={2}>
                <CartesianGrid strokeDasharray="3 3" stroke="#e3dfd6" vertical={false} />
                <XAxis dataKey="applicationName" tick={{ fontSize: 11, fill: '#6b7166' }} axisLine={false} tickLine={false} />
                <YAxis allowDecimals={false} tick={{ fontSize: 11, fill: '#6b7166' }} axisLine={false} tickLine={false} />
                <Tooltip contentStyle={tooltipStyle} cursor={false} />
                <Bar dataKey="approved" name="Approved" fill="#067647" radius={[3, 3, 0, 0]} />
                <Bar dataKey="pending" name="Pending" fill="#9a6700" radius={[3, 3, 0, 0]} />
                <Bar dataKey="rejected" name="Rejected" fill="#b42318" radius={[3, 3, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          </div>
        </div>

        <div className={`${cardClass} p-5`}>
          <h2 className="text-sm font-medium text-ink">Average approval time</h2>
          <p className="text-xs text-muted mt-0.5 mb-4">Hours spent at each stage</p>
          <div className="h-64">
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={data?.approvalTimes ?? []}>
                <CartesianGrid strokeDasharray="3 3" stroke="#e3dfd6" vertical={false} />
                <XAxis dataKey="stage" tick={{ fontSize: 12, fill: '#6b7166' }} axisLine={false} tickLine={false} />
                <YAxis tick={{ fontSize: 11, fill: '#6b7166' }} axisLine={false} tickLine={false} />
                <Tooltip contentStyle={tooltipStyle} cursor={false} />
                <Bar dataKey="avgHours" name="Avg hours" fill="#0f6e67" radius={[3, 3, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          </div>
        </div>
      </div>

      <div className={`${cardClass} overflow-hidden`}>
        <div className="px-5 py-4 border-b border-line">
          <h2 className="text-sm font-medium text-ink">Access expiring within 7 days</h2>
        </div>
        {!data?.expiringSoon || data.expiringSoon.length === 0 ? (
          <p className="p-6 text-sm text-muted">No access grants expire in the next 7 days.</p>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm text-left">
              <thead className={tableHead}>
                <tr>
                  <th className={thClass}>User</th>
                  <th className={thClass}>Application</th>
                  <th className={thClass}>Level</th>
                  <th className={thClass}>Expires</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-line">
                {data.expiringSoon.map((item: any) => (
                  <tr key={item.id} className="hover:bg-paper/60">
                    <td className={`${tdClass} font-medium text-ink`}>{item.requesterName}</td>
                    <td className={`${tdClass} text-ink-soft`}>{item.applicationName}</td>
                    <td className={`${tdClass} text-ink-soft`}>{item.level}</td>
                    <td className={`${tdClass} text-warn font-medium`}>
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
