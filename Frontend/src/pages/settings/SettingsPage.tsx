import { useEffect, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Save, FlaskConical, Eye, EyeOff } from 'lucide-react'
import { settingsApi } from '@/api/settings'
import { extractApiError } from '@/utils'
import Button from '@/components/ui/Button'
import Spinner from '@/components/ui/Spinner'
import toast from 'react-hot-toast'
import type { SettingDto } from '@/types'

// ─── Presets SMTP por proveedor ───────────────────────────────────────────────

const SMTP_PRESETS: Record<string, { host: string; port: string }> = {
  'gmail.com':       { host: 'smtp.gmail.com',       port: '587' },
  'googlemail.com':  { host: 'smtp.gmail.com',       port: '587' },
  'outlook.com':     { host: 'smtp.office365.com',   port: '587' },
  'hotmail.com':     { host: 'smtp.office365.com',   port: '587' },
  'live.com':        { host: 'smtp.office365.com',   port: '587' },
  'yahoo.com':       { host: 'smtp.mail.yahoo.com',  port: '587' },
  'yahoo.es':        { host: 'smtp.mail.yahoo.com',  port: '587' },
}

function detectPreset(email: string) {
  const domain = email.split('@')[1]?.toLowerCase().trim()
  return domain ? SMTP_PRESETS[domain] ?? null : null
}

// ─── Componentes base ─────────────────────────────────────────────────────────

function Toggle({ checked, onChange, disabled }: { checked: boolean; onChange: (v: boolean) => void; disabled?: boolean }) {
  return (
    <button
      type="button"
      role="switch"
      aria-checked={checked}
      disabled={disabled}
      onClick={() => !disabled && onChange(!checked)}
      className={`relative inline-flex h-6 w-11 shrink-0 items-center rounded-full transition-colors focus:outline-none focus:ring-2 focus:ring-green-500 focus:ring-offset-1 ${
        checked ? 'bg-green-600' : 'bg-gray-200'
      } ${disabled ? 'opacity-50 cursor-not-allowed' : 'cursor-pointer'}`}
    >
      <span className={`inline-block h-4 w-4 transform rounded-full bg-white shadow-sm transition-transform ${
        checked ? 'translate-x-6' : 'translate-x-1'
      }`} />
    </button>
  )
}

function TextInput({
  value, onChange, disabled, modified, type = 'text', placeholder,
}: {
  value: string; onChange: (v: string) => void; disabled?: boolean
  modified?: boolean; type?: string; placeholder?: string
}) {
  return (
    <input
      type={type}
      value={value}
      onChange={e => onChange(e.target.value)}
      disabled={disabled}
      placeholder={placeholder}
      className={`w-full rounded-lg border px-3 py-2 text-sm text-gray-900 placeholder-gray-400 transition-colors
        focus:outline-none focus:ring-2 focus:ring-green-500/20 focus:border-green-400
        disabled:bg-gray-50 disabled:text-gray-400 disabled:cursor-not-allowed
        ${modified ? 'border-amber-400 bg-amber-50/30' : 'border-gray-300 bg-white'}`}
    />
  )
}

