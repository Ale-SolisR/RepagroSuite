import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import axios from 'axios'
import { Eye, EyeOff, AlertCircle, Sprout, ExternalLink, Mail, Lock } from 'lucide-react'

// URL de la app web de Rastreo (PWA separada). Configurable por entorno; en dev usa el mismo origen.
// Define VITE_RASTREO_URL en .env para apuntar al despliegue real (Netlify, etc.).
const RASTREO_URL = (import.meta.env.VITE_RASTREO_URL ?? '').replace(/\/+$/, '')

const schema = z.object({
  correo: z.string().email('Correo inválido'),
  password: z.string().min(1, 'Ingresa tu contraseña'),
})
type FormData = z.infer<typeof schema>

interface RastreoLoginResponse {
  token: string
  correo: string
  nombre?: string
  rol?: string
  id: number
  expira: string
}

/**
 * Login del SISTEMA DE RASTREO desde el portal único de Repagro.
 * Valida contra RASTREO.Usuarios (endpoint /api/auth/login, esquema JWT "Rastreo", aislado).
 * En éxito entrega el token a la app de Rastreo vía fragmento de URL (#rt=...&ru=...) y redirige.
 */
export default function RastreoLoginForm() {
  const [showPassword, setShowPassword] = useState(false)
  const [generalError, setGeneralError] = useState<string | null>(null)
  const [conflict, setConflict] = useState<FormData | null>(null)   // sesión activa → ofrecer forzar
  const [loading, setLoading] = useState(false)

  const { register, handleSubmit, formState: { errors } } = useForm<FormData>({ resolver: zodResolver(schema) })

  async function doLogin(data: FormData, forzar: boolean) {
    setGeneralError(null)
    setLoading(true)
    try {
      // Ruta absoluta: el endpoint de Rastreo NO está bajo /api/v1 (lo proxea Vite a /api).
      const res = await axios.post<RastreoLoginResponse>('/api/auth/login', { ...data, forzar })
      const r = res.data
      // Entrega segura del token a la app de Rastreo (fragmento, no se envía al servidor ni queda en logs).
      const payload = encodeURIComponent(JSON.stringify({
        token: r.token,
        user: { uid: r.id, correo: r.correo, nombre: r.nombre, rol: r.rol },
      }))
      const target = `${RASTREO_URL || ''}/#rastreo_handoff=${payload}`
      window.location.assign(target)
    } catch (err) {
      if (axios.isAxiosError(err) && err.response?.status === 409) {
        setConflict(data)   // ya hay una sesión activa en otro dispositivo
      } else if (axios.isAxiosError(err) && err.response?.status === 401) {
        setGeneralError('Tu usuario no tiene acceso al Sistema de Rastreo o las credenciales son incorrectas.')
      } else {
        setGeneralError('No se pudo iniciar sesión. Intenta de nuevo.')
      }
    } finally {
      setLoading(false)
    }
  }

  return (
    <>
      <div className="mb-8">
        <span className="inline-flex items-center gap-1.5 rounded-full bg-teal-50 px-2.5 py-1 text-[12px] font-medium text-teal-700 ring-1 ring-teal-200">
          <Sprout className="h-3.5 w-3.5" /> Sistema de Rastreo
        </span>
        <h2 className="mt-3 text-[26px] sm:text-[32px] font-semibold tracking-tight leading-tight" style={{ color: '#13211C' }}>
          Iniciar sesión
        </h2>
        <p className="mt-2 text-[15px]" style={{ color: '#4A5750' }}>
          Acceso al sistema de registro de matanza y granjas.
        </p>
      </div>

      {generalError && (
        <div className="mb-5 flex items-start gap-2.5 rounded-md border px-4 py-3 text-sm"
          style={{ background: '#FEF2F2', borderColor: '#FECACA', color: '#991B1B' }} role="alert">
          <AlertCircle className="h-4 w-4 mt-0.5 shrink-0" style={{ color: '#B42318' }} />
          {generalError}
        </div>
      )}

      {conflict ? (
        <div className="space-y-4">
          <div className="flex items-start gap-2.5 rounded-md border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-800">
            <AlertCircle className="h-4 w-4 mt-0.5 shrink-0 text-amber-600" />
            <p>Ya hay una sesión activa de este usuario en otro dispositivo. Si continúas, esa sesión se cerrará.</p>
          </div>
          <div className="flex gap-2">
            <button type="button" onClick={() => setConflict(null)}
              className="h-12 flex-1 rounded-[6px] border text-[15px] font-medium" style={{ borderColor: '#D1D5DB', color: '#13211C' }}>
              Cancelar
            </button>
            <button type="button" disabled={loading} onClick={() => doLogin(conflict, true)}
              className="h-12 flex-1 rounded-[6px] text-[15px] font-medium text-white disabled:opacity-60" style={{ background: '#0f766e' }}>
              {loading ? 'Entrando…' : 'Cerrar la otra sesión e ingresar'}
            </button>
          </div>
        </div>
      ) : (
        <form onSubmit={handleSubmit(d => doLogin(d, false))} noValidate className="space-y-5">
          <div className="flex flex-col gap-1.5">
            <label htmlFor="r-correo" className="text-[13px] font-medium" style={{ color: '#13211C' }}>Correo</label>
            <div className="relative">
              <Mail className="pointer-events-none absolute left-3.5 top-1/2 -translate-y-1/2 h-4 w-4 text-ink2/50" strokeWidth={1.75} aria-hidden="true" />
              <input id="r-correo" type="email" placeholder="usuario@granja.com" autoComplete="email"
                {...register('correo')} className="form-input" style={{ paddingLeft: '2.75rem' }} />
            </div>
            {errors.correo && <p className="text-[13px]" style={{ color: '#B42318' }}>{errors.correo.message}</p>}
          </div>

          <div className="flex flex-col gap-1.5">
            <label htmlFor="r-password" className="text-[13px] font-medium" style={{ color: '#13211C' }}>Contraseña</label>
            <div className="relative">
              <Lock className="pointer-events-none absolute left-3.5 top-1/2 -translate-y-1/2 h-4 w-4 text-ink2/50" strokeWidth={1.75} aria-hidden="true" />
              <input id="r-password" type={showPassword ? 'text' : 'password'} placeholder="••••••••"
                autoComplete="current-password" {...register('password')} className="form-input" style={{ paddingLeft: '2.75rem', paddingRight: '2.75rem' }} />
              <button type="button" onClick={() => setShowPassword(p => !p)}
                className="absolute right-3 top-1/2 -translate-y-1/2 rounded p-0.5" style={{ color: '#9CA3AF' }}
                aria-label={showPassword ? 'Ocultar contraseña' : 'Mostrar contraseña'}>
                {showPassword ? <EyeOff className="h-4 w-4" strokeWidth={1.5} /> : <Eye className="h-4 w-4" strokeWidth={1.5} />}
              </button>
            </div>
            {errors.password && <p className="text-[13px]" style={{ color: '#B42318' }}>{errors.password.message}</p>}
          </div>

          <button type="submit" disabled={loading}
            className="flex h-12 w-full items-center justify-center gap-2 rounded-[6px] text-[15px] font-medium text-white transition disabled:opacity-60"
            style={{ background: '#0f766e' }}>
            {loading ? 'Verificando…' : <>Ingresar a Rastreo <ExternalLink className="h-4 w-4" /></>}
          </button>
        </form>
      )}

      <p className="mt-8 text-center text-[13px]" style={{ color: '#9CA3AF' }}>
        ¿Olvidaste tu contraseña? Contacta a un administrador de Repagro para restablecerla.
      </p>
    </>
  )
}
