import { useEffect, useState, type FormEvent } from 'react'
import type { MatchSnapshot } from '../types/api'

type SideDraft = {
  teamId: string
  cardPoints: number
  naturalCanastas: number
  mixedCanastas: number
  redThrees: number
  goingOut: boolean
  countsAgainst: number
}

function emptySide(teamId: string): SideDraft {
  return {
    teamId,
    cardPoints: 0,
    naturalCanastas: 0,
    mixedCanastas: 0,
    redThrees: 0,
    goingOut: false,
    countsAgainst: 0,
  }
}

type Props = {
  match: MatchSnapshot
  onSubmitRound: (sides: SideDraft[]) => void
  onUndo: () => void
  onReset: () => void
}

export function CanastaScoresheet({ match, onSubmitRound, onUndo, onReset }: Props) {
  const teams = match.standings.teams ?? match.standings.seats.map((s) => ({
    teamId: s.teamId ?? s.seatId,
    displayName: s.displayName,
    score: s.score,
  }))
  const meldThresholds = (match.standings.details?.meldThresholds ?? {}) as Record<string, number>
  const rounds = (match.standings.details?.rounds ?? []) as Array<{
    eventId: string
    sides: Array<{ teamId: string; roundScore: number }>
    occurredAtUtc: string
  }>

  const [drafts, setDrafts] = useState<SideDraft[]>(() => teams.map((t) => emptySide(t.teamId)))

  useEffect(() => {
    setDrafts(teams.map((t) => emptySide(t.teamId)))
    // Reset draft inputs when the match version changes after a successful round.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [match.version])

  function updateDraft(teamId: string, patch: Partial<SideDraft>) {
    setDrafts((current) =>
      current.map((side) => {
        if (side.teamId !== teamId)
          return patch.goingOut ? { ...side, goingOut: false } : side
        return { ...side, ...patch }
      }),
    )
  }

  function onSubmit(event: FormEvent) {
    event.preventDefault()
    onSubmitRound(drafts)
  }

  const undoable = [...match.events].reverse().find((e) => e.eventType === 'submit_round')

  return (
    <div className="space-y-6">
      <section className="grid gap-3 sm:grid-cols-2">
        {teams.map((team) => (
          <div key={team.teamId} className="rounded-xl border border-slate-800 bg-slate-900 p-4">
            <h2 className="font-medium">{team.displayName}</h2>
            <p className="mt-1 text-3xl font-semibold text-amber-300">{team.score}</p>
            <p className="mt-1 text-sm text-slate-400">
              Initial meld guidance: {meldThresholds[team.teamId] ?? 50}
            </p>
          </div>
        ))}
      </section>

      {match.standings.isComplete && (
        <p className="rounded-md bg-emerald-950/50 px-3 py-2 text-emerald-300">
          Winner: {teams.find((t) => t.teamId === match.standings.winnerTeamId)?.displayName}
        </p>
      )}

      <form onSubmit={onSubmit} className="space-y-4 rounded-xl border border-slate-800 bg-slate-900 p-4">
        <div className="flex items-center justify-between gap-3">
          <h2 className="text-lg font-medium">Enter round</h2>
          <div className="flex gap-2">
            <button
              type="button"
              className="rounded-md border border-slate-700 px-3 py-1 text-sm"
              disabled={!undoable}
              onClick={onUndo}
            >
              Undo round
            </button>
            <button type="button" className="rounded-md border border-slate-700 px-3 py-1 text-sm" onClick={onReset}>
              Reset game
            </button>
          </div>
        </div>

        <div className="grid gap-4 lg:grid-cols-2">
          {drafts.map((side) => {
            const team = teams.find((t) => t.teamId === side.teamId)
            return (
              <div key={side.teamId} className="space-y-2 rounded-lg border border-slate-800 p-3">
                <h3 className="font-medium">{team?.displayName}</h3>
                <NumberField
                  label="Card points"
                  value={side.cardPoints}
                  onChange={(cardPoints) => updateDraft(side.teamId, { cardPoints })}
                />
                <NumberField
                  label="Natural canastas"
                  value={side.naturalCanastas}
                  onChange={(naturalCanastas) => updateDraft(side.teamId, { naturalCanastas })}
                />
                <NumberField
                  label="Mixed canastas"
                  value={side.mixedCanastas}
                  onChange={(mixedCanastas) => updateDraft(side.teamId, { mixedCanastas })}
                />
                <NumberField
                  label="Red threes"
                  value={side.redThrees}
                  onChange={(redThrees) => updateDraft(side.teamId, { redThrees })}
                />
                <NumberField
                  label="Counts against"
                  value={side.countsAgainst}
                  onChange={(countsAgainst) => updateDraft(side.teamId, { countsAgainst })}
                />
                <label className="flex items-center gap-2 text-sm">
                  <input
                    type="checkbox"
                    checked={side.goingOut}
                    onChange={(e) => updateDraft(side.teamId, { goingOut: e.target.checked })}
                  />
                  Going out (+100)
                </label>
              </div>
            )
          })}
        </div>

        <button type="submit" className="rounded-lg bg-amber-500 px-4 py-2 font-medium text-slate-950 hover:bg-amber-400">
          Submit round
        </button>
      </form>

      <section className="rounded-xl border border-slate-800 bg-slate-900 p-4">
        <h2 className="mb-3 text-lg font-medium">Round history</h2>
        {rounds.length === 0 ? (
          <p className="text-slate-400">No rounds yet.</p>
        ) : (
          <ul className="space-y-2 text-sm">
            {[...rounds].reverse().map((round, index) => (
              <li key={round.eventId} className="rounded-md border border-slate-800 px-3 py-2">
                <div className="mb-1 text-slate-400">Round {rounds.length - index}</div>
                <div className="flex flex-wrap gap-3">
                  {round.sides.map((side) => {
                    const team = teams.find((t) => t.teamId === side.teamId)
                    return (
                      <span key={side.teamId}>
                        {team?.displayName}: {side.roundScore >= 0 ? '+' : ''}
                        {side.roundScore}
                      </span>
                    )
                  })}
                </div>
              </li>
            ))}
          </ul>
        )}
      </section>
    </div>
  )
}

function NumberField({
  label,
  value,
  onChange,
}: {
  label: string
  value: number
  onChange: (value: number) => void
}) {
  return (
    <label className="flex items-center justify-between gap-3 text-sm">
      <span className="text-slate-300">{label}</span>
      <input
        type="number"
        className="w-24 rounded-md border border-slate-700 bg-slate-950 px-2 py-1"
        value={value}
        onChange={(e) => onChange(Number(e.target.value))}
      />
    </label>
  )
}
