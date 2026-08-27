import { createSlice } from '@reduxjs/toolkit'
import type { AuthProvider, AuthUser } from '../types/api'
import { fetchMe, fetchProviders, logout } from './authApi'

type AuthState = {
  user: AuthUser | null
  providers: AuthProvider[]
  status: 'idle' | 'loading' | 'ready' | 'error'
  error: string | null
}

const initialState: AuthState = {
  user: null,
  providers: [],
  status: 'idle',
  error: null,
}

const authSlice = createSlice({
  name: 'auth',
  initialState,
  reducers: {},
  extraReducers: (builder) => {
    builder
      .addCase(fetchMe.pending, (state) => {
        state.status = 'loading'
        state.error = null
      })
      .addCase(fetchMe.fulfilled, (state, action) => {
        state.user = action.payload
        state.status = 'ready'
      })
      .addCase(fetchMe.rejected, (state, action) => {
        state.status = 'error'
        state.error = action.error.message ?? 'Failed to load auth'
      })
      .addCase(fetchProviders.fulfilled, (state, action) => {
        state.providers = action.payload
      })
      .addCase(logout.fulfilled, (state) => {
        state.user = { isAuthenticated: false, userId: null, displayName: null, email: null }
      })
  },
})

export default authSlice.reducer
