# Módulo de TI / Inventario de Activos Tecnológicos — RepagroSuite

> Propuesta de arquitectura, diseño funcional, modelo de datos y plan de implementación
> para convertir la *Bitácora de Equipos TI* (Excel `.xlsm`) en un módulo ERP profesional.
>
> **Autor:** Equipo Arquitectura RepagroSuite · **Fecha:** 2026-05-29 · **Estado:** Propuesta v1
> **Base técnica:** Clean Architecture .NET 9 + React 19 (stack RepagroSuite existente).

---

## 0. Decisiones de arquitectura previas (cómo encaja en RepagroSuite)

Antes del detalle, fijo cómo se integra con lo que **ya existe** en el repo, para no duplicar infraestructura:

| Recurso existente | Reutilización en el módulo TI |
|---|---|
| `Domain/Common/BaseEntity` (`Id:Guid`, `CreatedAt/By`, `UpdatedAt/By`, `IsDeleted`, `DeletedAt/By`, `RowVersion`) | **Todas** las entidades TI heredan de `BaseEntity`. Soft-delete, auditoría base y control de concurrencia optimista ya resueltos. |
| `Domain/Entities/AuditLog` (`Action`, `EntityName`, `EntityId`, `OldValues`, `NewValues`, `IpAddress`, `UserAgent`, `Module`, `Success`) | Auditoría central del módulo (`Module = "TI"`). No se crea una tabla `AuditoriaTI` aparte salvo necesidad fina (ver §12). |
| `Domain/Common/BusinessClock` (hora Costa Rica) | Todos los timestamps de **negocio** (boletas, asignaciones, movimientos). UTC sólo para JWT/seguridad. |
| `Permission` / `Role` / `RolePermission` / `SystemModule` | Permisos del módulo TI se modelan como `Permission` con prefijo `ti.*` y se asocian a roles existentes. |
| Patrón `Features/<Feature>/{DTOs,Services}`, controladores `API/Controllers/v1/*` | El módulo se agrega como `Features/ITAssets/...` siguiendo el mismo patrón vertical. |
| `IUnitOfWork` + repositorios genéricos | Transaccionalidad de boletas/consecutivos sobre el UoW existente. |

**Decisión de nomenclatura.** El codebase usa entidades en inglés PascalCase (`Room`, `Reservation`, `User`). Para mantener consistencia, las **clases de dominio y tablas** usan inglés (`ItAsset`, `ItTicket`…), y los **nombres de negocio en español** (Activo, Boleta) viven en la capa de presentación/etiquetas. La §9 incluye el mapeo Español↔Inglés. *(Si prefieres tablas en español como en el enunciado, es un find/replace mecánico; la recomendación profesional es no mezclar idiomas en el esquema.)*

---

## 1. Diagnóstico funcional del Excel actual

### 1.1 Qué hay hoy
Bitácora `.xlsm` operativa con hojas `Inicio`, `Bitacora`, `Dashboard`, `Acta`, `Historico`, `Listas`, `PIVOTS`; tabla `tblBitacora` con **53 filas (sólo 34 activos reales)**; macros VBA para boleta PDF, adjuntar imágenes, registrar histórico, limpiar boleta y navegar.

### 1.2 Datos que SÍ sirven para migrar (con limpieza)
| Campo Excel | Destino ERP | Nota |
|---|---|---|
| Código interno | `ItAsset.InternalCode` | Resolver duplicados antes de migrar. |
| Tipo de dispositivo | `ItAssetType` (catálogo) | Normalizar a catálogo cerrado. |
| Unidad de negocio | `BusinessUnit` / `Department` | Normalizar. |
| Ubicación + Detalle ubicación | `ItLocation` + `Asset.LocationDetail` | Separar catálogo vs texto libre. |
| Marca / Modelo | `ItBrand` + `ItAsset.Model` | Deduplicar y normalizar (mayúsculas/typos). |
| Responsable | `Employee` (FK) | Normalizar a colaborador real, no texto. |
| AnyDesk ID | `ItAssetSpec.AnyDeskId` | El **ID** se conserva; la **contraseña NO** (ver 1.4). |
| Estado Kaspersky / M365 | Specs / flags | Mover a especificaciones, no al activo base. |
| Usuario 365 | `ItAssetSpec.Microsoft365User` | Dato operativo, no secreto. |
| Comentarios | `ItAsset.Notes` + `HistorialActivo` legacy | Migrar como observación histórica. |
| Boleta (referencia) | `ItTicket` legacy / documento adjunto | Migrar como documento histórico, no editable. |

### 1.3 Datos incompletos / faltantes que el módulo debe exigir
- **Número de serie**: no existe como obligatorio → será **requerido** y único (cuando aplique al tipo).
- **Estado del ciclo de vida** formal del activo (sólo había estados implícitos).
- **Placa/etiqueta**, **fecha de compra, proveedor, costo, moneda, garantía**, especificaciones técnicas (CPU/RAM/Disco/MAC/IP) en su mayoría ausentes.
- **Trazabilidad** de quién/cuándo/desde dónde se hizo cada cambio.

