import { createAsyncThunk } from '@reduxjs/toolkit'
import { apiGet, apiPost } from '../api/client'
import type { AuthProvider, AuthUser } from '../types/api'

export const fetchMe = createAsyncThunk('auth/fetchMe', async () =>
  apiGet<AuthUser>('/api/auth/me'),
)

export const fetchProviders = createAsyncThunk('auth/fetchProviders', async () =>
  apiGet<AuthProvider[]>('/api/auth/providers'),
)

export const logout = createAsyncThunk('auth/logout', async () => {
  await apiPost<void>('/api/auth/logout')
})
