import React, { useEffect, useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '../api/client';
import { PageHeader } from '../components/PageHeader';
import { btnPrimary, cardClass, fieldClass, tableHead, textareaClass, tdClass, thClass } from '../components/ui';

export const ApplicationsPage: React.FC = () => {
  const queryClient = useQueryClient();
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [ownerId, setOwnerId] = useState('');
  const [formError, setFormError] = useState('');
  const [formSuccess, setFormSuccess] = useState('');

  const { data: apps } = useQuery({
    queryKey: ['applications'],
    queryFn: async () => (await apiClient.get('/api/applications')).data,
  });

  const { data: users = [] } = useQuery({
    queryKey: ['jml-users'],
    queryFn: async () => (await apiClient.get('/api/jml/users')).data,
  });

  const owners = users.filter((user: any) => user.isActive && user.roles?.includes('AppOwner'));

  useEffect(() => {
    if (!ownerId && owners.length > 0) setOwnerId(String(owners[0].id));
  }, [ownerId, owners]);

  const { data: sodRules } = useQuery({
    queryKey: ['sod-conflicts'],
    queryFn: async () => (await apiClient.get('/api/sod-conflicts')).data,
  });

  const createAppMutation = useMutation({
    mutationFn: () =>
      apiClient.post('/api/applications', {
        name,
        description,
        ownerId: Number(ownerId),
      }),
    onSuccess: () => {
      setName('');
      setDescription('');
      setFormError('');
      setFormSuccess('Application added to the catalog.');
      queryClient.invalidateQueries({ queryKey: ['applications'] });
    },
    onError: () => {
      setFormSuccess('');
      setFormError('The application could not be created. Check the details and try again.');
    },
  });

  return (
    <div className="space-y-8">
      <PageHeader
        eyebrow="Catalog"
        title="Applications and SoD"
        description="Manage the application catalog and review separation-of-duties controls."
      />

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
        <div className={`lg:col-span-2 ${cardClass} overflow-hidden`}>
          <div className="px-5 py-4 border-b border-line">
            <h2 className="text-sm font-medium text-ink">Application catalog</h2>
          </div>
          <div className="overflow-x-auto">
            <table className="w-full text-sm text-left">
              <thead className={tableHead}>
                <tr>
                  <th className={thClass}>Name</th>
                  <th className={thClass}>Owner</th>
                  <th className={thClass}>Description</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-line">
                {(apps ?? []).map((a: any) => (
                  <tr key={a.id} className="hover:bg-paper/60">
                    <td className={`${tdClass} font-medium text-ink`}>{a.name}</td>
                    <td className={`${tdClass} text-ink-soft`}>{a.ownerName}</td>
                    <td className={`${tdClass} text-muted text-xs`}>{a.description}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>

        <form
          onSubmit={(e) => {
            e.preventDefault();
            createAppMutation.mutate();
          }}
          className={`${cardClass} p-5 space-y-3`}
        >
          <h2 className="text-sm font-medium text-ink">Add application</h2>
          {formError && <p role="alert" className="rounded-md border border-rose-200 bg-rose-50 px-3 py-2 text-xs text-danger">{formError}</p>}
          {formSuccess && <p role="status" className="rounded-md border border-emerald-200 bg-emerald-50 px-3 py-2 text-xs text-ok">{formSuccess}</p>}
          <input required value={name} onChange={(e) => setName(e.target.value)} placeholder="Application name" className={fieldClass} />
          <textarea rows={2} value={description} onChange={(e) => setDescription(e.target.value)} placeholder="Description" className={textareaClass} />
          <select required value={ownerId} onChange={(e) => setOwnerId(e.target.value)} className={fieldClass}>
            {!ownerId && <option value="">Select an active application owner</option>}
            {owners.map((owner: any) => (
              <option key={owner.id} value={owner.id}>{owner.fullName}</option>
            ))}
          </select>
          <button type="submit" disabled={createAppMutation.isPending || !ownerId} className={`${btnPrimary} w-full`}>
            {createAppMutation.isPending ? 'Adding application…' : 'Add application'}
          </button>
        </form>
      </div>

      <div className={`${cardClass} p-5`}>
        <h2 className="text-sm font-medium text-ink mb-3">Separation of duties</h2>
        <div className="space-y-2">
          {(sodRules ?? []).length === 0 ? (
            <p className="rounded-md bg-paper px-3 py-3 text-sm text-muted">No SoD rules are configured.</p>
          ) : (sodRules ?? []).map((r: any) => (
            <div key={r.id} className="p-3 rounded-md bg-paper border border-line flex items-center justify-between gap-3 text-sm">
              <div>
                <span className="font-medium text-ink">
                  {r.firstApplicationName} ({r.firstLevel})
                </span>
                <span className="mx-2 text-muted">↔</span>
                <span className="font-medium text-ink">
                  {r.secondApplicationName} ({r.secondLevel})
                </span>
                <p className="text-xs text-muted mt-1">{r.description}</p>
              </div>
              <span
                className={`shrink-0 px-2 py-0.5 rounded-md text-[11px] font-medium ${
                  r.isBlocking ? 'bg-rose-50 text-danger ring-1 ring-rose-200' : 'bg-amber-50 text-warn ring-1 ring-amber-200'
                }`}
              >
                {r.isBlocking ? 'Blocking' : 'Flag for review'}
              </span>
            </div>
          ))}
        </div>
      </div>

    </div>
  );
};