### 1.4 Datos que NO deben seguir guardándose tal cual
- **Contraseñas** (AnyDesk, equipos) en texto plano dentro del inventario → **prohibido**. Si se requiere gestionar secretos, va a un *vault*/secret manager separado y cifrado (ver §11). En la migración estos campos se **descartan** y se deja un flag "tenía secreto en Excel" para revisión.
- Fotos **embebidas** en celdas → migran a almacenamiento documental (ver §7).

### 1.5 Errores del Excel a corregir ANTES de migrar
1. **Filas vacías contadas como activos** (53→34): el dashboard infla totales. Migrar sólo filas con identificador válido.
2. **Códigos duplicados**: resolver con sufijo o reasignación + reporte de conflictos.
3. **Marcas/modelos inconsistentes** ("HP"/"Hewlett Packard"/"hp"): normalizar a catálogo.
4. **Responsables como texto libre**: mapear a colaboradores reales.
5. **Ubicaciones mezcladas** (catálogo + detalle en un solo campo).
6. **Secretos en celdas**: extraer/eliminar.

### 1.6 Riesgos de copiar la estructura tal cual a BD
- Se perpetúa el conteo erróneo y los duplicados → **integridad referencial rota** desde el día 1.
- Sin FKs ni catálogos → datos sucios imposibles de reportar.
- Sin estado de ciclo de vida → no se puede impedir asignar un equipo dado de baja.
- Secretos en BD → **incidente de seguridad** y hallazgo de auditoría.
- Sin auditoría ni `RowVersion` → no hay trazabilidad ni control de concurrencia.
- Boletas/fotos atadas a rutas locales → se rompen al cambiar de equipo.

---

## 2. Diseño funcional del módulo

Submódulos:

1. **Inventario de Activos TI** (catálogo maestro de equipos y su ficha técnica).
2. **Asignaciones** (entrega a colaborador, con evidencia y firma).
3. **Devoluciones** (recepción, estado, daños, cierre de asignación).
4. **Boletas TI** (documentos formales con consecutivo, firma y PDF inmutable).
5. **Mantenimientos / Reparaciones**.
6. **Bajas de activo** (con aprobación y evidencia).
7. **Licencias y Accesorios**.
8. **Catálogos** (tipos, marcas, ubicaciones, departamentos, proveedores).
9. **Dashboard y Reportes TI**.
10. **Auditoría TI** (consulta de trazabilidad).

### 2.1 Tipos de activo soportados (catálogo `ItAssetType`)
Laptop, Desktop, Tablet, Celular, Impresora, Monitor, Cámara, Switch, Access Point, UPS, Servidor, Equipo de red, Licencia, Accesorio, Otro. *Cada tipo define si exige número de serie, si tiene especificaciones de cómputo, si es asignable, etc. (metadatos del tipo).*

### 2.2 Ficha del activo (campos)
**Identificación:** Código interno · Tipo · Marca · Modelo · Número de serie · Placa/etiqueta.
**Estado:** Estado administrativo (ciclo de vida, §3) · Estado físico (Nuevo/Bueno/Regular/Malo/Inservible) · Estado lógico (`IsDeleted`).
**Ubicación/organización:** Ubicación · Departamento · Unidad de negocio · Responsable actual (FK colaborador).
**Compra:** Fecha de compra · Proveedor · Costo · Moneda · Garantía (sí/no) · Fecha vencimiento garantía.
**Técnicas (tabla 1:1 `ItAssetSpec`):** SO · Procesador · RAM · Disco · MAC Ethernet · MAC WiFi · IP asignada · Nombre en dominio · AnyDesk ID · Usuario M365 · Estado antivirus · Observaciones técnicas.
**Auditoría:** heredada de `BaseEntity` + `AuditLog`.

---

## 3. Estados del activo (ciclo de vida)

Enum `ItAssetStatus`:

`Disponible → Asignado → Prestado → EnRevision → EnMantenimiento → EnReparacion → Devuelto → Dañado → Perdido → Robado → DadoDeBaja → Inactivo`

### Máquina de estados (transiciones permitidas)

| Estado | Acciones permitidas | Transiciona a |
|---|---|---|
| **Disponible** | Asignar, Prestar, Enviar a mantenimiento, Dar de baja, Editar | Asignado, Prestado, EnMantenimiento, DadoDeBaja |
| **Asignado** | Devolver, Trasladar, Cambiar responsable | Devuelto, EnRevision, EnReparacion, Dañado |
| **Prestado** | Devolver | Devuelto, Disponible |
| **EnRevision** | Aprobar reingreso, Enviar a reparación, Dar de baja | Disponible, EnReparacion, DadoDeBaja |
| **EnMantenimiento** | Cerrar mantenimiento | Disponible, EnReparacion |
| **EnReparacion** | Cerrar reparación | Disponible, Dañado, DadoDeBaja |
| **Devuelto** | Reasignar, Enviar a revisión | Disponible, EnRevision |
| **Dañado** | Reparar, Dar de baja | EnReparacion, DadoDeBaja |
| **Perdido / Robado** | Reportar (requiere evidencia + autorización), Dar de baja | DadoDeBaja |
| **DadoDeBaja** | *(terminal)* sólo consulta | — |
| **Inactivo** | Reactivar | Disponible |

