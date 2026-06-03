import { useEffect, useMemo, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  X, Building2, Search, Plus, Pencil, Check, Power, PowerOff, Loader2, AlertTriangle, Inbox,
} from 'lucide-react'
import toast from 'react-hot-toast'
import { itCatalogsApi } from '@/api/itAssets'
import { qk } from '@/lib/queryKeys'
import { extractApiError } from '@/utils'
import type { ItDepartmentDto } from '@/types'

const BRAND = '#0E6B4B'
const inputCls = 'h-9 w-full rounded-[8px] border border-line bg-paper px-3 text-sm text-ink placeholder:text-ink2 focus:border-brand-400 focus:outline-none'

interface Props {
  open: boolean
  onClose: () => void
  /** Se llama tras cualquier cambio (crear/editar/estado) por si el padre quiere reaccionar. */
  onChanged?: () => void
}

export default function DepartmentManagerModal({ open, onClose, onChanged }: Props) {
  const qc = useQueryClient()
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')

  // Alta
  const [newName, setNewName] = useState('')
  const [newCode, setNewCode] = useState('')
  // Edición inline
  const [editingId, setEditingId] = useState<string | null>(null)
  const [editName, setEditName] = useState('')
  const [editCode, setEditCode] = useState('')
  // Confirmación de inactivar cuando tiene activos asignados
  const [confirmId, setConfirmId] = useState<string | null>(null)

  // Debounce de la búsqueda (400ms).
  useEffect(() => {
    const t = setTimeout(() => setSearch(searchInput.trim()), 400)
    return () => clearTimeout(t)
  }, [searchInput])

  // Reset al cerrar.
  useEffect(() => {
    if (!open) { setSearchInput(''); setSearch(''); setNewName(''); setNewCode(''); setEditingId(null); setConfirmId(null) }
  }, [open])

  const { data, isLoading } = useQuery({
    queryKey: qk.ti.departments(search),
    queryFn: () => itCatalogsApi.listDepartments(search || undefined).then(r => r.data.data ?? []),
    enabled: open,
    staleTime: 15_000,
  })

  function afterChange() {
    qc.invalidateQueries({ queryKey: ['ti', 'departments'] })
    qc.invalidateQueries({ queryKey: qk.ti.catalogs })  // refresca el select del formulario de activo
    onChanged?.()
  }

  const createMut = useMutation({
    mutationFn: () => itCatalogsApi.createDepartment({ name: newName.trim(), code: newCode.trim() || undefined }),
    onSuccess: () => { toast.success('Departamento creado.'); setNewName(''); setNewCode(''); afterChange() },
    onError: (e) => toast.error(extractApiError(e)),
  })

  const updateMut = useMutation({
    mutationFn: (id: string) => itCatalogsApi.updateDepartment(id, { name: editName.trim(), code: editCode.trim() || undefined }),
    onSuccess: () => { toast.success('Departamento actualizado.'); setEditingId(null); afterChange() },
    onError: (e) => toast.error(extractApiError(e)),
  })

  const statusMut = useMutation({
    mutationFn: ({ id, isActive }: { id: string; isActive: boolean }) => itCatalogsApi.setDepartmentStatus(id, isActive),
    onSuccess: (_r, v) => { toast.success(v.isActive ? 'Departamento activado.' : 'Departamento inactivado.'); setConfirmId(null); afterChange() },
    onError: (e) => toast.error(extractApiError(e)),
  })

  const list = data ?? []
  const activeCount = useMemo(() => list.filter(d => d.isActive).length, [list])

  function startEdit(d: ItDepartmentDto) {
    setConfirmId(null); setEditingId(d.id); setEditName(d.name); setEditCode(d.code ?? '')
  }
  function submitCreate() {
    if (!newName.trim()) return toast.error('Escribe el nombre del departamento.')
    createMut.mutate()
  }
  function submitEdit(id: string) {
    if (!editName.trim()) return toast.error('El nombre no puede quedar vacío.')
    updateMut.mutate(id)
  }
  function toggle(d: ItDepartmentDto) {
    if (d.isActive && d.assetCount > 0) { setConfirmId(d.id); return }   // pedir confirmación
    statusMut.mutate({ id: d.id, isActive: !d.isActive })
  }

  if (!open) return null

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      <div className="absolute inset-0 bg-black/40 backdrop-blur-sm" onClick={onClose} />
      <div className="relative flex max-h-[90vh] w-full max-w-2xl flex-col rounded-[12px] border border-line bg-paper shadow-xl">
        {/* Header */}
        <div className="flex items-center justify-between border-b border-line px-5 py-3">
          <div>
            <h2 className="flex items-center gap-2 text-sm font-semibold text-ink">
              <Building2 className="h-4 w-4" style={{ color: BRAND }} /> Administrar departamentos
            </h2>
            <p className="mt-0.5 text-[11px] text-ink2">Crea, edita o activa/inactiva. Los inactivos no aparecen al registrar activos.</p>
          </div>
          <button onClick={onClose} className="rounded p-1 text-ink2 hover:bg-bg hover:text-ink"><X className="h-4 w-4" /></button>
        </div>

        {/* Alta + búsqueda */}
        <div className="space-y-3 border-b border-line px-5 py-3">
          <div className="flex flex-wrap items-end gap-2">
            <div className="min-w-[160px] flex-1">
              <label className="mb-1 block text-[11px] font-medium text-ink2">Nuevo departamento</label>
              <input className={inputCls} value={newName} onChange={e => setNewName(e.target.value)}
                placeholder="Nombre" onKeyDown={e => { if (e.key === 'Enter') submitCreate() }} />
            </div>
            <div className="w-28">
              <label className="mb-1 block text-[11px] font-medium text-ink2">Código</label>
              <input className={inputCls} value={newCode} onChange={e => setNewCode(e.target.value)}
                placeholder="Opc." onKeyDown={e => { if (e.key === 'Enter') submitCreate() }} />
            </div>
            <button onClick={submitCreate} disabled={createMut.isPending}
              className="inline-flex h-9 shrink-0 items-center gap-1.5 rounded-[8px] px-3 text-sm font-medium text-white hover:opacity-90 disabled:opacity-50"
              style={{ background: BRAND }}>
              {createMut.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : <Plus className="h-4 w-4" />} Agregar
            </button>
          </div>
          <div className="relative">
            <Search className="pointer-events-none absolute left-3 top-1/2 h-3.5 w-3.5 -translate-y-1/2 text-ink2" />
            <input className={`${inputCls} pl-8`} value={searchInput} onChange={e => setSearchInput(e.target.value)}
              placeholder="Buscar por nombre o código…" />
          </div>
        </div>

        {/* Lista */}
        <div className="min-h-0 flex-1 overflow-y-auto">
          {isLoading ? (
            <div className="flex justify-center py-12"><Loader2 className="h-5 w-5 animate-spin text-ink2" /></div>
          ) : list.length === 0 ? (
            <div className="flex flex-col items-center gap-2 py-12 text-center text-ink2">
              <Inbox className="h-7 w-7" strokeWidth={1.5} />
              <p className="text-sm">{search ? 'Sin resultados para la búsqueda.' : 'Aún no hay departamentos. Crea el primero arriba.'}</p>
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full min-w-[520px] text-sm">
                <thead>
                  <tr className="border-b border-line text-[11px] uppercase tracking-wider text-ink2">
                    <th className="px-5 py-2 text-left font-semibold">Departamento</th>
                    <th className="px-3 py-2 text-left font-semibold">Código</th>
                    <th className="px-3 py-2 text-center font-semibold">Activos</th>
                    <th className="px-3 py-2 text-left font-semibold">Estado</th>
                    <th className="px-5 py-2 text-right font-semibold">Acciones</th>
                  </tr>
                </thead>
                <tbody>
                  {list.map(d => {
                    const editing = editingId === d.id
                    const confirming = confirmId === d.id
                    return (
                      <tr key={d.id} className="border-b border-line/60 last:border-0">
                        {/* Nombre */}
                        <td className="px-5 py-2.5">
                          {editing ? (
                            <input className={inputCls} value={editName} onChange={e => setEditName(e.target.value)}
                              autoFocus onKeyDown={e => { if (e.key === 'Enter') submitEdit(d.id); if (e.key === 'Escape') setEditingId(null) }} />
                          ) : (
                            <span className={d.isActive ? 'font-medium text-ink' : 'text-ink2 line-through'}>{d.name}</span>
                          )}
                        </td>
                        {/* Código */}
                        <td className="px-3 py-2.5">
                          {editing ? (
                            <input className={`${inputCls} w-24`} value={editCode} onChange={e => setEditCode(e.target.value)}
                              placeholder="Opc." onKeyDown={e => { if (e.key === 'Enter') submitEdit(d.id); if (e.key === 'Escape') setEditingId(null) }} />
                          ) : (
                            <span className="text-ink2">{d.code || '—'}</span>
                          )}
                        </td>
                        {/* Conteo */}
                        <td className="px-3 py-2.5 text-center tabular-nums text-ink2">{d.assetCount}</td>
                        {/* Estado */}
                        <td className="px-3 py-2.5">
                          <span className={`inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-[11px] font-medium ${d.isActive ? 'bg-emerald-50 text-emerald-700' : 'bg-slate-100 text-slate-500'}`}>
                            <span className={`h-1.5 w-1.5 rounded-full ${d.isActive ? 'bg-emerald-500' : 'bg-slate-400'}`} />
                            {d.isActive ? 'Activo' : 'Inactivo'}
                          </span>
                        </td>
                        {/* Acciones */}
                        <td className="px-5 py-2.5">
                          {confirming ? (
                            <div className="flex items-center justify-end gap-2">
                              <span className="flex items-center gap-1 text-[11px] text-amber-700">
                                <AlertTriangle className="h-3.5 w-3.5" /> Tiene {d.assetCount} activo(s). ¿Inactivar?
                              </span>
                              <button onClick={() => statusMut.mutate({ id: d.id, isActive: false })} disabled={statusMut.isPending}
                                className="rounded-[6px] bg-red-600 px-2 py-1 text-[12px] font-medium text-white hover:bg-red-700 disabled:opacity-50">Sí</button>
                              <button onClick={() => setConfirmId(null)}
                                className="rounded-[6px] border border-line px-2 py-1 text-[12px] text-ink hover:bg-bg">No</button>
                            </div>
                          ) : editing ? (
                            <div className="flex items-center justify-end gap-1.5">
                              <button onClick={() => submitEdit(d.id)} disabled={updateMut.isPending} title="Guardar"
                                className="inline-flex items-center gap-1 rounded-[6px] px-2 py-1 text-[12px] font-medium text-white hover:opacity-90 disabled:opacity-50" style={{ background: BRAND }}>
                                {updateMut.isPending ? <Loader2 className="h-3.5 w-3.5 animate-spin" /> : <Check className="h-3.5 w-3.5" />} Guardar
                              </button>
                              <button onClick={() => setEditingId(null)} className="rounded-[6px] border border-line px-2 py-1 text-[12px] text-ink hover:bg-bg">Cancelar</button>
                            </div>
                          ) : (
                            <div className="flex items-center justify-end gap-1">
                              <button onClick={() => startEdit(d)} title="Editar"
                                className="rounded-[6px] p-1.5 text-ink2 hover:bg-bg hover:text-ink"><Pencil className="h-4 w-4" /></button>
                              <button onClick={() => toggle(d)} disabled={statusMut.isPending}
                                title={d.isActive ? 'Inactivar' : 'Activar'}
                                className={`rounded-[6px] p-1.5 hover:bg-bg ${d.isActive ? 'text-ink2 hover:text-red-600' : 'text-emerald-600 hover:text-emerald-700'}`}>
                                {d.isActive ? <PowerOff className="h-4 w-4" /> : <Power className="h-4 w-4" />}
                              </button>
                            </div>
                          )}
                        </td>
                      </tr>
                    )
                  })}
                </tbody>
              </table>
            </div>
          )}
        </div>

        {/* Footer */}
        <div className="flex items-center justify-between border-t border-line px-5 py-3">
          <span className="text-[12px] text-ink2">{list.length} departamento(s) · {activeCount} activo(s)</span>
          <button onClick={onClose} className="rounded-[8px] border border-line bg-paper px-4 py-2 text-sm font-medium text-ink hover:bg-bg">Cerrar</button>
        </div>
      </div>
    </div>
  )
}
