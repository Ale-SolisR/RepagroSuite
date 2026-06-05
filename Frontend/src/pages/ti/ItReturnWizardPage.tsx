import { useState } from 'react'
import { useNavigate, useParams, Link } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { ArrowLeft, Loader2, Undo2 } from 'lucide-react'
import toast from 'react-hot-toast'

import { itAssetsApi } from '@/api/itAssets'
import { itTicketsApi } from '@/api/itTickets'
import { qk, staleTimes, invalidate } from '@/lib/queryKeys'
import { extractApiError } from '@/utils'
import PhotoCapture from '@/components/ti/PhotoCapture'
import SignaturePad from '@/components/ti/SignaturePad'
import { CONDITION_OPTIONS, RETURN_RESULT_OPTIONS } from '@/components/ti/itStatus'
import type { PhysicalCondition, ItAssetStatus, SignatureInput } from '@/types'

const BRAND = '#0E6B4B'
const inputCls = 'h-10 w-full rounded-[8px] border border-line bg-paper px-3 text-sm text-ink focus:border-brand-400 focus:outline-none'

export default function ItReturnWizardPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const qc = useQueryClient()

  const [condition, setCondition] = useState<PhysicalCondition>('Good')
  const [resulting, setResulting] = useState<ItAssetStatus>('Available')
  const [notes, setNotes] = useState('')
  const [photos, setPhotos] = useState<string[]>([])
  const [sigEmployee, setSigEmployee] = useState<string | null>(null)
  const [sigIt, setSigIt] = useState<string | null>(null)

  const asset = useQuery({
    queryKey: qk.ti.asset(id ?? ''),
    queryFn: () => itAssetsApi.getById(id!).then(r => r.data.data!),
    enabled: !!id,
    staleTime: staleTimes.ti,
  })

  const mutation = useMutation({
    mutationFn: () => {
      const signatures: SignatureInput[] = []
      if (sigEmployee) signatures.push({ signerType: 'Colaborador', signerName: asset.data?.currentHolderName ?? undefined, imageBase64: sigEmployee })
      if (sigIt) signatures.push({ signerType: 'ResponsableTI', imageBase64: sigIt })
      return itTicketsApi.createReturn({
        assetId: id!, conditionIn: condition, resultingStatus: resulting,
        returnNotes: notes || undefined, photos, signatures,
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
    if (!sigEmployee) return toast.error('Falta la firma del colaborador.')
    if (!sigIt) return toast.error('Falta la firma del responsable de TI.')
    mutation.mutate()
  }

  return (
    <div className="flex min-h-full flex-col">
      <header className="sticky top-0 z-10 flex flex-wrap items-center gap-3 border-b border-line bg-paper px-4 sm:px-6 py-3" style={{ minHeight: 64 }}>
        <button onClick={() => navigate(-1)} className="rounded p-1.5 text-ink2 hover:bg-bg hover:text-ink" aria-label="Volver"><ArrowLeft className="h-5 w-5" /></button>
        <div className="min-w-0 flex-1">
          <p className="font-mono text-[12px] text-ink2 mb-0.5 leading-none">TI / Boletas / Devolución</p>
          <h1 className="text-[18px] font-semibold text-ink leading-tight tracking-tight flex items-center gap-2">
            <Undo2 className="h-4.5 w-4.5" style={{ color: BRAND }} /> Devolver equipo
            {asset.data && <span className="font-mono text-[14px] text-ink2">· {asset.data.internalCode}</span>}
          </h1>
        </div>
      </header>

      <div className="flex-1 p-4 sm:p-6 bg-bg">
        <div className="mx-auto grid max-w-5xl gap-3.5 lg:grid-cols-2">
          <section className="rounded-[10px] border border-line bg-paper p-5 shadow-sh1">
            <h2 className="mb-3 text-sm font-semibold text-ink">Recepción</h2>
            {asset.data && (
              <p className="mb-4 rounded-lg bg-bg p-3 text-[13px] text-ink2">
                {asset.data.assetTypeName}{asset.data.model ? ` · ${asset.data.model}` : ''} ·
                Responsable actual: <span className="font-medium text-ink">{asset.data.currentHolderName ?? '—'}</span>
              </p>
            )}
            <div className="grid gap-3 sm:grid-cols-2">
              <div>
                <label className="mb-1 block text-[12px] font-medium text-ink2">Estado físico al recibir</label>
                <select className={inputCls} value={condition} onChange={e => setCondition(e.target.value as PhysicalCondition)}>
                  {CONDITION_OPTIONS.map(o => <option key={o.value} value={o.value}>{o.label}</option>)}
                </select>
              </div>
              <div>
                <label className="mb-1 block text-[12px] font-medium text-ink2">Estado resultante del activo</label>
                <select className={inputCls} value={resulting} onChange={e => setResulting(e.target.value as ItAssetStatus)}>
                  {RETURN_RESULT_OPTIONS.map(o => <option key={o.value} value={o.value}>{o.label}</option>)}
                </select>
              </div>
            </div>
            <label className="mb-1 mt-3 block text-[12px] font-medium text-ink2">Daños, faltantes u observaciones</label>
            <textarea className={`${inputCls} h-24 py-2`} value={notes} onChange={e => setNotes(e.target.value)} />
          </section>

          <section className="space-y-3.5">
            <div className="rounded-[10px] border border-line bg-paper p-5 shadow-sh1">
              <PhotoCapture value={photos} onChange={setPhotos} max={3} hint="Fotos del equipo devuelto / daños." />
            </div>
            <SignaturePad label="Firma del colaborador" value={sigEmployee} onConfirm={setSigEmployee} onClear={() => setSigEmployee(null)} />
            <SignaturePad label="Firma del responsable TI" value={sigIt} onConfirm={setSigIt} onClear={() => setSigIt(null)} />
            <div className="flex items-center justify-end gap-2.5">
              <Link to={`/ti/assets/${id}`} className="rounded-[8px] border border-line bg-paper px-4 py-2.5 text-sm font-medium text-ink hover:bg-bg">Cancelar</Link>
              <button onClick={submit} disabled={mutation.isPending}
                className="inline-flex items-center gap-2 rounded-[8px] px-5 py-2.5 text-sm font-medium text-white transition-colors hover:opacity-90 disabled:opacity-50"
                style={{ background: BRAND }}>
                {mutation.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : <Undo2 className="h-4 w-4" />}
                Emitir boleta de devolución
              </button>
            </div>
          </section>
        </div>
      </div>
    </div>
  )
}