**Reglas duras (validadas en dominio, no sólo UI):**
- Un activo **Asignado** no puede reasignarse sin **devolución previa**.
- Un activo **DadoDeBaja** no genera nuevas boletas (terminal).
- Un activo **EnReparacion/EnMantenimiento** no puede aparecer como Disponible ni entregarse.
- **Robado/Perdido** exige `evidencia` + `usuario autorizador` antes de confirmar.

> Implementación: método `ItAsset.ChangeStatus(target, context)` que valida contra una tabla de transiciones; lanza `DomainException` si la transición no está permitida. La UI sólo muestra acciones válidas, pero la regla **se aplica en el backend**.

---

## 4. Flujo de Asignación

```
1. Seleccionar colaborador  → validar Employee.IsActive == true
2. Seleccionar 1..n activos  → validar cada uno en estado Disponible (lock)
3. Registrar accesorios incluidos (cargador, mouse, funda…)
4. Registrar estado físico de entrega por activo + observaciones
5. Capturar hasta 3 fotos por activo/boleta
6. Capturar firma del colaborador (canvas)
7. Capturar firma del responsable TI (canvas)
8. ── TRANSACCIÓN ──────────────────────────────────────────────
     a. Crear ItTicket (tipo Entrega) con consecutivo (§10)
     b. Crear ItTicketDetail por cada activo + accesorios
     c. Crear ItAssignment activa por activo
     d. ItAsset.ChangeStatus(Asignado)
     e. Insertar ItAssetPhoto + ItTicketSignature
     f. Insertar HistorialActivo + AuditLog
     g. Commit  → estado boleta = Firmada/Emitida
   ──────────────────────────────────────────────────────────────
9. Generar PDF (POST-commit) → guardar hash SHA-256 → boleta Emitida
```

Regla: **no puede existir más de una asignación activa por activo** (índice único filtrado, §9).

---

## 5. Flujo de Devolución

```
1. Buscar asignación activa (por activo o por colaborador)
2. Mostrar boleta original de entrega (PDF/datos)
3. Registrar estado físico de recepción por activo
4. Tomar fotos del equipo devuelto (hasta 3)
5. Registrar daños / faltantes / accesorios no devueltos
6. Capturar firmas (colaborador + responsable TI)
7. ── TRANSACCIÓN ──
     a. ItTicket (tipo Devolución) + consecutivo
     b. Cerrar ItAssignment (FechaDevolucion, estado=Cerrada)
     c. ItAsset.ChangeStatus según evaluación:
          sin daños         → Disponible
          requiere revisión → EnRevision
          con daño leve     → EnReparacion
          daño severo       → Dañado / DadoDeBaja (con aprobación)
     d. Fotos + firmas + Historial + AuditLog
     e. Commit
8. Generar PDF de devolución + hash
```

---

## 6. Boletas TI

**Tipos** (`ItTicketType`): Entrega · Devolución · Préstamo · Mantenimiento · Reparación · Traslado · Cambio de responsable · Asignación de accesorios · Baja.

**Campos:** Consecutivo · Tipo · Fecha/hora (BusinessClock) · Estado · Colaborador · Responsable TI · Activos · Accesorios · Observaciones · Fotos · Firmas · PDF (ruta + hash) · Usuario creador · Fecha creación · Usuario anulador · Motivo anulación.

**Estados de boleta** (`ItTicketStatus`): `Borrador → PendienteFirma → Firmada → Emitida → Anulada`.

**Reglas:**
- Una boleta **Emitida es inmutable**: no se edita; se **anula con motivo** y se emite una nueva (corrección por reversa, no por edición).
- Anulación exige `motivo` + usuario + timestamp; queda auditada.
- El PDF se genera **sólo tras commit** correcto de la transacción.
- Fotos y firmas asociadas a boleta Emitida **no se eliminan**.

---

## 7. Estrategia de Fotos (recomendación)

Hasta 3 fotos por boleta/activo: (1) frontal, (2) serie/etiqueta, (3) estado físico/daño.

**Opciones evaluadas:**

| Opción | Pros | Contras | Veredicto |
|---|---|---|---|
| `VARBINARY(MAX)` en SQL Server | Backup atómico, transaccional | Infla la BD, degrada rendimiento, backups enormes | ❌ no a escala |
| FILESTREAM / FileTable | Transaccional + en disco | Config compleja, lock-in SQL | ⚠️ sólo si ya se opera FILESTREAM |
| **Blob externo (Azure Blob / S3 / MinIO) + metadata en SQL** | Escalable, barato, CDN, BD liviana | Requiere servicio extra | ✅ **recomendado** |
| File Server / disco local del API | Simple | No escala, riesgo en multi-instancia | ⚠️ sólo on-prem pequeño |

**Recomendación:** **Blob externo + metadata en SQL Server.** Dado el hosting actual (Site4Now/Netlify), arrancar con **almacenamiento en disco del servidor o un bucket S3/MinIO/Azure Blob** según disponibilidad, abstraído tras `IDocumentStorage` para poder cambiar de proveedor sin tocar dominio.

