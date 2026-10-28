import { useState } from 'react';
import { Check, Loader2, AlertCircle } from 'lucide-react';
import { apiClient } from '@/api/client';
import { useStore } from '@/store/useStore';

export function SubscriptionPage() {
    const [loadingPlan, setLoadingPlan] = useState<string | null>(null);
    const [checkoutError, setCheckoutError] = useState('');
    useStore();

    const plans = [
        { id: 'plan_free', name: 'Free', price: '$0', features: ['Up to 50 links', 'Basic analytics', 'Community support'] },
        { id: 'plan_pro', name: 'Pro', price: '$9.99/mo', features: ['Unlimited links', 'Advanced analytics', 'Custom domains', 'Priority support'], recommended: true },
        { id: 'plan_team', name: 'Team', price: '$29.99/mo', features: ['Everything in Pro', 'Up to 10 team members', 'Team directories', 'Role-based access'] },
    ];

    const handleCheckout = async (planId: string) => {
        if (planId === 'plan_free') return;
        setLoadingPlan(planId);
        setCheckoutError('');
        try {
            // apiClient auto-prefixes /api and injects the Authorization header
            const res = await apiClient<{ checkoutUrl: string; sessionId: string }>('/subscriptions/checkout', {
                method: 'POST',
                body: { planId },
            });

            if (res.error) throw new Error(res.error);
            if (res.data?.checkoutUrl) {
                window.location.href = res.data.checkoutUrl;
            } else {
                throw new Error('No checkout URL returned. Make sure Stripe keys are configured.');
            }
        } catch (err: unknown) {
            setCheckoutError((err as Error).message || 'Failed to initiate checkout. Please try again.');
        } finally {
            setLoadingPlan(null);
        }
    };

    return (
        <div className="max-w-6xl mx-auto space-y-8 p-4 pt-12">
            <div className="text-center space-y-4 mb-16">
                <h1 className="text-4xl font-extrabold text-[var(--text-primary)] tracking-tight">Upgrade your workspace</h1>
                <p className="text-lg text-[var(--text-secondary)] max-w-2xl mx-auto">
                    Choose the perfect plan for your needs. Upgrade anytime to unlock advanced features.
                </p>
            </div>

            {checkoutError && (
                <div className="flex items-center gap-3 p-4 rounded-xl mb-6 text-sm font-medium"
                    style={{ background: '#EF444415', border: '1px solid #EF444430', color: '#F87171' }}>
                    <AlertCircle className="w-4 h-4 shrink-0" />
                    {checkoutError}
                </div>
            )}

            <div className="grid md:grid-cols-3 gap-8">
                {plans.map(plan => (
                    <div
                        key={plan.id}
                        className={`relative p-8 rounded-[2rem] border transition-transform duration-300 flex flex-col ${plan.recommended ? 'shadow-2xl scale-105 z-10' : 'shadow-lg hover:scale-105'}`}
                        style={{
                            background: 'var(--bg-secondary)',
                            borderColor: plan.recommended ? 'var(--accent-primary)' : 'var(--border-subtle)'
                        }}
                    >
                        {plan.recommended && (
                            <div className="absolute top-0 left-1/2 -translate-x-1/2 -translate-y-1/2 px-4 py-1.5 rounded-full text-xs font-bold text-white uppercase tracking-wider shadow-md"
                                style={{ background: 'linear-gradient(135deg, #5865F2, #7C3AED)' }}>
                                Most Popular
                            </div>
                        )}

                        <h3 className="text-xl font-bold text-[var(--text-primary)] mb-2">{plan.name}</h3>
                        <div className="mb-6 flex items-baseline gap-2">
                            <span className="text-4xl font-extrabold text-[var(--text-primary)]">{plan.price}</span>
                        </div>

                        <p className="text-sm font-medium text-[var(--text-muted)] mb-8 h-10">
                            {plan.id === 'plan_free' ? 'Perfect for getting started.' : plan.id === 'plan_pro' ? 'For power users and professionals.' : 'For growing teams and organizations.'}
                        </p>

                        <ul className="space-y-4 mb-10 flex-1">
                            {plan.features.map(f => (
                                <li key={f} className="flex items-start gap-3">
                                    <Check className="w-5 h-5 text-green-500 shrink-0" />
                                    <span className="text-sm font-medium text-[var(--text-secondary)]">{f}</span>
                                </li>
                            ))}
                        </ul>

                        <button
                            onClick={() => handleCheckout(plan.id)}
                            disabled={loadingPlan !== null || plan.id === 'plan_free'}
                            className={`w-full py-3.5 rounded-xl font-bold transition-all flex items-center justify-center gap-2 ${plan.id === 'plan_free' ? 'opacity-50 cursor-not-allowed' : 'hover:opacity-90 active:scale-[0.98]'}`}
                            style={{
                                background: plan.recommended ? 'linear-gradient(135deg, #5865F2, #7C3AED)' : 'var(--bg-tertiary)',
                                color: plan.recommended ? 'white' : 'var(--text-primary)',
                                border: plan.recommended ? 'none' : '1px solid var(--border-strong)',
                                boxShadow: plan.recommended ? '0 8px 16px rgba(88, 101, 242, 0.25)' : 'none'
                            }}
                        >
                            {loadingPlan === plan.id ? <Loader2 className="w-5 h-5 animate-spin" /> : null}
                            {plan.id === 'plan_free' ? 'Current Plan' : 'Upgrade Now'}
                        </button>
                    </div>
                ))}
            </div>

            <div className="mt-16 text-center text-sm text-[var(--text-muted)]">
                <p>Questions about our plans? <a href="#" className="text-[var(--accent-primary)] hover:underline">Contact our support team.</a></p>
                <p className="mt-2">Secure payments powered by Stripe.</p>
            </div>
        </div>
    );
}
