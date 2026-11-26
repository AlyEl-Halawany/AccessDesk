import React, { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { apiClient } from '../api/client';

export const AuditLogPage: React.FC = () => {
  const [action, setAction] = useState('');
  const [entityType, setEntityType] = useState('');
  const [page, setPage] = useState(1);

  const { data, isLoading } = useQuery({
    queryKey: ['audit-logs', action, entityType, page],
    queryFn: async () => {
      const params = new URLSearchParams({ page: String(page), pageSize: '25' });
      if (action) params.set('action', action);
      if (entityType) params.set('entityType', entityType);
      return (await apiClient.get(`/api/audit-logs?${params.toString()}`)).data;
    },
  });

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-slate-900">Audit Log</h1>
        <p className="text-sm text-slate-500">Immutable, append-only record of all security-relevant actions.</p>
      </div>

      {/* Filters */}
      <div className="bg-white rounded-xl border border-slate-200 p-4 flex flex-wrap gap-3">
        <input
          value={action}
          onChange={(e) => {
            setAction(e.target.value);
            setPage(1);
          }}
          placeholder="Filter by action (e.g. Approved, JML)..."
          className="px-3 py-2 border border-slate-300 rounded-lg text-sm w-64"
        />
        <select
          value={entityType}
          onChange={(e) => {
            setEntityType(e.target.value);
            setPage(1);
          }}
          className="px-3 py-2 border border-slate-300 rounded-lg text-sm bg-white"
        >
          <option value="">All Entity Types</option>
          <option value="AccessRequest">AccessRequest</option>
          <option value="Application">Application</option>
          <option value="ApplicationUser">ApplicationUser</option>
          <option value="AccessReview">AccessReview</option>
          <option value="SodConflict">SodConflict</option>
        </select>
      </div>

      <div className="bg-white rounded-xl border border-slate-200 overflow-hidden">
        {isLoading ? (
          <div className="p-6 text-sm text-slate-500">Loading audit logs...</div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-xs text-left">
              <thead className="bg-slate-50 text-slate-500 uppercase border-b border-slate-200">
                <tr>
                  <th className="py-3 px-4">Timestamp</th>
                  <th className="py-3 px-4">Actor</th>
                  <th className="py-3 px-4">Action</th>
                  <th className="py-3 px-4">Entity</th>
                  <th className="py-3 px-4">Old → New</th>
                  <th className="py-3 px-4">Details</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {(data?.items ?? []).map((log: any) => (
                  <tr key={log.id} className="hover:bg-slate-50">
                    <td className="py-2.5 px-4 text-slate-500 whitespace-nowrap">
                      {new Date(log.timestamp).toLocaleString()}
                    </td>
                    <td className="py-2.5 px-4 font-medium">{log.actorName}</td>
                    <td className="py-2.5 px-4 font-mono text-indigo-700">{log.action}</td>
                    <td className="py-2.5 px-4">
                      {log.entityType} #{log.entityId}
                    </td>
                    <td className="py-2.5 px-4 text-slate-600">
                      {log.oldValues || log.newValues ? `${log.oldValues ?? '—'} → ${log.newValues ?? '—'}` : '—'}
                    </td>
                    <td className="py-2.5 px-4 text-slate-600 max-w-md truncate">{log.details ?? '—'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {data && data.totalCount > 25 && (
        <div className="flex items-center justify-between text-xs text-slate-600">
          <span>
            Page {page} · {data.totalCount} total events
          </span>
          <div className="flex gap-2">
            <button
              disabled={page <= 1}
              onClick={() => setPage((p) => p - 1)}
              className="px-3 py-1.5 border border-slate-300 rounded-lg disabled:opacity-50"
            >
              Previous
            </button>
            <button
              disabled={page * 25 >= data.totalCount}
              onClick={() => setPage((p) => p + 1)}
              className="px-3 py-1.5 border border-slate-300 rounded-lg disabled:opacity-50"
            >
              Next
            </button>
          </div>
        </div>
      )}
    </div>
  );
};
