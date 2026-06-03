import { useEffect, useMemo, useState } from 'react'
import { useNavigate, useParams, Link } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Loader2, ArrowLeft, Save, Cpu, Settings2 } from 'lucide-react'
import toast from 'react-hot-toast'

import { itAssetsApi, itCatalogsApi } from '@/api/itAssets'
import { itEmployeesApi } from '@/api/itEmployees'
import { qk, staleTimes, invalidate } from '@/lib/queryKeys'
import { useAuthStore } from '@/store/authStore'
import { extractApiError } from '@/utils'
import PhotoCapture from '@/components/ti/PhotoCapture'
import EmployeeCreateModal from '@/components/ti/EmployeeCreateModal'
import DepartmentManagerModal from '@/components/ti/DepartmentManagerModal'
import BrandManagerModal from '@/components/ti/BrandManagerModal'
import SupplierManagerModal from '@/components/ti/SupplierManagerModal'
import { CONDITION_OPTIONS } from '@/components/ti/itStatus'
import type {
  CreateItAssetRequest, ItAssetSpecDto, PhysicalCondition,
} from '@/types'

const BRAND = '#0E6B4B'

type FormState = CreateItAssetRequest & { spec: ItAssetSpecDto; photos: string[] }

const EMPTY: FormState = {
  internalCode: '', assetTypeId: '', brandId: '', model: '', serialNumber: '', assetTag: '',
  physicalCondition: 'Good', locationId: '', locationDetail: '', departmentId: '',
  currentHolderEmployeeId: '', purchaseDate: '', supplierId: '', cost: undefined, currency: '',
  hasWarranty: false, warrantyEndDate: '', notes: '', photos: [], spec: {},
}

const inputCls = 'h-10 w-full rounded-[8px] border border-line bg-paper px-3 text-sm text-ink placeholder:text-ink2 focus:border-brand-400 focus:outline-none'
const labelCls = 'mb-1 block text-[12px] font-medium text-ink2'

function Field({ label, required, children }: { label: string; required?: boolean; children: React.ReactNode }) {
  return (
    <div>
      <label className={labelCls}>{label}{required && <span className="text-red-600"> *</span>}</label>
      {children}
    </div>
  )
}

