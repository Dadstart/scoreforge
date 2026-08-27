export type AuthProvider = {
  scheme: string
  displayName: string
  configured: boolean
}

export type AuthUser = {
  isAuthenticated: boolean
  userId: string | null
  displayName: string | null
  email: string | null
}

export type GameCatalogItem = {
  gameId: string
  name: string
  description: string
}

export type MatchSummary = {
  matchId: string
  name: string
  gameId: string
  status: string
  version: number
  updatedAtUtc: string
}

export type MatchSeat = {
  seatId: string
  displayName: string
  teamId: string | null
  sortOrder: number
  claimedByUserId: string | null
}

export type MatchEvent = {
  eventId: string
  eventType: string
  payload: unknown
  actorUserId: string | null
  clientEventId: string
  version: number
  occurredAtUtc: string
}

export type SeatStanding = {
  seatId: string
  displayName: string
  teamId: string | null
  score: number
}

export type TeamStanding = {
  teamId: string
  displayName: string
  score: number
}

export type MatchStandings = {
  seats: SeatStanding[]
  teams: TeamStanding[] | null
  winnerSeatId: string | null
  winnerTeamId: string | null
  isComplete: boolean
  details: Record<string, unknown> | null
}

export type MatchSnapshot = {
  matchId: string
  name: string
  gameId: string
  status: string
  version: number
  ownerUserId: string
  options: Record<string, unknown>
  createdAtUtc: string
  updatedAtUtc: string
  seats: MatchSeat[]
  events: MatchEvent[]
  standings: MatchStandings
}

export type CreateMatchRequest = {
  name: string
  gameId: string
  options?: Record<string, unknown>
  seats: { displayName: string; teamId?: string | null }[]
}
