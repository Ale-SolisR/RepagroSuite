import { useEffect, useState } from 'react'
import { useNavigate, useSearchParams, Link } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { ArrowLeft, Loader2, ClipboardCheck, Cpu, Search, X, Check } from 'lucide-react'
import toast from 'react-hot-toast'

import { itAssetsApi } from '@/api/itAssets'
import { itTicketsApi } from '@/api/itTickets'
import { itEmployeesApi } from '@/api/itEmployees'
import { qk, staleTimes, invalidate } from '@/lib/queryKeys'
import { useAuthStore } from '@/store/authStore'
import { extractApiError } from '@/utils'
import PhotoCapture from '@/components/ti/PhotoCapture'
import SignaturePad from '@/components/ti/SignaturePad'
import EmployeeCreateModal from '@/components/ti/EmployeeCreateModal'
import { CONDITION_OPTIONS } from '@/components/ti/itStatus'
import type { PhysicalCondition, SignatureInput } from '@/types'

const BRAND = '#0E6B4B'
// h-11 (44px táctil) y 16px en móvil para evitar el auto-zoom de iOS al enfocar; 14px en ≥sm.
const inputCls = 'h-11 w-full rounded-[8px] border border-line bg-paper px-3 text-[16px] sm:text-sm text-ink focus:border-brand-400 focus:outline-none'
const DRAFT_KEY = 'ti-assign-draft'

type PickedAsset = { id: string; internalCode: string; assetTypeName?: string; model?: string }

interface Draft {
  employeeId: string
  asset: PickedAsset | null
  condition: PhysicalCondition
  accessories: string
  notes: string
  photos: string[]
  sigEmployee: string | null
  sigIt: string | null
}

function readDraft(): Draft | null {
  try { return JSON.parse(sessionStorage.getItem(DRAFT_KEY) || 'null') as Draft | null } catch { return null }
}