export default function ItAssetFormPage() {
  const { id } = useParams<{ id: string }>()
  const isEdit = !!id
  const navigate = useNavigate()
  const qc = useQueryClient()
  const { hasPermission } = useAuthStore()

  const [form, setForm] = useState<FormState>(EMPTY)
  const [rowVersion, setRowVersion] = useState<string | undefined>()
  const [empModal, setEmpModal] = useState(false)
  const [deptModal, setDeptModal] = useState(false)
  const [brandModal, setBrandModal] = useState(false)
  const [supplierModal, setSupplierModal] = useState(false)

  const set = <K extends keyof FormState>(k: K, v: FormState[K]) => setForm(f => ({ ...f, [k]: v }))
  const setSpec = (k: keyof ItAssetSpecDto, v: string | number | undefined) =>
    setForm(f => ({ ...f, spec: { ...f.spec, [k]: v } }))

  const catalogs = useQuery({
    queryKey: qk.ti.catalogs,
    queryFn: () => itCatalogsApi.getAll().then(r => r.data.data!),
    staleTime: staleTimes.tiCatalogs,
  })

  // Responsables = colaboradores TI (creados por cédula).
  const employees = useQuery({
    queryKey: [...qk.ti.all, 'employees-active'] as const,
    queryFn: () => itEmployeesApi.getActive().then(r => r.data.data ?? []),
    staleTime: staleTimes.ti,
  })

  const existing = useQuery({
    queryKey: qk.ti.asset(id ?? ''),
    queryFn: () => itAssetsApi.getById(id!).then(r => r.data.data!),
    enabled: isEdit,
    staleTime: staleTimes.ti,
  })

  useEffect(() => {
    if (!existing.data) return
    const a = existing.data
    setForm({
      internalCode: a.internalCode, assetTypeId: a.assetTypeId, brandId: a.brandId ?? '',
      model: a.model ?? '', serialNumber: a.serialNumber ?? '', assetTag: a.assetTag ?? '',
      physicalCondition: a.physicalCondition, locationId: a.locationId ?? '', locationDetail: a.locationDetail ?? '',
      departmentId: a.departmentId ?? '', currentHolderEmployeeId: a.currentHolderEmployeeId ?? '',
      purchaseDate: a.purchaseDate?.slice(0, 10) ?? '', supplierId: a.supplierId ?? '', cost: a.cost,
      currency: a.currency ?? '', hasWarranty: a.hasWarranty, warrantyEndDate: a.warrantyEndDate?.slice(0, 10) ?? '',
      notes: a.notes ?? '', photos: (a.photos ?? []).map(p => p.url), spec: a.spec ?? {},
    })
    setRowVersion(a.rowVersion)
  }, [existing.data])

  const selectedType = useMemo(
    () => catalogs.data?.types.find(t => t.id === form.assetTypeId),
    [catalogs.data, form.assetTypeId],
  )

  function buildPayload(): CreateItAssetRequest {
    const clean = (s?: string) => (s && s.trim() ? s.trim() : undefined)
    const specEmpty = Object.values(form.spec).every(v => v === undefined || v === '' || v === null)
    return {
      internalCode: form.internalCode.trim(),
      assetTypeId: form.assetTypeId,
      brandId: clean(form.brandId),
      model: clean(form.model),
      serialNumber: clean(form.serialNumber),
      assetTag: clean(form.assetTag),
      physicalCondition: form.physicalCondition,
      locationId: clean(form.locationId),
      locationDetail: clean(form.locationDetail),
      departmentId: clean(form.departmentId),
      currentHolderEmployeeId: clean(form.currentHolderEmployeeId),
      purchaseDate: clean(form.purchaseDate),
      supplierId: clean(form.supplierId),
      cost: form.cost === undefined || Number.isNaN(form.cost) ? undefined : Number(form.cost),
      currency: clean(form.currency),
      hasWarranty: form.hasWarranty,
      warrantyEndDate: clean(form.warrantyEndDate),
      notes: clean(form.notes),
      photos: form.photos,
      spec: specEmpty ? undefined : form.spec,
    }
  }

  const mutation = useMutation({
    mutationFn: async () => {
      const payload = buildPayload()
      if (isEdit) return itAssetsApi.update(id!, { ...payload, rowVersion }).then(r => r.data.data!)
      return itAssetsApi.create(payload).then(r => r.data.data!)
    },
    onSuccess: (asset) => {
      invalidate.ti(qc)
      toast.success(isEdit ? 'Activo actualizado.' : 'Activo registrado.')
      navigate(`/ti/assets/${asset.id}`)
    },
    onError: (e) => toast.error(extractApiError(e)),
  })

  function submit(e: React.FormEvent) {
    e.preventDefault()
    if (!form.internalCode.trim()) return toast.error('El código interno es obligatorio.')
    if (!form.assetTypeId) return toast.error('Seleccione el tipo de activo.')
    if (selectedType?.requiresSerial && !form.serialNumber?.trim())
      return toast.error(`El tipo «${selectedType.name}» exige número de serie.`)
    mutation.mutate()
  }

  const loadingExisting = isEdit && existing.isLoading

  return (
    <div className="flex min-h-full flex-col">
      <header className="sticky top-0 z-10 flex items-center gap-3 border-b border-line bg-paper px-6 py-3" style={{ minHeight: 64 }}>
        <button onClick={() => navigate(-1)} className="rounded p-1.5 text-ink2 hover:bg-bg hover:text-ink" aria-label="Volver">
          <ArrowLeft className="h-5 w-5" />
        </button>
        <div className="min-w-0 flex-1">
          <p className="font-mono text-[12px] text-ink2 mb-0.5 leading-none">TI / Inventario / {isEdit ? 'Editar' : 'Nuevo'}</p>
          <h1 className="text-[18px] font-semibold text-ink leading-tight tracking-tight flex items-center gap-2">
            <Cpu className="h-4.5 w-4.5" style={{ color: BRAND }} /> {isEdit ? 'Editar activo' : 'Registrar activo'}
          </h1>
        </div>
      </header>

      {loadingExisting ? (
        <div className="flex flex-1 items-center justify-center"><Loader2 className="h-6 w-6 animate-spin text-ink2" /></div>
      ) : (
        <form onSubmit={submit} className="flex-1 p-6 bg-bg">
          <div className="mx-auto max-w-3xl space-y-3.5">

            {/* Identificación */}
            <section className="rounded-[10px] border border-line bg-paper p-5 shadow-sh1">
              <h2 className="mb-4 text-sm font-semibold text-ink">Identificación</h2>
              <div className="grid gap-4 sm:grid-cols-2">
                <Field label="Código interno" required>
                  <input className={inputCls} value={form.internalCode} onChange={e => set('internalCode', e.target.value)} placeholder="EJ. TI-LAP-001" />
                </Field>
                <Field label="Tipo de activo" required>
                  <select className={inputCls} value={form.assetTypeId} onChange={e => set('assetTypeId', e.target.value)}>
                    <option value="">Seleccione…</option>
                    {(catalogs.data?.types ?? []).map(t => <option key={t.id} value={t.id}>{t.name}</option>)}
                  </select>
                </Field>
                <Field label="Marca">
                  <div className="flex gap-2">
                    <select className={inputCls} value={form.brandId} onChange={e => set('brandId', e.target.value)}>
                      <option value="">—</option>
                      {(catalogs.data?.brands ?? []).map(b => <option key={b.id} value={b.id}>{b.name}</option>)}
                    </select>
                    {hasPermission('Ti.Catalog.Manage') && (
                      <button type="button" onClick={() => setBrandModal(true)} title="Administrar marcas"
                        className="inline-flex shrink-0 items-center rounded-[8px] border border-line bg-paper px-3 text-sm font-medium text-ink hover:bg-bg">
                        <Settings2 className="h-4 w-4" />
                      </button>
                    )}
                  </div>
                </Field>
                <Field label="Modelo">
                  <input className={inputCls} value={form.model} onChange={e => set('model', e.target.value)} />
                </Field>
                <Field label={`Número de serie${selectedType?.requiresSerial ? '' : ' (opcional)'}`} required={selectedType?.requiresSerial}>
                  <input className={inputCls} value={form.serialNumber} onChange={e => set('serialNumber', e.target.value)} />
                </Field>
                <Field label="Placa / etiqueta">
                  <input className={inputCls} value={form.assetTag} onChange={e => set('assetTag', e.target.value)} />
                </Field>
                <Field label="Estado físico">
                  <select className={inputCls} value={form.physicalCondition} onChange={e => set('physicalCondition', e.target.value as PhysicalCondition)}>
                    {CONDITION_OPTIONS.map(o => <option key={o.value} value={o.value}>{o.label}</option>)}
                  </select>
                </Field>
              </div>
            </section>

            {/* Ubicación / responsable */}
            <section className="rounded-[10px] border border-line bg-paper p-5 shadow-sh1">
              <h2 className="mb-4 text-sm font-semibold text-ink">Ubicación y responsable</h2>
              <div className="grid gap-4 sm:grid-cols-2">
                <Field label="Ubicación">
                  <select className={inputCls} value={form.locationId} onChange={e => set('locationId', e.target.value)}>
                    <option value="">—</option>
                    {(catalogs.data?.locations ?? []).map(l => <option key={l.id} value={l.id}>{l.name}</option>)}
                  </select>
                </Field>
                <Field label="Detalle de ubicación">
                  <input className={inputCls} value={form.locationDetail} onChange={e => set('locationDetail', e.target.value)} placeholder="Oficina, piso, escritorio…" />
                </Field>
                <Field label="Departamento">
                  <div className="flex gap-2">
                    <select className={inputCls} value={form.departmentId} onChange={e => set('departmentId', e.target.value)}>
                      <option value="">—</option>
                      {(catalogs.data?.departments ?? []).map(d => <option key={d.id} value={d.id}>{d.name}</option>)}
                    </select>
                    {hasPermission('Ti.Catalog.Manage') && (
                      <button type="button" onClick={() => setDeptModal(true)} title="Administrar departamentos"
                        className="inline-flex shrink-0 items-center rounded-[8px] border border-line bg-paper px-3 text-sm font-medium text-ink hover:bg-bg">
                        <Settings2 className="h-4 w-4" />
                      </button>
                    )}
                  </div>
                </Field>
                <Field label="Responsable actual">
                  <div className="flex gap-2">
                    <select className={inputCls} value={form.currentHolderEmployeeId} onChange={e => set('currentHolderEmployeeId', e.target.value)}>
                      <option value="">— Sin asignar —</option>
                      {(employees.data ?? []).map(emp => <option key={emp.id} value={emp.id}>{emp.fullName}{emp.position ? ` · ${emp.position}` : ''}</option>)}
                    </select>
                    {hasPermission('Ti.Employee.Manage') && (
                      <button type="button" onClick={() => setEmpModal(true)} title="Nuevo colaborador"
                        className="inline-flex shrink-0 items-center rounded-[8px] border border-line bg-paper px-3 text-sm font-medium text-ink hover:bg-bg">+</button>
                    )}
                  </div>
                </Field>
              </div>
            </section>

            {/* Compra / garantía */}
            <section className="rounded-[10px] border border-line bg-paper p-5 shadow-sh1">
              <h2 className="mb-4 text-sm font-semibold text-ink">Compra y garantía</h2>
              <div className="grid gap-4 sm:grid-cols-2">
                <Field label="Fecha de compra">
                  <input type="date" className={inputCls} value={form.purchaseDate} onChange={e => set('purchaseDate', e.target.value)} />
                </Field>
                <Field label="Proveedor">
                  <div className="flex gap-2">
                    <select className={inputCls} value={form.supplierId} onChange={e => set('supplierId', e.target.value)}>
                      <option value="">—</option>
                      {(catalogs.data?.suppliers ?? []).map(s => <option key={s.id} value={s.id}>{s.name}</option>)}
                    </select>
                    {hasPermission('Ti.Catalog.Manage') && (
                      <button type="button" onClick={() => setSupplierModal(true)} title="Administrar proveedores"
                        className="inline-flex shrink-0 items-center rounded-[8px] border border-line bg-paper px-3 text-sm font-medium text-ink hover:bg-bg">
                        <Settings2 className="h-4 w-4" />
                      </button>
                    )}
                  </div>
                </Field>
                <Field label="Costo">
                  <input type="number" min="0" step="0.01" className={inputCls} value={form.cost ?? ''} onChange={e => set('cost', e.target.value === '' ? undefined : Number(e.target.value))} />
                </Field>
                <Field label="Moneda">
                  <select className={inputCls} value={form.currency} onChange={e => set('currency', e.target.value)}>
                    <option value="">—</option>
                    <option value="CRC">CRC (₡)</option>
                    <option value="USD">USD ($)</option>
                  </select>
                </Field>
                <div className="flex items-center gap-2 pt-6">
                  <input id="hasWarranty" type="checkbox" checked={form.hasWarranty} onChange={e => set('hasWarranty', e.target.checked)} className="h-4 w-4 rounded border-line" />
                  <label htmlFor="hasWarranty" className="text-sm text-ink">Tiene garantía</label>
                </div>
                {form.hasWarranty && (
                  <Field label="Vencimiento de garantía">
                    <input type="date" className={inputCls} value={form.warrantyEndDate} onChange={e => set('warrantyEndDate', e.target.value)} />
                  </Field>
                )}
              </div>
            </section>

            {/* Especificaciones técnicas (sólo tipos de cómputo) */}
            {selectedType?.hasComputeSpecs && (
              <section className="rounded-[10px] border border-line bg-paper p-5 shadow-sh1">
                <h2 className="mb-1 text-sm font-semibold text-ink">Especificaciones técnicas</h2>
                <p className="mb-4 text-[11px] text-ink2">El AnyDesk ID es sólo identificador — nunca se guardan contraseñas.</p>
                <div className="grid gap-4 sm:grid-cols-2">
                  <Field label="Sistema operativo"><input className={inputCls} value={form.spec.operatingSystem ?? ''} onChange={e => setSpec('operatingSystem', e.target.value)} /></Field>
                  <Field label="Procesador"><input className={inputCls} value={form.spec.processor ?? ''} onChange={e => setSpec('processor', e.target.value)} /></Field>
                  <Field label="RAM (GB)"><input type="number" min="0" className={inputCls} value={form.spec.ramGb ?? ''} onChange={e => setSpec('ramGb', e.target.value === '' ? undefined : Number(e.target.value))} /></Field>
                  <Field label="Disco (GB)"><input type="number" min="0" className={inputCls} value={form.spec.diskGb ?? ''} onChange={e => setSpec('diskGb', e.target.value === '' ? undefined : Number(e.target.value))} /></Field>
                  <Field label="MAC Ethernet"><input className={inputCls} value={form.spec.macEthernet ?? ''} onChange={e => setSpec('macEthernet', e.target.value)} /></Field>
                  <Field label="MAC WiFi"><input className={inputCls} value={form.spec.macWifi ?? ''} onChange={e => setSpec('macWifi', e.target.value)} /></Field>
                  <Field label="Dirección IP"><input className={inputCls} value={form.spec.ipAddress ?? ''} onChange={e => setSpec('ipAddress', e.target.value)} /></Field>
                  <Field label="Nombre en dominio"><input className={inputCls} value={form.spec.domainName ?? ''} onChange={e => setSpec('domainName', e.target.value)} /></Field>
                  <Field label="AnyDesk ID"><input className={inputCls} value={form.spec.anyDeskId ?? ''} onChange={e => setSpec('anyDeskId', e.target.value)} /></Field>
                  <Field label="Usuario Microsoft 365"><input className={inputCls} value={form.spec.microsoft365User ?? ''} onChange={e => setSpec('microsoft365User', e.target.value)} /></Field>
                  <Field label="Estado antivirus"><input className={inputCls} value={form.spec.antivirusStatus ?? ''} onChange={e => setSpec('antivirusStatus', e.target.value)} /></Field>
                </div>
              </section>
            )}

            {/* Foto + observaciones */}
            <section className="rounded-[10px] border border-line bg-paper p-5 shadow-sh1">
              <h2 className="mb-4 text-sm font-semibold text-ink">Foto y observaciones</h2>
              <div className="mb-4">
                <PhotoCapture
                  value={form.photos}
                  onChange={(photos) => set('photos', photos)}
                  max={5}
                  label="Fotos del activo"
                  hint="Hasta 5 fotos (frontal, etiqueta/serie, estado físico). En móvil puedes tomarlas con la cámara. Se guardan en la base de datos."
                />
              </div>
              <Field label="Observaciones">
                <textarea className={`${inputCls} h-24 py-2`} value={form.notes} onChange={e => set('notes', e.target.value)} />
              </Field>
            </section>

            {/* Acciones */}
            <div className="flex items-center justify-end gap-2.5 pb-2">
              <Link to="/ti/assets" className="rounded-[8px] border border-line bg-paper px-4 py-2.5 text-sm font-medium text-ink hover:bg-bg">Cancelar</Link>
              <button type="submit" disabled={mutation.isPending}
                className="inline-flex items-center gap-2 rounded-[8px] px-5 py-2.5 text-sm font-medium text-white transition-colors hover:opacity-90 disabled:opacity-50"
                style={{ background: BRAND }}>
                {mutation.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : <Save className="h-4 w-4" />}
                {isEdit ? 'Guardar cambios' : 'Registrar activo'}
              </button>
            </div>
          </div>
        </form>
      )}

      <EmployeeCreateModal
        open={empModal}
        onClose={() => setEmpModal(false)}
        onCreated={(emp) => { employees.refetch(); set('currentHolderEmployeeId', emp.id) }}
      />

      <DepartmentManagerModal
        open={deptModal}
        onClose={() => setDeptModal(false)}
        onChanged={() => catalogs.refetch()}
      />

      <BrandManagerModal
        open={brandModal}
        onClose={() => setBrandModal(false)}
        onChanged={() => catalogs.refetch()}
      />

      <SupplierManagerModal
        open={supplierModal}
        onClose={() => setSupplierModal(false)}
        onChanged={() => catalogs.refetch()}
      />
    </div>
  )
}
