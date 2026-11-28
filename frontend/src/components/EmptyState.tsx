import React from 'react';
import type { LucideIcon } from 'lucide-react';

export const EmptyState: React.FC<{
  icon?: LucideIcon;
  title: string;
  description?: string;
}> = ({ icon: Icon, title, description }) => (
  <div className="bg-surface border border-line rounded-xl px-6 py-14 text-center">
    {Icon && (
      <div className="mx-auto mb-3 flex h-10 w-10 items-center justify-center rounded-full bg-paper text-muted">
        <Icon className="h-4 w-4" />
      </div>
    )}
    <p className="text-sm font-medium text-ink">{title}</p>
    {description && <p className="mt-1 text-sm text-muted">{description}</p>}
  </div>
);

export const LoadingState: React.FC<{ label?: string }> = ({ label = 'Loading…' }) => (
  <div className="flex items-center gap-3 py-10 text-sm text-muted">
    <span className="h-4 w-4 rounded-full border-2 border-line border-t-accent animate-spin" />
    {label}
  </div>
);
