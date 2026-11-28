import React from 'react';
import { Link, Navigate } from 'react-router-dom';
import { ShieldAlert } from 'lucide-react';
import { useAuth } from '../auth/AuthContext';
import { btnSecondary, cardClass } from './ui';

interface Props {
  children: React.ReactNode;
  roles?: string[];
}

export const ProtectedRoute: React.FC<Props> = ({ children, roles }) => {
  const { user, hasRole } = useAuth();

  if (!user) {
    return <Navigate to="/login" replace />;
  }

  if (roles && roles.length > 0 && !hasRole(...roles)) {
    return (
      <main className="min-h-screen bg-paper px-5 py-12 grid place-items-center">
        <section className={`${cardClass} max-w-md p-8 text-center`}>
          <div className="mx-auto mb-4 flex h-11 w-11 items-center justify-center rounded-full bg-amber-50 text-warn">
            <ShieldAlert className="h-5 w-5" />
          </div>
          <p className="text-[10px] font-medium uppercase tracking-[0.14em] text-muted">Access restricted</p>
          <h1 className="mt-1 font-serif text-2xl text-ink">This page isn’t available to your role</h1>
          <p className="mt-2 text-sm leading-relaxed text-muted">
            This area is limited to {roles.join(' or ')}. Your current access has not been changed.
          </p>
          <Link to="/" className={`${btnSecondary} mt-6`}>
            Return to dashboard
          </Link>
        </section>
      </main>
    );
  }

  return <>{children}</>;
};
