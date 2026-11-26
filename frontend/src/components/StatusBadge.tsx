import React from 'react';

const STATUS_STYLES: Record<string, string> = {
  PendingManager: 'bg-amber-100 text-amber-800 border-amber-200',
  PendingOwner: 'bg-blue-100 text-blue-800 border-blue-200',
  Approved: 'bg-emerald-100 text-emerald-800 border-emerald-200',
  Rejected: 'bg-rose-100 text-rose-800 border-rose-200',
  Cancelled: 'bg-slate-100 text-slate-700 border-slate-200',
  Expired: 'bg-orange-100 text-orange-800 border-orange-200',
  FlaggedForReview: 'bg-purple-100 text-purple-800 border-purple-200',
};

export const StatusBadge: React.FC<{ status: string }> = ({ status }) => {
  const style = STATUS_STYLES[status] ?? 'bg-slate-100 text-slate-700 border-slate-200';
  return (
    <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium border ${style}`}>
      {status}
    </span>
  );
};
