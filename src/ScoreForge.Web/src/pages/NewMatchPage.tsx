import { useEffect, useMemo, useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { apiGet } from '../api/client'
import type { GameCatalogItem } from '../types/api'
import { createMatch } from '../store/matchesSlice'
import { useAppDispatch } from '../store/hooks'

export function NewMatchPage() {
  const dispatch = useAppDispatch()
  const navigate = useNavigate()
  const [games, setGames] = useState<GameCatalogItem[]>([])
  const [gameId, setGameId] = useState('cribbage')
  const [name, setName] = useState('Table match')
  const [playerCount, setPlayerCount] = useState(2)
  const [winningScore, setWinningScore] = useState(121)
  const [seatNames, setSeatNames] = useState(['Player 1', 'Player 2', 'Player 3', 'Player 4'])
  const [error, setError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    void apiGet<GameCatalogItem[]>('/api/games').then(setGames).catch((err: Error) => setError(err.message))
  }, [])

  function selectGame(nextGameId: string) {
    setGameId(nextGameId)
    setPlayerCount(2)
    setWinningScore(nextGameId === 'cribbage' ? 121 : 5000)
  }

  const activeSeats = useMemo(() => seatNames.slice(0, playerCount), [seatNames, playerCount])

  async function onSubmit(event: FormEvent) {
    event.preventDefault()
    setSaving(true)
    setError(null)
    try {
      const seats =
        gameId === 'canasta' && playerCount === 4
          ? activeSeats.map((displayName, index) => ({
              displayName,
              teamId: index < 2 ? 'team-a' : 'team-b',
            }))
          : activeSeats.map((displayName) => ({ displayName }))

      const snapshot = await dispatch(
        createMatch({
          name,
          gameId,
          options: { winningScore, playerCount },
          seats,
        }),
      ).unwrap()
      navigate(`/matches/${snapshot.matchId}`)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create match')
    } finally {
      setSaving(false)
    }
  }

  return (
    <form onSubmit={onSubmit} className="mx-auto max-w-xl space-y-5 rounded-2xl border border-slate-800 bg-slate-900 p-6">
      <h1 className="text-2xl font-semibold">New match</h1>
      {error && <p className="rounded-md bg-rose-950/60 px-3 py-2 text-sm text-rose-300">{error}</p>}

      <label className="block space-y-1 text-sm">
        <span className="text-slate-300">Match name</span>
        <input
          className="w-full rounded-md border border-slate-700 bg-slate-950 px-3 py-2"
          value={name}
          onChange={(e) => setName(e.target.value)}
          required
          minLength={2}
        />
      </label>

      <label className="block space-y-1 text-sm">
        <span className="text-slate-300">Game</span>
        <select
          className="w-full rounded-md border border-slate-700 bg-slate-950 px-3 py-2"
          value={gameId}
          onChange={(e) => selectGame(e.target.value)}
        >
          {games.map((game) => (
            <option key={game.gameId} value={game.gameId}>
              {game.name}
            </option>
          ))}
        </select>
      </label>

      <label className="block space-y-1 text-sm">
        <span className="text-slate-300">Players</span>
        <select
          className="w-full rounded-md border border-slate-700 bg-slate-950 px-3 py-2"
          value={playerCount}
          onChange={(e) => setPlayerCount(Number(e.target.value))}
        >
          <option value={2}>2</option>
          {(gameId === 'cribbage' || gameId === 'canasta') && <option value={4}>4</option>}
          {gameId === 'cribbage' && <option value={3}>3</option>}
        </select>
      </label>

      <label className="block space-y-1 text-sm">
        <span className="text-slate-300">Winning score</span>
        <select
          className="w-full rounded-md border border-slate-700 bg-slate-950 px-3 py-2"
          value={winningScore}
          onChange={(e) => setWinningScore(Number(e.target.value))}
        >
          {gameId === 'cribbage' ? (
            <>
              <option value={121}>121</option>
              <option value={61}>61</option>
            </>
          ) : (
            <option value={5000}>5000</option>
          )}
        </select>
      </label>

      <div className="space-y-3">
        {activeSeats.map((seatName, index) => (
          <label key={index} className="block space-y-1 text-sm">
            <span className="text-slate-300">
              Seat {index + 1}
              {gameId === 'canasta' && playerCount === 4
                ? ` (Team ${index < 2 ? 'A' : 'B'})`
                : ''}
            </span>
            <input
              className="w-full rounded-md border border-slate-700 bg-slate-950 px-3 py-2"
              value={seatName}
              onChange={(e) => {
                const next = [...seatNames]
                next[index] = e.target.value
                setSeatNames(next)
              }}
              required
            />
          </label>
        ))}
      </div>

      <button
        type="submit"
        disabled={saving}
        className="w-full rounded-lg bg-amber-500 px-4 py-2 font-medium text-slate-950 hover:bg-amber-400 disabled:opacity-60"
      >
        {saving ? 'Creating…' : 'Create match'}
      </button>
    </form>
  )
}
