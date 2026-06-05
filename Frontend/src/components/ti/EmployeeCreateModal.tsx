import { useEffect, useRef, useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { X, Loader2, UserPlus, CheckCircle2, Save, UserX, UserCheck } from 'lucide-react'
import toast from 'react-hot-toast'
import { itEmployeesApi } from '@/api/itEmployees'
import { qk } from '@/lib/queryKeys'
import { extractApiError } from '@/utils'
import type { ItEmployeeDto } from '@/types'

const BRAND = '#0E6B4B'
const inputCls = 'h-10 w-full rounded-[8px] border border-line bg-paper px-3 text-sm text-ink placeholder:text-ink2 focus:border-brand-400 focus:outline-none'
const labelCls = 'mb-1 block text-[12px] font-medium text-ink2'

interface Props {
  open: boolean
  onClose: () => void
  employee?: ItEmployeeDto | null
  onCreated?: (employee: ItEmployeeDto) => void
  onSaved?: (employee: ItEmployeeDto) => void
}

type LookupState = 'idle' | 'found' | 'notfound'

export default function EmployeeCreateModal({ open, onClose, employee, onCreated, onSaved }: Props) {
  const qc = useQueryClient()
  const isEdit = !!employee
  const [cedula, setCedula] = useState('')
  const [fullName, setFullName] = useState('')
  const [position, setPosition] = useState('')
  const [department, setDepartment] = useState('')
  const [email, setEmail] = useState('')
  const [phoneNumber, setPhoneNumber] = useState('')
  const [isActive, setIsActive] = useState(true)
  const [looked, setLooked] = useState<LookupState>('idle')
  const lastLookup = useRef('')

  function fillFromEmployee(emp?: ItEmployeeDto | null) {
    setCedula(emp?.identificationNumber ?? '')
    setFullName(emp?.fullName ?? '')
    setPosition(emp?.position ?? '')
    setDepartment(emp?.department ?? '')
    setEmail(emp?.email ?? '')
    setPhoneNumber(emp?.phoneNumber ?? '')
    setIsActive(emp?.isActive ?? true)
    setLooked('idle')
    lastLookup.current = emp?.identificationNumber?.replace(/\D/g, '') ?? ''
  }

  useEffect(() => {
    if (open) fillFromEmployee(employee)
  }, [open, employee])

  const lookup = useMutation({
    mutationFn: (digits: string) => itEmployeesApi.lookup(digits).then(r => r.data.data!),
    onSuccess: (res) => {
      if (res.found && res.fullName) {
        setFullName(res.fullName)
        setLooked('found')
      } else {
        setLooked('notfound')
      }
    },
    onError: (e) => toast.error(extractApiError(e)),
  })

  useEffect(() => {
    if (!open) return
    if (isEdit) return
    const digits = cedula.replace(/\D/g, '')
    if (digits.length < 9 || digits === lastLookup.current) return
    const t = setTimeout(() => {
      lastLookup.current = digits
      lookup.mutate(digits)
    }, 400)
    return () => clearTimeout(t)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [cedula, open])

  function payload(nextActive = isActive) {
    return {
      identificationNumber: cedula.trim(),
      fullName: fullName.trim(),
      position: position.trim() || undefined,
      department: department.trim() || undefined,
      email: email.trim() || undefined,
      phoneNumber: phoneNumber.trim() || undefined,
      isActive: nextActive,
    }
  }

  const save = useMutation({
    mutationFn: async (nextActive?: boolean) => {
      const data = payload(nextActive ?? isActive)
      if (isEdit) return itEmployeesApi.update(employee!.id, data).then(r => r.data.data!)
      return itEmployeesApi.create({
        identificationNumber: data.identificationNumber,
        fullName: data.fullName,
        position: data.position,
        department: data.department,
        email: data.email,
        phoneNumber: data.phoneNumber,
      }).then(r => r.data.data!)
    },
    onSuccess: (emp) => {
      qc.invalidateQueries({ queryKey: qk.ti.all })
      toast.success(isEdit ? 'Colaborador actualizado.' : 'Colaborador creado.')
      if (!isEdit) onCreated?.(emp)
      onSaved?.(emp)
      onClose()
    },
    onError: (e) => toast.error(extractApiError(e)),
  })

  function validate() {
    const digits = cedula.replace(/\D/g, '')
    if (digits.length < 9) {
      toast.error('Ingrese una cédula válida.')
      return false
    }
    if (!fullName.trim()) {
      toast.error('Falta el nombre. Espere la búsqueda o escríbalo.')
      return false
    }
    return true
  }

  function submit() {
    if (!validate()) return
    save.mutate(undefined)
  }

  function toggleActive() {
    if (!validate()) return
    const nextActive = !isActive
    setIsActive(nextActive)
    save.mutate(nextActive)
  }

  function close() {
    if (!save.isPending) onClose()
  }

  if (!open) return null

  return (
    <div className="fixed inset-0 z-50 flex items-end justify-center p-0 sm:items-center sm:p-4">
      <div className="absolute inset-0 bg-black/40 backdrop-blur-sm" onClick={close} />
      <div className="relative w-full max-w-2xl rounded-t-[14px] border border-line bg-paper shadow-xl sm:rounded-[12px]">
        <div className="flex items-center justify-between border-b border-line px-5 py-3">
          <h2 className="flex items-center gap-2 text-sm font-semibold text-ink">
            {isEdit ? <Save className="h-4 w-4" style={{ color: BRAND }} /> : <UserPlus className="h-4 w-4" style={{ color: BRAND }} />}
            {isEdit ? 'Editar colaborador' : 'Nuevo colaborador'}
          </h2>
          <button onClick={close} className="rounded p-1 text-ink2 hover:bg-bg hover:text-ink" aria-label="Cerrar">
            <X className="h-4 w-4" />
          </button>
        </div>

        <div className="grid gap-4 p-5 sm:grid-cols-2">
          <div>
            <label className={labelCls}>Cédula *</label>
            <div className="relative">
              <input
                className={`${inputCls} ${isEdit ? 'bg-bg text-ink2' : ''}`}
                value={cedula}
                readOnly={isEdit}
                onChange={e => { setCedula(e.target.value); setLooked('idle') }}
                placeholder="1 0123 0456"
                inputMode="numeric"
                autoFocus={!isEdit}
              />
              {lookup.isPending && <Loader2 className="absolute right-3 top-1/2 h-4 w-4 -translate-y-1/2 animate-spin text-ink2" />}
            </div>
            {isEdit && <p className="mt-1 text-[11px] text-ink2">La cédula no se puede modificar después de crear el colaborador.</p>}
            {!isEdit && looked === 'idle' && cedula.replace(/\D/g, '').length > 0 && cedula.replace(/\D/g, '').length < 9 && (
              <p className="mt-1 text-[11px] text-ink2">Digite la cédula completa para buscar el nombre automáticamente.</p>
            )}
            {!isEdit && looked === 'found' && <p className="mt-1 flex items-center gap-1 text-[11px] text-emerald-700"><CheckCircle2 className="h-3 w-3" /> Nombre autocompletado desde el registro civil.</p>}
            {!isEdit && looked === 'notfound' && <p className="mt-1 text-[11px] text-amber-700">No se encontró la cédula. Puede escribir el nombre manualmente.</p>}
          </div>

          <div>
            <label className={labelCls}>Nombre completo *</label>
            <input className={inputCls} value={fullName} onChange={e => setFullName(e.target.value)} placeholder="Se autocompleta con la cédula" />
          </div>

          <div>
            <label className={labelCls}>Puesto</label>
            <input className={inputCls} value={position} onChange={e => setPosition(e.target.value)} placeholder="Ej. Asistente de ventas" />
          </div>

          <div>
            <label className={labelCls}>Departamento</label>
            <input className={inputCls} value={department} onChange={e => setDepartment(e.target.value)} placeholder="Ej. TI" />
          </div>

          <div>
            <label className={labelCls}>Correo</label>
            <input className={inputCls} value={email} onChange={e => setEmail(e.target.value)} placeholder="correo@repagro.com" type="email" />
          </div>

          <div>
            <label className={labelCls}>Teléfono</label>
            <input className={inputCls} value={phoneNumber} onChange={e => setPhoneNumber(e.target.value)} placeholder="8888-8888" inputMode="tel" />
          </div>

          {isEdit && (
            <label className="flex items-center gap-2 rounded-[8px] border border-line bg-bg px-3 py-2 text-sm text-ink sm:col-span-2">
              <input type="checkbox" checked={isActive} onChange={e => setIsActive(e.target.checked)} className="h-4 w-4 rounded border-line" />
              Colaborador activo
            </label>
          )}
        </div>

        <div className="flex flex-col-reverse gap-2.5 border-t border-line px-5 py-3 sm:flex-row sm:items-center sm:justify-between">
          <div>
            {isEdit && (
              <button
                onClick={toggleActive}
                disabled={save.isPending}
                className="inline-flex items-center justify-center gap-2 rounded-[8px] border border-line bg-paper px-4 py-2 text-sm font-medium text-ink hover:bg-bg disabled:opacity-50"
              >
                {save.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : isActive ? <UserX className="h-4 w-4" /> : <UserCheck className="h-4 w-4" />}
                {isActive ? 'Inactivar' : 'Reactivar'}
              </button>
            )}
          </div>
          <div className="flex items-center justify-end gap-2.5">
            <button onClick={close} disabled={save.isPending} className="rounded-[8px] border border-line bg-paper px-4 py-2 text-sm font-medium text-ink hover:bg-bg disabled:opacity-50">Cancelar</button>
            <button onClick={submit} disabled={save.isPending}
              className="inline-flex items-center gap-2 rounded-[8px] px-4 py-2 text-sm font-medium text-white hover:opacity-90 disabled:opacity-50" style={{ background: BRAND }}>
              {save.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : isEdit ? <Save className="h-4 w-4" /> : <UserPlus className="h-4 w-4" />}
              {isEdit ? 'Guardar cambios' : 'Crear colaborador'}
            </button>
          </div>
        </div>
      </div>
    </div>
  )
}
