import React from 'react';
import { Link } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '../api/client';
import { StatusBadge } from '../components/StatusBadge';

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

  if (isLoading) return <div className="text-sm text-slate-500">Loading your requests...</div>;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-slate-900">My Requests</h1>
          <p className="text-sm text-slate-500">Track the approval status of your submitted access requests.</p>
        </div>
        <Link
          to="/requests/new"
          className="px-4 py-2 bg-indigo-600 hover:bg-indigo-700 text-white text-sm font-medium rounded-lg"
        >
          + New Request
        </Link>
      </div>

      <div className="bg-white rounded-xl border border-slate-200 overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-sm text-left">
            <thead className="bg-slate-50 text-xs text-slate-500 uppercase border-b border-slate-200">
              <tr>
                <th className="py-3 px-4">Application</th>
                <th className="py-3 px-4">Level</th>
                <th className="py-3 px-4">Duration</th>
                <th className="py-3 px-4">Status</th>
                <th className="py-3 px-4">Submitted</th>
                <th className="py-3 px-4">Notes</th>
                <th className="py-3 px-4 text-right">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {requests.length === 0 ? (
                <tr>
                  <td colSpan={7} className="py-8 text-center text-slate-500">
                    You have not submitted any access requests yet.
                  </td>
                </tr>
              ) : (
                requests.map((r: any) => {
                  const canCancel = r.status === 'PendingManager' || r.status === 'PendingOwner' || r.status === 'FlaggedForReview';
                  return (
                    <tr key={r.id} className="hover:bg-slate-50">
                      <td className="py-3 px-4 font-medium text-slate-900">{r.applicationName}</td>
                      <td className="py-3 px-4">{r.requestedLevel}</td>
                      <td className="py-3 px-4">{r.durationDays} days</td>
                      <td className="py-3 px-4">
                        <StatusBadge status={r.status} />
                      </td>
                      <td className="py-3 px-4 text-slate-500">{new Date(r.createdAt).toLocaleDateString()}</td>
                      <td className="py-3 px-4 text-xs text-slate-600 max-w-xs truncate">
                        {r.rejectionReason ? `Rejected: ${r.rejectionReason}` : r.hasSodConflict ? 'SoD Conflict Flagged' : r.justification}
                      </td>
                      <td className="py-3 px-4 text-right">
                        {canCancel && (
                          <button
                            onClick={() => cancelMutation.mutate(r.id)}
                            className="text-xs font-medium text-rose-600 hover:text-rose-800"
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
