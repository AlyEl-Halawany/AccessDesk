import React from 'react';

export const PageHeader: React.FC<{
  eyebrow?: string;
  title: string;
  description?: string;
  actions?: React.ReactNode;
}> = ({ eyebrow, title, description, actions }) => (
  <div className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
    <div className="max-w-2xl">
      {eyebrow && (
        <p className="text-[11px] font-medium tracking-[0.14em] uppercase text-muted mb-1.5">{eyebrow}</p>
      )}
      <h1 className="font-serif text-[1.75rem] leading-tight text-ink">{title}</h1>
      {description && <p className="mt-1.5 text-sm text-muted leading-relaxed">{description}</p>}
    </div>
    {actions && <div className="flex items-center gap-2 shrink-0">{actions}</div>}
  </div>
);
