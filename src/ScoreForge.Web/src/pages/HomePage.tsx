import { useEffect } from 'react'
import { Link } from 'react-router-dom'
import { fetchMatches } from '../store/matchesSlice'
import { useAppDispatch, useAppSelector } from '../store/hooks'

export function HomePage() {
  const dispatch = useAppDispatch()
  const { list, listStatus } = useAppSelector((s) => s.matches)

  useEffect(() => {
    void dispatch(fetchMatches())
  }, [dispatch])

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-semibold">Your matches</h1>
          <p className="text-slate-400">Open a live scoreboard or start a new game.</p>
        </div>
        <Link
          to="/matches/new"
          className="rounded-lg bg-amber-500 px-4 py-2 font-medium text-slate-950 hover:bg-amber-400"
        >
          New match
        </Link>
      </div>

      {listStatus === 'loading' && <p className="text-slate-400">Loading matches…</p>}
      {listStatus === 'ready' && list.length === 0 && (
        <div className="rounded-xl border border-dashed border-slate-700 p-8 text-center text-slate-400">
          No matches yet. Create a Cribbage or Canasta match to get started.
        </div>
      )}
      <ul className="grid gap-3 sm:grid-cols-2">
        {list.map((match) => (
          <li key={match.matchId}>
            <Link
              to={`/matches/${match.matchId}`}
              className="block rounded-xl border border-slate-800 bg-slate-900 p-4 hover:border-amber-500/50"
            >
              <div className="flex items-center justify-between gap-2">
                <h2 className="font-medium text-white">{match.name}</h2>
                <span className="rounded-full bg-slate-800 px-2 py-0.5 text-xs uppercase text-slate-300">
                  {match.gameId}
                </span>
              </div>
              <p className="mt-2 text-sm text-slate-400">
                {match.status} · v{match.version} ·{' '}
                {new Date(match.updatedAtUtc).toLocaleString()}
              </p>
            </Link>
          </li>
        ))}
      </ul>
    </div>
  )
}
