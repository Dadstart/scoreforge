import styles from './CribbageBoard.module.css'
import type { MatchSnapshot } from '../types/api'

const SEGMENT_SIZE = 30
const SEGMENT_COUNT = 4
const HOLE_GROUP_SIZE = 5

function getSegmentHoles(segmentIndex: number, winningScore: number) {
  const start = segmentIndex * SEGMENT_SIZE + 1
  const count = Math.min(SEGMENT_SIZE, winningScore - start + 1)
  if (count <= 0)
    return [] as number[]
  const holes = Array.from({ length: count }, (_, i) => start + i)
  const shouldReverse = segmentIndex % 2 === 1 && segmentIndex !== SEGMENT_COUNT - 1
  return shouldReverse ? [...holes].reverse() : holes
}

function getSegmentHoleGroups(segmentIndex: number, winningScore: number) {
  const segment = getSegmentHoles(segmentIndex, winningScore)
  const groups: number[][] = []
  for (let i = 0; i < segment.length; i += HOLE_GROUP_SIZE)
    groups.push(segment.slice(i, i + HOLE_GROUP_SIZE))
  return groups
}

type Props = {
  match: MatchSnapshot
  onAddPoints: (seatId: string, points: number) => void
  onSetScore: (seatId: string, score: number) => void
  onUndo: () => void
  onReset: () => void
}

export function CribbageBoard({ match, onAddPoints, onSetScore, onUndo, onReset }: Props) {
  const winningScore = Number(match.options.winningScore ?? match.standings.details?.winningScore ?? 121)
  const previousScores = (match.standings.details?.previousScores ?? {}) as Record<string, number | null>
  const seats = match.standings.seats
  const undoable = [...match.events].reverse().find((e) => e.eventType !== 'undo')

  return (
    <div className={styles.layout}>
      <section className="rounded-xl border border-slate-800 bg-slate-900 p-4">
        <h2 className="mb-3 text-lg font-medium">Scoring</h2>
        {seats.map((seat, playerIndex) => (
          <div key={seat.seatId} className="mb-3 rounded-lg border border-slate-800 p-3">
            <h3 className="mb-1 font-medium">{seat.displayName}</h3>
            <p className="mb-2 text-sm text-slate-400">
              Score: <strong className="text-white">{seat.score}</strong> / {winningScore}
            </p>
            <div className="flex flex-wrap gap-2">
              {[1, 2, 3, 4].map((points) => (
                <button
                  key={points}
                  type="button"
                  className="rounded-md bg-indigo-600 px-3 py-1 text-sm hover:bg-indigo-500"
                  onClick={() => onAddPoints(seat.seatId, points)}
                >
                  +{points}
                </button>
              ))}
            </div>
            <p className={`mt-2 text-xs ${playerIndex === 0 ? 'text-sky-300' : 'text-rose-300'}`}>
              Lane {playerIndex + 1}
            </p>
          </div>
        ))}
        <div className="flex gap-2">
          <button
            type="button"
            className="rounded-md border border-slate-700 px-3 py-1 text-sm hover:bg-slate-800"
            disabled={!undoable}
            onClick={onUndo}
          >
            Undo last
          </button>
          <button
            type="button"
            className="rounded-md border border-slate-700 px-3 py-1 text-sm hover:bg-slate-800"
            onClick={onReset}
          >
            Reset game
          </button>
        </div>
      </section>

      <section className="rounded-xl border border-slate-800 bg-slate-900 p-4">
        <h2 className="mb-3 text-lg font-medium">Board</h2>
        <div className={styles.board}>
          <div className={styles.startFinish}>
            <span>Start</span>
            <span>Finish {winningScore}</span>
          </div>
          <div className={styles.lanes}>
            {seats.map((seat, playerIndex) => (
              <div key={seat.seatId} className={styles.lane}>
                <div className={styles.laneHeader}>
                  <span>{seat.displayName}</span>
                  <span className={styles.laneScore}>{seat.score}</span>
                </div>
                {Array.from({ length: SEGMENT_COUNT }, (_, segmentIndex) => {
                  const groups = getSegmentHoleGroups(segmentIndex, winningScore)
                  return (
                    <div key={segmentIndex}>
                      <div className={styles.holeRow}>
                        {segmentIndex === 0 && (
                          <HoleButton
                            playerIndex={playerIndex}
                            hole={0}
                            current={seat.score}
                            previous={previousScores[seat.seatId] ?? null}
                            winningScore={winningScore}
                            onClick={() => onSetScore(seat.seatId, 0)}
                          />
                        )}
                        {groups.map((group, groupIndex) => (
                          <div key={groupIndex} className={styles.holeGroup}>
                            {group.map((hole) => (
                              <HoleButton
                                key={hole}
                                playerIndex={playerIndex}
                                hole={hole}
                                current={seat.score}
                                previous={previousScores[seat.seatId] ?? null}
                                winningScore={winningScore}
                                onClick={() => onSetScore(seat.seatId, hole)}
                              />
                            ))}
                          </div>
                        ))}
                        {segmentIndex === SEGMENT_COUNT - 1 && (
                          <HoleButton
                            playerIndex={playerIndex}
                            hole={winningScore}
                            current={seat.score}
                            previous={previousScores[seat.seatId] ?? null}
                            winningScore={winningScore}
                            onClick={() => onSetScore(seat.seatId, winningScore)}
                          />
                        )}
                      </div>
                    </div>
                  )
                })}
              </div>
            ))}
          </div>
        </div>
        {match.standings.isComplete && (
          <p className="mt-3 rounded-md bg-emerald-950/50 px-3 py-2 text-emerald-300">
            Winner: {seats.find((s) => s.seatId === match.standings.winnerSeatId)?.displayName}
          </p>
        )}
      </section>

      <section className="rounded-xl border border-slate-800 bg-slate-900 p-4">
        <h2 className="mb-3 text-lg font-medium">Recent entries</h2>
        <ul className="space-y-2 text-sm">
          {[...match.events]
            .filter((e) => e.eventType !== 'undo')
            .reverse()
            .slice(0, 25)
            .map((event) => {
              const payload = event.payload as { seatId?: string; points?: number; score?: number }
              const seat = match.seats.find((s) => s.seatId === payload.seatId)
              return (
                <li key={event.eventId} className="flex justify-between text-slate-300">
                  <span>
                    {seat?.displayName ?? 'Seat'}{' '}
                    {event.eventType === 'add_points'
                      ? `+${payload.points}`
                      : event.eventType === 'set_score'
                        ? `→ ${payload.score}`
                        : event.eventType}
                  </span>
                  <span className="text-slate-500">
                    {new Date(event.occurredAtUtc).toLocaleTimeString()}
                  </span>
                </li>
              )
            })}
        </ul>
      </section>
    </div>
  )
}

function HoleButton({
  playerIndex,
  hole,
  current,
  previous,
  winningScore,
  onClick,
}: {
  playerIndex: number
  hole: number
  current: number
  previous: number | null
  winningScore: number
  onClick: () => void
}) {
  const classes = [
    styles.hole,
    playerIndex === 0 ? styles.p1 : styles.p2,
    hole === current ? styles.current : '',
    previous === hole ? styles.previous : '',
    hole === 0 || hole === winningScore ? styles.marker : '',
    hole === 90 || hole === 120 ? styles.skunk : '',
  ]
    .filter(Boolean)
    .join(' ')

  return (
    <button
      type="button"
      className={classes}
      title={`Hole ${hole}`}
      aria-label={`Hole ${hole}`}
      onClick={onClick}
    />
  )
}
