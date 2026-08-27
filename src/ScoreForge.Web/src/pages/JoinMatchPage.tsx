import { useEffect } from 'react'
import { Link, useParams } from 'react-router-dom'
import { claimSeat, fetchMatch } from '../store/matchesSlice'
import { useAppDispatch, useAppSelector } from '../store/hooks'

export function JoinMatchPage() {
  const { matchId } = useParams()
  const dispatch = useAppDispatch()
  const match = useAppSelector((s) => s.matches.active)
  const userId = useAppSelector((s) => s.auth.user?.userId)

  useEffect(() => {
    if (matchId)
      void dispatch(fetchMatch(matchId))
  }, [dispatch, matchId])

  if (!match)
    return <p className="text-slate-400">Loading seats…</p>

  return (
    <div className="mx-auto max-w-lg space-y-4">
      <h1 className="text-2xl font-semibold">Join {match.name}</h1>
      <p className="text-slate-400">Claim a seat so teammates know who is scoring on this device.</p>
      <ul className="space-y-2">
        {match.seats.map((seat) => {
          const claimed = seat.claimedByUserId != null
          const mine = seat.claimedByUserId === userId
          return (
            <li
              key={seat.seatId}
              className="flex items-center justify-between rounded-xl border border-slate-800 bg-slate-900 px-4 py-3"
            >
              <div>
                <div className="font-medium">{seat.displayName}</div>
                <div className="text-xs text-slate-400">
                  {mine ? 'Claimed by you' : claimed ? 'Claimed' : 'Open'}
                  {seat.teamId ? ` · ${seat.teamId}` : ''}
                </div>
              </div>
              <button
                type="button"
                disabled={claimed && !mine}
                className="rounded-md bg-indigo-600 px-3 py-1 text-sm disabled:opacity-40"
                onClick={() => void dispatch(claimSeat({ matchId: match.matchId, seatId: seat.seatId }))}
              >
                {mine ? 'Seated' : 'Claim'}
              </button>
            </li>
          )
        })}
      </ul>
      <Link to={`/matches/${match.matchId}`} className="inline-block text-amber-300 hover:underline">
        Back to scoreboard
      </Link>
    </div>
  )
}