export default function ItAssignmentWizardPage() {
  const navigate = useNavigate()
  const qc = useQueryClient()
  const { hasPermission } = useAuthStore()
  const [params] = useSearchParams()
  const preAsset = params.get('assetId')

  // Borrador previo (sobrevive recargas de la pestaña). Si la URL trae un assetId distinto al del
  // borrador, ignoramos el borrador (es de otro proceso) para no mezclar.
  const saved = readDraft()
  const base = saved && (!preAsset || saved.asset?.id === preAsset) ? saved : null

  const [employeeId, setEmployeeId] = useState(base?.employeeId ?? '')
  const [empModal, setEmpModal] = useState(false)
  const [asset, setAsset] = useState<PickedAsset | null>(base?.asset ?? null)
  const [pickerOpen, setPickerOpen] = useState(false)
  const [assetSearch, setAssetSearch] = useState('')
  const [condition, setCondition] = useState<PhysicalCondition>(base?.condition ?? 'Good')
  const [accessories, setAccessories] = useState(base?.accessories ?? '')
  const [notes, setNotes] = useState(base?.notes ?? '')
  const [photos, setPhotos] = useState<string[]>(base?.photos ?? [])
  const [sigEmployee, setSigEmployee] = useState<string | null>(base?.sigEmployee ?? null)
  const [sigIt, setSigIt] = useState<string | null>(base?.sigIt ?? null)

  // Persiste el borrador en cada cambio: en móvil, abrir la cámara puede recargar la pestaña y
  // perder todo el progreso. Así se restaura al volver. Se limpia al emitir o cancelar.
  useEffect(() => {
    const draft: Draft = { employeeId, asset, condition, accessories, notes, photos, sigEmployee, sigIt }
    try { sessionStorage.setItem(DRAFT_KEY, JSON.stringify(draft)) } catch { /* sin cuota: se ignora */ }
  }, [employeeId, asset, condition, accessories, notes, photos, sigEmployee, sigIt])

  const clearDraft = () => { try { sessionStorage.removeItem(DRAFT_KEY) } catch { /* noop */ } }

  const employees = useQuery({
    queryKey: [...qk.ti.all, 'employees-active'] as const,
    queryFn: () => itEmployeesApi.getActive().then(r => r.data.data ?? []),
    staleTime: staleTimes.ti,
  })

  const assets = useQuery({
    queryKey: qk.ti.availableAssets,
    queryFn: () => itAssetsApi.getAll({ status: 'Available', pageSize: 200 }).then(r => r.data.data?.items ?? []),
    staleTime: staleTimes.ti,
    enabled: pickerOpen,   // solo se carga la lista cuando se abre el selector
  })

  // Activo preseleccionado desde la ficha (?assetId=): lo cargamos para mostrarlo por defecto,
  // sin desplegar ninguna lista. Puede no estar "Disponible" (p. ej. Devuelto), por eso se carga aparte.
  const preAssetQuery = useQuery({
    queryKey: qk.ti.asset(preAsset ?? ''),
    queryFn: () => itAssetsApi.getById(preAsset!).then(r => r.data.data!),
    enabled: !!preAsset && !asset,
    staleTime: staleTimes.ti,
  })
  useEffect(() => {
    if (preAsset && !asset && preAssetQuery.data) {
      const d = preAssetQuery.data
      setAsset({ id: d.id, internalCode: d.internalCode, assetTypeName: d.assetTypeName, model: d.model ?? undefined })
    }
  }, [preAsset, asset, preAssetQuery.data])

  const term = assetSearch.trim().toLowerCase()
  const pickerAssets = (assets.data ?? []).filter(a => {
    if (!term) return true
    return [a.internalCode, a.assetTypeName, a.model, a.serialNumber, a.brandName].some(v => v?.toLowerCase().includes(term))
  })

  const mutation = useMutation({
    mutationFn: () => {
      const signatures: SignatureInput[] = []
      if (sigEmployee) signatures.push({ signerType: 'Colaborador', signerName: employees.data?.find(e => e.id === employeeId)?.fullName, imageBase64: sigEmployee })
      if (sigIt) signatures.push({ signerType: 'ResponsableTI', imageBase64: sigIt })
      return itTicketsApi.createAssignment({
        employeeId, assetIds: asset ? [asset.id] : [], conditionOut: condition,
        accessories: accessories || undefined, notes: notes || undefined, photos, signatures,
      }).then(r => r.data.data!)
    },
    onSuccess: (ticket) => {
      clearDraft()
      invalidate.ti(qc)
      toast.success(`Boleta ${ticket.ticketNumber} emitida.`)
      navigate(`/ti/tickets/${ticket.id}`)
    },
    onError: (e) => toast.error(extractApiError(e)),
  })

  function submit() {
    if (!employeeId) return toast.error('Seleccione el colaborador.')
    if (!asset) return toast.error('Seleccione el activo.')
    if (!sigEmployee) return toast.error('Falta la firma del colaborador.')
    if (!sigIt) return toast.error('Falta la firma del responsable de TI.')
    mutation.mutate()
  }

  function pick(a: PickedAsset) {
    setAsset(a)
    setPickerOpen(false)
    setAssetSearch('')
  }

  const loadingPre = !!preAsset && !asset && preAssetQuery.isLoading

  return (
    <div className="flex min-h-full flex-col">
      <header className="sticky top-0 z-10 flex flex-wrap items-center gap-3 border-b border-line bg-paper px-4 sm:px-6 py-3" style={{ minHeight: 64 }}>
        <button onClick={() => navigate(-1)} className="rounded p-1.5 text-ink2 hover:bg-bg hover:text-ink" aria-label="Volver"><ArrowLeft className="h-5 w-5" /></button>
        <div className="min-w-0 flex-1">
          <p className="font-mono text-[12px] text-ink2 mb-0.5 leading-none">TI / Boletas / Asignación</p>
          <h1 className="text-[18px] font-semibold text-ink leading-tight tracking-tight flex items-center gap-2">
            <ClipboardCheck className="h-4.5 w-4.5" style={{ color: BRAND }} /> Entregar equipo
          </h1>
        </div>
      </header>

      <div className="flex-1 p-4 sm:p-6 bg-bg">
        <div className="mx-auto grid max-w-5xl gap-3.5 lg:grid-cols-2">
          {/* Colaborador + activo */}
          <section className="rounded-[10px] border border-line bg-paper p-5 shadow-sh1">
            <h2 className="mb-3 text-sm font-semibold text-ink">Colaborador y equipo</h2>
            <label className="mb-1 block text-[12px] font-medium text-ink2">Colaborador *</label>
            <div className="mb-4 flex gap-2">
              <select className={inputCls} value={employeeId} onChange={e => setEmployeeId(e.target.value)}>
                <option value="">Seleccione…</option>
                {(employees.data ?? []).map(emp => <option key={emp.id} value={emp.id}>{emp.fullName}{emp.position ? ` · ${emp.position}` : ''}</option>)}
              </select>
              {hasPermission('Ti.Employee.Manage') && (
                <button type="button" onClick={() => setEmpModal(true)} title="Nuevo colaborador"
                  className="inline-flex shrink-0 items-center rounded-[8px] border border-line bg-paper px-3 text-sm font-medium text-ink hover:bg-bg">+</button>
              )}
            </div>

            <label className="mb-1 block text-[12px] font-medium text-ink2">Activo *</label>
            {loadingPre ? (
              <div className="flex min-h-[44px] items-center gap-2 rounded-[8px] border border-line bg-bg px-3 text-sm text-ink2">
                <Loader2 className="h-4 w-4 animate-spin" /> Cargando activo…
              </div>
            ) : asset ? (
              <div className="flex items-center gap-3 rounded-[8px] border border-line bg-bg p-3">
                <Cpu className="h-5 w-5 shrink-0" style={{ color: BRAND }} />
                <div className="min-w-0 flex-1">
                  <p className="font-mono text-sm font-semibold text-ink">{asset.internalCode}</p>
                  <p className="truncate text-[12px] text-ink2">{[asset.assetTypeName, asset.model].filter(Boolean).join(' · ') || '—'}</p>
                </div>
                <button type="button" onClick={() => setPickerOpen(true)}
                  className="shrink-0 rounded-[8px] border border-line bg-paper px-3 py-2 text-[13px] font-medium text-ink hover:bg-bg">
                  Cambiar
                </button>
              </div>
            ) : (
              <button type="button" onClick={() => setPickerOpen(true)}
                className="flex min-h-[44px] w-full items-center justify-center gap-2 rounded-[8px] border border-dashed border-line bg-bg text-sm font-medium text-ink2 transition-colors hover:border-brand-400 hover:text-ink">
                <Search className="h-4 w-4" /> Seleccionar activo
              </button>
            )}

            <div className="mt-4 grid gap-3 sm:grid-cols-2">
              <div>
                <label className="mb-1 block text-[12px] font-medium text-ink2">Estado físico al entregar</label>
                <select className={inputCls} value={condition} onChange={e => setCondition(e.target.value as PhysicalCondition)}>
                  {CONDITION_OPTIONS.map(o => <option key={o.value} value={o.value}>{o.label}</option>)}
                </select>
              </div>
              <div>
                <label className="mb-1 block text-[12px] font-medium text-ink2">Accesorios incluidos</label>
                <input className={inputCls} value={accessories} onChange={e => setAccessories(e.target.value)} placeholder="Cargador, mouse, funda…" />
              </div>
            </div>
            <label className="mb-1 mt-3 block text-[12px] font-medium text-ink2">Observaciones</label>
            <textarea className={`${inputCls} h-20 py-2`} value={notes} onChange={e => setNotes(e.target.value)} />
          </section>

          {/* Evidencia + firmas */}
          <section className="space-y-3.5">
            <div className="rounded-[10px] border border-line bg-paper p-5 shadow-sh1">
              <PhotoCapture value={photos} onChange={setPhotos} max={3} />
            </div>
            <SignaturePad label="Firma del colaborador" value={sigEmployee} onConfirm={setSigEmployee} onClear={() => setSigEmployee(null)} />
            <SignaturePad label="Firma del responsable TI" value={sigIt} onConfirm={setSigIt} onClear={() => setSigIt(null)} />

            <div className="flex flex-col gap-2.5 sm:flex-row sm:items-center sm:justify-end">
              <Link to="/ti/assets" onClick={clearDraft}
                className="inline-flex min-h-[44px] w-full items-center justify-center rounded-[8px] border border-line bg-paper px-4 text-sm font-medium text-ink transition-colors touch-manipulation hover:bg-bg sm:w-auto">
                Cancelar
              </Link>
              <button onClick={submit} disabled={mutation.isPending}
                className="inline-flex min-h-[44px] w-full items-center justify-center gap-2 rounded-[8px] px-5 text-sm font-medium text-white transition-colors touch-manipulation hover:opacity-90 disabled:opacity-50 sm:w-auto"
                style={{ background: BRAND }}>
                {mutation.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : <ClipboardCheck className="h-4 w-4" />}
                Emitir boleta de entrega
              </button>
            </div>
          </section>
        </div>
      </div>

      {/* Selector de activo (un solo activo) */}
      {pickerOpen && (
        <div className="fixed inset-0 z-[1000] flex items-end justify-center bg-black/55 sm:items-center sm:p-4"
          onMouseDown={e => { if (e.target === e.currentTarget) setPickerOpen(false) }}
          role="dialog" aria-modal="true" aria-label="Seleccionar activo">
          <div className="flex max-h-[85dvh] w-full flex-col overflow-hidden rounded-t-2xl bg-paper shadow-xl sm:max-w-md sm:rounded-2xl">
            <div className="flex items-center gap-3 border-b border-line px-4 py-3">
              <h2 className="flex-1 text-[15px] font-semibold text-ink">Seleccionar activo</h2>
              <button onClick={() => setPickerOpen(false)} className="flex h-9 w-9 items-center justify-center rounded-full text-ink2 hover:bg-bg hover:text-ink" aria-label="Cerrar"><X className="h-5 w-5" /></button>
            </div>
            <div className="border-b border-line p-3">
              <label className="relative flex items-center">
                <Search className="pointer-events-none absolute left-3 h-4 w-4 text-ink2" />
                <input
                  type="search" autoFocus value={assetSearch} onChange={e => setAssetSearch(e.target.value)}
                  placeholder="Buscar por código, tipo, modelo o serie"
                  className="h-11 w-full rounded-[8px] border border-line bg-paper pl-9 pr-3 text-sm text-ink placeholder:text-ink2 focus:border-brand-400 focus:outline-none"
                />
              </label>
            </div>
            <div className="flex-1 overflow-y-auto">
              {!assets.data ? (
                <div className="p-4 text-sm text-ink2">Cargando…</div>
              ) : pickerAssets.length === 0 ? (
                <div className="p-4 text-sm text-ink2">{term ? `Ningún activo coincide con «${assetSearch.trim()}».` : 'No hay activos disponibles.'}</div>
              ) : pickerAssets.map(a => (
                <button key={a.id} type="button"
                  onClick={() => pick({ id: a.id, internalCode: a.internalCode, assetTypeName: a.assetTypeName, model: a.model ?? undefined })}
                  className="flex min-h-[48px] w-full items-center gap-3 border-b border-line px-4 py-3 text-left last:border-0 hover:bg-bg">
                  <Cpu className="h-4 w-4 shrink-0 text-ink2" />
                  <span className="min-w-0 flex-1">
                    <span className="block font-mono text-sm font-medium text-ink">{a.internalCode}</span>
                    <span className="block truncate text-[12px] text-ink2">{[a.assetTypeName, a.model].filter(Boolean).join(' · ')}</span>
                  </span>
                  {asset?.id === a.id && <Check className="h-4 w-4 shrink-0" style={{ color: BRAND }} />}
                </button>
              ))}
            </div>
          </div>
        </div>
      )}

      <EmployeeCreateModal
        open={empModal}
        onClose={() => setEmpModal(false)}
        onCreated={(emp) => { employees.refetch(); setEmployeeId(emp.id) }}
      />
    </div>
  )
}
