import { HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr'
import { useEffect } from 'react'
import type { MatchSnapshot } from '../types/api'
import { applyRemoteSnapshot, setConnection } from '../store/matchesSlice'
import { useAppDispatch } from '../store/hooks'

export function useMatchHub(matchId: string | undefined) {
  const dispatch = useAppDispatch()

  useEffect(() => {
    if (!matchId)
      return

    const connection = new HubConnectionBuilder()
      .withUrl('/hubs/match', {
        withCredentials: true,
        headers: { 'X-Requested-With': 'XMLHttpRequest' },
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build()

    connection.on('MatchUpdated', (snapshot: MatchSnapshot) => {
      dispatch(applyRemoteSnapshot(snapshot))
    })

    connection.onreconnecting(() => dispatch(setConnection('connecting')))
    connection.onreconnected(() => {
      dispatch(setConnection('connected'))
      void connection.invoke('JoinMatch', matchId)
    })
    connection.onclose(() => dispatch(setConnection('disconnected')))

    let cancelled = false

    async function start() {
      dispatch(setConnection('connecting'))
      await connection.start()
      if (cancelled)
        return
      await connection.invoke('JoinMatch', matchId)
      dispatch(setConnection('connected'))
    }

    void start().catch(() => dispatch(setConnection('disconnected')))

    return () => {
      cancelled = true
      if (connection.state !== HubConnectionState.Disconnected)
        void connection.stop()
      dispatch(setConnection('disconnected'))
    }
  }, [dispatch, matchId])
}
