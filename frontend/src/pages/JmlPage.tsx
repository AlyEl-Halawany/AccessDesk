import React, { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { UserRoundPlus, UserRoundX, RefreshCw } from 'lucide-react';
import { apiClient } from '../api/client';

type JmlUser = {
  id: number;
  fullName: string;
  email: string;
  managerId: number | null;
  managerName: string | null;
  isActive: boolean;
  roles: string[];
};

function errorMessage(error: any) {
  return error?.response?.data?.message ?? 'The lifecycle action could not be completed. Please try again.';
}

export const JmlPage: React.FC = () => {
  const queryClient = useQueryClient();
  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [email, setEmail] = useState('');
  const [managerId, setManagerId] = useState('');
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  const { data: users = [], isLoading, isError, refetch } = useQuery<JmlUser[]>({
    queryKey: ['jml-users'],
    queryFn: async () => (await apiClient.get('/api/jml/users')).data,
  });

  const finishAction = (message: string) => {
    setError('');
    setSuccess(message);
    queryClient.invalidateQueries({ queryKey: ['jml-users'] });
    queryClient.invalidateQueries({ queryKey: ['audit-logs'] });
  };

  const joiner = useMutation({
    mutationFn: () => apiClient.post('/api/jml/joiner', {
      firstName,
      lastName,
      email,
      managerId: managerId ? Number(managerId) : null,
      role: 'Employee',
      initialPassword: null,
    }),
    onSuccess: () => {
      setFirstName(''); setLastName(''); setEmail('');
      finishAction('Joiner account created and recorded in the audit log.');
    },
    onError: (failure) => { setSuccess(''); setError(errorMessage(failure)); },
  });

  const mover = useMutation({
    mutationFn: ({ userId, newManagerId }: { userId: number; newManagerId: number }) =>
      apiClient.post('/api/jml/mover', { userId, newManagerId }),
    onSuccess: () => finishAction('Manager assignment updated and the mover event was recorded.'),
    onError: (failure) => { setSuccess(''); setError(errorMessage(failure)); },
  });

  const leaver = useMutation({
    mutationFn: (userId: number) => apiClient.post('/api/jml/leaver', { userId }),
    onSuccess: () => finishAction('Leaver processed. Active access and refresh tokens were revoked.'),
    onError: (failure) => { setSuccess(''); setError(errorMessage(failure)); },
  });

  const activeManagers = users.filter((user) => user.isActive && user.roles.includes('Manager'));
  const busy = joiner.isPending || mover.isPending || leaver.isPending;

  return (
    <div className="space-y-6">
      <div className="flex items-start justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-slate-900">Joiner · Mover · Leaver</h1>
          <p className="mt-1 text-sm text-slate-500">Manage employee lifecycle events. Each change is recorded in the audit log.</p>
        </div>
        <button onClick={() => refetch()} className="inline-flex items-center gap-2 rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm text-slate-700 hover:bg-slate-50">
          <RefreshCw className="h-4 w-4" /> Refresh
        </button>
      </div>

      {error && <div role="alert" className="rounded-lg border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">{error}</div>}
      {success && <div role="status" className="rounded-lg border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-700">{success}</div>}

      <section className="rounded-xl border border-slate-200 bg-white p-5">
        <div className="mb-4 flex items-center gap-2">
          <UserRoundPlus className="h-5 w-5 text-indigo-600" />
          <div>
            <h2 className="font-semibold text-slate-900">Onboard an employee</h2>
            <p className="text-xs text-slate-500">Creates an Employee account and assigns its manager.</p>
          </div>
        </div>
        <form onSubmit={(event) => { event.preventDefault(); joiner.mutate(); }} className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-5">
          <input required value={firstName} onChange={(event) => setFirstName(event.target.value)} placeholder="First name" className="rounded-lg border border-slate-300 px-3 py-2 text-sm" />
          <input required value={lastName} onChange={(event) => setLastName(event.target.value)} placeholder="Last name" className="rounded-lg border border-slate-300 px-3 py-2 text-sm" />
          <input required type="email" value={email} onChange={(event) => setEmail(event.target.value)} placeholder="Work email" className="rounded-lg border border-slate-300 px-3 py-2 text-sm" />
          <select value={managerId} onChange={(event) => setManagerId(event.target.value)} className="rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm">
            <option value="">No manager assigned</option>
            {activeManagers.map((manager) => <option key={manager.id} value={manager.id}>{manager.fullName}</option>)}
          </select>
          <button disabled={busy} className="inline-flex items-center justify-center gap-2 rounded-lg bg-indigo-600 px-4 py-2 text-sm font-medium text-white hover:bg-indigo-700 disabled:opacity-50">
            <UserRoundPlus className="h-4 w-4" /> Create joiner
          </button>
        </form>
        <p className="mt-3 text-xs text-slate-400">The development seeder uses demo credentials. New users should receive a real onboarding credential flow before production use.</p>
      </section>

      <section className="overflow-hidden rounded-xl border border-slate-200 bg-white">
        <div className="border-b border-slate-200 px-5 py-4">
          <h2 className="font-semibold text-slate-900">Employee directory</h2>
          <p className="text-xs text-slate-500">Mover changes can trigger an access review. Leaver processing revokes active grants and refresh tokens.</p>
        </div>
        {isLoading ? <div className="p-8 text-center text-sm text-slate-500">Loading employee directory…</div> :
          isError ? <div className="p-8 text-center text-sm text-rose-600">Could not load users. Refresh and try again.</div> :
            users.length === 0 ? <div className="p-8 text-center text-sm text-slate-500">No users found.</div> : (
              <div className="overflow-x-auto">
                <table className="w-full min-w-[760px] text-left text-sm">
                  <thead className="bg-slate-50 text-xs uppercase text-slate-500">
                    <tr><th className="px-5 py-3">Employee</th><th className="px-5 py-3">Manager</th><th className="px-5 py-3">Roles</th><th className="px-5 py-3">Status</th><th className="px-5 py-3 text-right">Lifecycle actions</th></tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100">
                    {users.map((user) => (
                      <tr key={user.id} className="hover:bg-slate-50/70">
                        <td className="px-5 py-4"><div className="font-medium text-slate-900">{user.fullName}</div><div className="text-xs text-slate-500">{user.email}</div></td>
                        <td className="px-5 py-4 text-slate-600">{user.managerName ?? 'Unassigned'}</td>
                        <td className="px-5 py-4"><div className="flex flex-wrap gap-1">{user.roles.map((role) => <span key={role} className="rounded-full bg-indigo-50 px-2 py-0.5 text-[10px] font-medium text-indigo-700">{role}</span>)}</div></td>
                        <td className="px-5 py-4"><span className={`rounded-full px-2.5 py-1 text-xs font-medium ${user.isActive ? 'bg-emerald-50 text-emerald-700' : 'bg-slate-100 text-slate-500'}`}>{user.isActive ? 'Active' : 'Inactive'}</span></td>
                        <td className="px-5 py-4">
                          <div className="flex justify-end gap-2">
                            {user.isActive && <select aria-label={`New manager for ${user.fullName}`} defaultValue="" disabled={busy} onChange={(event) => { if (event.target.value) mover.mutate({ userId: user.id, newManagerId: Number(event.target.value) }); event.target.value = ''; }} className="max-w-40 rounded-md border border-slate-300 bg-white px-2 py-1.5 text-xs">
                              <option value="">Change manager…</option>
                              {activeManagers.filter((manager) => manager.id !== user.id && manager.id !== user.managerId).map((manager) => <option key={manager.id} value={manager.id}>{manager.fullName}</option>)}
                            </select>}
                            {user.isActive && <button disabled={busy} onClick={() => { if (window.confirm(`Process leaver for ${user.fullName}? Active access and refresh tokens will be revoked.`)) leaver.mutate(user.id); }} className="inline-flex items-center gap-1 rounded-md border border-rose-200 px-2 py-1.5 text-xs font-medium text-rose-700 hover:bg-rose-50 disabled:opacity-50">
                              <UserRoundX className="h-3.5 w-3.5" /> Leaver
                            </button>}
                            {!user.isActive && <span className="text-xs text-slate-400">No actions</span>}
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
      </section>
    </div>
  );
};
