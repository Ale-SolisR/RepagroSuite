import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useNavigate, Link } from 'react-router-dom'
import { Eye, EyeOff, AlertCircle } from 'lucide-react'
import { authApi } from '@/api/auth'
import { useAuthStore } from '@/store/authStore'
import { extractApiError } from '@/utils'
import repagroLogo from '@/assets/repagro-logo.png'

// ─── Schema ───────────────────────────────────────────────────────────────────
const schema = z.object({
  email: z.string().email('Correo inválido'),
  password: z.string().min(8, 'La contraseña debe tener al menos 8 caracteres'),
})
type FormData = z.infer<typeof schema>


// ─── Panel izquierdo (marca Repagro) ─────────────────────────────────────────
function BrandPanel() {
  return (
    <div
      className="hidden lg:flex flex-col justify-between p-12 relative overflow-hidden"
      style={{ background: 'linear-gradient(135deg, #003E2D 0%, #005947 55%, #006F55 100%)' }}
      aria-hidden="true"
    >
      {/* Motivo geométrico dorado — esquina superior derecha */}
      <div className="absolute top-0 right-0 w-[340px] h-[340px] pointer-events-none">
        {/* Halo radial difuso */}
        <div
          className="absolute rounded-full"
          style={{
            top: -80, right: -80, width: 280, height: 280,
            background: 'radial-gradient(circle, rgba(245,197,24,.28), transparent 65%)',
            filter: 'blur(20px)',
          }}
        />
        {/* SVG — barras y constelación */}
        <svg
          viewBox="0 0 340 340"
          className="absolute inset-0 w-full h-full"
          xmlns="http://www.w3.org/2000/svg"
        >
          <defs>
            <linearGradient id="goldShine" x1="0%" y1="0%" x2="100%" y2="0%">
              <stop offset="0%"   stopColor="#FFEFA8" stopOpacity="0" />
              <stop offset="30%"  stopColor="#FFD84D" stopOpacity="1" />
              <stop offset="55%"  stopColor="#FFF6D6" stopOpacity="1" />
              <stop offset="80%"  stopColor="#F5C518" stopOpacity="1" />
              <stop offset="100%" stopColor="#D9A800" stopOpacity="1" />
            </linearGradient>
            <linearGradient id="goldShineThin" x1="0%" y1="0%" x2="100%" y2="0%">
              <stop offset="0%"   stopColor="#FFEFA8" stopOpacity="0" />
              <stop offset="100%" stopColor="#F5C518" stopOpacity="1" />
            </linearGradient>
          </defs>
          {/* Barra principal */}
          <rect x="52"  y="180" width="288" height="14" rx="2" fill="url(#goldShine)" />
          {/* Barra media */}
          <rect x="92"  y="202" width="248" height="6"  rx="1" fill="url(#goldShineThin)" />
          {/* Barra fina */}
          <rect x="172" y="214" width="168" height="3"  rx="1" fill="url(#goldShineThin)" opacity="0.85" />
          {/* Tick horizontal */}
          <rect x="252" y="222" width="88"  height="2"  rx="1" fill="#FFD84D" opacity="0.7" />
          {/* Acento vertical */}
          <rect x="320" y="120" width="3"   height="118" rx="1" fill="#F5C518" opacity="0.85" />
          {/* Acento diagonal */}
          <rect x="280" y="140" width="2.5" height="60"  rx="1" fill="#FFD84D" opacity="0.55"
            transform="rotate(28 280 140)" />
          {/* Constelación */}
          <circle cx="290" cy="155" r="3"   fill="#F5C518" />
          <circle cx="308" cy="168" r="2"   fill="#FFD84D" />
          <circle cx="275" cy="172" r="1.5" fill="#FFEFA8" />
        </svg>
        {/* Anillo exterior */}
        <div
          className="absolute rounded-full"
          style={{
            top: 110, right: 30,
            width: 160, height: 160,
            border: '1.5px solid rgba(245,197,24,.35)',
          }}
        />
        {/* Anillo interior */}
        <div
          className="absolute rounded-full"
          style={{
            top: 136, right: 56,
            width: 108, height: 108,
            border: '1px solid rgba(255,239,168,.20)',
          }}
        />
      </div>

      {/* Logo */}
      <div className="relative z-10 flex items-center">
        <div className="flex items-center justify-center rounded-xl bg-white px-5 py-3 shrink-0">
          <img src={repagroLogo} alt="Repagro" className="h-16 w-auto" />
        </div>
      </div>

      {/* Copy central */}
      <div className="relative z-10">
        <p
          className="font-mono text-[13px] tracking-[.14em] uppercase mb-5"
          style={{ color: '#FFD84D' }}
        >
          Repagro Suite
        </p>
        <h1
          className="text-[40px] font-medium leading-[1.18] text-white mb-4"
          style={{ maxWidth: 380 }}
        >
          Una plataforma<br />para tu trabajo<br />diario.
        </h1>
        <p className="text-[15px]" style={{ color: 'rgba(255,255,255,.70)', maxWidth: 380 }}>
          Salas, inventario, activos TI, boletas y más.<br />Todo en un solo lugar.
        </p>
      </div>

      {/* Pie de panel */}
      <div className="relative z-10 flex items-center justify-between">
        <span className="font-mono text-[12px]" style={{ color: 'rgba(255,255,255,.5)' }}>
          © 2026 Repagro · v6.1
        </span>
        {/* Decoración dorada */}
        <div className="flex items-center gap-1.5">
          <div
            className="h-px w-14"
            style={{ background: 'linear-gradient(to right, transparent, #F5C518)' }}
          />
          <div
            className="h-2 w-2 rounded-full shrink-0"
            style={{ background: '#F5C518', boxShadow: '0 0 8px 3px rgba(245,197,24,.55)' }}
          />
          <div
            className="h-px w-14"
            style={{ background: 'linear-gradient(to left, transparent, #F5C518)' }}
          />
        </div>
      </div>
    </div>
  )
}