**Pipeline de subida (`ItAssetPhoto` / `ItDocument`):**
1. Validar MIME (`image/jpeg|png|webp`) y tamaño máx (p.ej. 8 MB).
2. **Recomprimir** a JPEG/WebP de calidad razonable + **generar miniatura** (~320px).
3. Calcular **SHA-256** del binario.
4. Subir al storage → obtener `StoragePath`.
5. Guardar metadata: `MimeType`, `Width`, `Height`, `SizeBytes`, `Sha256`, `EntityType` + `EntityId`, `UploadedBy`, `UploadedAt` (BusinessClock), `IpAddress`.
6. Servir **siempre por endpoint autenticado** (`GET /api/ti/photos/{id}`), nunca URL pública directa.

---

## 8. Firma en pantalla (canvas)

Componente React `<SignaturePad>` sobre `<canvas>` con **Pointer Events** (cubre mouse, touchpad, dedo en celular/tablet y pantalla táctil con un solo código).

**Funciones:** Firmar · Limpiar · Confirmar · Guardar (export a PNG/SVG base64) · Asociar a boleta · Mostrar en PDF.

**Metadata de trazabilidad por firma (`ItTicketSignature`):** `SignerType` (Colaborador/ResponsableTI) · `SignerEmployeeId` · imagen (storage + hash) · `SignedAt` (BusinessClock) · `IpAddress` · `UserAgent` · `AuthenticatedUserId` · `Sha256`.

> **Aviso legal (obligatorio incluir en la boleta):** esto es una **firma electrónica en pantalla** (evidencia de aceptación), **no** una firma digital certificada legalmente. Si se requiere validez legal plena (Ley 8454 CR de firma digital), se debe integrar **certificado digital / proveedor autorizado** (p.ej. firma con certificado del BCCR). El diseño deja `ItTicketSignature` extensible para alojar un campo `CertificateThumbprint` futuro.

---

## 9. Modelo de base de datos (SQL Server)

Convención: PK `Id UNIQUEIDENTIFIER DEFAULT NEWSEQUENTIALID()` (vía `BaseEntity`), `RowVersion ROWVERSION`, soft-delete `IsDeleted BIT`, auditoría `CreatedAt/By`, `UpdatedAt/By`. Todas las FKs con `NO ACTION` o `RESTRICT` (no cascada en activos).

**Mapeo Español ↔ entidad/tabla:**

| Negocio (enunciado) | Tabla / Entidad |
|---|---|
| ActivosTI | `ItAssets` / `ItAsset` |
| TiposActivoTI | `ItAssetTypes` |
| EstadosActivoTI | enum `ItAssetStatus` (+ tabla lookup opcional) |
| MarcasTI | `ItBrands` |
| UbicacionesTI | `ItLocations` |
| Departamentos | `Departments` |
| Colaboradores | `Employees` |
| EspecificacionesActivoTI | `ItAssetSpecs` |
| AsignacionesActivoTI | `ItAssignments` |
| TiposBoletaTI | `ItTicketTypes` |
| BoletasTI | `ItTickets` |
| DetalleBoletaTI | `ItTicketDetails` |
| FotosActivoTI | `ItAssetPhotos` |
| FirmasBoletaTI | `ItTicketSignatures` |
| DocumentosTI | `ItDocuments` |
| HistorialActivoTI | `ItAssetHistory` |
| AuditoriaTI | `AuditLogs` (existente, `Module='TI'`) |
| MantenimientosTI | `ItMaintenances` |
| ReparacionesTI | `ItRepairs` |
| BajasActivoTI | `ItAssetDisposals` |
| LicenciasTI | `ItLicenses` |
| AccesoriosTI | `ItAccessories` |
| ConsecutivosDocumentoTI | `ItDocumentSequences` |

### 9.1 Tablas clave (resumen de campos / claves / índices)

**`ItAssets`** — núcleo del inventario
- PK `Id`. FKs: `AssetTypeId→ItAssetTypes`, `BrandId→ItBrands`, `LocationId→ItLocations`, `DepartmentId→Departments`, `CurrentHolderEmployeeId→Employees (null)`, `SupplierId→Suppliers (null)`.
- Campos: `InternalCode`, `Model`, `SerialNumber`, `AssetTag`, `Status (ItAssetStatus)`, `PhysicalCondition`, `PurchaseDate`, `Cost`, `Currency`, `HasWarranty`, `WarrantyEndDate`, `Notes`.
- **Únicos:** `UX_ItAssets_InternalCode` (filtrado `WHERE IsDeleted=0`); `UX_ItAssets_SerialNumber` (filtrado `WHERE SerialNumber IS NOT NULL AND IsDeleted=0`).
- **Índices:** `IX_Status`, `IX_AssetTypeId`, `IX_CurrentHolderEmployeeId`, `IX_DepartmentId`, `IX_WarrantyEndDate`.
- Validaciones: `Cost >= 0`; `Currency IN ('CRC','USD')`; `SerialNumber` requerido si `AssetType.RequiresSerial`.

**`ItAssetSpecs`** (1:1 con activo) — `AssetId (UQ, FK)`, `OS`, `Processor`, `RamGb`, `DiskGb`, `MacEthernet`, `MacWifi`, `IpAddress`, `DomainName`, `AnyDeskId`, `Microsoft365User`, `AntivirusStatus`, `TechNotes`. (Sin contraseñas.)

