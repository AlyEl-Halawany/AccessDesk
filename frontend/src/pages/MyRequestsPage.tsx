import React from 'react';
import { Link } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '../api/client';
import { StatusBadge } from '../components/StatusBadge';
import { PageHeader } from '../components/PageHeader';
import { LoadingState } from '../components/EmptyState';
import { btnPrimary, cardClass, tableHead, thClass, tdClass } from '../components/ui';

export const MyRequestsPage: React.FC = () => {
  const queryClient = useQueryClient();

  const { data, isLoading } = useQuery({
    queryKey: ['my-requests'],
    queryFn: async () => (await apiClient.get('/api/requests/my')).data,
  });
  const requests = data?.items ?? [];

  const cancelMutation = useMutation({
    mutationFn: (id: number) => apiClient.post(`/api/requests/${id}/cancel`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['my-requests'] }),
  });

  if (isLoading) return <LoadingState label="Loading your requests…" />;

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Requests"
        title="My requests"
        description="Track approval status for access you have submitted."
        actions={
          <Link to="/requests/new" className={btnPrimary}>
            New request
          </Link>
        }
      />

      <div className={`${cardClass} overflow-hidden`}>
        <div className="overflow-x-auto">
          <table className="w-full text-sm text-left">
            <thead className={tableHead}>
              <tr>
                <th className={thClass}>Application</th>
                <th className={thClass}>Level</th>
                <th className={thClass}>Duration</th>
                <th className={thClass}>Status</th>
                <th className={thClass}>Submitted</th>
                <th className={thClass}>Notes</th>
                <th className={`${thClass} text-right`}>Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-line">
              {requests.length === 0 ? (
                <tr>
                  <td colSpan={7} className="py-12 text-center text-muted">
                    You have not submitted any access requests yet.
                  </td>
                </tr>
              ) : (
                requests.map((r: any) => {
                  const canCancel =
                    r.status === 'PendingManager' || r.status === 'PendingOwner' || r.status === 'FlaggedForReview';
                  return (
                    <tr key={r.id} className="hover:bg-paper/60">
                      <td className={`${tdClass} font-medium text-ink`}>{r.applicationName}</td>
                      <td className={`${tdClass} text-ink-soft`}>{r.requestedLevel}</td>
                      <td className={`${tdClass} text-ink-soft`}>{r.durationDays} days</td>
                      <td className={tdClass}>
                        <StatusBadge status={r.status} />
                      </td>
                      <td className={`${tdClass} text-muted`}>{new Date(r.createdAt).toLocaleDateString()}</td>
                      <td className={`${tdClass} text-xs text-ink-soft max-w-xs truncate`}>
                        {r.rejectionReason
                          ? `Rejected: ${r.rejectionReason}`
                          : r.hasSodConflict
                            ? 'SoD conflict flagged'
                            : r.justification}
                      </td>
                      <td className={`${tdClass} text-right`}>
                        {canCancel && (
                          <button
                            onClick={() => cancelMutation.mutate(r.id)}
                            className="text-xs font-medium text-danger hover:underline"
                          >
                            Cancel
                          </button>
                        )}
                      </td>
                    </tr>
                  );
                })
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
};
