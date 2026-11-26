import React from 'react';
import { Navigate } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';

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
      <div className="p-8 text-center">
        <h2 className="text-xl font-semibold text-slate-800">Access Denied</h2>
        <p className="mt-2 text-sm text-slate-600">
          You do not have the required role ({roles.join(' or ')}) to view this page.
        </p>
      </div>
    );
  }

  return <>{children}</>;
};