**`ItAssignments`** — `Id`, `AssetId(FK)`, `EmployeeId(FK)`, `AssignedTicketId(FK)`, `ReturnTicketId(FK,null)`, `AssignedAt`, `ReturnedAt(null)`, `PhysicalConditionOut`, `PhysicalConditionIn(null)`, `Status(Activa/Cerrada)`, `Accessories`.
- **Único parcial:** `UX_ActiveAssignment` sobre `AssetId` `WHERE Status='Activa' AND IsDeleted=0` → **garantiza una sola asignación activa por activo**.

**`ItTickets`** — `Id`, `TicketNumber (UQ)`, `TicketTypeId(FK)`, `Status(ItTicketStatus)`, `IssuedAt`, `EmployeeId(FK)`, `ItResponsibleUserId(FK)`, `Notes`, `PdfPath`, `PdfSha256`, `CreatedBy`, `VoidedBy(null)`, `VoidReason(null)`, `VoidedAt(null)`.
- **Único:** `TicketNumber`. Índices: `IX_TicketTypeId_Status`, `IX_IssuedAt`, `IX_EmployeeId`.

**`ItTicketDetails`** — `Id`, `TicketId(FK)`, `AssetId(FK,null)`, `AccessoryId(FK,null)`, `LineType`, `Description`, `Quantity`, `Condition`.

**`ItAssetPhotos`** — `Id`, `EntityType`, `EntityId`, `AssetId(FK,null)`, `TicketId(FK,null)`, `StoragePath`, `ThumbnailPath`, `MimeType`, `Width`, `Height`, `SizeBytes`, `Sha256`, `UploadedBy`, `IpAddress`. Máx 3 por entidad (validado en servicio + CHECK por trigger opcional).

**`ItTicketSignatures`** — ver §8.

**`ItAssetHistory`** — `Id`, `AssetId(FK)`, `EventType`, `FromStatus`, `ToStatus`, `TicketId(FK,null)`, `EmployeeId(FK,null)`, `Description`, `OccurredAt`, `PerformedBy`. Índice `IX_AssetId_OccurredAt`.

**`ItDocumentSequences`** — ver §10.

**Catálogos** (`ItAssetTypes`, `ItBrands`, `ItLocations`, `Departments`, `Suppliers`, `ItAccessories`, `ItLicenses`, `ItTicketTypes`): `Id`, `Name`/`Code (UQ)`, `IsActive`, flags de comportamiento (`RequiresSerial`, `IsAssignable`, `HasComputeSpecs`, `Prefix`…). Soft-delete + auditoría.

**`ItMaintenances` / `ItRepairs`** — `AssetId(FK)`, `TicketId(FK,null)`, `StartedAt`, `FinishedAt(null)`, `Provider`, `Cost`, `Currency`, `Description`, `Result`. Permiten KPI "activos con más reparaciones".

**`ItAssetDisposals`** (bajas) — `AssetId(FK,UQ)`, `Reason`, `RequestedBy`, `ApprovedBy(null)`, `ApprovedAt(null)`, `EvidenceTicketId(FK)`, `Status(Solicitada/Aprobada/Rechazada)`. La baja **requiere aprobación** antes de pasar el activo a `DadoDeBaja`.

---

## 10. Consecutivos seguros

Tabla **`ItDocumentSequences`**: `Id`, `TicketTypeCode` (ENT, DEV, PRE, MAN, REP, TRA, CRE, ACC, BAJ), `Year`, `Prefix`, `LastNumber`, `RowVersion`. **Único:** (`TicketTypeCode`, `Year`).

**Algoritmo (dentro de la misma transacción de la boleta):**
```sql
BEGIN TRAN;
  UPDATE ItDocumentSequences WITH (UPDLOCK, ROWLOCK)
     SET LastNumber = LastNumber + 1, @new = LastNumber + 1
   WHERE TicketTypeCode = @code AND [Year] = @year;
  -- si no existe la fila del año, insertarla con LastNumber=1 (MERGE)
  -- formatear: TI-{code}-{year}-{new:000000}
COMMIT;
```
- `UPDLOCK` evita duplicados por concurrencia; el `UPDATE` y la inserción de la boleta van **en la misma transacción** → si la boleta falla, el consecutivo no se "quema".
- Formato: `TI-ENT-2026-000001`, `TI-DEV-2026-000001`, `TI-REP-2026-000001`, `TI-BAJ-2026-000001`.
- Cada generación se audita (`AuditLog`).
- Reinicio por año automático vía la clave (`TicketTypeCode`,`Year`).

> Alternativa .NET: encapsular en `ISequenceGenerator.Next(typeCode)` ejecutado dentro del `IUnitOfWork` con `IsolationLevel.Serializable` o el patrón `UPDLOCK` anterior (preferido por menor contención).

---

## 11. Seguridad y permisos

**Permisos atómicos** (entidad `Permission`, prefijo `ti.`): `ti.inventory.view/create/edit`, `ti.assign`, `ti.return`, `ti.ticket.create/sign/void`, `ti.photo.view`, `ti.pdf.download`, `ti.asset.dispose`, `ti.asset.dispose.approve`, `ti.audit.view`, `ti.report.export`, `ti.catalog.manage`, `ti.sensitive.view`.

### Matriz de permisos

