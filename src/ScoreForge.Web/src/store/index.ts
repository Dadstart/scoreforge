import { configureStore } from '@reduxjs/toolkit'
import authReducer from './authSlice'
import matchesReducer from './matchesSlice'

export const store = configureStore({
  reducer: {
    auth: authReducer,
    matches: matchesReducer,
  },
})

export type RootState = ReturnType<typeof store.getState>
export type AppDispatch = typeof store.dispatch
