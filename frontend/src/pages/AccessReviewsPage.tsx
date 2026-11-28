import React, { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '../api/client';
import { useAuth } from '../auth/AuthContext';
import { PageHeader } from '../components/PageHeader';
import { btnApprove, btnPrimary, btnReject, cardClass, fieldClass, tableHead, tdClass, thClass } from '../components/ui';

export const AccessReviewsPage: React.FC = () => {
  const { hasRole } = useAuth();
  const isAdmin = hasRole('SecurityAdmin');
  const queryClient = useQueryClient();

  const [campaignName, setCampaignName] = useState('');
  const [dueInDays, setDueInDays] = useState(14);
  const [campaignError, setCampaignError] = useState('');
  const [campaignSuccess, setCampaignSuccess] = useState('');

  const { data: pendingItems } = useQuery({
    queryKey: ['my-review-items'],
    queryFn: async () => (await apiClient.get('/api/reviews/my-pending')).data,
  });

  const { data: campaigns } = useQuery({
    queryKey: ['review-campaigns'],
    queryFn: async () => (await apiClient.get('/api/reviews')).data,
    enabled: isAdmin,
  });

  const startMutation = useMutation({
    mutationFn: () => apiClient.post('/api/reviews', { name: campaignName, dueInDays }),
    onSuccess: () => {
      setCampaignName('');
      setCampaignError('');
      setCampaignSuccess('Review campaign launched.');
      queryClient.invalidateQueries({ queryKey: ['review-campaigns'] });
      queryClient.invalidateQueries({ queryKey: ['my-review-items'] });
    },
    onError: () => {
      setCampaignSuccess('');
      setCampaignError('The review campaign could not be launched. Check the details and try again.');
    },
  });

  const decideMutation = useMutation({
    mutationFn: ({ itemId, confirm }: { itemId: number; confirm: boolean }) =>
      apiClient.post(`/api/reviews/items/${itemId}/decide`, {
        confirm,
        note: confirm ? 'Confirmed' : 'Revoked during review',
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['my-review-items'] });
      queryClient.invalidateQueries({ queryKey: ['review-campaigns'] });
    },
  });

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Certification"
        title="Access reviews"
        description="Periodic certification of active user access to enforce least privilege."
      />

      {isAdmin && (
        <div className={`${cardClass} p-5 space-y-4`}>
          <h2 className="text-sm font-medium text-ink">Launch a campaign</h2>
          {campaignError && <p role="alert" className="rounded-md border border-rose-200 bg-rose-50 px-3 py-2 text-xs text-danger">{campaignError}</p>}
          {campaignSuccess && <p role="status" className="rounded-md border border-emerald-200 bg-emerald-50 px-3 py-2 text-xs text-ok">{campaignSuccess}</p>}
          <form
            onSubmit={(e) => {
              e.preventDefault();
              startMutation.mutate();
            }}
            className="grid grid-cols-1 gap-3 sm:grid-cols-[minmax(0,1fr)_7rem_auto]"
          >
            <div className="min-w-0">
              <label htmlFor="campaign-name" className="mb-1.5 block text-xs font-medium text-ink-soft">Campaign name</label>
              <input
                id="campaign-name"
                required
                value={campaignName}
                onChange={(e) => setCampaignName(e.target.value)}
                placeholder="e.g. Q4 quarterly review"
                className={fieldClass}
              />
            </div>
            <div>
              <label htmlFor="campaign-due-days" className="mb-1.5 block text-xs font-medium text-ink-soft">Due in days</label>
              <input
                id="campaign-due-days"
                type="number"
                min={1}
                max={90}
                value={dueInDays}
                onChange={(e) => setDueInDays(Number(e.target.value))}
                className={fieldClass}
              />
            </div>
            <button type="submit" disabled={startMutation.isPending} className={`${btnPrimary} w-full whitespace-nowrap sm:mt-auto sm:w-auto`}>
              {startMutation.isPending ? 'Launching…' : 'Launch campaign'}
            </button>
          </form>

          {(campaigns ?? []).length > 0 && (
            <div className="overflow-x-auto pt-2">
              <table className="w-full text-sm text-left">
                <thead className={tableHead}>
                  <tr>
                    <th className={thClass}>Campaign</th>
                    <th className={thClass}>Due date</th>
                    <th className={thClass}>Progress</th>
                    <th className={thClass}>Status</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-line">
                  {campaigns.map((c: any) => (
                    <tr key={c.id} className="hover:bg-paper/60">
                      <td className={`${tdClass} font-medium text-ink`}>{c.name}</td>
                      <td className={`${tdClass} text-ink-soft`}>{new Date(c.dueDate).toLocaleDateString()}</td>
                      <td className={`${tdClass} text-ink-soft`}>
                        {c.confirmedItems + c.revokedItems} / {c.totalItems} decided
                      </td>
                      <td className={tdClass}>
                        <span
                          className={`px-2 py-0.5 rounded-md text-[11px] font-medium ${
                            c.isCompleted
                              ? 'bg-emerald-50 text-ok ring-1 ring-emerald-200'
                              : c.isOverdue
                                ? 'bg-rose-50 text-danger ring-1 ring-rose-200'
                                : 'bg-amber-50 text-warn ring-1 ring-amber-200'
                          }`}
                        >
                          {c.isCompleted ? 'Completed' : c.isOverdue ? 'Overdue' : 'In progress'}
                        </span>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      )}

      <div className={`${cardClass} p-5`}>
        <h2 className="text-sm font-medium text-ink mb-3">Assigned to me</h2>
        {!pendingItems || pendingItems.length === 0 ? (
          <p className="text-sm text-muted">No pending access review items assigned to you.</p>
        ) : (
          <div className="space-y-3">
            {pendingItems.map((item: any) => (
              <div key={item.id} className="p-4 rounded-md border border-line flex flex-col sm:flex-row sm:items-center justify-between gap-3">
                <div>
                  <div className="text-sm font-medium text-ink">
                    {item.employeeName} — {item.applicationName} ({item.requestedLevel})
                  </div>
                  <div className="text-xs text-muted mt-1">
                    {item.campaignName} · due {new Date(item.dueDate).toLocaleDateString()}
                    {item.isOverdue && <span className="ml-2 text-danger font-medium">Overdue</span>}
                  </div>
                </div>
                <div className="flex gap-2">
                  <button onClick={() => decideMutation.mutate({ itemId: item.id, confirm: true })} className={btnApprove}>
                    Confirm access
                  </button>
                  <button onClick={() => decideMutation.mutate({ itemId: item.id, confirm: false })} className={btnReject}>
                    Revoke access
                  </button>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
};
