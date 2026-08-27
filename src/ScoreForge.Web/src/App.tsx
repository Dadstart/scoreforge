import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { AppShell } from './components/AppShell'
import { RequireAuth } from './components/RequireAuth'
import { HomePage } from './pages/HomePage'
import { JoinMatchPage } from './pages/JoinMatchPage'
import { LoginPage } from './pages/LoginPage'
import { MatchPage } from './pages/MatchPage'
import { NewMatchPage } from './pages/NewMatchPage'

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route element={<RequireAuth />}>
          <Route element={<AppShell />}>
            <Route path="/" element={<HomePage />} />
            <Route path="/matches/new" element={<NewMatchPage />} />
            <Route path="/matches/:matchId" element={<MatchPage />} />
            <Route path="/matches/:matchId/join" element={<JoinMatchPage />} />
          </Route>
        </Route>
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  )
}
