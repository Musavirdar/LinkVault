import { useEffect } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { useStore } from '@/store/useStore';
import { parseJwtPermissions } from '@/api/client';
import { Loader2 } from 'lucide-react';


export function OAuthCallbackPage() {
    const navigate = useNavigate();
    const { setToken, setPermissions } = useStore();

    const location = useLocation();

    useEffect(() => {
        // Parse hash: #token=...&refreshToken=...
        const hash = location.hash;
        if (hash) {
            const params = new URLSearchParams(hash.substring(1));
            const token = params.get('token');

            if (token) {
                setToken(token);
                setPermissions(parseJwtPermissions(token));
                navigate('/dashboard', { replace: true });
                return;
            }

        }

        // Check query params for errors
        const searchParams = new URLSearchParams(location.search);
        const err = searchParams.get('error');
        if (err) {
            navigate(`/login?error=${encodeURIComponent(err)}`, { replace: true });
        } else {
            navigate('/login?error=Authentication%20failed', { replace: true });
        }
    }, [location, navigate, setToken, setPermissions]);


    return (
        <div className="min-h-screen flex flex-col items-center justify-center bg-[var(--bg-primary)]">
            <Loader2 className="w-12 h-12 text-[var(--accent-primary)] animate-spin mb-6" />
            <h2 className="text-[var(--text-primary)] text-xl font-semibold mb-2">Completing sign in...</h2>
            <p className="text-[var(--text-secondary)]">Please wait while we log you into LinkVault.</p>
        </div>
    );
}