| Acción | SuperAdmin | Admin TI | Técnico TI | Jefatura | Consultor | Auditor |
|---|:--:|:--:|:--:|:--:|:--:|:--:|
| Ver inventario | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Crear / Editar activos | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ |
| Asignar / Devolver | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ |
| Generar boletas | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ |
| Firmar boletas | ✅ | ✅ | ✅ | ✅¹ | ❌ | ❌ |
| Ver fotos / Descargar PDF | ✅ | ✅ | ✅ | ✅ | ✅² | ✅ |
| Anular boletas | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |
| Dar de baja (solicitar) | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ |
| **Aprobar bajas** | ✅ | ✅ | ❌ | ✅ | ❌ | ❌ |
| Ver auditoría | ✅ | ✅ | ❌ | ❌ | ❌ | ✅ |
| Exportar reportes | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ |
| Administrar catálogos | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |
| Ver info sensible (AnyDesk ID…) | ✅ | ✅ | ✅³ | ❌ | ❌ | ❌ |

¹ Jefatura firma como contraparte cuando aplica. ² Consultor sólo de su departamento. ³ Técnico con auditoría de acceso.

**Reglas obligatorias:**
- **Nunca** contraseñas (AnyDesk/equipos) en texto plano. Si se gestionan secretos → **vault** (Azure Key Vault / HashiCorp / DPAPI+cifrado AES) separado del inventario, con permiso `ti.sensitive.view` y **auditoría de cada acceso**.
- Permisos **separados** para inventario / evidencia / firma / auditoría (no un único "admin").
- **Auditar exportaciones** (quién exportó qué y cuándo).
- Fotos, firmas y PDFs **sólo por endpoint autenticado y autorizado** (nunca link público).
- **Menor privilegio** por defecto; roles aditivos.

---

## 12. Auditoría

Se usa la entidad existente **`AuditLog`** con `Module='TI'`, registrando: `EntityName`, `EntityId`, `Action`, `OldValues`/`NewValues` (JSON diff campo a campo), `UserId`, `Timestamp` (BusinessClock), `IpAddress`, `UserAgent`, `Success`. Se agrega `CorrelationId` (header de request) y, para cambios críticos (anulación, baja, cambio de estado a Robado/Perdido), un campo `Reason` obligatorio.

- **Interceptor de EF Core** (`SaveChangesInterceptor`) genera el diff automático de entidades TI → cero código manual por servicio.
- Trazabilidad **end-to-end**: alta del activo → asignaciones/devoluciones → mantenimientos/reparaciones → baja, reconstruible desde `ItAssetHistory` + `AuditLogs`.

---

## 13. Dashboard y reportes TI

**KPIs (cards):** Total activos **reales** · Asignados · Disponibles · En reparación · En mantenimiento · Dados de baja · Costo total del parque TI.

**Gráficos:** activos por **tipo** (barras), por **estado** (dona), por **departamento** y por **ubicación** (barras horizontales), por **responsable** (top N), boletas emitidas por **mes** (líneas), activos con **más reparaciones** (ranking).

**Tabla de alertas:** garantías por vencer (≤30/60/90 días) · licencias por vencer · equipos **sin serie** · **sin etiqueta** · **sin responsable** · boletas **pendientes de firma**.

**UI/UX dashboard:** reutilizar componentes ya existentes (`KpiCard`, `DashboardChart`, recharts), branding Repagro. Filtros avanzados (tipo, estado, depto, ubicación, rango de fechas), búsqueda rápida, indicadores por color (verde disponible / azul asignado / ámbar mantenimiento / rojo dañado-baja), responsive, skeleton loaders.

---

## 14. UI/UX (pantallas)

| Pantalla | Notas clave |
|---|---|
| Dashboard TI | KPIs + gráficos + alertas (sección §13). |
| Listado de activos | Tabla con filtros, búsqueda, chips de estado por color, paginación server-side, export. |
| Detalle de activo | Ficha + specs + historial (timeline) + fotos + boletas relacionadas. |
| Registro/edición de activo | Form con validación inline; serie obligatoria según tipo. |
| Asignación | Wizard: colaborador → activos → accesorios → estado → fotos → firmas → confirmar. |
| Devolución | Wizard espejo: buscar asignación → estado/daños → fotos → firmas → cerrar. |
| Captura de fotos | Acceso a cámara (`<input capture>` / getUserMedia), máx 3, preview, recompresión cliente. |
| Firma en pantalla | `<SignaturePad>` Pointer Events, botones Limpiar/Confirmar, aviso legal. |
| Vista previa de boleta | Render previo + botón Emitir; tras emitir, sólo ver/descargar/anular. |
| Historial del activo | Timeline cronológico (eventos + boletas + cambios de estado). |
| Reportes | Constructor de filtros + export CSV/PDF (auditado). |
| Auditoría | Tabla de `AuditLogs` filtrable (sólo Admin/Auditor). |
| Catálogos | CRUD de tipos/marcas/ubicaciones/deptos/proveedores. |

**Principios:** **mobile-first** (fotos y firma se harán desde celular), validaciones visuales claras, estados por color, **confirmaciones explícitas** para acciones críticas (emitir, anular, dar de baja, marcar robado), mensajes de error accionables, modo táctil cómodo (botones grandes en firma/foto).

---

## 15. Migración desde Excel

