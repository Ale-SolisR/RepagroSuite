import { useEffect, useMemo, useRef, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  Save, FlaskConical, Eye, EyeOff, Lock, Unlock, Mail, Settings as SettingsIcon,
  ShieldCheck, IdCard, AlertCircle, ChevronRight,
} from 'lucide-react'
import type { LucideIcon } from 'lucide-react'
import { settingsApi } from '@/api/settings'
import { qk, staleTimes, invalidate } from '@/lib/queryKeys'
import { extractApiError } from '@/utils'
import Button from '@/components/ui/Button'
import Spinner from '@/components/ui/Spinner'
import { useConfirm } from '@/components/ui/ConfirmDialog'
import toast from 'react-hot-toast'
import type { SettingDto } from '@/types'
import { useAuthStore } from '@/store/authStore'

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
      className={`relative inline-flex h-5 w-9 shrink-0 items-center rounded-full transition-colors focus:outline-none focus:ring-2 focus:ring-green-500/30 focus:ring-offset-1 ${
        checked ? 'bg-green-600' : 'bg-gray-200'
      } ${disabled ? 'opacity-50 cursor-not-allowed' : 'cursor-pointer'}`}
    >
      <span className={`inline-block h-3.5 w-3.5 transform rounded-full bg-white shadow-sm transition-transform ${
        checked ? 'translate-x-5' : 'translate-x-0.5'
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
      className={`w-full rounded-md border px-3 py-1.5 text-sm text-gray-900 placeholder-gray-400 transition-colors
        focus:outline-none focus:ring-2 focus:ring-green-500/20 focus:border-green-400
        disabled:bg-gray-50 disabled:text-gray-500 disabled:cursor-not-allowed
        ${modified ? 'border-amber-400 bg-amber-50/40' : 'border-gray-300 bg-white'}`}
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
        autoComplete="new-password"
        autoCorrect="off"
        spellCheck={false}
        data-lpignore="true"
        data-1p-ignore="true"
        className={`w-full rounded-md border px-3 py-1.5 pr-9 text-sm text-gray-900 placeholder-gray-400 transition-colors
          focus:outline-none focus:ring-2 focus:ring-green-500/20 focus:border-green-400
          disabled:bg-gray-50 disabled:text-gray-500 disabled:cursor-not-allowed
          ${modified ? 'border-amber-400 bg-amber-50/40' : 'border-gray-300 bg-white'}`}
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
    <label className="block mb-1">
      <span className="text-[13px] font-medium text-gray-700">{label}</span>
      {hint && <span className="ml-1.5 text-[10px] text-gray-400 font-mono">{hint}</span>}
    </label>
  )
}

// ─── Subsección — agrupa campos relacionados ─────────────────────────────────
function SubSection({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div>
      <p className="text-[10px] font-semibold text-gray-400 uppercase tracking-wider mb-2.5">{label}</p>
      {children}
    </div>
  )
}

// ─── Sección EMAIL ────────────────────────────────────────────────────────────

function EmailSection({
  settings, values, modified, onChange, onSaveAndTest, isSaving, isTesting, editMode,
}: {
  settings: SettingDto[]
  values: Record<string, string>
  modified: Set<string>
  onChange: (key: string, val: string) => void
  onSaveAndTest: () => void
  isSaving: boolean
  isTesting: boolean
  editMode: boolean
}) {
  const get = (key: string) => values[key] ?? ''
  const set = (key: string) => (val: string) => onChange(key, val)
  const setBool = (key: string) => (val: boolean) => onChange(key, val ? 'true' : 'false')

  const enabled = get('EMAIL.ENABLED') === 'true'
  const hasSetting = (key: string) => settings.some(s => s.key === key)

  function handleUsernameChange(val: string) {
    onChange('EMAIL.SMTP_USERNAME', val)
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
    <div className="space-y-5">
      {/* Estado del servicio */}
      <div className="flex items-center justify-between rounded-lg border border-gray-200 bg-gray-50/50 px-4 py-3">
        <div className="flex items-center gap-3">
          <div className={`h-2 w-2 rounded-full ${enabled ? 'bg-emerald-500' : 'bg-gray-300'}`} />
          <div>
            <p className="text-sm font-medium text-gray-800">Servicio de correo</p>
            <p className="text-[11px] text-gray-500">
              {enabled ? 'Notificaciones automáticas activas' : 'Las notificaciones no se enviarán'}
            </p>
          </div>
        </div>
        <Toggle checked={enabled} onChange={setBool('EMAIL.ENABLED')} disabled={!editMode} />
      </div>

      {/* Cuenta */}
      <SubSection label="Cuenta de correo">
        <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
          {hasSetting('EMAIL.SMTP_USERNAME') && (
            <div>
              <FieldLabel label="Correo electrónico" />
              <TextInput
                type="email"
                value={get('EMAIL.SMTP_USERNAME')}
                onChange={handleUsernameChange}
                disabled={!editMode}
                modified={modified.has('EMAIL.SMTP_USERNAME')}
                placeholder="tu@gmail.com"
              />
            </div>
          )}
          {hasSetting('EMAIL.SMTP_PASSWORD') && (
            <div>
              <FieldLabel label="Contraseña" />
              <PasswordInput
                value={get('EMAIL.SMTP_PASSWORD')}
                onChange={set('EMAIL.SMTP_PASSWORD')}
                disabled={!editMode}
                modified={modified.has('EMAIL.SMTP_PASSWORD')}
              />
            </div>
          )}
        </div>
        {isGmail && (
          <div className="mt-3 flex gap-2.5 rounded-md border border-amber-200 bg-amber-50 px-3 py-2.5">
            <AlertCircle className="h-4 w-4 mt-0.5 shrink-0 text-amber-600" />
            <div className="text-[12px] text-amber-800 leading-relaxed">
              <strong>Gmail requiere una contraseña de aplicación.</strong>{' '}
              Ve a tu cuenta Google → Seguridad → Verificación en 2 pasos → Contraseñas de aplicación.{' '}
              <a href="https://myaccount.google.com/apppasswords" target="_blank" rel="noreferrer"
                className="underline font-medium">Ir ahora →</a>
            </div>
          </div>
        )}
      </SubSection>

      {/* Remitente */}
      <SubSection label="Remitente visible">
        <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
          <div>
            <FieldLabel label="Nombre del remitente" />
            <TextInput
              value={get('EMAIL.FROM_NAME')}
              onChange={set('EMAIL.FROM_NAME')}
              disabled={!editMode}
              modified={modified.has('EMAIL.FROM_NAME')}
              placeholder="Repagro App"
            />
          </div>
          <div>
            <FieldLabel label="Correo del remitente" />
            <TextInput
              type="email"
              value={get('EMAIL.FROM_ADDRESS')}
              onChange={set('EMAIL.FROM_ADDRESS')}
              disabled={!editMode}
              modified={modified.has('EMAIL.FROM_ADDRESS')}
              placeholder="noreply@repagro.com"
            />
          </div>
        </div>
      </SubSection>

      {/* Servidor SMTP */}
      <SubSection label="Servidor SMTP">
        <div className="grid grid-cols-1 md:grid-cols-3 gap-3 mb-3">
          <div className="md:col-span-2">
            <FieldLabel label="Servidor" />
            <TextInput
              value={get('EMAIL.SMTP_HOST')}
              onChange={set('EMAIL.SMTP_HOST')}
              disabled={!editMode}
              modified={modified.has('EMAIL.SMTP_HOST')}
              placeholder="smtp.gmail.com"
            />
          </div>
          <div>
            <FieldLabel label="Puerto" />
            <TextInput
              type="number"
              value={get('EMAIL.SMTP_PORT')}
              onChange={set('EMAIL.SMTP_PORT')}
              disabled={!editMode}
              modified={modified.has('EMAIL.SMTP_PORT')}
              placeholder="587"
            />
          </div>
        </div>

        <div className="flex items-center gap-3 rounded-md border border-gray-200 bg-gray-50/50 px-3 py-2.5">
          <Toggle
            checked={get('EMAIL.SMTP_USE_SSL') === 'true'}
            onChange={setBool('EMAIL.SMTP_USE_SSL')}
            disabled={!editMode}
          />
          <div className="min-w-0 flex-1">
            <p className="text-[13px] font-medium text-gray-700">Usar SSL / STARTTLS</p>
            <p className="text-[11px] text-gray-500">Recomendado para puerto 587 (Gmail, Outlook, etc.)</p>
          </div>
        </div>
      </SubSection>

      {/* Probar */}
      <div className="flex items-center justify-between rounded-lg border border-dashed border-gray-200 px-4 py-3">
        <div className="flex items-start gap-2.5">
          <FlaskConical className="h-4 w-4 mt-0.5 text-gray-400" />
          <p className="text-[12px] text-gray-500 leading-relaxed">
            Guarda los cambios y envía un correo de prueba a tu cuenta para verificar la configuración.
          </p>
        </div>
        <Button
          variant="secondary"
          size="sm"
          onClick={onSaveAndTest}
          loading={isSaving || isTesting}
          disabled={isSaving || isTesting || !editMode}
        >
          Probar conexión
        </Button>
      </div>
    </div>
  )
}

// ─── Sección genérica ─────────────────────────────────────────────────────────

function GenericSection({
  moduleSettings, values, modified, onChange, editMode,
}: {
  moduleSettings: SettingDto[]
  values: Record<string, string>
  modified: Set<string>
  onChange: (key: string, val: string) => void
  editMode: boolean
}) {
  return (
    <div className="divide-y divide-gray-100 rounded-lg border border-gray-200 bg-white">
      {moduleSettings.map(s => (
        <div key={s.key} className="grid grid-cols-1 md:grid-cols-5 gap-3 items-center px-4 py-3">
          <div className="md:col-span-3">
            <p className="text-[13px] font-medium text-gray-800">{s.description ?? s.key}</p>
            <p className="text-[10px] text-gray-400 font-mono mt-0.5">{s.key}</p>
          </div>
          <div className="md:col-span-2">
            {s.dataType === 'bool' ? (
              <div className="flex items-center gap-2.5">
                <Toggle
                  checked={values[s.key] === 'true'}
                  disabled={!editMode || s.isReadOnly}
                  onChange={v => onChange(s.key, v ? 'true' : 'false')}
                />
                <span className={`text-[12px] font-medium ${values[s.key] === 'true' ? 'text-emerald-600' : 'text-gray-400'}`}>
                  {values[s.key] === 'true' ? 'Habilitado' : 'Deshabilitado'}
                </span>
              </div>
            ) : s.isEncrypted ? (
              <PasswordInput
                value={values[s.key] ?? ''}
                onChange={v => onChange(s.key, v)}
                disabled={!editMode || s.isReadOnly}
                modified={modified.has(s.key)}
              />
            ) : (
              <TextInput
                type={s.dataType === 'int' ? 'number' : 'text'}
                value={values[s.key] ?? ''}
                onChange={v => onChange(s.key, v)}
                disabled={!editMode || s.isReadOnly}
                modified={modified.has(s.key)}
              />
            )}
          </div>
        </div>
      ))}
      {moduleSettings.length === 0 && (
        <div className="px-4 py-10 text-center text-sm text-gray-400">
          No hay ajustes configurables en este módulo.
        </div>
      )}
    </div>
  )
}

// ─── Página principal ─────────────────────────────────────────────────────────

const TAB_META: Record<string, { label: string; icon: LucideIcon; description: string }> = {
  EMAIL:          { label: 'Correo',          icon: Mail,         description: 'Notificaciones automáticas vía SMTP' },
  GENERAL:        { label: 'General',         icon: SettingsIcon, description: 'Preferencias generales del sistema' },
  AUTH:           { label: 'Seguridad',       icon: ShieldCheck,  description: 'Políticas de autenticación' },
  IDENTIFICATION: { label: 'Identificaciones', icon: IdCard,      description: 'Validación de cédulas' },
}

// Normaliza valores para comparar — tolera variaciones como "True"/"true" o trailing spaces.
function normalize(val: string): string {
  const trimmed = (val ?? '').trim()
  const lower = trimmed.toLowerCase()
  if (lower === 'true' || lower === 'false') return lower
  return trimmed
}

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
  const confirm = useConfirm()
  const { user } = useAuthStore()
  const isMaster = user?.isMaster ?? false
  const [values, setValues] = useState<Record<string, string>>({})
  const [modified, setModified] = useState<Set<string>>(new Set())
  const [editMode, setEditMode] = useState(false)
  const [activeTab, setActiveTab] = useState<string>('')
  const originalValues = useRef<Record<string, string>>({})

  const { data: settings, isLoading } = useQuery({
    queryKey: qk.settings.all,
    queryFn: () => settingsApi.getAll().then(r => r.data.data ?? []),
    staleTime: staleTimes.settings,
  })

  useEffect(() => {
    if (settings) {
      const initial: Record<string, string> = {}
      settings.forEach(s => { initial[s.key] = s.value ?? '' })
      originalValues.current = initial
      setValues(initial)
      setModified(new Set())
    }
  }, [settings])

  const groups = useMemo(() => groupSettings(settings ?? []), [settings])

  // Tabs visibles para el usuario actual
  const availableTabs = useMemo(() => {
    const tabs: string[] = []
    if (isMaster && (groups['EMAIL']?.length ?? 0) > 0) tabs.push('EMAIL')
    Object.keys(groups)
      .filter(m => m !== 'EMAIL')
      .sort()
      .forEach(m => tabs.push(m))
    return tabs
  }, [groups, isMaster])

  // Seleccionar primer tab por defecto
  useEffect(() => {
    if (!activeTab && availableTabs.length > 0) setActiveTab(availableTabs[0])
  }, [availableTabs, activeTab])

  const saveMutation = useMutation({
    mutationFn: (keys?: Set<string>) => {
      const toSave: Record<string, string> = {}
      const target = keys ?? modified
      target.forEach(k => { toSave[k] = values[k] })
      return settingsApi.bulkUpdate({ settings: toSave })
    },
    onSuccess: () => {
      invalidate.settings(qc)
      setModified(new Set())
      setEditMode(false)
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
    setModified(m => {
      const next = new Set(m)
      const original = originalValues.current[key] ?? ''
      // Solo marcamos como modificado si el valor realmente difiere del original.
      // Esto evita falsos positivos por autofill del navegador o normalizaciones.
      if (normalize(val) === normalize(original)) {
        next.delete(key)
      } else {
        next.add(key)
      }
      return next
    })
  }

  async function handleToggleEditMode() {
    if (editMode && modified.size > 0) {
      const confirmed = await confirm({
        title: 'Descartar cambios',
        message: `Tienes ${modified.size} cambio(s) sin guardar. Si continúas se perderán.`,
        variant: 'danger',
        confirmText: 'Descartar',
        cancelText: 'Seguir editando',
      })
      if (!confirmed) return
      // Revert to original values
      if (settings) {
        const initial: Record<string, string> = {}
        settings.forEach(s => { initial[s.key] = s.value ?? '' })
        setValues(initial)
        setModified(new Set())
      }
    }
    setEditMode(m => !m)
  }

  async function handleSaveAndTest() {
    const emailKeys = new Set([...modified].filter(k => k.startsWith('EMAIL.')))
    if (emailKeys.size > 0) {
      try {
        const toSave: Record<string, string> = {}
        emailKeys.forEach(k => { toSave[k] = values[k] })
        await settingsApi.bulkUpdate({ settings: toSave })
        invalidate.settings(qc)
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

  if (isLoading) {
    return <div className="flex justify-center py-20"><Spinner /></div>
  }

  const emailSettings = groups['EMAIL'] ?? []
  const currentMeta = TAB_META[activeTab] ?? { label: activeTab, icon: SettingsIcon, description: '' }
  const CurrentIcon = currentMeta.icon

  return (
    <div className="p-4 sm:p-6 max-w-5xl mx-auto">

      {/* Header con título, edit toggle y guardar */}
      <div className="mb-5 flex flex-wrap items-center justify-between gap-3">
        <div>
          <p className="text-xs text-gray-400 tracking-wide mb-1">Administración / Configuración</p>
          <h1 className="text-2xl font-bold text-gray-900">Configuración del sistema</h1>
        </div>

        <div className="flex items-center gap-2">
          {/* Edit mode toggle — un único botón, sin elementos clickables anidados */}
          <button
            type="button"
            onClick={handleToggleEditMode}
            aria-pressed={editMode}
            className={`flex items-center gap-2.5 rounded-lg border px-3 py-1.5 text-sm font-medium transition-all select-none ${
              editMode
                ? 'border-amber-200 bg-amber-50 text-amber-700 hover:bg-amber-100'
                : 'border-gray-200 bg-white text-gray-600 hover:bg-gray-50'
            }`}
          >
            {editMode
              ? <Unlock className="h-4 w-4" strokeWidth={2} />
              : <Lock className="h-4 w-4" strokeWidth={2} />
            }
            <span>{editMode ? 'Modo edición' : 'Solo lectura'}</span>
            {/* Toggle visual — solo presentacional, no clickable */}
            <span
              aria-hidden="true"
              className={`relative inline-flex h-5 w-9 shrink-0 items-center rounded-full transition-colors ${
                editMode ? 'bg-amber-500' : 'bg-gray-300'
              }`}
            >
              <span
                className={`inline-block h-3.5 w-3.5 transform rounded-full bg-white shadow-sm transition-transform ${
                  editMode ? 'translate-x-5' : 'translate-x-0.5'
                }`}
              />
            </span>
          </button>

          {/* Save button — solo visible en modo edición */}
          {editMode && (
            <Button
              size="sm"
              onClick={() => saveMutation.mutate(undefined)}
              loading={saveMutation.isPending}
              disabled={modified.size === 0}
            >
              <Save className="h-4 w-4" />
              Guardar{modified.size > 0 ? ` (${modified.size})` : ''}
            </Button>
          )}
        </div>
      </div>

      {/* Si no hay tabs (usuario no master sin acceso) → solo mensaje informativo */}
      {availableTabs.length === 0 && (
        <div className="rounded-xl border border-gray-200 bg-white px-6 py-12 text-center">
          <AlertCircle className="h-8 w-8 mx-auto mb-2 text-gray-300" />
          <p className="text-sm text-gray-500">No hay ajustes disponibles para tu rol.</p>
        </div>
      )}

      {availableTabs.length > 0 && (
        <div className="grid grid-cols-1 lg:grid-cols-[220px_1fr] gap-5">

          {/* Sidebar vertical de tabs */}
          <aside>
            <nav className="rounded-xl border border-gray-200 bg-white p-1.5 space-y-0.5" aria-label="Categorías de configuración">
              {availableTabs.map(tab => {
                const meta = TAB_META[tab] ?? { label: tab, icon: SettingsIcon, description: '' }
                const TabIcon = meta.icon
                const active = activeTab === tab
                const modCount = [...modified].filter(k => {
                  const setting = settings?.find(s => s.key === k)
                  return (setting?.module ?? 'GENERAL') === tab
                }).length
                return (
                  <button
                    key={tab}
                    type="button"
                    onClick={() => setActiveTab(tab)}
                    className={`w-full flex items-center gap-2.5 rounded-lg px-2.5 py-2 text-left text-sm transition-all ${
                      active
                        ? 'bg-green-50 text-green-800 font-medium'
                        : 'text-gray-600 hover:bg-gray-50 hover:text-gray-900'
                    }`}
                  >
                    <TabIcon
                      className={`h-4 w-4 shrink-0 ${active ? 'text-green-600' : 'text-gray-400'}`}
                      strokeWidth={1.75}
                    />
                    <span className="flex-1 truncate">{meta.label}</span>
                    {modCount > 0 && (
                      <span className="inline-flex items-center justify-center min-w-[18px] h-[18px] rounded-full bg-amber-400 text-white text-[10px] font-semibold px-1">
                        {modCount}
                      </span>
                    )}
                    <ChevronRight className={`h-3.5 w-3.5 shrink-0 transition-colors ${active ? 'text-green-600' : 'text-gray-300'}`} />
                  </button>
                )
              })}
            </nav>

            {/* Aviso si no es master y mira la sección email no está */}
            {!isMaster && (
              <div className="mt-3 rounded-lg border border-gray-200 bg-gray-50/50 px-3 py-2.5">
                <p className="text-[11px] text-gray-500 leading-relaxed">
                  <Lock className="h-3 w-3 inline mr-1 -mt-0.5" />
                  La configuración de correo solo puede ser editada por el administrador maestro.
                </p>
              </div>
            )}
          </aside>

          {/* Contenido del tab activo */}
          <div className="rounded-xl border border-gray-200 bg-white">
            {/* Header de sección */}
            <div className="flex items-center gap-3 border-b border-gray-100 px-5 py-4">
              <div className="flex h-9 w-9 items-center justify-center rounded-lg bg-green-50">
                <CurrentIcon className="h-4 w-4 text-green-700" strokeWidth={1.75} />
              </div>
              <div className="min-w-0 flex-1">
                <h2 className="text-[15px] font-semibold text-gray-900 leading-tight">{currentMeta.label}</h2>
                <p className="text-[12px] text-gray-500 leading-tight mt-0.5">{currentMeta.description}</p>
              </div>
              {!editMode && (
                <span className="inline-flex items-center gap-1 rounded-full bg-gray-100 px-2 py-0.5 text-[11px] font-medium text-gray-600">
                  <Lock className="h-3 w-3" /> Bloqueado
                </span>
              )}
            </div>

            {/* Contenido */}
            <div className="p-5">
              {activeTab === 'EMAIL' && emailSettings.length > 0 && (
                <EmailSection
                  settings={emailSettings}
                  values={values}
                  modified={modified}
                  onChange={handleChange}
                  onSaveAndTest={handleSaveAndTest}
                  isSaving={saveMutation.isPending}
                  isTesting={testEmailMutation.isPending}
                  editMode={editMode}
                />
              )}
              {activeTab !== 'EMAIL' && (
                <GenericSection
                  moduleSettings={groups[activeTab] ?? []}
                  values={values}
                  modified={modified}
                  onChange={handleChange}
                  editMode={editMode}
                />
              )}
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
