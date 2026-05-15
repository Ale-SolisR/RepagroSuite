import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Link } from 'react-router-dom'
import { authApi } from '@/api/auth'
import { extractApiError } from '@/utils'
import Input from '@/components/ui/Input'
import Button from '@/components/ui/Button'
import toast from 'react-hot-toast'

const schema = z.object({ email: z.string().email('Correo inválido') })
type FormData = z.infer<typeof schema>

export default function ForgotPasswordPage() {
  const { register, handleSubmit, formState: { errors, isSubmitting, isSubmitSuccessful } } = useForm<FormData>({
    resolver: zodResolver(schema),
  })

  async function onSubmit(data: FormData) {
    try {
      await authApi.forgotPassword(data)
    } catch (err) {
      toast.error(extractApiError(err))
    }
  }

  if (isSubmitSuccessful) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-green-50 to-green-100 p-4">
        <div className="w-full max-w-md bg-white rounded-2xl shadow-xl p-8 text-center">
          <div className="text-5xl mb-4">✉️</div>
          <h2 className="text-xl font-semibold text-gray-800 mb-2">Revise su correo</h2>
          <p className="text-gray-500 text-sm">
            Si existe una cuenta con ese correo, recibirá las instrucciones para restablecer su contraseña.
          </p>
          <Link to="/login" className="mt-6 block text-green-700 font-medium hover:text-green-800">
            Volver al inicio de sesión
          </Link>
        </div>
      </div>
    )
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-green-50 to-green-100 p-4">
      <div className="w-full max-w-md">
        <div className="bg-white rounded-2xl shadow-xl p-8">
          <h2 className="text-xl font-semibold text-gray-800 mb-2">Recuperar Contraseña</h2>
          <p className="text-sm text-gray-500 mb-6">
            Ingrese su correo y le enviaremos un enlace para restablecer su contraseña.
          </p>

          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
            <Input
              label="Correo Electrónico"
              type="email"
              placeholder="usuario@repagro.com"
              error={errors.email?.message}
              required
              {...register('email')}
            />
            <Button type="submit" loading={isSubmitting} className="w-full">
              Enviar Enlace
            </Button>
          </form>

          <div className="text-center mt-4">
            <Link to="/login" className="text-sm text-green-700 hover:text-green-800">
              Volver al inicio de sesión
            </Link>
          </div>
        </div>
      </div>
    </div>
  )
}
