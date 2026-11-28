import React from 'react';
import { useQuery } from '@tanstack/react-query';
import { KeyRound, Clock } from 'lucide-react';
import { apiClient } from '../api/client';

export const MyAccessPage: React.FC = () => {
  const { data, isLoading } = useQuery({
    queryKey: ['my-access'],
    queryFn: async () => (await apiClient.get('/api/requests/my-access')).data,
  });
  const grants = data?.items ?? [];

  if (isLoading) return <div className="text-sm text-slate-500">Loading active access...</div>;

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-slate-900">My Active Access</h1>
        <p className="text-sm text-slate-500">Applications you currently have approved access to, with automatic expiry dates.</p>
      </div>

      {(!grants || grants.length === 0) ? (
        <div className="bg-white rounded-xl border border-slate-200 p-8 text-center text-sm text-slate-500">
          You do not have any active application access grants.
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {grants.map((g: any) => {
            const expiresAt = g.expiresAt ? new Date(g.expiresAt) : null;
            const daysLeft = expiresAt ? Math.max(0, Math.ceil((expiresAt.getTime() - Date.now()) / 86400000)) : null;
            return (
              <div key={g.id} className="bg-white rounded-xl border border-slate-200 p-5 flex flex-col justify-between">
                <div>
                  <div className="flex items-center justify-between mb-2">
                    <div className="p-2 rounded-lg bg-emerald-50 text-emerald-600">
                      <KeyRound className="w-4 h-4" />
                    </div>
                    <span className="px-2.5 py-0.5 rounded-full text-xs font-medium bg-indigo-50 text-indigo-700 border border-indigo-200">
                      {g.requestedLevel}
                    </span>
                  </div>
                  <h3 className="font-semibold text-slate-900">{g.applicationName}</h3>
                  <p className="text-xs text-slate-500 mt-1 line-clamp-2">{g.justification}</p>
                </div>

                <div className="mt-4 pt-3 border-t border-slate-100 flex items-center justify-between text-xs">
                  <span className="flex items-center gap-1 text-slate-500">
                    <Clock className="w-3.5 h-3.5" />
                    Expires: {expiresAt ? expiresAt.toLocaleDateString() : 'N/A'}
                  </span>
                  {daysLeft !== null && (
                    <span className={`font-semibold ${daysLeft <= 7 ? 'text-orange-600' : 'text-emerald-600'}`}>
                      {daysLeft}d left
                    </span>
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