**Pipeline ETL (script idempotente + reporte previo):**
1. **Extraer** sólo activos reales (filas con `InternalCode`/serie válida) → descartar las ~19 filas vacías (34 reales).
2. **Normalizar** tipos, marcas (diccionario de equivalencias), responsables (match a `Employees`), ubicaciones (split catálogo/detalle).
3. **Resolver códigos duplicados** → reporte de conflictos para decisión manual.
4. **Separar/Eliminar datos sensibles** (contraseñas) → no se migran; se marca `HadLegacySecret`.
5. **Crear colaboradores** faltantes (o mapear a `Users`/`Employees`).
6. **Crear activos** + specs.
7. **Migrar histórico de boletas** como **documentos legacy** (`ItDocuments`, no editables, tipo `Legacy`).
8. **Migrar comentarios** como observaciones históricas (`ItAssetHistory`/`Notes`).
9. **Validar totales** contra el Excel (conteo por tipo/estado) y emitir **reporte de inconsistencias** firmado **antes** de producción.
10. Ejecutar primero en **entorno de staging**; sólo tras visto bueno, correr en producción.

**Estrategia:** *dry-run* → reporte → corrección → carga real. El **Excel queda como respaldo histórico (read-only)**; tras go-live deja de ser fuente maestra.

---

## 16. Arquitectura técnica recomendada

Integrar en la solución **RepagroSuite** existente (no crear una solución aparte):

```
RepagroSuite.Domain
  Entities/IT/        → ItAsset, ItAssetSpec, ItAssignment, ItTicket, ItTicketDetail,
                        ItAssetPhoto, ItTicketSignature, ItAssetHistory, ItMaintenance,
                        ItRepair, ItAssetDisposal, ItLicense, ItAccessory, ItDocumentSequence + catálogos
  Enums/IT/           → ItAssetStatus, ItTicketStatus, ItTicketType, PhysicalCondition
  Interfaces/         → IDocumentStorage, ISequenceGenerator, IPdfGenerator

RepagroSuite.Application
  Features/ITAssets/  → {DTOs, Services, Validators, Mappings}  (patrón vertical existente)
  Features/ITTickets/
  Features/ITDashboard/

RepagroSuite.Infrastructure
  Services/           → DocumentStorageService (S3/Blob/Disk), PdfGeneratorService (QuestPDF),
                        SequenceGenerator, ImageProcessingService
  Data/               → EF config + migraciones del módulo TI + AuditSaveChangesInterceptor

RepagroSuite.API
  Controllers/v1/     → ItAssetsController, ItAssignmentsController, ItTicketsController,
                        ItDashboardController, ItCatalogsController
  BackgroundServices/ → ItAlertsWorker (garantías/licencias/mantenimientos por vencer)
```

