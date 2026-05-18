import { useEffect, useRef } from 'react'
import { HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr'
import { useAuthStore } from '@/store/authStore'

// Eventos que emite el servidor (ver AppHub.cs).
export type RealtimeEvent =
  | { type: 'reservation.changed'; payload: { reservationId: string; roomId: string; changeType: string } }
  | { type: 'room.changed';        payload: { roomId: string; changeType: string } }

type Handler = (event: RealtimeEvent) => void

// Conexión singleton compartida por toda la app (evita abrir un WebSocket por página).
let connection: HubConnection | null = null
const handlers = new Set<Handler>()

function notify(event: RealtimeEvent) {
  handlers.forEach(h => {
    try { h(event) } catch { /* el handler de un componente no debe romper al resto */ }
  })
}

function ensureConnection(token: string) {
  if (connection && connection.state !== HubConnectionState.Disconnected) return connection

  connection = new HubConnectionBuilder()
    .withUrl(`/hubs/app?access_token=${encodeURIComponent(token)}`)
    .withAutomaticReconnect([0, 2000, 10000, 30000])
    .configureLogging(LogLevel.Warning)
    .build()

  connection.on('reservation.changed', (payload: any) =>
    notify({ type: 'reservation.changed', payload }))
  connection.on('room.changed', (payload: any) =>
    notify({ type: 'room.changed', payload }))

  connection.start().catch(err => {
    console.warn('[SignalR] start failed:', err?.message ?? err)
  })

  return connection
}

// Hook a usar desde páginas: se suscribe a eventos y se desuscribe al desmontar.
// El handler se mantiene estable vía ref para no reconectar en cada render.
export function useRealtime(handler: Handler) {
  const accessToken = useAuthStore(s => s.accessToken)
  const handlerRef = useRef(handler)
  handlerRef.current = handler

  useEffect(() => {
    if (!accessToken) return

    ensureConnection(accessToken)
    const wrap: Handler = e => handlerRef.current(e)
    handlers.add(wrap)
    return () => { handlers.delete(wrap) }
  }, [accessToken])
}
