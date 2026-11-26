import React, { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '../api/client';

export const ApplicationsPage: React.FC = () => {
  const queryClient = useQueryClient();
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [ownerId, setOwnerId] = useState('3');

  const [joinerEmail, setJoinerEmail] = useState('');
  const [joinerFirst, setJoinerFirst] = useState('');
  const [joinerLast, setJoinerLast] = useState('');
  const [jmlMsg, setJmlMsg] = useState('');

  const { data: apps } = useQuery({
    queryKey: ['applications'],
    queryFn: async () => (await apiClient.get('/api/applications')).data,
  });

  const { data: users } = useQuery({
    queryKey: ['jml-users'],
    queryFn: async () => (await apiClient.get('/api/jml/users')).data,
  });

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
      queryClient.invalidateQueries({ queryKey: ['applications'] });
    },
  });

  const joinerMutation = useMutation({
    mutationFn: () =>
      apiClient.post('/api/jml/joiner', {
        firstName: joinerFirst,
        lastName: joinerLast,
        email: joinerEmail,
        managerId: 2,
        role: 'Employee',
      }),
    onSuccess: (res) => {
      setJmlMsg(res.data.message);
      setJoinerEmail('');
      setJoinerFirst('');
      setJoinerLast('');
      queryClient.invalidateQueries({ queryKey: ['jml-users'] });
    },
  });

  const leaverMutation = useMutation({
    mutationFn: (userId: number) => apiClient.post('/api/jml/leaver', { userId }),
    onSuccess: (res) => {
      setJmlMsg(res.data.message);
      queryClient.invalidateQueries({ queryKey: ['jml-users'] });
    },
  });

  return (
    <div className="space-y-8">
      <div>
        <h1 className="text-2xl font-bold text-slate-900">Applications, SoD &amp; JML</h1>
        <p className="text-sm text-slate-500">Manage company applications, Separation of Duties rules, and HR lifecycle events.</p>
      </div>

      {/* Applications Catalog */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div className="lg:col-span-2 bg-white rounded-xl border border-slate-200 p-5">
          <h2 className="text-sm font-semibold text-slate-800 mb-3">Application Catalog</h2>
          <table className="w-full text-sm text-left">
            <thead className="text-xs text-slate-500 uppercase border-b border-slate-200">
              <tr>
                <th className="py-2 pr-4">Name</th>
                <th className="py-2 pr-4">Owner</th>
                <th className="py-2">Description</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {(apps ?? []).map((a: any) => (
                <tr key={a.id}>
                  <td className="py-2.5 pr-4 font-medium">{a.name}</td>
                  <td className="py-2.5 pr-4 text-slate-600">{a.ownerName}</td>
                  <td className="py-2.5 text-slate-500 text-xs">{a.description}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        <form
          onSubmit={(e) => {
            e.preventDefault();
            createAppMutation.mutate();
          }}
          className="bg-white rounded-xl border border-slate-200 p-5 space-y-3"
        >
          <h2 className="text-sm font-semibold text-slate-800">Add Application</h2>
          <input
            required
            value={name}
            onChange={(e) => setName(e.target.value)}
            placeholder="Application name"
            className="w-full px-3 py-2 border border-slate-300 rounded-lg text-sm"
          />
          <textarea
            rows={2}
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            placeholder="Description"
            className="w-full px-3 py-2 border border-slate-300 rounded-lg text-sm"
          />
          <select
            value={ownerId}
            onChange={(e) => setOwnerId(e.target.value)}
            className="w-full px-3 py-2 border border-slate-300 rounded-lg text-sm bg-white"
          >
            {(users ?? []).filter((u: any) => u.isActive).map((u: any) => (
              <option key={u.id} value={u.id}>
                Owner: {u.fullName}
              </option>
            ))}
          </select>
          <button
            type="submit"
            disabled={createAppMutation.isPending}
            className="w-full py-2 bg-indigo-600 hover:bg-indigo-700 text-white text-xs font-medium rounded-lg"
          >
            Create Application
          </button>
        </form>
      </div>

      {/* SoD Conflict Rules */}
      <div className="bg-white rounded-xl border border-slate-200 p-5">
        <h2 className="text-sm font-semibold text-slate-800 mb-3">Separation of Duties (SoD) Rules</h2>
        <div className="space-y-2">
          {(sodRules ?? []).map((r: any) => (
            <div key={r.id} className="p-3 rounded-lg bg-slate-50 border border-slate-200 flex items-center justify-between text-xs">
              <div>
                <span className="font-semibold">{r.firstApplicationName} ({r.firstLevel})</span>
                <span className="mx-2 text-slate-400">↔</span>
                <span className="font-semibold">{r.secondApplicationName} ({r.secondLevel})</span>
                <p className="text-slate-500 mt-0.5">{r.description}</p>
              </div>
              <span className={`px-2 py-0.5 rounded font-medium ${r.isBlocking ? 'bg-rose-100 text-rose-700' : 'bg-amber-100 text-amber-800'}`}>
                {r.isBlocking ? 'Blocking' : 'Flag for Review'}
              </span>
            </div>
          ))}
        </div>
      </div>

      {/* JML Simulation */}
      <div className="bg-white rounded-xl border border-slate-200 p-5 space-y-4">
        <h2 className="text-sm font-semibold text-slate-800">Joiner / Mover / Leaver (JML) Simulator</h2>
        {jmlMsg && <div className="p-2.5 rounded-lg bg-emerald-50 text-emerald-700 text-xs">{jmlMsg}</div>}

        <form
          onSubmit={(e) => {
            e.preventDefault();
            joinerMutation.mutate();
          }}
          className="grid grid-cols-1 sm:grid-cols-4 gap-2"
        >
          <input required value={joinerFirst} onChange={(e) => setJoinerFirst(e.target.value)} placeholder="First name" className="px-3 py-2 border border-slate-300 rounded-lg text-xs" />
          <input required value={joinerLast} onChange={(e) => setJoinerLast(e.target.value)} placeholder="Last name" className="px-3 py-2 border border-slate-300 rounded-lg text-xs" />
          <input required type="email" value={joinerEmail} onChange={(e) => setJoinerEmail(e.target.value)} placeholder="Email" className="px-3 py-2 border border-slate-300 rounded-lg text-xs" />
          <button type="submit" className="px-3 py-2 bg-indigo-600 text-white text-xs font-medium rounded-lg">
            Simulate Joiner
          </button>
        </form>

        <div className="overflow-x-auto">
          <table className="w-full text-xs text-left">
            <thead className="text-slate-500 uppercase border-b border-slate-200">
              <tr>
                <th className="py-2 pr-4">Employee</th>
                <th className="py-2 pr-4">Manager</th>
                <th className="py-2 pr-4">Roles</th>
                <th className="py-2 pr-4">Status</th>
                <th className="py-2 text-right">Action</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {(users ?? []).map((u: any) => (
                <tr key={u.id}>
                  <td className="py-2 pr-4 font-medium">{u.fullName} ({u.email})</td>
                  <td className="py-2 pr-4 text-slate-500">{u.managerName ?? '—'}</td>
                  <td className="py-2 pr-4">{(u.roles ?? []).join(', ')}</td>
                  <td className="py-2 pr-4">
                    <span className={`px-2 py-0.5 rounded-full ${u.isActive ? 'bg-emerald-100 text-emerald-700' : 'bg-rose-100 text-rose-700'}`}>
                      {u.isActive ? 'Active' : 'Deactivated'}
                    </span>
                  </td>
                  <td className="py-2 text-right">
                    {u.isActive && !u.roles.includes('SecurityAdmin') && (
                      <button
                        onClick={() => leaverMutation.mutate(u.id)}
                        className="text-rose-600 hover:text-rose-800 font-medium"
                      >
                        Trigger Leaver
                      </button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
};
