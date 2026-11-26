import React, { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '../api/client';
import { useAuth } from '../auth/AuthContext';

export const AccessReviewsPage: React.FC = () => {
  const { hasRole } = useAuth();
  const isAdmin = hasRole('SecurityAdmin');
  const queryClient = useQueryClient();

  const [campaignName, setCampaignName] = useState('');
  const [dueInDays, setDueInDays] = useState(14);

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
      queryClient.invalidateQueries({ queryKey: ['review-campaigns'] });
      queryClient.invalidateQueries({ queryKey: ['my-review-items'] });
    },
  });

  const decideMutation = useMutation({
    mutationFn: ({ itemId, confirm }: { itemId: number; confirm: boolean }) =>
      apiClient.post(`/api/reviews/items/${itemId}/decide`, { confirm, note: confirm ? 'Confirmed' : 'Revoked during review' }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['my-review-items'] });
      queryClient.invalidateQueries({ queryKey: ['review-campaigns'] });
    },
  });

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-slate-900">Access Reviews</h1>
        <p className="text-sm text-slate-500">Periodic certification of active user access to enforce least privilege.</p>
      </div>

      {isAdmin && (
        <div className="bg-white rounded-xl border border-slate-200 p-5 space-y-4">
          <h2 className="text-sm font-semibold text-slate-800">Start Review Campaign (SecurityAdmin)</h2>
          <form
            onSubmit={(e) => {
              e.preventDefault();
              startMutation.mutate();
            }}
            className="flex flex-col sm:flex-row gap-3"
          >
            <input
              required
              value={campaignName}
              onChange={(e) => setCampaignName(e.target.value)}
              placeholder="Campaign name (e.g., Q4 Quarterly Access Review)"
              className="flex-1 px-3.5 py-2 border border-slate-300 rounded-lg text-sm"
            />
            <input
              type="number"
              min={1}
              max={90}
              value={dueInDays}
              onChange={(e) => setDueInDays(Number(e.target.value))}
              className="w-28 px-3 py-2 border border-slate-300 rounded-lg text-sm"
            />
            <button
              type="submit"
              disabled={startMutation.isPending}
              className="px-4 py-2 bg-indigo-600 hover:bg-indigo-700 text-white text-sm font-medium rounded-lg"
            >
              Launch Campaign
            </button>
          </form>

          {(campaigns ?? []).length > 0 && (
            <div className="overflow-x-auto pt-2">
              <table className="w-full text-xs text-left">
                <thead className="text-slate-500 uppercase border-b border-slate-200">
                  <tr>
                    <th className="py-2 pr-4">Campaign</th>
                    <th className="py-2 pr-4">Due Date</th>
                    <th className="py-2 pr-4">Progress</th>
                    <th className="py-2">Status</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {campaigns.map((c: any) => (
                    <tr key={c.id}>
                      <td className="py-2.5 pr-4 font-medium">{c.name}</td>
                      <td className="py-2.5 pr-4">{new Date(c.dueDate).toLocaleDateString()}</td>
                      <td className="py-2.5 pr-4">
                        {c.confirmedItems + c.revokedItems} / {c.totalItems} decided
                      </td>
                      <td className="py-2.5">
                        <span
                          className={`px-2 py-0.5 rounded-full font-medium ${
                            c.isCompleted
                              ? 'bg-emerald-100 text-emerald-700'
                              : c.isOverdue
                              ? 'bg-rose-100 text-rose-700'
                              : 'bg-amber-100 text-amber-800'
                          }`}
                        >
                          {c.isCompleted ? 'Completed' : c.isOverdue ? 'Overdue' : 'In Progress'}
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

      {/* Manager Pending Items */}
      <div className="bg-white rounded-xl border border-slate-200 p-5">
        <h2 className="text-sm font-semibold text-slate-800 mb-3">Pending Reviews Assigned to Me</h2>
        {(!pendingItems || pendingItems.length === 0) ? (
          <p className="text-sm text-slate-500">No pending access review items assigned to you.</p>
        ) : (
          <div className="space-y-3">
            {pendingItems.map((item: any) => (
              <div key={item.id} className="p-4 rounded-lg border border-slate-200 flex flex-col sm:flex-row sm:items-center justify-between gap-3">
                <div>
                  <div className="text-sm font-semibold text-slate-900">
                    {item.employeeName} — {item.applicationName} ({item.requestedLevel})
                  </div>
                  <div className="text-xs text-slate-500">
                    Campaign: {item.campaignName} · Due {new Date(item.dueDate).toLocaleDateString()}
                    {item.isOverdue && <span className="ml-2 text-rose-600 font-semibold">OVERDUE</span>}
                  </div>
                </div>
                <div className="flex gap-2">
                  <button
                    onClick={() => decideMutation.mutate({ itemId: item.id, confirm: true })}
                    className="px-3 py-1.5 bg-emerald-600 hover:bg-emerald-700 text-white text-xs font-medium rounded-lg"
                  >
                    Confirm Access
                  </button>
                  <button
                    onClick={() => decideMutation.mutate({ itemId: item.id, confirm: false })}
                    className="px-3 py-1.5 bg-rose-50 hover:bg-rose-100 text-rose-700 border border-rose-200 text-xs font-medium rounded-lg"
                  >
                    Revoke Access
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