function PasswordInput({
  value, onChange, disabled, modified,
}: {
  value: string; onChange: (v: string) => void; disabled?: boolean; modified?: boolean
}) {
  const [show, setShow] = useState(false)
  return (
    <div className="relative">
      <input
        type={show ? 'text' : 'password'}
        value={value}
        onChange={e => onChange(e.target.value)}
        disabled={disabled}
        placeholder="••••••••••••"
        className={`w-full rounded-lg border px-3 py-2 pr-9 text-sm text-gray-900 placeholder-gray-400 transition-colors
          focus:outline-none focus:ring-2 focus:ring-green-500/20 focus:border-green-400
          disabled:bg-gray-50 disabled:text-gray-400 disabled:cursor-not-allowed
          ${modified ? 'border-amber-400 bg-amber-50/30' : 'border-gray-300 bg-white'}`}
      />
      <button
        type="button"
        onClick={() => setShow(s => !s)}
        tabIndex={-1}
        className="absolute right-2.5 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600"
      >
        {show ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
      </button>
    </div>
  )
}

function FieldLabel({ label, hint }: { label: string; hint?: string }) {
  return (
    <label className="block mb-1.5">
      <span className="text-sm font-medium text-gray-700">{label}</span>
      {hint && <span className="ml-1.5 text-xs text-gray-400 font-mono">{hint}</span>}
    </label>
  )
}

// ─── Sección EMAIL ────────────────────────────────────────────────────────────

function EmailSection({
  settings, values, modified, onChange, onSaveAndTest, isSaving, isTesting,
}: {
  settings: SettingDto[]
  values: Record<string, string>
  modified: Set<string>
  onChange: (key: string, val: string) => void
  onSaveAndTest: () => void
  isSaving: boolean
  isTesting: boolean
}) {
  const get = (key: string) => values[key] ?? ''
  const set = (key: string) => (val: string) => onChange(key, val)
  const setBool = (key: string) => (val: boolean) => onChange(key, val ? 'true' : 'false')

  const enabled = get('EMAIL.ENABLED') === 'true'
  const hasSetting = (key: string) => settings.some(s => s.key === key)

  // Auto-fill servidor SMTP al escribir el usuario/correo
  function handleUsernameChange(val: string) {
    onChange('EMAIL.SMTP_USERNAME', val)
    // Auto-fill también el campo FROM_ADDRESS si está vacío
    if (!get('EMAIL.FROM_ADDRESS')) onChange('EMAIL.FROM_ADDRESS', val)
    const preset = detectPreset(val)
    if (preset) {
      onChange('EMAIL.SMTP_HOST', preset.host)
      onChange('EMAIL.SMTP_PORT', preset.port)
      onChange('EMAIL.SMTP_USE_SSL', 'true')
    }
  }

  const isGmail = get('EMAIL.SMTP_HOST').includes('gmail')

  return (
    <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
      {/* Header */}
      <div className="px-5 py-4 border-b border-gray-100 flex items-center justify-between">
        <div>
          <h2 className="font-semibold text-gray-800">Configuración de Correo</h2>
          <p className="text-xs text-gray-400 mt-0.5">Notificaciones automáticas del sistema</p>
        </div>
        <div className="flex items-center gap-2.5">
          <span className={`text-sm font-medium ${enabled ? 'text-emerald-600' : 'text-gray-400'}`}>
            {enabled ? 'Activo' : 'Inactivo'}
          </span>
          <Toggle checked={enabled} onChange={setBool('EMAIL.ENABLED')} />
        </div>
      </div>

      <div className="p-5 space-y-6">

        {/* Credenciales primero — auto-fill desde aquí */}
        <div>
          <p className="text-[11px] font-semibold text-gray-400 uppercase tracking-wider mb-3">Cuenta de correo</p>
          <div className="grid grid-cols-2 gap-4">
            {hasSetting('EMAIL.SMTP_USERNAME') && (
              <div>
                <FieldLabel label="Correo electrónico" hint="EMAIL.SMTP_USERNAME" />
                <TextInput
                  type="email"
                  value={get('EMAIL.SMTP_USERNAME')}
                  onChange={handleUsernameChange}
                  modified={modified.has('EMAIL.SMTP_USERNAME')}
                  placeholder="tu@gmail.com"
                />
              </div>
            )}
            {hasSetting('EMAIL.SMTP_PASSWORD') && (
              <div>
                <FieldLabel label="Contraseña" hint="EMAIL.SMTP_PASSWORD" />
                <PasswordInput
                  value={get('EMAIL.SMTP_PASSWORD')}
                  onChange={set('EMAIL.SMTP_PASSWORD')}
                  modified={modified.has('EMAIL.SMTP_PASSWORD')}
                />
              </div>
            )}
          </div>

          {/* Aviso Gmail */}
          {isGmail && (
            <div className="mt-3 flex gap-2.5 rounded-lg border border-amber-200 bg-amber-50 px-3.5 py-3">
              <span className="text-amber-500 text-base shrink-0">⚠</span>
              <div className="text-xs text-amber-800 leading-relaxed">
                <strong>Gmail requiere una contraseña de aplicación.</strong> Google no permite usar la contraseña normal
                para SMTP. Ve a tu cuenta Google → Seguridad → Verificación en 2 pasos → Contraseñas de aplicación,
                genera una y úsala aquí.{' '}
                <a href="https://myaccount.google.com/apppasswords" target="_blank" rel="noreferrer"
                  className="underline font-medium">Ir ahora →</a>
              </div>
            </div>
          )}
        </div>

        {/* Remitente */}
        <div className="pt-4 border-t border-gray-100">
          <p className="text-[11px] font-semibold text-gray-400 uppercase tracking-wider mb-3">Remitente visible</p>
          <div className="grid grid-cols-2 gap-4">
            <div>
              <FieldLabel label="Nombre del remitente" hint="EMAIL.FROM_NAME" />
              <TextInput
                value={get('EMAIL.FROM_NAME')}
                onChange={set('EMAIL.FROM_NAME')}
                modified={modified.has('EMAIL.FROM_NAME')}
                placeholder="RepagroSuite"
              />
            </div>
            <div>
              <FieldLabel label="Correo del remitente" hint="EMAIL.FROM_ADDRESS" />
              <TextInput
                type="email"
                value={get('EMAIL.FROM_ADDRESS')}
                onChange={set('EMAIL.FROM_ADDRESS')}
                modified={modified.has('EMAIL.FROM_ADDRESS')}
                placeholder="noreply@repagro.com"
              />
            </div>
          </div>
        </div>

        {/* Servidor SMTP */}
        <div className="pt-4 border-t border-gray-100">
          <p className="text-[11px] font-semibold text-gray-400 uppercase tracking-wider mb-3">Servidor SMTP</p>
          <div className="grid grid-cols-3 gap-4 mb-4">
            <div className="col-span-2">
              <FieldLabel label="Servidor" hint="EMAIL.SMTP_HOST" />
              <TextInput
                value={get('EMAIL.SMTP_HOST')}
                onChange={set('EMAIL.SMTP_HOST')}
                modified={modified.has('EMAIL.SMTP_HOST')}
                placeholder="smtp.gmail.com"
              />
            </div>
            <div>
              <FieldLabel label="Puerto" hint="EMAIL.SMTP_PORT" />
              <TextInput
                type="number"
                value={get('EMAIL.SMTP_PORT')}
                onChange={set('EMAIL.SMTP_PORT')}
                modified={modified.has('EMAIL.SMTP_PORT')}
                placeholder="587"
              />
            </div>
          </div>

          <div className="flex items-center gap-3 px-4 py-3 bg-gray-50 rounded-lg border border-gray-200">
            <Toggle
              checked={get('EMAIL.SMTP_USE_SSL') === 'true'}
              onChange={setBool('EMAIL.SMTP_USE_SSL')}
            />
            <div>
              <p className="text-sm font-medium text-gray-700">Usar SSL / STARTTLS</p>
              <p className="text-xs text-gray-400 mt-0.5">Recomendado para el puerto 587 (Gmail, Outlook, etc.)</p>
            </div>
          </div>
        </div>

        {/* Botón probar — guarda y prueba en un solo clic */}
        <div className="pt-4 border-t border-gray-100 flex items-center justify-between">
          <p className="text-xs text-gray-400">
            Al probar, se guardan los cambios automáticamente y se envía un correo de prueba a tu cuenta.
          </p>
          <Button variant="secondary" onClick={onSaveAndTest} loading={isSaving || isTesting} disabled={isSaving || isTesting}>
            <FlaskConical className="h-4 w-4" /> Guardar y Probar SMTP
          </Button>
        </div>
      </div>
    </div>
  )
}

// ─── Sección genérica ─────────────────────────────────────────────────────────

const MODULE_LABELS: Record<string, string> = {
  GENERAL: 'Configuración General',
  AUTH: 'Autenticación y Seguridad',
  IDENTIFICATION: 'Identificaciones',
}

function GenericSection({
  module, moduleSettings, values, modified, onChange,
}: {
  module: string
  moduleSettings: SettingDto[]
  values: Record<string, string>
  modified: Set<string>
  onChange: (key: string, val: string) => void
}) {
  return (
    <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
      <div className="px-5 py-4 border-b border-gray-100">
        <h2 className="font-semibold text-gray-800">{MODULE_LABELS[module] ?? module}</h2>
      </div>
      <div className="divide-y divide-gray-50">
        {moduleSettings.map(s => (
          <div key={s.key} className="px-5 py-4 grid grid-cols-5 gap-4 items-center">
            <div className="col-span-3">
              <p className="text-sm font-medium text-gray-700">{s.description ?? s.key}</p>
              <p className="text-xs text-gray-400 font-mono mt-0.5">{s.key}</p>
            </div>
            <div className="col-span-2">
              {s.dataType === 'bool' ? (
                <div className="flex items-center gap-2.5">
                  <Toggle
                    checked={values[s.key] === 'true'}
                    disabled={s.isReadOnly}
                    onChange={v => onChange(s.key, v ? 'true' : 'false')}
                  />
                  <span className={`text-sm font-medium ${values[s.key] === 'true' ? 'text-emerald-600' : 'text-gray-400'}`}>
                    {values[s.key] === 'true' ? 'Habilitado' : 'Deshabilitado'}
                  </span>
                </div>
              ) : s.isEncrypted ? (
                <PasswordInput
                  value={values[s.key] ?? ''}
                  onChange={v => onChange(s.key, v)}
                  disabled={s.isReadOnly}
                  modified={modified.has(s.key)}
                />
              ) : (
                <TextInput
                  type={s.dataType === 'int' ? 'number' : 'text'}
                  value={values[s.key] ?? ''}
                  onChange={v => onChange(s.key, v)}
                  disabled={s.isReadOnly}
                  modified={modified.has(s.key)}
                />
              )}
            </div>
          </div>
        ))}
      </div>
    </div>
  )
}

// ─── Página principal ─────────────────────────────────────────────────────────

function groupSettings(settings: SettingDto[]) {
  return settings.reduce<Record<string, SettingDto[]>>((acc, s) => {
    const module = s.module ?? 'GENERAL'
    if (!acc[module]) acc[module] = []
    acc[module].push(s)
    return acc
  }, {})
}

export default function SettingsPage() {
  const qc = useQueryClient()
  const [values, setValues] = useState<Record<string, string>>({})
  const [modified, setModified] = useState<Set<string>>(new Set())

  const { data: settings, isLoading } = useQuery({
    queryKey: ['settings'],
    queryFn: () => settingsApi.getAll().then(r => r.data.data ?? []),
  })

  useEffect(() => {
    if (settings) {
      const initial: Record<string, string> = {}
      settings.forEach(s => { initial[s.key] = s.value ?? '' })
      setValues(initial)
    }
  }, [settings])

  const saveMutation = useMutation({
    mutationFn: (keys?: Set<string>) => {
      const toSave: Record<string, string> = {}
      const target = keys ?? modified
      target.forEach(k => { toSave[k] = values[k] })
      return settingsApi.bulkUpdate({ settings: toSave })
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['settings'] })
      setModified(new Set())
      toast.success('Configuración guardada correctamente')
    },
    onError: (err) => toast.error(extractApiError(err)),
  })

  const testEmailMutation = useMutation({
    mutationFn: () => settingsApi.testEmail(),
    onSuccess: () => toast.success('¡Correo de prueba enviado! Revisa tu bandeja de entrada.'),
    onError: (err) => toast.error(extractApiError(err)),
  })

  function handleChange(key: string, val: string) {
    setValues(v => ({ ...v, [key]: val }))
    setModified(m => new Set([...m, key]))
  }

  // Guardar configuración de email y luego probar la conexión
  async function handleSaveAndTest() {
    // Recoger todas las claves EMAIL modificadas (o todas las de EMAIL si ninguna fue modificada)
    const emailKeys = new Set(
      [...modified].filter(k => k.startsWith('EMAIL.'))
    )
    // Si hay cambios pendientes de email, guardar primero
    if (emailKeys.size > 0) {
      try {
        const toSave: Record<string, string> = {}
        emailKeys.forEach(k => { toSave[k] = values[k] })
        await settingsApi.bulkUpdate({ settings: toSave })
        qc.invalidateQueries({ queryKey: ['settings'] })
        setModified(m => {
          const next = new Set(m)
          emailKeys.forEach(k => next.delete(k))
          return next
        })
        toast.success('Ajustes guardados — probando conexión...')
      } catch (err) {
        toast.error(extractApiError(err))
        return
      }
    }
    testEmailMutation.mutate()
  }

  if (isLoading) return <Spinner />

  const groups = groupSettings(settings ?? [])
  const emailSettings = groups['EMAIL'] ?? []
  const otherGroups = Object.entries(groups).filter(([m]) => m !== 'EMAIL')

  return (
    <div className="p-6 max-w-4xl mx-auto space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between mb-2">
        <div>
          <p className="text-xs text-gray-400 tracking-wide mb-1">Administración / Configuración</p>
          <h1 className="text-2xl font-bold text-gray-900">Configuración del Sistema</h1>
        </div>
        <Button
          size="sm"
          onClick={() => saveMutation.mutate()}
          loading={saveMutation.isPending}
          disabled={modified.size === 0}
        >
          <Save className="h-4 w-4" />
          Guardar{modified.size > 0 ? ` (${modified.size})` : ''}
        </Button>
      </div>

      {/* Sección EMAIL */}
      {emailSettings.length > 0 && (
        <EmailSection
          settings={emailSettings}
          values={values}
          modified={modified}
          onChange={handleChange}
          onSaveAndTest={handleSaveAndTest}
          isSaving={saveMutation.isPending}
          isTesting={testEmailMutation.isPending}
        />
      )}

      {/* Resto de módulos */}
      {otherGroups.map(([module, moduleSettings]) => (
        <GenericSection
          key={module}
          module={module}
          moduleSettings={moduleSettings}
          values={values}
          modified={modified}
          onChange={handleChange}
        />
      ))}
    </div>
  )
}
