import React, { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { apiClient } from '../api/client';
import { PageHeader } from '../components/PageHeader';
import { btnSecondary, cardClass, fieldClass, tableHead, tdClass, thClass } from '../components/ui';

function formatAction(action: string) {
  return action.replace(/([a-z])([A-Z])/g, '$1 $2').replace(/\./g, ' · ');
}

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
      <PageHeader
        eyebrow="Evidence"
        title="Audit log"
        description="Append-only record of security-relevant actions, searchable by action and entity."
      />

      <div className={`${cardClass} grid grid-cols-1 gap-3 p-4 sm:grid-cols-[minmax(0,1fr)_14rem]`}>
        <label className="min-w-0">
          <span className="mb-1.5 block text-xs font-medium text-ink-soft">Action contains</span>
          <input
            value={action}
            onChange={(e) => {
              setAction(e.target.value);
              setPage(1);
            }}
            placeholder="Approval, JML, expiry…"
            className={fieldClass}
          />
        </label>
        <label>
          <span className="mb-1.5 block text-xs font-medium text-ink-soft">Entity type</span>
          <select
            value={entityType}
            onChange={(e) => {
              setEntityType(e.target.value);
              setPage(1);
            }}
            className={fieldClass}
          >
            <option value="">All entity types</option>
            <option value="AccessRequest">AccessRequest</option>
            <option value="Application">Application</option>
            <option value="ApplicationUser">ApplicationUser</option>
            <option value="AccessReview">AccessReview</option>
            <option value="SodConflict">SodConflict</option>
          </select>
        </label>
      </div>

      <div className={`${cardClass} overflow-hidden`}>
        {isLoading ? (
          <div className="p-6 text-sm text-muted">Loading audit logs…</div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full min-w-[1000px] table-fixed text-left text-[12px]">
              <colgroup>
                <col style={{ width: 145 }} />
                <col style={{ width: 110 }} />
                <col style={{ width: 190 }} />
                <col style={{ width: 140 }} />
                <col style={{ width: 130 }} />
                <col />
              </colgroup>
              <thead className={tableHead}>
                <tr>
                  <th className={thClass}>Timestamp</th>
                  <th className={thClass}>Actor</th>
                  <th className={thClass}>Action</th>
                  <th className={thClass}>Entity</th>
                  <th className={thClass}>Old → New</th>
                  <th className={thClass}>Details</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-line">
                {(data?.items ?? []).map((log: any) => (
                  <tr key={log.id} className="hover:bg-paper/60">
                    <td className={`${tdClass} text-muted whitespace-nowrap text-[11px]`}>
                      {new Date(log.timestamp).toLocaleString()}
                    </td>
                    <td className={`${tdClass} font-medium text-ink`}>{log.actorName}</td>
                    <td className={`${tdClass} text-xs font-medium leading-relaxed text-accent`}>{formatAction(log.action)}</td>
                    <td className={`${tdClass} text-ink-soft`}>
                      {log.entityType} #{log.entityId}
                    </td>
                    <td className={`${tdClass} text-muted text-[11px] break-words`}>
                      {log.oldValues || log.newValues ? `${log.oldValues ?? '—'} → ${log.newValues ?? '—'}` : '—'}
                    </td>
                    <td className={`${tdClass} text-muted text-[11px] leading-relaxed break-words`}>{log.details ?? '—'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {data && data.totalCount > 25 && (
        <div className="flex items-center justify-between text-xs text-muted">
          <span>
            Page {page} · {data.totalCount} events
          </span>
          <div className="flex gap-2">
            <button disabled={page <= 1} onClick={() => setPage((p) => p - 1)} className={btnSecondary}>
              Previous
            </button>
            <button disabled={page * 25 >= data.totalCount} onClick={() => setPage((p) => p + 1)} className={btnSecondary}>
              Next
            </button>
          </div>
        </div>
      )}
    </div>
  );
};
