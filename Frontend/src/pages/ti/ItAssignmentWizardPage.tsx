import { useState } from 'react'
import { useNavigate, useSearchParams, Link } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { ArrowLeft, Loader2, ClipboardCheck, Cpu, Search } from 'lucide-react'
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
const inputCls = 'h-10 w-full rounded-[8px] border border-line bg-paper px-3 text-sm text-ink focus:border-brand-400 focus:outline-none'

export default function ItAssignmentWizardPage() {
  const navigate = useNavigate()
  const qc = useQueryClient()
  const { hasPermission } = useAuthStore()
  const [params] = useSearchParams()
  const preAsset = params.get('assetId')

  const [employeeId, setEmployeeId] = useState('')
  const [empModal, setEmpModal] = useState(false)
  const [assetIds, setAssetIds] = useState<string[]>(preAsset ? [preAsset] : [])
  const [assetSearch, setAssetSearch] = useState('')
  const [condition, setCondition] = useState<PhysicalCondition>('Good')
  const [accessories, setAccessories] = useState('')
  const [notes, setNotes] = useState('')
  const [photos, setPhotos] = useState<string[]>([])
  const [sigEmployee, setSigEmployee] = useState<string | null>(null)
  const [sigIt, setSigIt] = useState<string | null>(null)

  const employees = useQuery({
    queryKey: [...qk.ti.all, 'employees-active'] as const,
    queryFn: () => itEmployeesApi.getActive().then(r => r.data.data ?? []),
    staleTime: staleTimes.ti,
  })

  const assets = useQuery({
    queryKey: qk.ti.availableAssets,
    queryFn: () => itAssetsApi.getAll({ status: 'Available', pageSize: 200 }).then(r => r.data.data?.items ?? []),
    staleTime: staleTimes.ti,
  })

  // Activo preseleccionado (viene de la ficha con ?assetId=). Puede no estar en la lista de
  // "Disponibles" (p. ej. estaba "Devuelto"): lo cargamos aparte para mostrarlo marcado.
  const preAssetQuery = useQuery({
    queryKey: qk.ti.asset(preAsset ?? ''),
    queryFn: () => itAssetsApi.getById(preAsset!).then(r => r.data.data!),
    enabled: !!preAsset,
    staleTime: staleTimes.ti,
  })

  function toggleAsset(id: string) {
    setAssetIds(s => s.includes(id) ? s.filter(x => x !== id) : [...s, id])
  }

  // Lista base = disponibles + el preseleccionado (si no estuviera ya), siempre de primero.
  const baseAssets = (() => {
    const list = (assets.data ?? []).slice()
    const pa = preAssetQuery.data
    if (pa && !list.some(a => a.id === pa.id)) list.unshift(pa)
    return list
  })()

  // Filtro instantáneo por nombre/código/modelo/serie. La lista ya llega del servidor
  // ordenada por fecha de creación (más recientes primero); el filtro conserva ese orden.
  const term = assetSearch.trim().toLowerCase()
  const visibleAssets = baseAssets.filter(a => {
    if (!term) return true
    return [a.internalCode, a.assetTypeName, a.model, a.serialNumber, a.brandName]
      .some(v => v?.toLowerCase().includes(term))
  })

  const mutation = useMutation({
    mutationFn: () => {
      const signatures: SignatureInput[] = []
      if (sigEmployee) signatures.push({ signerType: 'Colaborador', signerName: employees.data?.find(e => e.id === employeeId)?.fullName, imageBase64: sigEmployee })
      if (sigIt) signatures.push({ signerType: 'ResponsableTI', imageBase64: sigIt })
      return itTicketsApi.createAssignment({
        employeeId, assetIds, conditionOut: condition,
        accessories: accessories || undefined, notes: notes || undefined, photos, signatures,
      }).then(r => r.data.data!)
    },
    onSuccess: (ticket) => {
      invalidate.ti(qc)
      toast.success(`Boleta ${ticket.ticketNumber} emitida.`)
      navigate(`/ti/tickets/${ticket.id}`)
    },
    onError: (e) => toast.error(extractApiError(e)),
  })

  function submit() {
    if (!employeeId) return toast.error('Seleccione el colaborador.')
    if (assetIds.length === 0) return toast.error('Seleccione al menos un activo.')
    if (!sigEmployee) return toast.error('Falta la firma del colaborador.')
    if (!sigIt) return toast.error('Falta la firma del responsable de TI.')
    mutation.mutate()
  }

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
          {/* Colaborador + activos */}
          <section className="rounded-[10px] border border-line bg-paper p-5 shadow-sh1">
            <h2 className="mb-3 text-sm font-semibold text-ink">Colaborador y equipos</h2>
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

            <label className="mb-1 block text-[12px] font-medium text-ink2">Activos disponibles *</label>
            <label className="relative mb-2 flex items-center">
              <Search className="pointer-events-none absolute left-3 h-4 w-4 text-ink2" />
              <input
                type="search"
                value={assetSearch}
                onChange={e => setAssetSearch(e.target.value)}
                placeholder="Buscar por código, tipo, modelo o serie"
                className="h-10 w-full rounded-[8px] border border-line bg-paper pl-9 pr-3 text-sm text-ink placeholder:text-ink2 focus:border-brand-400 focus:outline-none"
              />
            </label>
            <div className="max-h-64 overflow-y-auto rounded-[8px] border border-line">
              {assets.isLoading || preAssetQuery.isLoading ? (
                <div className="p-4 text-sm text-ink2">Cargando…</div>
              ) : baseAssets.length === 0 ? (
                <div className="p-4 text-sm text-ink2">No hay activos disponibles.</div>
              ) : visibleAssets.length === 0 ? (
                <div className="p-4 text-sm text-ink2">Ningún activo coincide con «{assetSearch.trim()}».</div>
              ) : visibleAssets.map(a => (
                <label key={a.id} className="flex cursor-pointer items-center gap-3 border-b border-line px-3 py-2 last:border-0 hover:bg-bg">
                  <input type="checkbox" checked={assetIds.includes(a.id)} onChange={() => toggleAsset(a.id)} className="h-4 w-4" />
                  <Cpu className="h-4 w-4 text-ink2" />
                  <span className="flex-1 text-sm text-ink">
                    <span className="font-mono font-medium">{a.internalCode}</span>
                    <span className="text-ink2"> · {a.assetTypeName}{a.model ? ` · ${a.model}` : ''}</span>
                  </span>
                </label>
              ))}
            </div>
            <p className="mt-2 text-[12px] text-ink2">
              {assetIds.length} seleccionado(s)
              {term && ` · ${visibleAssets.length} resultado(s)`}
            </p>

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

            <div className="flex items-center justify-end gap-2.5">
              <Link to="/ti/assets" className="rounded-[8px] border border-line bg-paper px-4 py-2.5 text-sm font-medium text-ink hover:bg-bg">Cancelar</Link>
              <button onClick={submit} disabled={mutation.isPending}
                className="inline-flex items-center gap-2 rounded-[8px] px-5 py-2.5 text-sm font-medium text-white transition-colors hover:opacity-90 disabled:opacity-50"
                style={{ background: BRAND }}>
                {mutation.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : <ClipboardCheck className="h-4 w-4" />}
                Emitir boleta de entrega
              </button>
            </div>
          </section>
        </div>
      </div>

      <EmployeeCreateModal
        open={empModal}
        onClose={() => setEmpModal(false)}
        onCreated={(emp) => { employees.refetch(); setEmployeeId(emp.id) }}
      />
    </div>
  )
}
