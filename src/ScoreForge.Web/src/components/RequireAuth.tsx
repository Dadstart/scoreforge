import { useEffect } from 'react'
import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { fetchMe, fetchProviders } from '../store/authApi'
import { useAppDispatch, useAppSelector } from '../store/hooks'

export function RequireAuth() {
  const dispatch = useAppDispatch()
  const location = useLocation()
  const { user, status } = useAppSelector((s) => s.auth)

  useEffect(() => {
    if (status === 'idle') {
      void dispatch(fetchMe())
      void dispatch(fetchProviders())
    }
  }, [dispatch, status])

  if (status === 'idle' || status === 'loading')
    return <div className="p-8 text-slate-300">Loading…</div>

  if (!user?.isAuthenticated)
    return <Navigate to="/login" replace state={{ from: location.pathname }} />

  return <Outlet />
}
