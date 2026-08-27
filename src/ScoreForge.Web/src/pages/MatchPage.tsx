import { useEffect } from 'react'
import { Link, useParams } from 'react-router-dom'
import { CanastaScoresheet } from '../games/CanastaScoresheet'
import { CribbageBoard } from '../games/CribbageBoard'
import { useMatchHub } from '../hooks/useMatchHub'
import { appendEvent, clearActive, fetchMatch, undoEvent } from '../store/matchesSlice'
import { useAppDispatch, useAppSelector } from '../store/hooks'

export function MatchPage() {
  const { matchId } = useParams()
  const dispatch = useAppDispatch()
  const match = useAppSelector((s) => s.matches.active)
  const status = useAppSelector((s) => s.matches.activeStatus)
  const connection = useAppSelector((s) => s.matches.connection)
  const userId = useAppSelector((s) => s.auth.user?.userId)

  useMatchHub(matchId)

  useEffect(() => {
    if (matchId)
      void dispatch(fetchMatch(matchId))
    return () => {
      dispatch(clearActive())
    }
  }, [dispatch, matchId])

  if (status === 'loading' || !match)
    return <p className="text-slate-400">Loading match…</p>

  if (status === 'error')
    return <p className="text-rose-300">Unable to load match.</p>

  const lastScorable = [...match.events].reverse().find((e) => e.eventType !== 'undo')

  async function sendEvent(eventType: string, payload: unknown) {
    if (!match)
      return
    await dispatch(
      appendEvent({
        matchId: match.matchId,
        eventType,
        payload,
        baseVersion: match.version,
      }),
    )
  }

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold">{match.name}</h1>
          <p className="text-sm text-slate-400">
            {match.gameId} · v{match.version} · live sync {connection}
          </p>
        </div>
        <Link
          to={`/matches/${match.matchId}/join`}
          className="rounded-md border border-slate-700 px-3 py-1 text-sm hover:bg-slate-800"
        >
          Claim a seat
        </Link>
      </div>

      {match.gameId === 'cribbage' && (
        <CribbageBoard
          match={match}
          onAddPoints={(seatId, points) => void sendEvent('add_points', { seatId, points })}
          onSetScore={(seatId, score) => void sendEvent('set_score', { seatId, score })}
          onUndo={() => {
            if (!lastScorable)
              return
            void dispatch(
              undoEvent({
                matchId: match.matchId,
                targetEventId: lastScorable.eventId,
                baseVersion: match.version,
              }),
            )
          }}
          onReset={() => void sendEvent('reset', {})}
        />
      )}

      {match.gameId === 'canasta' && (
        <CanastaScoresheet
          match={match}
          onSubmitRound={(sides) => void sendEvent('submit_round', { sides })}
          onUndo={() => {
            const lastRound = [...match.events].reverse().find((e) => e.eventType === 'submit_round')
            if (!lastRound)
              return
            void dispatch(
              undoEvent({
                matchId: match.matchId,
                targetEventId: lastRound.eventId,
                baseVersion: match.version,
              }),
            )
          }}
          onReset={() => void sendEvent('reset', {})}
        />
      )}

      <p className="text-xs text-slate-500">Signed in as {userId}</p>
    </div>
  )
}
