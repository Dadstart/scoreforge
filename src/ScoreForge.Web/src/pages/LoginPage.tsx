import { useEffect } from 'react'
import { Navigate, useLocation } from 'react-router-dom'
import { fetchMe, fetchProviders } from '../store/authApi'
import { useAppDispatch, useAppSelector } from '../store/hooks'

export function LoginPage() {
  const dispatch = useAppDispatch()
  const location = useLocation()
  const { user, providers, status } = useAppSelector((s) => s.auth)
  const from = (location.state as { from?: string } | null)?.from ?? '/'

  useEffect(() => {
    void dispatch(fetchProviders())
    void dispatch(fetchMe())
  }, [dispatch])

  if (status === 'ready' && user?.isAuthenticated)
    return <Navigate to={from} replace />

  const configured = providers.filter((p) => p.configured)

  return (
    <div className="flex min-h-screen items-center justify-center bg-slate-950 px-4">
      <div className="w-full max-w-md rounded-2xl border border-slate-800 bg-slate-900 p-8 shadow-xl">
        <h1 className="text-2xl font-semibold text-amber-300">ScoreForge</h1>
        <p className="mt-2 text-slate-400">Sign in to keep score across devices at the table.</p>
        <div className="mt-6 flex flex-col gap-3">
          {configured.length === 0 && (
            <p className="text-sm text-rose-300">No authentication providers are configured.</p>
          )}
          {configured.map((provider) => (
            <a
              key={provider.scheme}
              href={`/api/auth/login/${provider.scheme}?returnUrl=${encodeURIComponent(window.location.origin + from)}`}
              className="rounded-lg bg-indigo-600 px-4 py-3 text-center font-medium text-white hover:bg-indigo-500"
            >
              Continue with {provider.displayName}
            </a>
          ))}
        </div>
      </div>
    </div>
  )
}
