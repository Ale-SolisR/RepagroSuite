import { create } from 'zustand'
import { persist } from 'zustand/middleware'
import type { UserInfo } from '@/types'

// Nota de seguridad: NO almacenamos el refresh token aquí.
// El refresh token vive en una cookie httpOnly + Secure + SameSite=Strict gestionada por el backend
// y nunca es visible al JavaScript del navegador (mitigación XSS).
// El access token sí vive en localStorage por simplicidad y porque su vida es corta (60min) — un robo
// de access token es mucho menos grave que el robo de un refresh token de 30 días.

interface AuthState {
  accessToken: string | null
  user: UserInfo | null
  isAuthenticated: boolean
  setAuth: (accessToken: string, user: UserInfo) => void
  setAccessToken: (accessToken: string) => void
  logout: () => void
  hasPermission: (permission: string) => boolean
  hasRole: (role: string) => boolean
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set, get) => ({
      accessToken: null,
      user: null,
      isAuthenticated: false,

      setAuth: (accessToken, user) => {
        // Reinicia el reloj de inactividad en cada login para no heredar una marca vieja
        // de una sesión anterior (evita el falso "sesión expirada" justo al entrar).
        localStorage.setItem('repagro-last-activity', String(Date.now()))
        set({ accessToken, user, isAuthenticated: true })
      },

      setAccessToken: (accessToken) =>
        set({ accessToken }),

      logout: () =>
        set({ accessToken: null, user: null, isAuthenticated: false }),

      hasPermission: (permission) =>
        get().user?.permissions.includes(permission) ?? false,

      hasRole: (role) =>
        get().user?.roles.includes(role) ?? false,
    }),
    {
      name: 'repagro-auth',
      partialize: (state) => ({
        accessToken: state.accessToken,
        user: state.user,
        isAuthenticated: state.isAuthenticated,
      }),
    }
  )
)
