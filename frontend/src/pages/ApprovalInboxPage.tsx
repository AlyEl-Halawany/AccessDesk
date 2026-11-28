import React, { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Check, Inbox, X } from 'lucide-react';
import { apiClient } from '../api/client';
import { StatusBadge } from '../components/StatusBadge';
import { PageHeader } from '../components/PageHeader';
import { EmptyState, LoadingState } from '../components/EmptyState';
import { btnApprove, btnReject, btnSecondary, cardClass, textareaClass } from '../components/ui';

export const ApprovalInboxPage: React.FC = () => {
  const queryClient = useQueryClient();
  const [rejectingId, setRejectingId] = useState<number | null>(null);
  const [reason, setReason] = useState('');

  const { data: pending, isLoading } = useQuery({
    queryKey: ['pending-approvals'],
    queryFn: async () => (await apiClient.get('/api/requests/pending')).data,
  });
  const requests = pending?.items ?? [];

  const approveMutation = useMutation({
    mutationFn: (id: number) => apiClient.post(`/api/requests/${id}/approve`, {}),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['pending-approvals'] });
      queryClient.invalidateQueries({ queryKey: ['dashboard'] });
    },
  });

  const rejectMutation = useMutation({
    mutationFn: ({ id, reason }: { id: number; reason: string }) =>
      apiClient.post(`/api/requests/${id}/reject`, { reason }),
    onSuccess: () => {
      setRejectingId(null);
      setReason('');
      queryClient.invalidateQueries({ queryKey: ['pending-approvals'] });
      queryClient.invalidateQueries({ queryKey: ['dashboard'] });
    },
  });

  if (isLoading) return <LoadingState label="Loading approval inbox…" />;

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Workflow"
        title="Approval inbox"
        description="Decide on access requests waiting for your sign-off."
      />

      {requests.length === 0 ? (
        <EmptyState icon={Inbox} title="Inbox is clear" description="There are no requests awaiting your decision." />
      ) : (
        <div className="space-y-3">
          {requests.map((r: any) => (
            <div key={r.id} className={`${cardClass} p-5 flex flex-col sm:flex-row sm:items-start justify-between gap-4`}>
              <div className="space-y-2 min-w-0">
                <div className="flex flex-wrap items-center gap-2">
                  <span className="font-medium text-ink">{r.requesterName}</span>
                  <span className="text-muted">→</span>
                  <span className="font-medium text-accent">{r.applicationName}</span>
                  <span className="px-2 py-0.5 text-[11px] rounded-md bg-paper text-ink-soft ring-1 ring-line">
                    {r.requestedLevel} · {r.durationDays}d
                  </span>
                  <StatusBadge status={r.status} />
                </div>
                <p className="text-sm text-ink-soft leading-relaxed">{r.justification}</p>
                <p className="text-xs text-muted">Submitted {new Date(r.createdAt).toLocaleString()}</p>
              </div>
              <div className="flex items-center gap-2 shrink-0">
                <button
                  onClick={() => approveMutation.mutate(r.id)}
                  disabled={approveMutation.isPending}
                  className={btnApprove}
                >
                  <Check className="w-3.5 h-3.5" />
                  Approve
                </button>
                <button onClick={() => setRejectingId(r.id)} className={btnReject}>
                  <X className="w-3.5 h-3.5" />
                  Reject
                </button>
              </div>
            </div>
          ))}
        </div>
      )}

      {rejectingId !== null && (
        <div className="fixed inset-0 z-50 bg-ink/40 flex items-center justify-center p-4">
          <div className="bg-surface border border-line rounded-xl max-w-md w-full p-6 space-y-4 shadow-card">
            <h3 className="font-serif text-xl text-ink">Reject access request</h3>
            <p className="text-sm text-muted">A reason is required and is written to the audit log.</p>
            <textarea
              rows={3}
              value={reason}
              onChange={(e) => setReason(e.target.value)}
              placeholder="Explain why this request is being rejected…"
              className={textareaClass}
            />
            <div className="flex justify-end gap-2">
              <button
                onClick={() => {
                  setRejectingId(null);
                  setReason('');
                }}
                className={btnSecondary}
              >
                Cancel
              </button>
              <button
                disabled={!reason.trim() || rejectMutation.isPending}
                onClick={() => rejectMutation.mutate({ id: rejectingId, reason })}
                className="inline-flex items-center h-10 px-4 rounded-md bg-danger text-white text-sm font-medium disabled:opacity-50"
              >
                Confirm rejection
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
