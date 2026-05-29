import { useState } from 'react'
import { useNavigate, useSearchParams, Link } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { ArrowLeft, Loader2, ClipboardCheck, Cpu } from 'lucide-react'
import toast from 'react-hot-toast'

import { itAssetsApi } from '@/api/itAssets'
import { itTicketsApi } from '@/api/itTickets'
import { usersApi } from '@/api/users'
import { qk, staleTimes, invalidate } from '@/lib/queryKeys'
import { extractApiError } from '@/utils'
import PhotoCapture from '@/components/ti/PhotoCapture'
import SignaturePad from '@/components/ti/SignaturePad'
import { CONDITION_OPTIONS } from '@/components/ti/itStatus'
import type { PhysicalCondition, SignatureInput } from '@/types'

const BRAND = '#0E6B4B'
const inputCls = 'h-10 w-full rounded-[8px] border border-line bg-paper px-3 text-sm text-ink focus:border-brand-400 focus:outline-none'

export default function ItAssignmentWizardPage() {
  const navigate = useNavigate()
  const qc = useQueryClient()
  const [params] = useSearchParams()
  const preAsset = params.get('assetId')

  const [employeeId, setEmployeeId] = useState('')
  const [assetIds, setAssetIds] = useState<string[]>(preAsset ? [preAsset] : [])
  const [condition, setCondition] = useState<PhysicalCondition>('Good')
  const [accessories, setAccessories] = useState('')
  const [notes, setNotes] = useState('')
  const [photos, setPhotos] = useState<string[]>([])
  const [sigEmployee, setSigEmployee] = useState<string | null>(null)
  const [sigIt, setSigIt] = useState<string | null>(null)

  const users = useQuery({
    queryKey: qk.users.list,
    queryFn: () => usersApi.getAll({ pageSize: 200, status: 'Active' }).then(r => r.data.data?.items ?? []),
    staleTime: staleTimes.roomsList,
  })

  const assets = useQuery({
    queryKey: qk.ti.availableAssets,
    queryFn: () => itAssetsApi.getAll({ status: 'Available', pageSize: 200 }).then(r => r.data.data?.items ?? []),
    staleTime: staleTimes.ti,
  })

  function toggleAsset(id: string) {
    setAssetIds(s => s.includes(id) ? s.filter(x => x !== id) : [...s, id])
  }

  const mutation = useMutation({
    mutationFn: () => {
      const signatures: SignatureInput[] = []
      if (sigEmployee) signatures.push({ signerType: 'Colaborador', signerName: users.data?.find(u => u.id === employeeId)?.fullName, imageBase64: sigEmployee })
      if (sigIt) signatures.push({ signerType: 'ResponsableTI', imageBase64: sigIt })
      return itTicketsApi.createAssignment({
        employeeUserId: employeeId, assetIds, conditionOut: condition,
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
    mutation.mutate()
  }

  return (
    <div className="flex min-h-full flex-col">
      <header className="sticky top-0 z-10 flex items-center gap-3 border-b border-line bg-paper px-6 py-3" style={{ minHeight: 64 }}>
        <button onClick={() => navigate(-1)} className="rounded p-1.5 text-ink2 hover:bg-bg hover:text-ink" aria-label="Volver"><ArrowLeft className="h-5 w-5" /></button>
        <div className="min-w-0 flex-1">
          <p className="font-mono text-[12px] text-ink2 mb-0.5 leading-none">TI / Boletas / Asignación</p>
          <h1 className="text-[18px] font-semibold text-ink leading-tight tracking-tight flex items-center gap-2">
            <ClipboardCheck className="h-4.5 w-4.5" style={{ color: BRAND }} /> Entregar equipo
          </h1>
        </div>
      </header>

      <div className="flex-1 p-6 bg-bg">
        <div className="mx-auto grid max-w-5xl gap-3.5 lg:grid-cols-2">
          {/* Colaborador + activos */}
          <section className="rounded-[10px] border border-line bg-paper p-5 shadow-sh1">
            <h2 className="mb-3 text-sm font-semibold text-ink">Colaborador y equipos</h2>
            <label className="mb-1 block text-[12px] font-medium text-ink2">Colaborador *</label>
            <select className={`${inputCls} mb-4`} value={employeeId} onChange={e => setEmployeeId(e.target.value)}>
              <option value="">Seleccione…</option>
              {(users.data ?? []).map(u => <option key={u.id} value={u.id}>{u.fullName}</option>)}
            </select>

            <label className="mb-1 block text-[12px] font-medium text-ink2">Activos disponibles *</label>
            <div className="max-h-64 overflow-y-auto rounded-[8px] border border-line">
              {assets.isLoading ? (
                <div className="p-4 text-sm text-ink2">Cargando…</div>
              ) : (assets.data ?? []).length === 0 ? (
                <div className="p-4 text-sm text-ink2">No hay activos disponibles.</div>
              ) : (assets.data ?? []).map(a => (
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
            <p className="mt-2 text-[12px] text-ink2">{assetIds.length} seleccionado(s)</p>

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
    </div>
  )
}
