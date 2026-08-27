export function CanastaEngineRoundScore(side: {
  cardPoints: number
  naturalCanastas: number
  mixedCanastas: number
  redThrees: number
  goingOut: boolean
  countsAgainst: number
}) {
  return (
    side.cardPoints +
    side.naturalCanastas * 500 +
    side.mixedCanastas * 300 +
    side.redThrees * 100 +
    (side.goingOut ? 100 : 0) -
    side.countsAgainst
  )
}
