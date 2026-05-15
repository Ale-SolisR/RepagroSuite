import axios from 'axios'
import { useAuthStore } from '@/store/authStore'

const api = axios.create({
  baseURL: '/api/v1',
  headers: { 'Content-Type': 'application/json' },
})

api.interceptors.request.use((config) => {
  const token = useAuthStore.getState().accessToken
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

let refreshing = false
let queue: Array<{ resolve: (v: unknown) => void; reject: (e: unknown) => void }> = []

api.interceptors.response.use(
  (res) => res,
  async (error) => {
    const original = error.config
    if (error.response?.status === 401 && !original._retry) {
      if (refreshing) {
        return new Promise((resolve, reject) => queue.push({ resolve, reject }))
          .then(() => api(original))
          .catch((e) => Promise.reject(e))
      }
      original._retry = true
      refreshing = true
      try {
        const { refreshToken, setTokens, logout } = useAuthStore.getState()
        if (!refreshToken) { logout(); return Promise.reject(error) }
        const { data } = await axios.post('/api/v1/auth/refresh', { refreshToken })
        setTokens(data.data.accessToken, data.data.refreshToken)
        queue.forEach((p) => p.resolve(null))
        queue = []
        return api(original)
      } catch {
        queue.forEach((p) => p.reject(error))
        queue = []
        useAuthStore.getState().logout()
        return Promise.reject(error)
      } finally {
        refreshing = false
      }
    }
    return Promise.reject(error)
  }
)

export default api
