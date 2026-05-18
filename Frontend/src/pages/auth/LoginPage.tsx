import { useState, useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useNavigate, Link } from 'react-router-dom'
import { Eye, EyeOff, AlertCircle, ArrowLeft, CheckCircle2, XCircle } from 'lucide-react'
import { authApi } from '@/api/auth'
import { useAuthStore } from '@/store/authStore'
import { extractApiError } from '@/utils'
import api from '@/api/client'
import { usersApi } from '@/api/users'
import Input from '@/components/ui/Input'
import BrandPanel from '@/components/auth/BrandPanel'
import toast from 'react-hot-toast'
import type { IdentificationResultDto } from '@/types'

// ─── Schemas ─────────────────────────────────────────────────────────────────
const loginSchema = z.object({
  email: z.string().email('Correo inválido'),
  password: z.string().min(8, 'La contraseña debe tener al menos 8 caracteres'),
})
type LoginFormData = z.infer<typeof loginSchema>

const registerSchema = z.object({
  identificationNumber: z.string().min(9, 'Ingrese al menos 9 dígitos'),
  email: z.string().email('Correo inválido'),
  phoneNumber: z.string().optional(),
  department: z.string().optional(),
  position: z.string().optional(),
})
type RegisterFormData = z.infer<typeof registerSchema>
type LookupState = 'idle' | 'loading' | 'found' | 'error'

type Mode = 'login' | 'register'


