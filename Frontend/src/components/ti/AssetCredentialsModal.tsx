import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  Plus, Copy, Eye, EyeOff, Pencil, Trash2, Loader2, KeyRound,
  Monitor, Mail, Cpu, Network, Globe, ArrowLeft, Save, AlertTriangle,
} from 'lucide-react'
import toast from 'react-hot-toast'
import Modal from '@/components/ui/Modal'
import { itAssetCredentialsApi } from '@/api/itAssetCredentials'
import { extractApiError, classNames } from '@/utils'
import type { ItAssetCredentialDto, ItCredentialType, CreateItAssetCredentialRequest } from '@/types'

const BRAND = '#0E6B4B'
const inputCls = 'h-10 w-full rounded-[8px] border border-line bg-paper px-3 text-sm text-ink placeholder:text-ink2 focus:border-brand-400 focus:outline-none'
const labelCls = 'mb-1 block text-[12px] font-medium text-ink2'

const TYPES: { value: ItCredentialType; label: string; Icon: React.ElementType }[] = [
  { value: 'AnyDesk', label: 'AnyDesk', Icon: Monitor },
  { value: 'Windows', label: 'Windows', Icon: Monitor },
  { value: 'Microsoft365', label: 'Microsoft 365', Icon: Mail },
  { value: 'Email', label: 'Correo', Icon: Mail },
  { value: 'Bios', label: 'BIOS', Icon: Cpu },
  { value: 'Network', label: 'Red / Router', Icon: Network },
  { value: 'Application', label: 'Aplicación', Icon: Globe },
  { value: 'Other', label: 'Otro', Icon: KeyRound },
]
const typeMeta = (t: ItCredentialType) => TYPES.find(x => x.value === t) ?? TYPES[7]

async function copyText(text: string, what: string) {
  try {
    await navigator.clipboard.writeText(text)
    toast.success(`${what} copiado`)
  } catch {
    toast.error('No se pudo copiar')
  }
}

interface Props {
  open: boolean
  onClose: () => void
  assetId: string
  assetCode?: string
  canManage: boolean
}

type FormState = CreateItAssetCredentialRequest & { clearSecret?: boolean }
const EMPTY_FORM: FormState = { type: 'Other', label: '', username: '', secret: '', host: '', notes: '' }

