import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useNavigate } from 'react-router-dom'
import { KeyRound, Eye, EyeOff } from 'lucide-react'
import { authApi } from '@/api/auth'
import { useAuthStore } from '@/store/authStore'
import { extractApiError } from '@/utils'
import BrandPanel from '@/components/auth/BrandPanel'
import toast from 'react-hot-toast'

const schema = z.object({
  currentPassword: z.string().min(1, 'Ingrese su contraseña actual'),
  newPassword: z.string().min(8, 'Mínimo 8 caracteres').regex(
    /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d])/,
    'Debe contener mayúscula, minúscula, número y carácter especial'
  ),
  confirmNewPassword: z.string(),
}).refine(d => d.newPassword === d.confirmNewPassword, {
  message: 'Las contraseñas no coinciden',
  path: ['confirmNewPassword'],
})

type FormData = z.infer<typeof schema>

interface PasswordFieldProps {
  id: string
  label: string
  helperText?: string
  error?: string
  show: boolean
  onToggle: () => void
  registration: ReturnType<ReturnType<typeof useForm<FormData>>['register']>
}

function PasswordField({ id, label, helperText, error, show, onToggle, registration }: PasswordFieldProps) {
  return (
    <div className="flex flex-col gap-1.5">
      <label htmlFor={id} className="text-[13px] font-medium" style={{ color: '#13211C' }}>
        {label} <span style={{ color: '#B42318' }}>*</span>
      </label>
      <div className="relative">
        <input
          id={id}
          type={show ? 'text' : 'password'}
          className="form-input"
          style={{ paddingRight: '2.75rem' }}
          {...registration}
        />
        <button
          type="button"
          onClick={onToggle}
          className="absolute right-3 top-1/2 -translate-y-1/2 rounded p-0.5 transition-colors"
          style={{ color: '#9CA3AF' }}
          aria-label={show ? 'Ocultar contraseña' : 'Mostrar contraseña'}
        >
          {show ? <EyeOff className="h-4 w-4" strokeWidth={1.5} /> : <Eye className="h-4 w-4" strokeWidth={1.5} />}
        </button>
      </div>
      {error && <p className="text-[13px]" style={{ color: '#B42318' }}>{error}</p>}
      {helperText && !error && <p className="text-[12px]" style={{ color: '#6B7280' }}>{helperText}</p>}
    </div>
  )
}