// ─── Página principal ─────────────────────────────────────────────────────────
export default function LoginPage() {
  const navigate = useNavigate()
  const { setAuth } = useAuthStore()
  const [showPassword, setShowPassword] = useState(false)
  const [rememberMe, setRememberMe]     = useState(false)
  const [generalError, setGeneralError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<FormData>({ resolver: zodResolver(schema) })

  async function onSubmit(data: FormData) {
    setGeneralError(null)
    try {
      const res = await authApi.login(data)
      const { accessToken, refreshToken, user } = res.data.data!
      setAuth(accessToken, refreshToken, user)
      navigate(user.mustChangePassword ? '/forced-change-password' : '/dashboard')
    } catch (err) {
      setGeneralError(extractApiError(err))
    }
  }

  return (
    <div className="min-h-screen grid lg:grid-cols-2">
      <BrandPanel />

      {/* Panel del formulario */}
      <div className="flex flex-col items-center justify-center bg-paper px-6 py-12">
        <div className="w-full" style={{ maxWidth: 420 }}>

          {/* Encabezado */}
          <div className="mb-8">
            <h2
              className="text-[32px] font-semibold tracking-tight leading-tight"
              style={{ color: '#1F2933' }}
            >
              Iniciar sesión
            </h2>
            <p className="mt-2 text-[15px]" style={{ color: '#5F6B7A' }}>
              Ingresa con tu cuenta corporativa.
            </p>
          </div>

          {/* Error general */}
          {generalError && (
            <div
              className="mb-5 flex items-start gap-2.5 rounded-md border px-4 py-3 text-sm"
              style={{ background: '#FEF2F2', borderColor: '#FECACA', color: '#991B1B' }}
              role="alert"
            >
              <AlertCircle className="h-4 w-4 mt-0.5 shrink-0" style={{ color: '#B42318' }} aria-hidden="true" />
              {generalError}
            </div>
          )}

          <form onSubmit={handleSubmit(onSubmit)} noValidate className="space-y-5">

            {/* Campo correo */}
            <div className="flex flex-col gap-1.5">
              <label htmlFor="email" className="text-[13px] font-medium" style={{ color: '#1F2933' }}>
                Correo
              </label>
              <input
                id="email"
                type="email"
                placeholder="tu.nombre@repagro.com"
                autoComplete="email"
                aria-invalid={errors.email ? 'true' : 'false'}
                aria-describedby={errors.email ? 'email-err' : undefined}
                {...register('email')}
                className="form-input"
              />
              {errors.email && (
                <p id="email-err" className="text-[13px]" style={{ color: '#B42318' }}>
                  {errors.email.message}
                </p>
              )}
            </div>

            {/* Campo contraseña */}
            <div className="flex flex-col gap-1.5">
              <div className="flex items-center justify-between">
                <label htmlFor="password" className="text-[13px] font-medium" style={{ color: '#1F2933' }}>
                  Contraseña
                </label>
                <Link
                  to="/forgot-password"
                  className="text-[13px] font-medium transition-colors hover:underline"
                  style={{ color: '#005947' }}
                >
                  ¿Olvidaste tu contraseña?
                </Link>
              </div>
              <div className="relative">
                <input
                  id="password"
                  type={showPassword ? 'text' : 'password'}
                  placeholder="••••••••"
                  autoComplete="current-password"
                  aria-invalid={errors.password ? 'true' : 'false'}
                  aria-describedby={errors.password ? 'password-err' : undefined}
                  {...register('password')}
                  className="form-input"
                  style={{ paddingRight: '2.75rem' }}
                />
                <button
                  type="button"
                  onClick={() => setShowPassword(p => !p)}
                  className="absolute right-3 top-1/2 -translate-y-1/2 rounded p-0.5 transition-colors"
                  style={{ color: '#9CA3AF' }}
                  aria-label={showPassword ? 'Ocultar contraseña' : 'Mostrar contraseña'}
                >
                  {showPassword
                    ? <EyeOff className="h-4 w-4" strokeWidth={1.5} />
                    : <Eye    className="h-4 w-4" strokeWidth={1.5} />
                  }
                </button>
              </div>
              {errors.password && (
                <p id="password-err" className="text-[13px]" style={{ color: '#B42318' }}>
                  {errors.password.message}
                </p>
              )}
            </div>

            {/* Mantener sesión */}
            <label className="flex cursor-pointer items-center gap-2.5 select-none">
              <button
                type="button"
                role="checkbox"
                aria-checked={rememberMe}
                onClick={() => setRememberMe(p => !p)}
                className="flex h-4 w-4 shrink-0 items-center justify-center rounded-[4px] border transition"
                style={{
                  background:   rememberMe ? '#006F55' : '#fff',
                  borderColor:  rememberMe ? '#006F55' : '#D9DEE5',
                }}
              >
                {rememberMe && (
                  <svg width="10" height="8" viewBox="0 0 10 8" aria-hidden="true">
                    <path
                      d="M1 4l2.5 2.5L9 1"
                      stroke="#fff" strokeWidth="1.5"
                      strokeLinecap="round" strokeLinejoin="round"
                      fill="none"
                    />
                  </svg>
                )}
              </button>
              <span className="text-sm" style={{ color: '#1F2933' }}>
                Mantener sesión iniciada
              </span>
            </label>

            {/* Botón ingresar */}
            <button
              type="submit"
              disabled={isSubmitting}
              className="flex h-12 w-full items-center justify-center gap-2 rounded-[6px] text-[15px] font-medium text-white transition disabled:opacity-60"
              style={{ background: '#006F55' }}
              onMouseEnter={e => { if (!isSubmitting) e.currentTarget.style.background = '#005947' }}
              onMouseLeave={e => { if (!isSubmitting) e.currentTarget.style.background = '#006F55' }}
            >
              {isSubmitting ? (
                <>
                  <svg className="h-4 w-4 animate-spin" viewBox="0 0 24 24" fill="none" aria-hidden="true">
                    <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                    <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8H4z" />
                  </svg>
                  Verificando…
                </>
              ) : (
                'Ingresar'
              )}
            </button>
          </form>

          {/* Pie de formulario */}
          <p className="mt-8 text-center text-[14px]" style={{ color: '#5F6B7A' }}>
            ¿No tienes cuenta?{' '}
            <Link
              to="/register"
              className="font-medium transition-colors hover:underline"
              style={{ color: '#006F55' }}
            >
              Solicita acceso
            </Link>
          </p>
        </div>
      </div>
    </div>
  )
}
