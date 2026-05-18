import { useEffect, useRef } from 'react'
import { useNavigate } from 'react-router-dom'
import toast from 'react-hot-toast'
import { useAuthStore } from '@/store/authStore'

const IDLE_TIMEOUT_MS = 60 * 60 * 1000
const STORAGE_KEY = 'repagro-last-activity'
const ACTIVITY_EVENTS = ['mousedown', 'keydown', 'touchstart', 'scroll', 'visibilitychange'] as const

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

    const touch = () => {
      localStorage.setItem(STORAGE_KEY, String(Date.now()))
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
