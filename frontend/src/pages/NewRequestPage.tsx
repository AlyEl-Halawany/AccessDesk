import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useQuery, useMutation } from '@tanstack/react-query';
import { apiClient } from '../api/client';

export const NewRequestPage: React.FC = () => {
  const navigate = useNavigate();
  const [applicationId, setApplicationId] = useState('');
  const [requestedLevel, setRequestedLevel] = useState('Read');
  const [durationDays, setDurationDays] = useState(30);
  const [justification, setJustification] = useState('');
  const [error, setError] = useState('');

  const { data: apps } = useQuery({
    queryKey: ['applications'],
    queryFn: async () => (await apiClient.get('/api/applications')).data,
  });

  const submitMutation = useMutation({
    mutationFn: async () => {
      const res = await apiClient.post('/api/requests', {
        applicationId: Number(applicationId),
        requestedLevel,
        durationDays: Number(durationDays),
        justification,
      });
      return res.data;
    },
    onSuccess: () => navigate('/requests'),
    onError: (err: any) => {
      const msg = err.response?.data?.message || 'Failed to submit request.';
      const fieldErrors = err.response?.data?.errors;
      if (fieldErrors) {
        const first = Object.values(fieldErrors).flat()[0];
        setError(String(first));
      } else {
        setError(msg);
      }
    },
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    submitMutation.mutate();
  };

  return (
    <div className="max-w-2xl space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-slate-900">New Access Request</h1>
        <p className="text-sm text-slate-500">
          Requests require approval from your manager and the application owner before access is granted.
        </p>
      </div>

      <form onSubmit={handleSubmit} className="bg-white rounded-xl border border-slate-200 p-6 space-y-5">
        {error && (
          <div className="p-3 rounded-lg bg-rose-50 border border-rose-200 text-rose-700 text-sm">
            {error}
          </div>
        )}

        <div>
          <label className="block text-sm font-medium text-slate-700 mb-1">Application</label>
          <select
            required
            value={applicationId}
            onChange={(e) => setApplicationId(e.target.value)}
            className="w-full px-3.5 py-2 border border-slate-300 rounded-lg text-sm bg-white focus:ring-2 focus:ring-indigo-500 focus:outline-none"
          >
            <option value="">Select an application...</option>
            {(apps ?? []).map((app: any) => (
              <option key={app.id} value={app.id}>
                {app.name} (Owner: {app.ownerName})
              </option>
            ))}
          </select>
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <div>
            <label className="block text-sm font-medium text-slate-700 mb-1">Access Level</label>
            <select
              value={requestedLevel}
              onChange={(e) => setRequestedLevel(e.target.value)}
              className="w-full px-3.5 py-2 border border-slate-300 rounded-lg text-sm bg-white focus:ring-2 focus:ring-indigo-500 focus:outline-none"
            >
              <option value="Read">Read</option>
              <option value="Write">Write</option>
              <option value="Admin">Admin</option>
            </select>
          </div>

          <div>
            <label className="block text-sm font-medium text-slate-700 mb-1">Duration (Days)</label>
            <input
              type="number"
              min={1}
              max={365}
              required
              value={durationDays}
              onChange={(e) => setDurationDays(Number(e.target.value))}
              className="w-full px-3.5 py-2 border border-slate-300 rounded-lg text-sm focus:ring-2 focus:ring-indigo-500 focus:outline-none"
            />
          </div>
        </div>

        <div>
          <label className="block text-sm font-medium text-slate-700 mb-1">
            Business Justification <span className="text-slate-400 font-normal">(min 20 characters)</span>
          </label>
          <textarea
            rows={4}
            required
            minLength={20}
            value={justification}
            onChange={(e) => setJustification(e.target.value)}
            placeholder="Explain why you need access to this application and what tasks it supports..."
            className="w-full px-3.5 py-2 border border-slate-300 rounded-lg text-sm focus:ring-2 focus:ring-indigo-500 focus:outline-none"
          />
        </div>

        <div className="flex justify-end gap-3">
          <button
            type="button"
            onClick={() => navigate('/requests')}
            className="px-4 py-2 text-sm font-medium text-slate-700 bg-slate-100 hover:bg-slate-200 rounded-lg"
          >
            Cancel
          </button>
          <button
            type="submit"
            disabled={submitMutation.isPending}
            className="px-5 py-2 text-sm font-medium text-white bg-indigo-600 hover:bg-indigo-700 rounded-lg disabled:opacity-50"
          >
            {submitMutation.isPending ? 'Submitting...' : 'Submit Request'}
          </button>
        </div>
      </form>
    </div>
  );
};