export default function AssetCredentialsModal({ open, onClose, assetId, assetCode, canManage }: Props) {
  const qc = useQueryClient()
  const key = ['it-asset-credentials', assetId] as const
  const [view, setView] = useState<'list' | 'form'>('list')
  const [editId, setEditId] = useState<string | null>(null)
  const [form, setForm] = useState<FormState>(EMPTY_FORM)
  const [confirmDelete, setConfirmDelete] = useState<ItAssetCredentialDto | null>(null)
  const [revealed, setRevealed] = useState<Record<string, string>>({})
  const [busy, setBusy] = useState<string | null>(null)   // id en proceso de revelar/copiar

  const { data: creds = [], isLoading } = useQuery({
    queryKey: key,
    queryFn: () => itAssetCredentialsApi.list(assetId).then(r => r.data.data ?? []),
    enabled: open,
  })

  function resetToList() {
    setView('list'); setEditId(null); setForm(EMPTY_FORM)
  }

  const saveMut = useMutation({
    mutationFn: async () => {
      const payload: CreateItAssetCredentialRequest = {
        type: form.type,
        label: form.label.trim(),
        username: form.username?.trim() || undefined,
        secret: form.secret ? form.secret : undefined,
        host: form.host?.trim() || undefined,
        notes: form.notes?.trim() || undefined,
      }
      if (editId) return itAssetCredentialsApi.update(assetId, editId, { ...payload, clearSecret: form.clearSecret })
      return itAssetCredentialsApi.create(assetId, payload)
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: key })
      toast.success(editId ? 'Credencial actualizada.' : 'Credencial guardada.')
      resetToList()
    },
    onError: (e) => toast.error(extractApiError(e)),
  })

  const deleteMut = useMutation({
    mutationFn: (id: string) => itAssetCredentialsApi.remove(assetId, id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: key })
      toast.success('Credencial eliminada.')
      setConfirmDelete(null)
    },
    onError: (e) => toast.error(extractApiError(e)),
  })

  async function toggleReveal(c: ItAssetCredentialDto) {
    if (revealed[c.id] != null) {
      setRevealed(r => { const n = { ...r }; delete n[c.id]; return n })
      return
    }
    setBusy(c.id)
    try {
      const r = await itAssetCredentialsApi.reveal(assetId, c.id)
      setRevealed(x => ({ ...x, [c.id]: r.data.data?.secret ?? '' }))
    } catch (e) { toast.error(extractApiError(e)) } finally { setBusy(null) }
  }

  async function copySecret(c: ItAssetCredentialDto) {
    if (revealed[c.id] != null) return copyText(revealed[c.id], 'Contraseña')
    setBusy(c.id)
    try {
      const r = await itAssetCredentialsApi.reveal(assetId, c.id)
      const s = r.data.data?.secret ?? ''
      if (!s) return toast.error('Sin contraseña guardada')
      await copyText(s, 'Contraseña')
    } catch (e) { toast.error(extractApiError(e)) } finally { setBusy(null) }
  }

  function startEdit(c: ItAssetCredentialDto) {
    setEditId(c.id)
    setForm({ type: c.type, label: c.label, username: c.username ?? '', secret: '', host: c.host ?? '', notes: c.notes ?? '', clearSecret: false })
    setView('form')
  }
  function startNew() {
    setEditId(null); setForm(EMPTY_FORM); setView('form')
  }

  const title = view === 'form'
    ? (editId ? 'Editar credencial' : 'Nueva credencial')
    : `Credenciales${assetCode ? ` — ${assetCode}` : ''}`

  return (
    <Modal open={open} onClose={() => { resetToList(); onClose() }} title={title} size="lg">
      {view === 'list' ? (
        <div className="space-y-3">
          {canManage && (
            <button
              onClick={startNew}
              className="inline-flex items-center gap-1.5 rounded-[8px] px-3.5 py-2 text-sm font-medium text-white transition-colors hover:opacity-90"
              style={{ background: BRAND }}
            >
              <Plus className="h-4 w-4" /> Agregar credencial
            </button>
          )}

          {isLoading ? (
            <div className="space-y-2">
              {[0, 1].map(i => <div key={i} className="h-24 animate-pulse rounded-[10px] bg-gray-100" />)}
            </div>
          ) : creds.length === 0 ? (
            <div className="flex flex-col items-center gap-2 rounded-[10px] border border-dashed border-line bg-bg/50 py-10 text-center">
              <KeyRound className="h-7 w-7 text-ink2/50" />
              <p className="text-sm font-medium text-ink">Sin credenciales</p>
              <p className="text-[12px] text-ink2">Guarda aquí las cuentas de este equipo (AnyDesk, Windows, correo…).</p>
            </div>
          ) : (
            <ul className="space-y-2.5">
              {creds.map(c => {
                const meta = typeMeta(c.type)
                const Icon = meta.Icon
                const shown = revealed[c.id]
                return (
                  <li key={c.id} className="rounded-[10px] border border-line bg-paper p-3.5 shadow-sh1">
                    <div className="mb-2 flex items-start justify-between gap-2">
                      <div className="flex items-center gap-2 min-w-0">
                        <span className="flex h-8 w-8 shrink-0 items-center justify-center rounded-lg" style={{ background: '#F0F7F3', color: BRAND }}>
                          <Icon className="h-4 w-4" />
                        </span>
                        <div className="min-w-0">
                          <p className="truncate text-sm font-semibold text-ink leading-tight">{c.label}</p>
                          <span className="text-[11px] font-medium text-ink2">{c.typeName}</span>
                        </div>
                      </div>
                      {canManage && (
                        <div className="flex shrink-0 items-center gap-1">
                          <button onClick={() => startEdit(c)} aria-label="Editar" title="Editar"
                            className="flex h-8 w-8 items-center justify-center rounded-md text-ink2 hover:bg-bg hover:text-ink"><Pencil className="h-4 w-4" /></button>
                          <button onClick={() => setConfirmDelete(c)} aria-label="Eliminar" title="Eliminar"
                            className="flex h-8 w-8 items-center justify-center rounded-md text-ink2 hover:bg-red-50 hover:text-danger"><Trash2 className="h-4 w-4" /></button>
                        </div>
                      )}
                    </div>

                    <div className="space-y-1.5">
                      {c.username && <CredRow label="Usuario" value={c.username} mono onCopy={() => copyText(c.username!, 'Usuario')} />}
                      <div className="flex items-center gap-2">
                        <span className="w-20 shrink-0 text-[11px] uppercase tracking-wider text-ink2/70">Contraseña</span>
                        <span className="flex-1 truncate font-mono text-[13px] text-ink">
                          {c.hasSecret ? (shown != null ? (shown || '—') : '••••••••') : <span className="text-ink2/60">—</span>}
                        </span>
                        {c.hasSecret && (
                          <div className="flex shrink-0 items-center gap-1">
                            <button onClick={() => toggleReveal(c)} aria-label={shown != null ? 'Ocultar' : 'Ver'} title={shown != null ? 'Ocultar' : 'Ver'}
                              className="flex h-7 w-7 items-center justify-center rounded-md text-ink2 hover:bg-bg hover:text-ink">
                              {busy === c.id ? <Loader2 className="h-3.5 w-3.5 animate-spin" /> : shown != null ? <EyeOff className="h-3.5 w-3.5" /> : <Eye className="h-3.5 w-3.5" />}
                            </button>
                            <button onClick={() => copySecret(c)} aria-label="Copiar contraseña" title="Copiar contraseña"
                              className="flex h-7 w-7 items-center justify-center rounded-md text-ink2 hover:bg-bg hover:text-ink"><Copy className="h-3.5 w-3.5" /></button>
                          </div>
                        )}
                      </div>
                      {c.host && <CredRow label="URL" value={c.host} onCopy={() => copyText(c.host!, 'URL')} />}
                      {c.notes && (
                        <div className="flex items-start gap-2 pt-0.5">
                          <span className="w-20 shrink-0 text-[11px] uppercase tracking-wider text-ink2/70">Notas</span>
                          <span className="flex-1 text-[12px] text-ink2 whitespace-pre-wrap">{c.notes}</span>
                        </div>
                      )}
                    </div>
                  </li>
                )
              })}
            </ul>
          )}

          {!canManage && (
            <p className="text-[11px] text-ink2/70">Tienes acceso de solo lectura. Para agregar o editar credenciales necesitas permiso de edición de inventario.</p>
          )}
        </div>
      ) : (
        // ─── Formulario crear/editar ───
        <form onSubmit={e => { e.preventDefault(); if (!form.label.trim()) return toast.error('La cuenta es obligatoria.'); saveMut.mutate() }} className="space-y-4">
          <button type="button" onClick={resetToList} className="inline-flex items-center gap-1.5 text-[13px] font-medium text-ink2 hover:text-ink">
            <ArrowLeft className="h-3.5 w-3.5" /> Volver a la lista
          </button>

          <div className="grid gap-4 sm:grid-cols-2">
            <div>
              <label className={labelCls}>Tipo</label>
              <select className={inputCls} value={form.type} onChange={e => setForm(f => ({ ...f, type: e.target.value as ItCredentialType }))}>
                {TYPES.map(t => <option key={t.value} value={t.value}>{t.label}</option>)}
              </select>
            </div>
            <div>
              <label className={labelCls}>Cuenta <span className="text-red-600">*</span></label>
              <input className={inputCls} value={form.label} onChange={e => setForm(f => ({ ...f, label: e.target.value }))} placeholder="Ej. Admin local" />
            </div>
            <div>
              <label className={labelCls}>Usuario</label>
              <input className={inputCls} value={form.username} onChange={e => setForm(f => ({ ...f, username: e.target.value }))} autoComplete="off" />
            </div>
            <div>
              <label className={labelCls}>Contraseña</label>
              <input type="text" className={`${inputCls} font-mono`} value={form.secret}
                onChange={e => setForm(f => ({ ...f, secret: e.target.value, clearSecret: false }))}
                placeholder={editId ? '•••• (sin cambios si lo dejas vacío)' : ''} autoComplete="off" />
              {editId && (
                <label className="mt-1.5 flex items-center gap-1.5 text-[11px] text-ink2">
                  <input type="checkbox" checked={!!form.clearSecret} onChange={e => setForm(f => ({ ...f, clearSecret: e.target.checked, secret: '' }))} className="h-3.5 w-3.5 rounded border-line" />
                  Borrar la contraseña guardada
                </label>
              )}
            </div>
            <div>
              <label className={labelCls}>URL (opcional)</label>
              <input className={inputCls} value={form.host} onChange={e => setForm(f => ({ ...f, host: e.target.value }))} placeholder="https://portal / 192.168.1.1" />
            </div>
            <div className="sm:col-span-2">
              <label className={labelCls}>Notas (opcional)</label>
              <textarea className={`${inputCls} h-20 py-2`} value={form.notes} onChange={e => setForm(f => ({ ...f, notes: e.target.value }))} />
            </div>
          </div>

          <div className="flex items-center justify-end gap-2.5 pt-1">
            <button type="button" onClick={resetToList} className="rounded-[8px] border border-line bg-paper px-4 py-2.5 text-sm font-medium text-ink hover:bg-bg">Cancelar</button>
            <button type="submit" disabled={saveMut.isPending}
              className="inline-flex items-center gap-2 rounded-[8px] px-5 py-2.5 text-sm font-medium text-white transition-colors hover:opacity-90 disabled:opacity-50" style={{ background: BRAND }}>
              {saveMut.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : <Save className="h-4 w-4" />}
              {editId ? 'Guardar cambios' : 'Guardar credencial'}
            </button>
          </div>
        </form>
      )}

      {/* Confirmación de borrado */}
      {confirmDelete && (
        <div className="fixed inset-0 z-[60] flex items-end justify-center p-0 sm:items-center sm:p-4">
          <div className="absolute inset-0 bg-black/50" onClick={() => setConfirmDelete(null)} />
          <div className="relative w-full sm:max-w-sm rounded-t-2xl sm:rounded-xl bg-white p-5 shadow-2xl">
            <div className="flex flex-col items-center text-center">
              <div className="mb-3 flex h-12 w-12 items-center justify-center rounded-full bg-red-50">
                <AlertTriangle className="h-6 w-6 text-danger" />
              </div>
              <h4 className="font-bold text-ink">¿Eliminar credencial?</h4>
              <p className="mt-1 text-sm text-ink2">Se eliminará <b>{confirmDelete.label}</b>. Esta acción no se puede deshacer.</p>
            </div>
            <div className="mt-4 flex gap-2">
              <button onClick={() => setConfirmDelete(null)} className="flex-1 rounded-[8px] border border-line bg-paper px-4 py-2.5 text-sm font-medium text-ink hover:bg-bg">Cancelar</button>
              <button onClick={() => deleteMut.mutate(confirmDelete.id)} disabled={deleteMut.isPending}
                className="flex-1 inline-flex items-center justify-center gap-2 rounded-[8px] bg-danger px-4 py-2.5 text-sm font-medium text-white hover:opacity-90 disabled:opacity-50">
                {deleteMut.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : <Trash2 className="h-4 w-4" />} Eliminar
              </button>
            </div>
          </div>
        </div>
      )}
    </Modal>
  )
}

function CredRow({ label, value, mono, onCopy }: { label: string; value: string; mono?: boolean; onCopy: () => void }) {
  return (
    <div className="flex items-center gap-2">
      <span className="w-20 shrink-0 text-[11px] uppercase tracking-wider text-ink2/70">{label}</span>
      <span className={classNames('flex-1 truncate text-[13px] text-ink', mono && 'font-mono')}>{value}</span>
      <button onClick={onCopy} aria-label={`Copiar ${label}`} title={`Copiar ${label}`}
        className="flex h-7 w-7 shrink-0 items-center justify-center rounded-md text-ink2 hover:bg-bg hover:text-ink"><Copy className="h-3.5 w-3.5" /></button>
    </div>
  )
}