- **Frontend:** React 19 + Tailwind v4 (igual que hoy), nueva sección de rutas `/ti/*`.
- **PDF:** **QuestPDF** (.NET, licencia friendly, plantillas en C#) → genera boleta inmutable + hash.
- **Storage documental:** abstracción `IDocumentStorage` (disco hoy, S3/Blob mañana).
- **Jobs:** `BackgroundService` (como el de auto-aprobación de reservas ya existente) para alertas.
- **Integraciones futuras:** Active Directory / M365 (sincronizar usuarios y equipos en dominio), antivirus (estado Kaspersky), compras (orden→activo). Diseñar con puertos/adaptadores para no acoplar.

---

## 17. Endpoints REST sugeridos (`/api/v1/ti/...`)

```
# Activos
GET    /ti/assets                 (filtros, paginación)
POST   /ti/assets
GET    /ti/assets/{id}
PUT    /ti/assets/{id}
POST   /ti/assets/{id}/photos
GET    /ti/assets/{id}/history
POST   /ti/assets/{id}/status        (cambio de estado validado)

# Asignaciones / Devoluciones
POST   /ti/assignments               (crea entrega + boleta)
POST   /ti/returns                   (devolución + boleta)
GET    /ti/assignments/active

# Boletas
GET    /ti/tickets
POST   /ti/tickets
GET    /ti/tickets/{id}
POST   /ti/tickets/{id}/signatures
GET    /ti/tickets/{id}/pdf          (stream autenticado)
POST   /ti/tickets/{id}/void         (anular con motivo)

# Evidencia
GET    /ti/photos/{id}               (stream autenticado)

# Mantenimiento / Reparación / Bajas / Licencias
POST   /ti/maintenances    GET /ti/maintenances
POST   /ti/repairs         GET /ti/repairs
POST   /ti/disposals       POST /ti/disposals/{id}/approve
GET    /ti/licenses        POST /ti/licenses

# Reportes / Dashboard / Catálogos
GET    /ti/dashboard
GET    /ti/reports/{key}             (export auditado)
GET/POST/PUT /ti/catalogs/{type}
```
Todos protegidos por `[Authorize]` + policy de permiso `ti.*`. PDFs/fotos por stream autenticado.

---

## 18. Reglas críticas de negocio (resumen)

1. **Máx. una asignación activa por activo** (índice único filtrado + validación dominio).
2. Activo **DadoDeBaja** no se asigna ni genera boletas.
3. Activo **EnReparacion/EnMantenimiento** no se entrega.
4. Boleta **Emitida** es **inmutable** (sólo anular).
5. Boleta **anulada** requiere **motivo**.
6. Firma **confirmada** no se edita.
7. Foto de boleta **emitida** no se elimina.
8. **Código interno** no se repite (único filtrado).
9. **Número de serie** no se repite (único filtrado, cuando aplica al tipo).
10. **Sin contraseñas en texto plano** en ninguna tabla del módulo.
11. **Todo cambio crítico auditado** (anulación, baja, robo/pérdida, cambio de estado).
12. **PDF se genera sólo post-commit** de la transacción.
13. **Baja requiere aprobación** (`dispose` ≠ `dispose.approve`).
14. **Robado/Perdido** exige evidencia + autorización.

---

## 19. Checklist QA

- [ ] **CRUD** activos/catálogos (validaciones de campos requeridos, únicos, longitudes).
- [ ] **Asignación**: feliz, con accesorios, multi-activo, colaborador inactivo (debe fallar), activo no disponible (debe fallar).
- [ ] **Devolución**: sin daños, con daños, faltantes, accesorios no devueltos, cierre de asignación correcto.
- [ ] **Consecutivos**: secuencia sin saltos, reinicio por año, formato correcto por tipo.
- [ ] **Concurrencia**: 2 asignaciones simultáneas del mismo activo (sólo una gana); generación simultánea de consecutivos (sin duplicados) — *test de carga*.
- [ ] **Fotos**: ≤3, rechazo de MIME inválido, límite de tamaño, miniatura, hash correcto, recompresión.
- [ ] **Firma en celular/tablet** (iOS Safari, Android Chrome): trazo fluido, limpiar, confirmar, metadata guardada.
- [ ] **PDF**: contenido correcto, hash estable, no se genera si la transacción falla, descarga autenticada.
- [ ] **Permisos**: cada rol sólo accede a lo permitido (matriz §11); endpoints rechazan sin permiso (403).
- [ ] **Auditoría**: diff campo a campo, IP/UserAgent/usuario/correlationId; cambios críticos con motivo.
- [ ] **Reportes/Dashboard**: totales = activos reales (no filas vacías), filtros, export auditado.
- [ ] **Responsive**: 360px → desktop; wizards usables en móvil.
- [ ] **Migración**: conteos vs Excel, duplicados resueltos, secretos excluidos, dry-run vs real idempotente.
- [ ] **Seguridad**: sin secretos en BD/logs, fotos/PDF no accesibles sin auth, inyección/IDOR en `{id}`.
- [ ] **Anulaciones**: boleta emitida no editable; anulación exige motivo; reversa de estado correcta.
- [ ] **Backups/restore**: respaldo de BD + storage documental; prueba de restauración consistente (hashes coinciden).

---

## 20. Entregable final / Recomendación

### Fases de desarrollo sugeridas
1. **Fase 0 — Fundaciones (1–2 sem):** entidades + migraciones EF, catálogos, `IDocumentStorage`, `ISequenceGenerator`, interceptor de auditoría, permisos `ti.*`.
2. **Fase 1 — Inventario (2 sem):** CRUD activos + specs, listado/detalle/historial, dashboard básico.
3. **Fase 2 — Boletas + Asignación/Devolución (2–3 sem):** consecutivos, transacciones, máquina de estados, PDF (QuestPDF).
4. **Fase 3 — Evidencia (1–2 sem):** fotos (subida/compresión/thumbnail/hash) + firma canvas + aviso legal.
5. **Fase 4 — Mantenimiento/Reparación/Bajas/Licencias (1–2 sem)** + jobs de alertas.
6. **Fase 5 — Reportes, export auditado, dashboard completo (1 sem).**
7. **Fase 6 — Migración Excel (dry-run → reporte → real) + QA + hardening de seguridad.**

### Riesgos técnicos y mitigación
- **Calidad de datos del Excel** → ETL con dry-run y reporte de inconsistencias antes de producción.
- **Concurrencia en asignaciones/consecutivos** → índices únicos filtrados + `UPDLOCK` + `RowVersion`.
- **Almacenamiento de evidencia** → abstracción `IDocumentStorage`; empezar simple, migrar a Blob/S3 sin tocar dominio.
- **Hosting actual (Site4Now)** → validar soporte de FILESTREAM/almacenamiento; si no, disco + backup o bucket externo.
- **Expectativa de "firma legal"** → dejar claro que es firma electrónica de evidencia; ruta de upgrade a certificado digital.
- **Secretos heredados** → política firme: no migrar contraseñas; vault si se requiere.

### Recomendación final
Construir el módulo **dentro de la solución RepagroSuite existente**, reutilizando `BaseEntity`, `AuditLog`, `BusinessClock`, permisos y patrón de features. Priorizar **integridad y trazabilidad** desde la Fase 0 (índices únicos, máquina de estados en dominio, auditoría por interceptor, consecutivos transaccionales). Migrar el Excel **sólo después** de validar datos en staging, dejando el `.xlsm` como respaldo read-only. Con esto se elimina el conteo erróneo (34 reales vs 53 filas), se normalizan datos, se eliminan secretos en claro y se obtiene un módulo **auditable, escalable y listo para producción** sin perder trazabilidad ni integridad.

---

*Documento de propuesta — listo para revisión funcional y aprobación de alcance por fases.*
