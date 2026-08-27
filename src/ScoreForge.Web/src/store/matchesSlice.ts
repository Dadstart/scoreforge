import { createAsyncThunk, createSlice, type PayloadAction } from '@reduxjs/toolkit'
import { apiGet, apiPost, newClientEventId } from '../api/client'
import type { CreateMatchRequest, MatchSnapshot, MatchSummary } from '../types/api'

type MatchesState = {
  list: MatchSummary[]
  listStatus: 'idle' | 'loading' | 'ready' | 'error'
  active: MatchSnapshot | null
  activeStatus: 'idle' | 'loading' | 'ready' | 'error'
  connection: 'disconnected' | 'connecting' | 'connected'
  error: string | null
}

const initialState: MatchesState = {
  list: [],
  listStatus: 'idle',
  active: null,
  activeStatus: 'idle',
  connection: 'disconnected',
  error: null,
}

export const fetchMatches = createAsyncThunk('matches/list', async () =>
  apiGet<MatchSummary[]>('/api/matches'),
)

export const fetchMatch = createAsyncThunk('matches/get', async (matchId: string) =>
  apiGet<MatchSnapshot>(`/api/matches/${matchId}`),
)

export const createMatch = createAsyncThunk('matches/create', async (request: CreateMatchRequest) =>
  apiPost<MatchSnapshot>('/api/matches', request),
)

export const claimSeat = createAsyncThunk(
  'matches/claimSeat',
  async ({ matchId, seatId }: { matchId: string; seatId: string }) =>
    apiPost<MatchSnapshot>(`/api/matches/${matchId}/seats/${seatId}/claim`),
)

export const appendEvent = createAsyncThunk(
  'matches/appendEvent',
  async (
    {
      matchId,
      eventType,
      payload,
      baseVersion,
    }: { matchId: string; eventType: string; payload: unknown; baseVersion: number },
    { rejectWithValue },
  ) => {
    try {
      return await apiPost<MatchSnapshot>(`/api/matches/${matchId}/events`, {
        eventType,
        payload,
        baseVersion,
        clientEventId: newClientEventId(),
      })
    } catch (error) {
      const err = error as Error & { status?: number }
      if (err.status === 409) {
        const refreshed = await apiGet<MatchSnapshot>(`/api/matches/${matchId}`)
        return rejectWithValue({ conflict: true, snapshot: refreshed, message: err.message })
      }
      throw error
    }
  },
)

export const undoEvent = createAsyncThunk(
  'matches/undoEvent',
  async (
    {
      matchId,
      targetEventId,
      baseVersion,
    }: { matchId: string; targetEventId: string; baseVersion: number },
    { rejectWithValue },
  ) => {
    try {
      return await apiPost<MatchSnapshot>(`/api/matches/${matchId}/events/undo`, {
        targetEventId,
        baseVersion,
        clientEventId: newClientEventId(),
      })
    } catch (error) {
      const err = error as Error & { status?: number }
      if (err.status === 409) {
        const refreshed = await apiGet<MatchSnapshot>(`/api/matches/${matchId}`)
        return rejectWithValue({ conflict: true, snapshot: refreshed, message: err.message })
      }
      throw error
    }
  },
)

const matchesSlice = createSlice({
  name: 'matches',
  initialState,
  reducers: {
    setConnection(state, action: PayloadAction<MatchesState['connection']>) {
      state.connection = action.payload
    },
    applyRemoteSnapshot(state, action: PayloadAction<MatchSnapshot>) {
      state.active = action.payload
      state.activeStatus = 'ready'
    },
    clearActive(state) {
      state.active = null
      state.activeStatus = 'idle'
      state.connection = 'disconnected'
    },
  },
  extraReducers: (builder) => {
    builder
      .addCase(fetchMatches.pending, (state) => {
        state.listStatus = 'loading'
      })
      .addCase(fetchMatches.fulfilled, (state, action) => {
        state.list = action.payload
        state.listStatus = 'ready'
      })
      .addCase(fetchMatches.rejected, (state, action) => {
        state.listStatus = 'error'
        state.error = action.error.message ?? 'Failed to load matches'
      })
      .addCase(fetchMatch.pending, (state) => {
        state.activeStatus = 'loading'
      })
      .addCase(fetchMatch.fulfilled, (state, action) => {
        state.active = action.payload
        state.activeStatus = 'ready'
      })
      .addCase(fetchMatch.rejected, (state, action) => {
        state.activeStatus = 'error'
        state.error = action.error.message ?? 'Failed to load match'
      })
      .addCase(createMatch.fulfilled, (state, action) => {
        state.active = action.payload
        state.activeStatus = 'ready'
      })
      .addCase(claimSeat.fulfilled, (state, action) => {
        state.active = action.payload
      })
      .addCase(appendEvent.fulfilled, (state, action) => {
        state.active = action.payload
      })
      .addCase(appendEvent.rejected, (state, action) => {
        const payload = action.payload as { snapshot?: MatchSnapshot } | undefined
        if (payload?.snapshot)
          state.active = payload.snapshot
      })
      .addCase(undoEvent.fulfilled, (state, action) => {
        state.active = action.payload
      })
      .addCase(undoEvent.rejected, (state, action) => {
        const payload = action.payload as { snapshot?: MatchSnapshot } | undefined
        if (payload?.snapshot)
          state.active = payload.snapshot
      })
  },
})

export const { setConnection, applyRemoteSnapshot, clearActive } = matchesSlice.actions
export default matchesSlice.reducer
