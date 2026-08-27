import { describe, expect, it } from 'vitest'
import { newClientEventId } from '../api/client'
import { CanastaEngineRoundScore } from './canastaMath'

describe('newClientEventId', () => {
  it('returns a uuid-like string', () => {
    const id = newClientEventId()
    expect(id.length).toBeGreaterThan(8)
  })
})

describe('canasta round math', () => {
  it('computes american canasta totals', () => {
    expect(
      CanastaEngineRoundScore({
        cardPoints: 200,
        naturalCanastas: 1,
        mixedCanastas: 0,
        redThrees: 1,
        goingOut: true,
        countsAgainst: 0,
      }),
    ).toBe(900)
  })
})
