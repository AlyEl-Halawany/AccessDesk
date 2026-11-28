import React from 'react';
import { useQuery } from '@tanstack/react-query';
import { KeyRound, Clock } from 'lucide-react';
import { apiClient } from '../api/client';
import { PageHeader } from '../components/PageHeader';
import { EmptyState, LoadingState } from '../components/EmptyState';
import { cardClass } from '../components/ui';

export const MyAccessPage: React.FC = () => {
  const { data, isLoading } = useQuery({
    queryKey: ['my-access'],
    queryFn: async () => (await apiClient.get('/api/requests/my-access')).data,
  });
  const grants = data?.items ?? [];

  if (isLoading) return <LoadingState label="Loading active access…" />;

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Entitlements"
        title="My active access"
        description="Applications you currently hold, with automatic expiry dates."
      />

      {!grants || grants.length === 0 ? (
        <EmptyState
          icon={KeyRound}
          title="No active grants"
          description="You do not currently have approved application access."
        />
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-3">
          {grants.map((g: any) => {
            const expiresAt = g.expiresAt ? new Date(g.expiresAt) : null;
            const daysLeft = expiresAt
              ? Math.max(0, Math.ceil((expiresAt.getTime() - Date.now()) / 86400000))
              : null;
            return (
              <div key={g.id} className={`${cardClass} p-5 flex flex-col justify-between`}>
                <div>
                  <div className="flex items-center justify-between mb-3">
                    <div className="p-2 rounded-md bg-accent-soft text-accent">
                      <KeyRound className="w-4 h-4" />
                    </div>
                    <span className="px-2 py-0.5 rounded-md text-[11px] font-medium bg-paper text-ink-soft ring-1 ring-line">
                      {g.requestedLevel}
                    </span>
                  </div>
                  <h3 className="font-medium text-ink">{g.applicationName}</h3>
                  <p className="text-xs text-muted mt-1.5 line-clamp-2 leading-relaxed">{g.justification}</p>
                </div>
                <div className="mt-4 pt-3 border-t border-line flex items-center justify-between text-xs">
                  <span className="flex items-center gap-1.5 text-muted">
                    <Clock className="w-3.5 h-3.5" />
                    {expiresAt ? expiresAt.toLocaleDateString() : 'No expiry'}
                  </span>
                  {daysLeft !== null && (
                    <span className={`font-medium ${daysLeft <= 7 ? 'text-warn' : 'text-ok'}`}>{daysLeft}d left</span>
                  )}
                </div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
};
