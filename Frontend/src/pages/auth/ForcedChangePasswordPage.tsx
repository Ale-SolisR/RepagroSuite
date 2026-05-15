import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useNavigate } from 'react-router-dom'
import { KeyRound, Eye, EyeOff } from 'lucide-react'
import { authApi } from '@/api/auth'
import { useAuthStore } from '@/store/authStore'
import { extractApiError } from '@/utils'
import Button from '@/components/ui/Button'
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
  label: string
  helperText?: string
  error?: string
  show: boolean
  onToggle: () => void
  registration: ReturnType<ReturnType<typeof useForm<FormData>>['register']>
}

function PasswordField({ label, helperText, error, show, onToggle, registration }: PasswordFieldProps) {
  return (
    <div className="flex flex-col gap-1">
      <label className="text-sm font-medium text-gray-700">
        {label} <span className="text-red-500">*</span>
      </label>
      <div className="relative">
        <input
          type={show ? 'text' : 'password'}
          className={[
            'w-full rounded-md border px-3 py-2 pr-10 text-sm shadow-sm outline-none transition',
            'placeholder:text-gray-400',
            error
              ? 'border-red-400 focus:border-red-500 focus:ring-1 focus:ring-red-500'
              : 'border-gray-300 focus:border-green-600 focus:ring-1 focus:ring-green-600',
          ].join(' ')}
          {...registration}
        />
        <button
          type="button"
          onClick={onToggle}
          className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600 transition-colors"
          aria-label={show ? 'Ocultar contraseña' : 'Mostrar contraseña'}
        >
          {show ? <EyeOff className="h-4 w-4" strokeWidth={1.5} /> : <Eye className="h-4 w-4" strokeWidth={1.5} />}
        </button>
      </div>
      {error && <p className="text-xs text-red-600">{error}</p>}
      {helperText && !error && <p className="text-xs text-gray-500">{helperText}</p>}
    </div>
  )
}

export default function ForcedChangePasswordPage() {
  const navigate = useNavigate()
  const { user, setAuth, accessToken, refreshToken, hasRole } = useAuthStore()
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
      if (user && accessToken && refreshToken) {
        setAuth(accessToken, refreshToken, { ...user, mustChangePassword: false })
      }
      toast.success('Contraseña actualizada correctamente')
      navigate(hasRole('ADMINISTRATOR') ? '/dashboard' : '/rooms')
    } catch (err) {
      toast.error(extractApiError(err))
    }
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-amber-50 to-amber-100 p-4">
      <div className="w-full max-w-md">
        <div className="bg-white rounded-2xl shadow-xl p-8">
          <div className="flex items-center gap-3 mb-6">
            <div className="h-10 w-10 rounded-full bg-amber-500 flex items-center justify-center shrink-0">
              <KeyRound className="h-5 w-5 text-white" />
            </div>
            <div>
              <h2 className="text-lg font-semibold text-gray-900">Cambio de Contraseña Requerido</h2>
              <p className="text-sm text-gray-500">Por seguridad debe establecer una nueva contraseña</p>
            </div>
          </div>

          <div className="rounded-lg bg-amber-50 border border-amber-200 p-3 mb-6 text-sm text-amber-800">
            Bienvenido, <strong>{user?.fullName}</strong>. Su contraseña es temporal y debe ser cambiada antes de continuar.
          </div>

          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
            <PasswordField
              label="Contraseña Actual (Temporal)"
              error={errors.currentPassword?.message}
              show={show.current}
              onToggle={() => setShow(s => ({ ...s, current: !s.current }))}
              registration={register('currentPassword')}
            />
            <PasswordField
              label="Nueva Contraseña"
              helperText="Mínimo 8 caracteres con mayúscula, número y carácter especial"
              error={errors.newPassword?.message}
              show={show.new}
              onToggle={() => setShow(s => ({ ...s, new: !s.new }))}
              registration={register('newPassword')}
            />
            {/* Confirm password — with match indicator */}
            <div className="flex flex-col gap-1">
              <label className="text-sm font-medium text-gray-700">
                Confirmar Nueva Contraseña <span className="text-red-500">*</span>
              </label>
              <div className="relative">
                <input
                  type={show.confirm ? 'text' : 'password'}
                  className={[
                    'w-full rounded-md border px-3 py-2 pr-10 text-sm shadow-sm outline-none transition',
                    'placeholder:text-gray-400',
                    errors.confirmNewPassword
                      ? 'border-red-400 focus:border-red-500 focus:ring-1 focus:ring-red-500'
                      : passwordsMatch
                      ? 'border-emerald-500 focus:border-emerald-500 focus:ring-1 focus:ring-emerald-500'
                      : passwordsMismatch
                      ? 'border-red-400 focus:border-red-500 focus:ring-1 focus:ring-red-500'
                      : 'border-gray-300 focus:border-green-600 focus:ring-1 focus:ring-green-600',
                  ].join(' ')}
                  {...register('confirmNewPassword')}
                />
                <button
                  type="button"
                  onClick={() => setShow(s => ({ ...s, confirm: !s.confirm }))}
                  className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600 transition-colors"
                  aria-label={show.confirm ? 'Ocultar contraseña' : 'Mostrar contraseña'}
                >
                  {show.confirm
                    ? <EyeOff className="h-4 w-4" strokeWidth={1.5} />
                    : <Eye    className="h-4 w-4" strokeWidth={1.5} />}
                </button>
              </div>
              {errors.confirmNewPassword && (
                <p className="text-xs text-red-600">{errors.confirmNewPassword.message}</p>
              )}
              {!errors.confirmNewPassword && passwordsMatch && (
                <p className="text-xs text-emerald-600 flex items-center gap-1">
                  <svg className="h-3.5 w-3.5" viewBox="0 0 20 20" fill="currentColor">
                    <path fillRule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clipRule="evenodd" />
                  </svg>
                  Las contraseñas coinciden
                </p>
              )}
              {!errors.confirmNewPassword && passwordsMismatch && (
                <p className="text-xs text-red-600 flex items-center gap-1">
                  <svg className="h-3.5 w-3.5" viewBox="0 0 20 20" fill="currentColor">
                    <path fillRule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clipRule="evenodd" />
                  </svg>
                  Las contraseñas no coinciden
                </p>
              )}
            </div>
            <Button type="submit" loading={isSubmitting} className="w-full" size="lg">
              Establecer Nueva Contraseña
            </Button>
          </form>
        </div>
      </div>
    </div>
  )
}
