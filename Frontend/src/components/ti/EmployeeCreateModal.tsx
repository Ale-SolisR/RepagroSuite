import { useEffect, useRef, useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { X, Loader2, UserPlus, CheckCircle2 } from 'lucide-react'
import toast from 'react-hot-toast'
import { itEmployeesApi } from '@/api/itEmployees'
import { qk } from '@/lib/queryKeys'
import { extractApiError } from '@/utils'
import type { ItEmployeeDto } from '@/types'

const BRAND = '#0E6B4B'
const inputCls = 'h-10 w-full rounded-[8px] border border-line bg-paper px-3 text-sm text-ink placeholder:text-ink2 focus:border-brand-400 focus:outline-none'

interface Props {
  open: boolean
  onClose: () => void
  onCreated?: (employee: ItEmployeeDto) => void
}

export default function EmployeeCreateModal({ open, onClose, onCreated }: Props) {
  const qc = useQueryClient()
  const [cedula, setCedula] = useState('')
  const [fullName, setFullName] = useState('')
  const [position, setPosition] = useState('')
  const [looked, setLooked] = useState<'idle' | 'found' | 'notfound'>('idle')
  const lastLookup = useRef('')   // última cédula buscada, evita repetir

  function reset() { setCedula(''); setFullName(''); setPosition(''); setLooked('idle'); lastLookup.current = '' }

  const lookup = useMutation({
    mutationFn: (digits: string) => itEmployeesApi.lookup(digits).then(r => r.data.data!),
    onSuccess: (res) => {
      if (res.found && res.fullName) { setFullName(res.fullName); setLooked('found') }
      else { setLooked('notfound') }
    },
    onError: (e) => toast.error(extractApiError(e)),
  })

  // Búsqueda automática instantánea (con debounce) en cuanto la cédula es válida.
  useEffect(() => {
    if (!open) return
    const digits = cedula.replace(/\D/g, '')
    if (digits.length < 9 || digits === lastLookup.current) return
    const t = setTimeout(() => {
      lastLookup.current = digits
      lookup.mutate(digits)
    }, 400)
    return () => clearTimeout(t)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [cedula, open])

  const create = useMutation({
    mutationFn: () => itEmployeesApi.create({
      identificationNumber: cedula.trim(), fullName: fullName.trim(), position: position.trim() || undefined,
    }).then(r => r.data.data!),
    onSuccess: (emp) => {
      qc.invalidateQueries({ queryKey: qk.ti.all })
      toast.success('Colaborador creado.')
      onCreated?.(emp)
      reset(); onClose()
    },
    onError: (e) => toast.error(extractApiError(e)),
  })

  function submit() {
    const digits = cedula.replace(/\D/g, '')
    if (digits.length < 9) return toast.error('Ingrese una cédula válida.')
    if (!fullName.trim()) return toast.error('Falta el nombre. Espere la búsqueda o escríbalo.')
    create.mutate()
  }

  if (!open) return null

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      <div className="absolute inset-0 bg-black/40 backdrop-blur-sm" onClick={onClose} />
      <div className="relative w-full max-w-md rounded-[12px] border border-line bg-paper shadow-xl">
        <div className="flex items-center justify-between border-b border-line px-5 py-3">
          <h2 className="flex items-center gap-2 text-sm font-semibold text-ink">
            <UserPlus className="h-4 w-4" style={{ color: BRAND }} /> Nuevo colaborador
          </h2>
          <button onClick={onClose} className="rounded p-1 text-ink2 hover:bg-bg hover:text-ink"><X className="h-4 w-4" /></button>
        </div>

        <div className="space-y-3 p-5">
          <div>
            <label className="mb-1 block text-[12px] font-medium text-ink2">Cédula *</label>
            <div className="relative">
              <input
                className={inputCls}
                value={cedula}
                onChange={e => { setCedula(e.target.value); setLooked('idle') }}
                placeholder="1 0123 0456"
                inputMode="numeric"
                autoFocus
              />
              {lookup.isPending && <Loader2 className="absolute right-3 top-1/2 h-4 w-4 -translate-y-1/2 animate-spin text-ink2" />}
            </div>
            {looked === 'idle' && cedula.replace(/\D/g, '').length > 0 && cedula.replace(/\D/g, '').length < 9 && (
              <p className="mt-1 text-[11px] text-ink2">Digite la cédula completa para buscar el nombre automáticamente.</p>
            )}
            {looked === 'found' && <p className="mt-1 flex items-center gap-1 text-[11px] text-emerald-700"><CheckCircle2 className="h-3 w-3" /> Nombre autocompletado desde el registro civil.</p>}
            {looked === 'notfound' && <p className="mt-1 text-[11px] text-amber-700">No se encontró la cédula. Puede escribir el nombre manualmente.</p>}
          </div>

          <div>
            <label className="mb-1 block text-[12px] font-medium text-ink2">Nombre completo *</label>
            <input className={inputCls} value={fullName} onChange={e => setFullName(e.target.value)} placeholder="Se autocompleta con la cédula" />
          </div>

          <div>
            <label className="mb-1 block text-[12px] font-medium text-ink2">Puesto</label>
            <input className={inputCls} value={position} onChange={e => setPosition(e.target.value)} placeholder="Ej. Asistente de ventas" />
          </div>
        </div>

        <div className="flex items-center justify-end gap-2.5 border-t border-line px-5 py-3">
          <button onClick={onClose} className="rounded-[8px] border border-line bg-paper px-4 py-2 text-sm font-medium text-ink hover:bg-bg">Cancelar</button>
          <button onClick={submit} disabled={create.isPending}
            className="inline-flex items-center gap-2 rounded-[8px] px-4 py-2 text-sm font-medium text-white hover:opacity-90 disabled:opacity-50" style={{ background: BRAND }}>
            {create.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : <UserPlus className="h-4 w-4" />} Crear colaborador
          </button>
        </div>
      </div>
    </div>
  )
}
