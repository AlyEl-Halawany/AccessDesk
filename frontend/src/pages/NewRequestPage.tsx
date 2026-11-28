import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useQuery, useMutation } from '@tanstack/react-query';
import { apiClient } from '../api/client';
import { PageHeader } from '../components/PageHeader';
import { btnPrimary, btnSecondary, cardClass, fieldClass, textareaClass } from '../components/ui';

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
      <PageHeader
        eyebrow="Requests"
        title="New access request"
        description="Manager and application-owner approval are required before a time-bound grant is issued."
      />

      <form onSubmit={handleSubmit} className={`${cardClass} p-6 space-y-5`}>
        {error && (
          <div className="p-3 rounded-md bg-rose-50 border border-rose-200 text-danger text-sm">{error}</div>
        )}

        <div>
          <label className="block text-xs font-medium text-ink-soft mb-1.5">Application</label>
          <select required value={applicationId} onChange={(e) => setApplicationId(e.target.value)} className={fieldClass}>
            <option value="">Select an application…</option>
            {(apps ?? []).map((app: any) => (
              <option key={app.id} value={app.id}>
                {app.name} — owner {app.ownerName}
              </option>
            ))}
          </select>
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <div>
            <label className="block text-xs font-medium text-ink-soft mb-1.5">Access level</label>
            <select value={requestedLevel} onChange={(e) => setRequestedLevel(e.target.value)} className={fieldClass}>
              <option value="Read">Read</option>
              <option value="Write">Write</option>
              <option value="Admin">Admin</option>
            </select>
          </div>
          <div>
            <label className="block text-xs font-medium text-ink-soft mb-1.5">Duration (days)</label>
            <input
              type="number"
              min={1}
              max={365}
              required
              value={durationDays}
              onChange={(e) => setDurationDays(Number(e.target.value))}
              className={fieldClass}
            />
          </div>
        </div>

        <div>
          <label className="block text-xs font-medium text-ink-soft mb-1.5">
            Business justification <span className="text-muted font-normal">(min 20 characters)</span>
          </label>
          <textarea
            rows={4}
            required
            minLength={20}
            value={justification}
            onChange={(e) => setJustification(e.target.value)}
            placeholder="Explain why you need this access and which work it supports…"
            className={textareaClass}
          />
        </div>

        <div className="flex justify-end gap-2 pt-1">
          <button type="button" onClick={() => navigate('/requests')} className={btnSecondary}>
            Cancel
          </button>
          <button type="submit" disabled={submitMutation.isPending} className={btnPrimary}>
            {submitMutation.isPending ? 'Submitting…' : 'Submit request'}
          </button>
        </div>
      </form>
    </div>
  );
};
