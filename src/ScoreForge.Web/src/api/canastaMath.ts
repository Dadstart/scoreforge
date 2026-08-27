export const GoingOutPoints = 100
export const GoingOutBlindPoints = 200

export type CanastaGoingOutChoice = 'none' | 'normal' | 'blind'

export function CanastaGoingOutBonus(side: { goingOut: boolean; goingOutBlind?: boolean }) {
  if (side.goingOutBlind)
    return GoingOutBlindPoints
  if (side.goingOut)
    return GoingOutPoints
  return 0
}

export function CanastaEngineRoundScore(side: {
  cardPoints: number
  naturalCanastas: number
  mixedCanastas: number
  redThrees: number
  goingOut: boolean
  goingOutBlind?: boolean
  countsAgainst: number
}) {
  return (
    side.cardPoints +
    side.naturalCanastas * 500 +
    side.mixedCanastas * 300 +
    side.redThrees * 100 +
    CanastaGoingOutBonus(side) -
    side.countsAgainst
  )
}
