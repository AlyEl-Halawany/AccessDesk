import React from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { AuthProvider } from './auth/AuthContext';
import { ProtectedRoute } from './components/ProtectedRoute';
import { Layout } from './components/Layout';
import { LoginPage } from './pages/LoginPage';
import { DashboardPage } from './pages/DashboardPage';
import { NewRequestPage } from './pages/NewRequestPage';
import { MyRequestsPage } from './pages/MyRequestsPage';
import { MyAccessPage } from './pages/MyAccessPage';
import { ApprovalInboxPage } from './pages/ApprovalInboxPage';
import { ApplicationsPage } from './pages/ApplicationsPage';
import { AccessReviewsPage } from './pages/AccessReviewsPage';
import { AuditLogPage } from './pages/AuditLogPage';
import { NotificationsPage } from './pages/NotificationsPage';
import { JmlPage } from './pages/JmlPage';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: 1,
      refetchOnWindowFocus: false,
    },
  },
});

const GuardedPage: React.FC<{ roles?: string[]; children: React.ReactNode }> = ({ roles, children }) => (
  <ProtectedRoute roles={roles}>
    <Layout>{children}</Layout>
  </ProtectedRoute>
);

export function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <BrowserRouter>
          <Routes>
            <Route path="/login" element={<LoginPage />} />
            <Route path="/" element={<GuardedPage><DashboardPage /></GuardedPage>} />
            <Route path="/requests/new" element={<GuardedPage><NewRequestPage /></GuardedPage>} />
            <Route path="/requests" element={<GuardedPage><MyRequestsPage /></GuardedPage>} />
            <Route path="/my-access" element={<GuardedPage><MyAccessPage /></GuardedPage>} />
            <Route path="/approvals" element={<GuardedPage roles={['Manager', 'AppOwner']}><ApprovalInboxPage /></GuardedPage>} />
            <Route path="/reviews" element={<GuardedPage roles={['Manager', 'SecurityAdmin']}><AccessReviewsPage /></GuardedPage>} />
            <Route path="/applications" element={<GuardedPage roles={['SecurityAdmin']}><ApplicationsPage /></GuardedPage>} />
            <Route path="/jml" element={<GuardedPage roles={['SecurityAdmin']}><JmlPage /></GuardedPage>} />
            <Route path="/audit-logs" element={<GuardedPage roles={['SecurityAdmin']}><AuditLogPage /></GuardedPage>} />
            <Route path="/notifications" element={<GuardedPage><NotificationsPage /></GuardedPage>} />
            <Route path="*" element={<Navigate to="/" replace />} />
          </Routes>
        </BrowserRouter>
      </AuthProvider>
    </QueryClientProvider>
  );
}

export default App;
