import React from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Bell, CheckCheck } from 'lucide-react';
import { apiClient } from '../api/client';
import { PageHeader } from '../components/PageHeader';
import { EmptyState, LoadingState } from '../components/EmptyState';
import { btnSecondary, cardClass } from '../components/ui';

export const NotificationsPage: React.FC = () => {
  const queryClient = useQueryClient();

  const { data, isLoading } = useQuery({
    queryKey: ['notifications'],
    queryFn: async () => (await apiClient.get('/api/notifications')).data,
  });

  const markReadMutation = useMutation({
    mutationFn: (id: number) => apiClient.post(`/api/notifications/${id}/read`),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['notifications'] });
      queryClient.invalidateQueries({ queryKey: ['unread-count'] });
    },
  });

  const markAllMutation = useMutation({
    mutationFn: () => apiClient.post('/api/notifications/read-all'),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['notifications'] });
      queryClient.invalidateQueries({ queryKey: ['unread-count'] });
    },
  });

  if (isLoading) return <LoadingState label="Loading notifications…" />;

  return (
    <div className="max-w-3xl space-y-6">
      <PageHeader
        eyebrow="Inbox"
        title="Notifications"
        description={`${data?.unreadCount ?? 0} unread`}
        actions={
          (data?.unreadCount ?? 0) > 0 ? (
            <button onClick={() => markAllMutation.mutate()} className={btnSecondary}>
              <CheckCheck className="w-4 h-4" />
              Mark all as read
            </button>
          ) : undefined
        }
      />

      {(data?.items ?? []).length === 0 ? (
        <EmptyState icon={Bell} title="Quiet for now" description="You have no notifications yet." />
      ) : (
        <div className={`${cardClass} divide-y divide-line`}>
          {data.items.map((n: any) => (
            <div key={n.id} className={`p-4 flex items-start justify-between gap-4 ${n.isRead ? 'bg-surface' : 'bg-accent-soft/50'}`}>
              <div className="flex items-start gap-3">
                <div className={`p-2 rounded-md mt-0.5 ${n.isRead ? 'bg-paper text-muted' : 'bg-accent-soft text-accent'}`}>
                  <Bell className="w-4 h-4" />
                </div>
                <div>
                  <p className="text-sm text-ink leading-relaxed">{n.message}</p>
                  <p className="text-xs text-muted mt-1">{new Date(n.createdAt).toLocaleString()}</p>
                </div>
              </div>
              {!n.isRead && (
                <button
                  onClick={() => markReadMutation.mutate(n.id)}
                  className="text-xs font-medium text-accent hover:underline shrink-0"
                >
                  Mark read
                </button>
              )}
            </div>
          ))}
        </div>
      )}
    </div>
  );
};
