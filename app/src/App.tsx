import { Routes, Route, Navigate } from 'react-router-dom';
import { useEffect } from 'react';
import { RootLayout } from '@/components/layout/RootLayout';
import { LoginPage } from '@/pages/LoginPage';
import { LandingPage } from '@/pages/LandingPage';
import { DashboardPage } from '@/pages/DashboardPage';
import { LinksPage } from '@/pages/LinksPage';
import { DirectoriesPage } from '@/pages/DirectoriesPage';
import { AuditPage } from '@/pages/AuditPage';
import { FilesPage } from '@/pages/FilesPage';
import { AnalyticsPage } from '@/pages/AnalyticsPage';
import { SettingsPage } from '@/pages/SettingsPage';
import { OAuthCallbackPage } from '@/pages/OAuthCallbackPage';
import { SubscriptionPage } from '@/pages/SubscriptionPage';
import { PublicProfilePage } from '@/pages/PublicProfilePage';
import { ProfileEditorPage } from '@/pages/ProfileEditorPage';
import { SharedFilePage } from '@/pages/SharedFilePage';
import { TransferPage } from '@/pages/TransferPage';
import { useStore } from '@/store/useStore';
import { parseJwtPermissions } from '@/api/client';


function ProtectedRoute({ children }: { children: React.ReactNode }) {
  const { token } = useStore();
  if (!token) return <Navigate to="/login" replace />;
  return <>{children}</>;
}

function App() {
  const { token, setPermissions } = useStore();

  // Bootstrap permissions from stored token on page refresh
  useEffect(() => {
    if (token) {
      setPermissions(parseJwtPermissions(token));
    }
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  return (
    <Routes>
      {/* Public routes — accessible without login */}
      <Route path="/" element={token ? <Navigate to="/dashboard" replace /> : <LandingPage />} />
      <Route path="/login" element={token ? <Navigate to="/dashboard" replace /> : <LoginPage />} />
      <Route path="/oauth-callback" element={<OAuthCallbackPage />} />
      {/* Public user profiles — no auth required */}
      <Route path="/u/:username" element={<PublicProfilePage />} />
      {/* Shared file page — public access */}
      <Route path="/s/:token" element={<SharedFilePage />} />
      {/* Transfer page — public access */}
      <Route path="/t/:token" element={<TransferPage />} />

      {/* Protected app routes */}
      <Route
        path="/app/*"
        element={
          <ProtectedRoute>
            <RootLayout>
              <Routes>
                <Route path="/" element={<Navigate to="/dashboard" replace />} />
                <Route path="/dashboard" element={<DashboardPage />} />
                <Route path="/links" element={<LinksPage />} />
                <Route path="/files" element={<FilesPage />} />
                <Route path="/directories" element={<DirectoriesPage />} />
                <Route path="/analytics" element={<AnalyticsPage />} />
                <Route path="/audit" element={<AuditPage />} />
                <Route path="/settings" element={<SettingsPage />} />
                <Route path="/subscription" element={<SubscriptionPage />} />
                <Route path="/profile-editor" element={<ProfileEditorPage />} />
                <Route path="*" element={<Navigate to="/dashboard" replace />} />
              </Routes>
            </RootLayout>
          </ProtectedRoute>
        }
      />

      {/* Legacy direct routes — redirect into /app/ if logged in, else landing */}
      <Route path="/dashboard" element={token ? <ProtectedRoute><RootLayout><DashboardPage /></RootLayout></ProtectedRoute> : <Navigate to="/" replace />} />
      <Route path="/links" element={token ? <ProtectedRoute><RootLayout><LinksPage /></RootLayout></ProtectedRoute> : <Navigate to="/" replace />} />
      <Route path="/files" element={token ? <ProtectedRoute><RootLayout><FilesPage /></RootLayout></ProtectedRoute> : <Navigate to="/" replace />} />
      <Route path="/directories" element={token ? <ProtectedRoute><RootLayout><DirectoriesPage /></RootLayout></ProtectedRoute> : <Navigate to="/" replace />} />
      <Route path="/analytics" element={token ? <ProtectedRoute><RootLayout><AnalyticsPage /></RootLayout></ProtectedRoute> : <Navigate to="/" replace />} />
      <Route path="/audit" element={token ? <ProtectedRoute><RootLayout><AuditPage /></RootLayout></ProtectedRoute> : <Navigate to="/" replace />} />
      <Route path="/settings" element={token ? <ProtectedRoute><RootLayout><SettingsPage /></RootLayout></ProtectedRoute> : <Navigate to="/" replace />} />
      <Route path="/subscription" element={token ? <ProtectedRoute><RootLayout><SubscriptionPage /></RootLayout></ProtectedRoute> : <Navigate to="/" replace />} />

      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}

export default App;
