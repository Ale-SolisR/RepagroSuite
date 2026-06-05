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

// Reintenta el ARRANQUE INICIAL con backoff. withAutomaticReconnect sólo cubre caídas
// POSTERIORES a un start() exitoso; si el primer start falla (p. ej. cold-start del hosting
// compartido en su primer request tras el deploy), sin esto el tiempo real quedaría muerto.
function startWithRetry(conn: HubConnection, attempt = 0) {
  conn.start().catch(err => {
    // Si otra instancia ya reemplazó la conexión, no seguimos reintentando esta.
    if (connection !== conn || conn.state !== HubConnectionState.Disconnected) return
    const delay = Math.min(30000, 2000 * 2 ** attempt) // 2s, 4s, 8s, 16s, 30s, 30s…
    console.warn(`[SignalR] start failed (intento ${attempt + 1}), reintento en ${delay}ms:`, err?.message ?? err)
    setTimeout(() => {
      if (connection === conn && conn.state === HubConnectionState.Disconnected) startWithRetry(conn, attempt + 1)
    }, delay)
  })
}

function ensureConnection(token: string) {
  if (connection && connection.state !== HubConnectionState.Disconnected) return connection

  const conn = new HubConnectionBuilder()
    .withUrl(`/hubs/app?access_token=${encodeURIComponent(token)}`)
    .withAutomaticReconnect([0, 2000, 10000, 30000])
    .configureLogging(LogLevel.Warning)
    .build()
  connection = conn

  conn.on('reservation.changed', (payload: any) =>
    notify({ type: 'reservation.changed', payload }))
  conn.on('room.changed', (payload: any) =>
    notify({ type: 'room.changed', payload }))

  // Si la reconexión automática se agota, la conexión queda cerrada: reintentamos el arranque
  // (siempre que siga habiendo sesión) para no perder el tiempo real de forma permanente.
  conn.onclose(() => {
    if (connection === conn && useAuthStore.getState().accessToken) startWithRetry(conn)
  })

  startWithRetry(conn)
  return conn
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
