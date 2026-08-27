import { Link, NavLink, Outlet } from 'react-router-dom'
import { logout } from '../store/authApi'
import { useAppDispatch, useAppSelector } from '../store/hooks'

export function AppShell() {
  const dispatch = useAppDispatch()
  const user = useAppSelector((s) => s.auth.user)
  const connection = useAppSelector((s) => s.matches.connection)

  return (
    <div className="min-h-screen bg-slate-950 text-slate-100">
      <header className="border-b border-slate-800 bg-slate-900/80 backdrop-blur">
        <div className="mx-auto flex max-w-6xl items-center justify-between gap-4 px-4 py-3">
          <div className="flex items-center gap-6">
            <Link to="/" className="text-lg font-semibold tracking-tight text-amber-300">
              ScoreForge
            </Link>
            <nav className="flex gap-3 text-sm">
              <NavLink
                to="/"
                className={({ isActive }) =>
                  isActive ? 'text-white' : 'text-slate-400 hover:text-slate-200'
                }
                end
              >
                Matches
              </NavLink>
              <NavLink
                to="/matches/new"
                className={({ isActive }) =>
                  isActive ? 'text-white' : 'text-slate-400 hover:text-slate-200'
                }
              >
                New match
              </NavLink>
            </nav>
          </div>
          <div className="flex items-center gap-3 text-sm">
            {connection !== 'disconnected' && (
              <span
                className={
                  connection === 'connected'
                    ? 'rounded-full bg-emerald-900/60 px-2 py-0.5 text-emerald-300'
                    : 'rounded-full bg-amber-900/60 px-2 py-0.5 text-amber-300'
                }
              >
                {connection}
              </span>
            )}
            <span className="text-slate-300">{user?.displayName}</span>
            <button
              type="button"
              className="rounded-md border border-slate-700 px-3 py-1 hover:bg-slate-800"
              onClick={() => void dispatch(logout())}
            >
              Sign out
            </button>
          </div>
        </div>
      </header>
      <main className="mx-auto max-w-6xl px-4 py-6">
        <Outlet />
      </main>
    </div>
  )
}
