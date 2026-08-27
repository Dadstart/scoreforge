import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { CanastaScoresheet } from './CanastaScoresheet'
import type { MatchSnapshot } from '../types/api'

function matchFixture(): MatchSnapshot {
  return {
    matchId: 'm1',
    name: 'Canasta night',
    gameId: 'canasta',
    status: 'active',
    version: 1,
    ownerUserId: 'u1',
    options: {},
    createdAtUtc: '2026-01-01T00:00:00Z',
    updatedAtUtc: '2026-01-01T00:00:00Z',
    seats: [
      { seatId: 's1', displayName: 'Ann', teamId: 'team-a', sortOrder: 0, claimedByUserId: null },
      { seatId: 's2', displayName: 'Bob', teamId: 'team-b', sortOrder: 1, claimedByUserId: null },
    ],
    events: [],
    standings: {
      seats: [
        { seatId: 's1', displayName: 'Ann', teamId: 'team-a', score: 0 },
        { seatId: 's2', displayName: 'Bob', teamId: 'team-b', score: 0 },
      ],
      teams: [
        { teamId: 'team-a', displayName: 'Ann', score: 0 },
        { teamId: 'team-b', displayName: 'Bob', score: 0 },
      ],
      winnerSeatId: null,
      winnerTeamId: null,
      isComplete: false,
      details: { meldThresholds: { 'team-a': 50, 'team-b': 50 }, rounds: [] },
    },
  }
}

describe('CanastaScoresheet going out', () => {
  it('offers normal and blind going out choices', () => {
    render(
      <CanastaScoresheet match={matchFixture()} onSubmitRound={vi.fn()} onUndo={vi.fn()} onReset={vi.fn()} />,
    )

    const [first] = screen.getAllByLabelText('Going out')
    expect(first).toHaveDisplayValue('Not going out')
    expect(screen.getAllByRole('option', { name: 'Going out (+100)' })).toHaveLength(2)
    expect(screen.getAllByRole('option', { name: 'Going out blind (+200)' })).toHaveLength(2)
  })

  it('clears the other side when one side goes out', async () => {
    const user = userEvent.setup()
    render(
      <CanastaScoresheet match={matchFixture()} onSubmitRound={vi.fn()} onUndo={vi.fn()} onReset={vi.fn()} />,
    )

    const [teamA, teamB] = screen.getAllByLabelText('Going out')
    await user.selectOptions(teamA, 'normal')
    expect(teamA).toHaveValue('normal')

    await user.selectOptions(teamB, 'blind')
    expect(teamB).toHaveValue('blind')
    expect(teamA).toHaveValue('none')
  })

  it('submits going out blind as +200', async () => {
    const user = userEvent.setup()
    const onSubmitRound = vi.fn()
    render(
      <CanastaScoresheet match={matchFixture()} onSubmitRound={onSubmitRound} onUndo={vi.fn()} onReset={vi.fn()} />,
    )

    const [teamA] = screen.getAllByLabelText('Going out')
    await user.selectOptions(teamA, 'blind')
    await user.click(screen.getByRole('button', { name: 'Submit round' }))

    expect(onSubmitRound).toHaveBeenCalledWith([
      expect.objectContaining({ teamId: 'team-a', goingOut: true, goingOutBlind: true }),
      expect.objectContaining({ teamId: 'team-b', goingOut: false, goingOutBlind: false }),
    ])
  })
})
