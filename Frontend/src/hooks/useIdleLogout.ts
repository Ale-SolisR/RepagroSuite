import { useEffect, useRef } from 'react'
import { useNavigate } from 'react-router-dom'
import toast from 'react-hot-toast'
import { useAuthStore } from '@/store/authStore'

const IDLE_TIMEOUT_MS = 3 * 60 * 60 * 1000   // 3 horas de inactividad
const STORAGE_KEY = 'repagro-last-activity'
// Incluye movimiento de mouse y clic. La escritura a localStorage se throttlea (abajo) para que
// mousemove/scroll no saturen, pero el temporizador se reprograma en cada evento.
const ACTIVITY_EVENTS = ['mousedown', 'mousemove', 'click', 'keydown', 'touchstart', 'scroll', 'visibilitychange'] as const
const WRITE_THROTTLE_MS = 15_000

export function useIdleLogout() {
  const navigate = useNavigate()
  const logout = useAuthStore(s => s.logout)
  const isAuthenticated = useAuthStore(s => s.isAuthenticated)
  const timerRef = useRef<number | null>(null)

  useEffect(() => {
    if (!isAuthenticated) return

    const expire = () => {
      logout()
      localStorage.removeItem(STORAGE_KEY)
      toast.error('Tu sesión ha expirado por inactividad. Inicia sesión nuevamente.', { duration: 6000 })
      navigate('/login', { replace: true })
    }

    const scheduleFromLastActivity = () => {
      if (timerRef.current !== null) window.clearTimeout(timerRef.current)
      const last = Number(localStorage.getItem(STORAGE_KEY)) || Date.now()
      const elapsed = Date.now() - last
      const remaining = IDLE_TIMEOUT_MS - elapsed
      if (remaining <= 0) {
        expire()
        return
      }
      timerRef.current = window.setTimeout(expire, remaining)
    }

    let lastWrite = 0
    const touch = () => {
      const now = Date.now()
      if (now - lastWrite >= WRITE_THROTTLE_MS) {
        lastWrite = now
        localStorage.setItem(STORAGE_KEY, String(now))
      }
      scheduleFromLastActivity()
    }

    if (!localStorage.getItem(STORAGE_KEY)) {
      localStorage.setItem(STORAGE_KEY, String(Date.now()))
    }
    scheduleFromLastActivity()

    ACTIVITY_EVENTS.forEach(evt => window.addEventListener(evt, touch, { passive: true }))

    const onStorage = (e: StorageEvent) => {
      if (e.key === STORAGE_KEY) scheduleFromLastActivity()
    }
    window.addEventListener('storage', onStorage)

    return () => {
      if (timerRef.current !== null) window.clearTimeout(timerRef.current)
      ACTIVITY_EVENTS.forEach(evt => window.removeEventListener(evt, touch))
      window.removeEventListener('storage', onStorage)
    }
  }, [isAuthenticated, logout, navigate])
}
