import React from 'react';

const STATUS: Record<string, { label: string; className: string }> = {
  PendingManager: { label: 'Manager review', className: 'bg-amber-50 text-amber-800 ring-amber-200/80' },
  PendingOwner: { label: 'Owner review', className: 'bg-sky-50 text-sky-800 ring-sky-200/80' },
  Approved: { label: 'Approved', className: 'bg-emerald-50 text-ok ring-emerald-200/80' },
  Rejected: { label: 'Rejected', className: 'bg-rose-50 text-danger ring-rose-200/80' },
  Cancelled: { label: 'Cancelled', className: 'bg-paper text-ink-soft ring-line' },
  Expired: { label: 'Expired', className: 'bg-orange-50 text-orange-800 ring-orange-200/80' },
  FlaggedForReview: { label: 'Flagged', className: 'bg-violet-50 text-violet-800 ring-violet-200/80' },
};

export const StatusBadge: React.FC<{ status: string }> = ({ status }) => {
  const item = STATUS[status] ?? { label: status, className: 'bg-paper text-ink-soft ring-line' };
  return (
    <span className={`inline-flex items-center h-6 px-2 rounded-md text-[11px] font-medium tracking-wide ring-1 ${item.className}`}>
      {item.label}
    </span>
  );
};
