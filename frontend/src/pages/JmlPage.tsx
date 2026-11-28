import React, { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { UserRoundPlus, UserRoundX, RefreshCw } from 'lucide-react';
import { apiClient } from '../api/client';
import { PageHeader } from '../components/PageHeader';
import { btnPrimary, btnSecondary, cardClass, fieldClass, tableHead, tdClass, thClass } from '../components/ui';

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
    mutationFn: () =>
      apiClient.post('/api/jml/joiner', {
        firstName,
        lastName,
        email,
        managerId: managerId ? Number(managerId) : null,
        role: 'Employee',
        initialPassword: null,
      }),
    onSuccess: () => {
      setFirstName('');
      setLastName('');
      setEmail('');
      finishAction('Joiner account created and recorded in the audit log.');
    },
    onError: (failure) => {
      setSuccess('');
      setError(errorMessage(failure));
    },
  });

  const mover = useMutation({
    mutationFn: ({ userId, newManagerId }: { userId: number; newManagerId: number }) =>
      apiClient.post('/api/jml/mover', { userId, newManagerId }),
    onSuccess: () => finishAction('Manager assignment updated and the mover event was recorded.'),
    onError: (failure) => {
      setSuccess('');
      setError(errorMessage(failure));
    },
  });

  const leaver = useMutation({
    mutationFn: (userId: number) => apiClient.post('/api/jml/leaver', { userId }),
    onSuccess: () => finishAction('Leaver processed. Active access and refresh tokens were revoked.'),
    onError: (failure) => {
      setSuccess('');
      setError(errorMessage(failure));
    },
  });

  const activeManagers = users.filter((user) => user.isActive && user.roles.includes('Manager'));
  const busy = joiner.isPending || mover.isPending || leaver.isPending;

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="People"
        title="Joiner, mover, leaver"
        description="Manage employee lifecycle events. Each change is recorded in the audit log."
        actions={
          <button onClick={() => refetch()} className={btnSecondary}>
            <RefreshCw className="h-4 w-4" /> Refresh
          </button>
        }
      />

      {error && (
        <div role="alert" className="rounded-md border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-danger">
          {error}
        </div>
      )}
      {success && (
        <div role="status" className="rounded-md border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-ok">
          {success}
        </div>
      )}

      <section className={`${cardClass} p-5`}>
        <div className="mb-4 flex items-center gap-3">
          <div className="p-2 rounded-md bg-accent-soft text-accent">
            <UserRoundPlus className="h-4 w-4" />
          </div>
          <div>
            <h2 className="text-sm font-medium text-ink">Onboard an employee</h2>
            <p className="text-xs text-muted">Creates an Employee account and assigns a manager.</p>
          </div>
        </div>
        <form
          onSubmit={(event) => {
            event.preventDefault();
            joiner.mutate();
          }}
          className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-5"
        >
          <input required value={firstName} onChange={(event) => setFirstName(event.target.value)} placeholder="First name" className={fieldClass} />
          <input required value={lastName} onChange={(event) => setLastName(event.target.value)} placeholder="Last name" className={fieldClass} />
          <input required type="email" value={email} onChange={(event) => setEmail(event.target.value)} placeholder="Work email" className={fieldClass} />
          <select value={managerId} onChange={(event) => setManagerId(event.target.value)} className={fieldClass}>
            <option value="">No manager assigned</option>
            {activeManagers.map((manager) => (
              <option key={manager.id} value={manager.id}>
                {manager.fullName}
              </option>
            ))}
          </select>
          <button disabled={busy} className={btnPrimary}>
            <UserRoundPlus className="h-4 w-4" /> Create joiner
          </button>
        </form>
        <p className="mt-3 text-xs text-muted">
          The development seeder uses demo credentials. New users should receive a real onboarding credential flow before production use.
        </p>
      </section>

      <section className={`${cardClass} overflow-hidden`}>
        <div className="border-b border-line px-5 py-4">
          <h2 className="text-sm font-medium text-ink">Employee directory</h2>
          <p className="text-xs text-muted mt-0.5">
            Mover changes can trigger an access review. Leaver processing revokes active grants and refresh tokens.
          </p>
        </div>
        {isLoading ? (
          <div className="p-8 text-center text-sm text-muted">Loading employee directory…</div>
        ) : isError ? (
          <div className="p-8 text-center text-sm text-danger">Could not load users. Refresh and try again.</div>
        ) : users.length === 0 ? (
          <div className="p-8 text-center text-sm text-muted">No users found.</div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full min-w-[760px] text-left text-sm">
              <thead className={tableHead}>
                <tr>
                  <th className={thClass}>Employee</th>
                  <th className={thClass}>Manager</th>
                  <th className={thClass}>Roles</th>
                  <th className={thClass}>Status</th>
                  <th className={`${thClass} text-right`}>Lifecycle actions</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-line">
                {users.map((user) => (
                  <tr key={user.id} className="hover:bg-paper/60">
                    <td className={tdClass}>
                      <div className="font-medium text-ink">{user.fullName}</div>
                      <div className="text-xs text-muted">{user.email}</div>
                    </td>
                    <td className={`${tdClass} text-ink-soft`}>{user.managerName ?? 'Unassigned'}</td>
                    <td className={tdClass}>
                      <div className="flex flex-wrap gap-1">
                        {user.roles.map((role) => (
                          <span key={role} className="rounded-md bg-accent-soft px-2 py-0.5 text-[10px] font-medium text-accent">
                            {role}
                          </span>
                        ))}
                      </div>
                    </td>
                    <td className={tdClass}>
                      <span
                        className={`rounded-md px-2 py-0.5 text-[11px] font-medium ${
                          user.isActive ? 'bg-emerald-50 text-ok ring-1 ring-emerald-200' : 'bg-paper text-muted ring-1 ring-line'
                        }`}
                      >
                        {user.isActive ? 'Active' : 'Inactive'}
                      </span>
                    </td>
                    <td className={tdClass}>
                      <div className="flex justify-end gap-2">
                        {user.isActive && (
                          <select
                            aria-label={`New manager for ${user.fullName}`}
                            defaultValue=""
                            disabled={busy}
                            onChange={(event) => {
                              if (event.target.value) mover.mutate({ userId: user.id, newManagerId: Number(event.target.value) });
                              event.target.value = '';
                            }}
                            className="max-w-40 h-8 rounded-md border border-line bg-surface px-2 text-xs"
                          >
                            <option value="">Change manager…</option>
                            {activeManagers
                              .filter((manager) => manager.id !== user.id && manager.id !== user.managerId)
                              .map((manager) => (
                                <option key={manager.id} value={manager.id}>
                                  {manager.fullName}
                                </option>
                              ))}
                          </select>
                        )}
                        {user.isActive && (
                          <button
                            disabled={busy}
                            onClick={() => {
                              if (
                                window.confirm(
                                  `Process leaver for ${user.fullName}? Active access and refresh tokens will be revoked.`,
                                )
                              )
                                leaver.mutate(user.id);
                            }}
                            className="inline-flex items-center gap-1 h-8 rounded-md border border-rose-200 px-2 text-xs font-medium text-danger hover:bg-rose-50 disabled:opacity-50"
                          >
                            <UserRoundX className="h-3.5 w-3.5" /> Leaver
                          </button>
                        )}
                        {!user.isActive && <span className="text-xs text-muted">No actions</span>}
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