export default function ForcedChangePasswordPage() {
  const navigate = useNavigate()
  const { user, setAuth, accessToken, hasRole } = useAuthStore()
  const [show, setShow] = useState({ current: false, new: false, confirm: false })

  const { register, handleSubmit, watch, formState: { errors, isSubmitting } } = useForm<FormData>({
    resolver: zodResolver(schema),
  })

  const newPassword     = watch('newPassword')     ?? ''
  const confirmPassword = watch('confirmNewPassword') ?? ''
  const passwordsMatch  = newPassword.length > 0 && confirmPassword.length > 0 && newPassword === confirmPassword
  const passwordsMismatch = confirmPassword.length > 0 && newPassword !== confirmPassword

  async function onSubmit(data: FormData) {
    try {
      await authApi.forcedChangePassword(data)
      if (user && accessToken) {
        setAuth(accessToken, { ...user, mustChangePassword: false })
      }
      toast.success('Contraseña actualizada correctamente')
      navigate(hasRole('ADMINISTRATOR') ? '/dashboard' : '/rooms')
    } catch (err) {
      toast.error(extractApiError(err))
    }
  }

  return (
    <div className="min-h-screen grid lg:grid-cols-2">
      <BrandPanel />

      {/* Panel del formulario */}
      <div className="flex flex-col items-center justify-center bg-paper px-6 py-12">
        <div className="w-full" style={{ maxWidth: 460 }}>

          {/* Encabezado */}
          <div className="mb-6 flex items-start gap-3">
            <div className="h-11 w-11 rounded-full flex items-center justify-center shrink-0" style={{ background: '#F59E0B' }}>
              <KeyRound className="h-5 w-5 text-white" />
            </div>
            <div>
              <h2
                className="text-[26px] font-semibold tracking-tight leading-tight"
                style={{ color: '#13211C' }}
              >
                Cambio de contraseña requerido
              </h2>
              <p className="mt-1 text-[14px]" style={{ color: '#4A5750' }}>
                Por seguridad debe establecer una nueva contraseña.
              </p>
            </div>
          </div>

          {/* Aviso */}
          <div
            className="mb-6 rounded-md border px-4 py-3 text-sm"
            style={{ background: '#FFFBEB', borderColor: '#FDE68A', color: '#92400E' }}
          >
            Bienvenido, <strong>{user?.fullName}</strong>. Su contraseña es temporal y debe ser cambiada antes de continuar.
          </div>

          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
            <PasswordField
              id="current-password"
              label="Contraseña actual (temporal)"
              error={errors.currentPassword?.message}
              show={show.current}
              onToggle={() => setShow(s => ({ ...s, current: !s.current }))}
              registration={register('currentPassword')}
            />
            <PasswordField
              id="new-password"
              label="Nueva contraseña"
              helperText="Mínimo 8 caracteres con mayúscula, número y carácter especial"
              error={errors.newPassword?.message}
              show={show.new}
              onToggle={() => setShow(s => ({ ...s, new: !s.new }))}
              registration={register('newPassword')}
            />

            {/* Confirmar contraseña con indicador */}
            <div className="flex flex-col gap-1.5">
              <label htmlFor="confirm-password" className="text-[13px] font-medium" style={{ color: '#13211C' }}>
                Confirmar nueva contraseña <span style={{ color: '#B42318' }}>*</span>
              </label>
              <div className="relative">
                <input
                  id="confirm-password"
                  type={show.confirm ? 'text' : 'password'}
                  className={[
                    'w-full rounded-md border px-3 py-2.5 text-sm outline-none transition',
                    'placeholder:text-gray-400',
                    errors.confirmNewPassword
                      ? 'border-red-400 focus:border-red-500 focus:ring-1 focus:ring-red-500'
                      : passwordsMatch
                      ? 'border-emerald-500 focus:border-emerald-500 focus:ring-1 focus:ring-emerald-500'
                      : passwordsMismatch
                      ? 'border-red-400 focus:border-red-500 focus:ring-1 focus:ring-red-500'
                      : 'border-gray-300 focus:border-green-600 focus:ring-1 focus:ring-green-600',
                  ].join(' ')}
                  style={{ paddingRight: '2.75rem' }}
                  {...register('confirmNewPassword')}
                />
                <button
                  type="button"
                  onClick={() => setShow(s => ({ ...s, confirm: !s.confirm }))}
                  className="absolute right-3 top-1/2 -translate-y-1/2 rounded p-0.5 transition-colors"
                  style={{ color: '#9CA3AF' }}
                  aria-label={show.confirm ? 'Ocultar contraseña' : 'Mostrar contraseña'}
                >
                  {show.confirm
                    ? <EyeOff className="h-4 w-4" strokeWidth={1.5} />
                    : <Eye    className="h-4 w-4" strokeWidth={1.5} />}
                </button>
              </div>
              {errors.confirmNewPassword && (
                <p className="text-[13px]" style={{ color: '#B42318' }}>{errors.confirmNewPassword.message}</p>
              )}
              {!errors.confirmNewPassword && passwordsMatch && (
                <p className="text-[12px] flex items-center gap-1" style={{ color: '#059669' }}>
                  <svg className="h-3.5 w-3.5" viewBox="0 0 20 20" fill="currentColor">
                    <path fillRule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clipRule="evenodd" />
                  </svg>
                  Las contraseñas coinciden
                </p>
              )}
              {!errors.confirmNewPassword && passwordsMismatch && (
                <p className="text-[12px] flex items-center gap-1" style={{ color: '#DC2626' }}>
                  <svg className="h-3.5 w-3.5" viewBox="0 0 20 20" fill="currentColor">
                    <path fillRule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clipRule="evenodd" />
                  </svg>
                  Las contraseñas no coinciden
                </p>
              )}
            </div>

            <button
              type="submit"
              disabled={isSubmitting}
              className="flex h-12 w-full items-center justify-center gap-2 rounded-[6px] text-[15px] font-medium text-white transition disabled:opacity-60 mt-2"
              style={{ background: '#0E6B4B' }}
              onMouseEnter={e => { if (!isSubmitting) e.currentTarget.style.background = '#0A5037' }}
              onMouseLeave={e => { if (!isSubmitting) e.currentTarget.style.background = '#0E6B4B' }}
            >
              {isSubmitting ? (
                <>
                  <svg className="h-4 w-4 animate-spin" viewBox="0 0 24 24" fill="none" aria-hidden="true">
                    <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                    <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8H4z" />
                  </svg>
                  Guardando…
                </>
              ) : (
                'Establecer nueva contraseña'
              )}
            </button>
          </form>
        </div>
      </div>
    </div>
  )
}