// ─── Formulario de Login ─────────────────────────────────────────────────────
function LoginForm({ onSwitchMode }: { onSwitchMode: (mode: Mode) => void }) {
  const navigate = useNavigate()
  const { setAuth } = useAuthStore()
  const [showPassword, setShowPassword] = useState(false)
  const [generalError, setGeneralError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<LoginFormData>({ resolver: zodResolver(loginSchema) })

  async function onSubmit(data: LoginFormData) {
    setGeneralError(null)
    try {
      const res = await authApi.login(data)
      const { accessToken, user, mustChangePassword } = res.data.data!
      // refresh token llega como cookie httpOnly — no lo tocamos.
      setAuth(accessToken, { ...user, mustChangePassword, isMaster: user.isMaster })
      if (mustChangePassword) {
        navigate('/forced-change-password')
      } else {
        const isAdmin = user.roles?.includes('ADMINISTRATOR')
        navigate(isAdmin ? '/dashboard' : '/rooms')
      }
    } catch (err) {
      setGeneralError(extractApiError(err))
    }
  }

  return (
    <>
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
        <button
          type="button"
          onClick={() => onSwitchMode('register')}
          className="font-medium transition-colors hover:underline"
          style={{ color: '#006F55' }}
        >
          Solicita acceso
        </button>
      </p>
    </>
  )
}


// ─── Formulario de Solicitud de Acceso ──────────────────────────────────────
function RegisterForm({ onSwitchMode }: { onSwitchMode: (mode: Mode) => void }) {
  const [lookupState, setLookupState] = useState<LookupState>('idle')
  const [idResult, setIdResult] = useState<IdentificationResultDto | null>(null)

  const { register, handleSubmit, watch, formState: { errors, isSubmitting } } = useForm<RegisterFormData>({
    resolver: zodResolver(registerSchema),
  })

  const idNumber = watch('identificationNumber')

  useEffect(() => {
    const digits = idNumber?.replace(/\D/g, '') ?? ''
    if (digits.length < 9) {
      setLookupState('idle')
      setIdResult(null)
      return
    }

    setLookupState('loading')
    const timer = setTimeout(async () => {
      try {
        const res = await api.get('/identifications/lookup', { params: { identificationNumber: digits } })
        setIdResult(res.data.data)
        setLookupState('found')
      } catch {
        setIdResult(null)
        setLookupState('error')
      }
    }, 500)

    return () => clearTimeout(timer)
  }, [idNumber])

  async function onSubmit(data: RegisterFormData) {
    try {
      await usersApi.register(data)
      toast.success('Solicitud enviada. Un administrador revisará su solicitud.')
      onSwitchMode('login')
    } catch (err) {
      toast.error(extractApiError(err))
    }
  }

  const idBorderClass =
    errors.identificationNumber
      ? 'border-red-400 focus:border-red-500 focus:ring-1 focus:ring-red-500'
      : lookupState === 'found'
      ? 'border-green-500 focus:border-green-600 focus:ring-1 focus:ring-green-600'
      : lookupState === 'error'
      ? 'border-amber-400 focus:border-amber-500 focus:ring-1 focus:ring-amber-500'
      : 'border-gray-300 focus:border-green-600 focus:ring-1 focus:ring-green-600'

  return (
    <>
      {/* Encabezado */}
      <div className="mb-6">
        <button
          type="button"
          onClick={() => onSwitchMode('login')}
          className="mb-3 inline-flex items-center gap-1.5 text-[13px] font-medium transition-colors hover:underline"
          style={{ color: '#005947' }}
        >
          <ArrowLeft className="h-3.5 w-3.5" />
          Volver a iniciar sesión
        </button>
        <h2
          className="text-[32px] font-semibold tracking-tight leading-tight"
          style={{ color: '#1F2933' }}
        >
          Solicitar acceso
        </h2>
        <p className="mt-2 text-[15px]" style={{ color: '#5F6B7A' }}>
          Completa el formulario y un administrador revisará tu solicitud.
        </p>
      </div>

      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">

        {/* Número de identificación con auto-lookup */}
        <div className="flex flex-col gap-1.5">
          <label htmlFor="id-number" className="text-[13px] font-medium" style={{ color: '#1F2933' }}>
            Número de Identificación <span style={{ color: '#B42318' }}>*</span>
          </label>
          <div className="relative">
            <input
              id="id-number"
              type="text"
              inputMode="numeric"
              placeholder="Ej: 123456789"
              className={[
                'w-full rounded-md border px-3 py-2.5 pr-9 text-sm outline-none transition',
                'placeholder:text-gray-400 disabled:bg-gray-50 disabled:text-gray-500',
                idBorderClass,
              ].join(' ')}
              {...register('identificationNumber')}
            />
            <div className="absolute right-2.5 top-1/2 -translate-y-1/2 pointer-events-none">
              {lookupState === 'loading' && (
                <svg className="h-4 w-4 animate-spin text-gray-400" viewBox="0 0 24 24" fill="none" aria-hidden="true">
                  <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                  <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8H4z" />
                </svg>
              )}
              {lookupState === 'found' && <CheckCircle2 className="h-4 w-4 text-green-600" />}
              {lookupState === 'error' && <XCircle className="h-4 w-4 text-amber-500" />}
            </div>
          </div>
          {errors.identificationNumber && (
            <p className="text-[13px]" style={{ color: '#B42318' }}>{errors.identificationNumber.message}</p>
          )}
        </div>

        {/* Resultado del lookup */}
        {lookupState === 'found' && idResult && (
          <div className="rounded-lg bg-green-50 border border-green-200 px-3 py-2.5 text-sm">
            <p className="font-semibold text-green-800">{idResult.fullName}</p>
            <p className="text-green-600 text-xs mt-0.5">
              {idResult.identificationTypeName ?? idResult.identificationType} · {idResult.identificationNumber}
            </p>
          </div>
        )}
        {lookupState === 'error' && (
          <p className="text-xs text-amber-600 -mt-1">
            No se encontró información para esta identificación. Puede continuar; será verificada al momento de la aprobación.
          </p>
        )}

        <Input
          label="Correo Electrónico Institucional"
          type="email"
          placeholder="usuario@repagro.com"
          error={errors.email?.message}
          required
          {...register('email')}
        />

        <div className="grid grid-cols-2 gap-3">
          <Input
            label="Teléfono"
            placeholder="8888-8888"
            {...register('phoneNumber')}
          />
          <Input
            label="Departamento"
            placeholder="Ej: Contabilidad"
            {...register('department')}
          />
        </div>

        <Input
          label="Puesto"
          placeholder="Ej: Analista"
          {...register('position')}
        />

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
              Enviando…
            </>
          ) : (
            'Enviar Solicitud'
          )}
        </button>
      </form>

      <p className="mt-6 text-center text-[12px]" style={{ color: '#9CA3AF' }}>
        Una vez enviada la solicitud, recibirás un correo con el resultado de la revisión.
        Si es aprobada, se te enviará una contraseña temporal para tu primer acceso.
      </p>
    </>
  )
}


// ─── Página principal ─────────────────────────────────────────────────────────
export default function LoginPage() {
  const [mode, setMode] = useState<Mode>('login')

  return (
    <div className="min-h-screen grid lg:grid-cols-2">
      <BrandPanel />

      {/* Panel del formulario */}
      <div className="flex flex-col items-center justify-center bg-paper px-6 py-12">
        <div className="w-full" style={{ maxWidth: 460 }}>
          {mode === 'login'
            ? <LoginForm onSwitchMode={setMode} />
            : <RegisterForm onSwitchMode={setMode} />
          }
        </div>
      </div>
    </div>
  )
}
