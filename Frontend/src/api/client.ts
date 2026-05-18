import axios from 'axios'
import { useAuthStore } from '@/store/authStore'

// withCredentials: imprescindible para que el navegador envíe la cookie httpOnly
// del refresh token (rp_rt) al hacer /auth/refresh y /auth/logout.
const api = axios.create({
  baseURL: '/api/v1',
  headers: { 'Content-Type': 'application/json' },
  withCredentials: true,
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
        // El refresh token va en cookie httpOnly: no necesitamos enviarlo en el body.
        // El navegador adjunta la cookie automáticamente por withCredentials.
        const { data } = await axios.post('/api/v1/auth/refresh', {}, { withCredentials: true })
        useAuthStore.getState().setAccessToken(data.data.accessToken)
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
