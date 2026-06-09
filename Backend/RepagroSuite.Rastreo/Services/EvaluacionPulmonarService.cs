using Microsoft.EntityFrameworkCore;
using Rastreo.Api.Data;
using Rastreo.Api.Models;

namespace Rastreo.Api.Services;

/// <summary>
/// Calcula metricas clinicas Madec ponderadas a partir de registros finalizados.
/// Pesos anatomicos estandar para pulmon porcino (Madec/Christensen) — suma 100.
/// </summary>
public class EvaluacionPulmonarService
{
    private readonly RastreoDbContext _db;
    public EvaluacionPulmonarService(RastreoDbContext db) => _db = db;

    // Pesos anatomicos Madec (% de area pulmonar) — suma 100
    public const double W_AL = 5.0;   // Apical izquierdo
    public const double W_CL = 6.0;   // Cardiaco izquierdo
    public const double W_DL = 32.0;  // Diafragmatico izquierdo
    public const double W_AD = 11.0;  // Apical derecho
    public const double W_CD = 10.0;  // Cardiaco derecho
    public const double W_DD = 25.0;  // Diafragmatico derecho
    public const double W_AC = 11.0;  // Accesorio

    public record LobuloInfo(string Codigo, string Nombre, double Peso);
    public static readonly LobuloInfo[] Lobulos = new[]
    {
        new LobuloInfo("AL", "Apical Izq",        W_AL),
        new LobuloInfo("CL", "Cardiaco Izq",      W_CL),
        new LobuloInfo("DL", "Diafragmatico Izq", W_DL),
        new LobuloInfo("AD", "Apical Der",        W_AD),
        new LobuloInfo("CD", "Cardiaco Der",      W_CD),
        new LobuloInfo("DD", "Diafragmatico Der", W_DD),
        new LobuloInfo("AC", "Accesorio",         W_AC),
    };

    /// <summary>% area pulmonar afectada (consolidacion) para UN animal (0-100).</summary>
    public static double ConsolidacionAnimal(RegistroDetalle d)
    {
        // grado/4 = fraccion afectada del lobulo; * peso% del lobulo
        double sum =
            d.ApicalIzquierdo        * W_AL +
            d.CardiacoIzquierdo      * W_CL +
            d.DiafragmaticoIzquierdo * W_DL +
            d.ApicalDerecho          * W_AD +
            d.CardiacoDerecho        * W_CD +
            d.DiafragmaticoDerecho   * W_DD +
            d.Accesorio              * W_AC;
        // sum esta en escala 0-400 (grado max 4 * 100% peso). Divido por 4 para volver a 0-100.
        return sum / 4.0;
    }

    /// <summary>Suma de scores (sin ponderar) — util para clasificar severidad.</summary>
    public static int SumaScores(RegistroDetalle d) =>
        d.ApicalIzquierdo + d.CardiacoIzquierdo + d.DiafragmaticoIzquierdo
        + d.ApicalDerecho + d.CardiacoDerecho + d.DiafragmaticoDerecho + d.Accesorio;

    /// <summary>Clasifica un animal por su consolidacion %: NORMAL/LEVE/MODERADO/SEVERO.</summary>
    public static string SeveridadAnimal(double consolidacionPct)
    {
        if (consolidacionPct < 1) return "NORMAL";
        if (consolidacionPct < 10) return "LEVE";
        if (consolidacionPct < 30) return "MODERADO";
        return "SEVERO";
    }

    /// <summary>Calcula el dashboard completo para una granja + periodo.</summary>
    public async Task<EvaluacionResultado> CalcularAsync(int granjaId, DateTime desde, DateTime hasta, CancellationToken ct = default)
    {
        // Solo registros finalizados dentro del rango
        var registros = await _db.Registros
            .AsNoTracking()
            .Include(r => r.Vacunas)
                .ThenInclude(rv => rv.Vacuna)
            .Include(r => r.Detalles)
                .ThenInclude(d => d.EnfermedadesValores)
            .Where(r => r.GranjaId == granjaId
                     && r.Estado == "FINALIZADO"
                     && r.FechaCreacion >= desde
                     && r.FechaCreacion <= hasta)
            .ToListAsync(ct);

        var detalles = registros.SelectMany(r => r.Detalles).ToList();
        var granja = await _db.Granjas.AsNoTracking().FirstOrDefaultAsync(g => g.Id == granjaId, ct);
        var enfermedades = await EnfermedadesReporte(ct);

        var resultado = new EvaluacionResultado
        {
            GranjaId = granjaId,
            GranjaCodigo = granja?.Codigo ?? "",
            GranjaNombre = granja?.Nombre ?? "",
            PeriodoDesde = desde,
            PeriodoHasta = hasta,
            TotalEvaluados = detalles.Count,
            TotalRegistros = registros.Count,
        };

        if (detalles.Count == 0)
        {
            resultado.SinDatos = true;
            return resultado;
        }

        // ===== Metricas por animal (compartidas con el consolidado) =====
        PoblarMetricasBase(resultado, detalles, enfermedades);

        // ===== Detalle por lote =====
        resultado.PorLote = registros
            .GroupBy(r => string.IsNullOrEmpty(r.Lote) ? "Sin lote" : r.Lote!)
            .Select(g =>
            {
                var dt = g.SelectMany(r => r.Detalles).ToList();
                var cons = dt.Select(ConsolidacionAnimal).ToList();
                var prev = dt.Count == 0 ? 0 : 100.0 * dt.Count(x => SumaScores(x) > 0) / dt.Count;
                // Vacunas aplicadas en el lote (nombres distintos) y dosis totales (animales x vacunas por registro).
                var vacunasLote = g
                    .SelectMany(r => r.Vacunas.Where(rv => rv.Vacuna != null).Select(rv => rv.Vacuna!.Nombre))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x)
                    .ToList();
                var dosisLote = g.Sum(r => r.Detalles.Count * r.Vacunas.Count(rv => rv.Vacuna != null));
                return new LoteInfo
                {
                    Lote = g.Key,
                    Animales = dt.Count,
                    ConsolidacionPct = Math.Round(cons.DefaultIfEmpty(0).Average(), 2),
                    PrevalenciaPct = Math.Round(prev, 2),
                    SpesPositivos = dt.Count(x => x.SPES > 0),
                    PericarditisPositivos = dt.Count(x => x.Pericarditis),
                    Hallazgos = enfermedades.Select(e => new HallazgoConteo
                    {
                        Clave = e.Codigo,
                        Etiqueta = e.Nombre,
                        Positivos = dt.Count(d => EnfermedadValorHelper.Positivo(d, e))
                    }).ToList(),
                    Vacunas = vacunasLote,
                    DosisAplicadas = dosisLote,
                };
            })
            .OrderByDescending(x => x.ConsolidacionPct)
            .ToList();

        // ===== Tendencia historica (12 meses anteriores incluyendo el actual) =====
        var hoy = hasta;
        var inicioTendencia = new DateTime(hoy.Year, hoy.Month, 1).AddMonths(-11);
        var historicos = await _db.Registros
            .AsNoTracking()
            .Include(r => r.Vacunas)
                .ThenInclude(rv => rv.Vacuna)
            .Include(r => r.Detalles)
                .ThenInclude(d => d.EnfermedadesValores)
            .Where(r => r.GranjaId == granjaId
                     && r.Estado == "FINALIZADO"
                     && r.FechaCreacion >= inicioTendencia)
            .ToListAsync(ct);

        resultado.Tendencia = Enumerable.Range(0, 12).Select(i =>
        {
            var mes = inicioTendencia.AddMonths(i);
            var siguiente = mes.AddMonths(1);
            var reg = historicos.Where(r => r.FechaCreacion >= mes && r.FechaCreacion < siguiente).ToList();
            var dt = reg.SelectMany(r => r.Detalles).ToList();
            var punto = new TendenciaPunto
            {
                Etiqueta = mes.ToString("MMM yy", new System.Globalization.CultureInfo("es-ES")),
                Animales = dt.Count,
                ConsolidacionPct = dt.Count == 0 ? 0 : Math.Round(dt.Select(ConsolidacionAnimal).Average(), 2),
                PrevalenciaPct = dt.Count == 0 ? 0 : Math.Round(100.0 * dt.Count(x => SumaScores(x) > 0) / dt.Count, 2),
                Idn = dt.Count(x => SumaScores(x) > 0) == 0 ? 0
                    : Math.Round(dt.Where(x => SumaScores(x) > 0).Select(ConsolidacionAnimal).Average() / 100.0 * 4.0, 2),
                // Prevalencia mensual de cada hallazgo (para la tendencia de enfermedades)
                PleuritisSpesPct       = Pct(dt.Count(x => x.SPES > 0),            dt.Count),
                PericarditisPct        = Pct(dt.Count(x => x.Pericarditis),        dt.Count),
                PericardioEngrosadoPct = Pct(dt.Count(x => x.PericardioEngrosado), dt.Count),
                AbscesoNoduloPct       = Pct(dt.Count(x => x.AbscesoNodulo),       dt.Count),
                MocoPct                = Pct(dt.Count(x => x.Moco),                dt.Count),
            };
            PoblarHallazgosTendencia(punto, dt, enfermedades);
            return punto;
        }).ToList();
        resultado.Vacunas = CalcularVacunas(registros, detalles);
        resultado.TendenciaVacunas = CalcularTendenciaVacunas(historicos, inicioTendencia);
        resultado.AnalisisVacunacion = await CalcularAnalisisVacunacionAsync(granjaId, enfermedades, ct);

        // ===== Tendencia por hallazgo: pendiente (regresion lineal) y direccion clinica exactas =====
        resultado.TendenciaHallazgos = CalcularTendenciaHallazgos(resultado.Tendencia, resultado.Hallazgos);

        // ===== Comparacion con periodo anterior =====
        var diasRango = (hasta - desde).TotalDays + 1;
        var anteriorDesde = desde.AddDays(-diasRango);
        var anteriorHasta = desde.AddSeconds(-1);
        var dtAnt = (await _db.Registros.AsNoTracking()
            .Include(r => r.Detalles)
                .ThenInclude(d => d.EnfermedadesValores)
            .Where(r => r.GranjaId == granjaId && r.Estado == "FINALIZADO"
                     && r.FechaCreacion >= anteriorDesde && r.FechaCreacion <= anteriorHasta)
            .ToListAsync(ct))
            .SelectMany(r => r.Detalles).ToList();
        if (dtAnt.Count > 0)
        {
            resultado.Comparativo = new ComparativoAnterior
            {
                Etiqueta = $"{anteriorDesde:dd MMM} - {anteriorHasta:dd MMM yyyy}",
                Animales = dtAnt.Count,
                ConsolidacionPctAnterior = Math.Round(dtAnt.Select(ConsolidacionAnimal).Average(), 2),
                PrevalenciaPctAnterior = Math.Round(100.0 * dtAnt.Count(x => SumaScores(x) > 0) / dtAnt.Count, 2),
                IdnAnterior = dtAnt.Count(x => SumaScores(x) > 0) == 0 ? 0
                    : Math.Round(dtAnt.Where(x => SumaScores(x) > 0).Select(ConsolidacionAnimal).Average() / 100.0 * 4.0, 2),
            };
            resultado.Comparativo.DeltaConsolidacion = Math.Round(resultado.ConsolidacionPct - resultado.Comparativo.ConsolidacionPctAnterior, 2);
            resultado.Comparativo.DeltaPrevalencia   = Math.Round(resultado.PrevalenciaPct  - resultado.Comparativo.PrevalenciaPctAnterior,  2);
            resultado.Comparativo.DeltaIdn           = Math.Round(resultado.Idn             - resultado.Comparativo.IdnAnterior,             2);
        }

        // ===== Comparativo entre granjas (mismo periodo, top 6 por consolidacion) =====
        var otrasGranjas = await _db.Registros.AsNoTracking().Include(r => r.Detalles).Include(r => r.Granja)
            .Where(r => r.GranjaId != null && r.Estado == "FINALIZADO"
                     && r.FechaCreacion >= desde && r.FechaCreacion <= hasta)
            .ToListAsync(ct);
        resultado.ComparativoGranjas = otrasGranjas
            .GroupBy(r => new { r.GranjaId, r.Granja!.Codigo, r.Granja!.Nombre })
            .Select(g => ResumenGranja(g.Key.GranjaId!.Value, g.Key.Codigo, g.Key.Nombre,
                                       g.SelectMany(r => r.Detalles).ToList(), g.Key.GranjaId == granjaId))
            .OrderByDescending(x => x.ConsolidacionPct)
            .Take(8)
            .ToList();

        // ===== Conclusiones y recomendaciones tecnicas (dinamicas segun severidad) =====
        resultado.Conclusiones = GenerarConclusiones(resultado);
        resultado.Recomendaciones = GenerarRecomendaciones(resultado);

        // ===== Interpretaciones por grafico (narrativa ejecutiva dinamica) =====
        resultado.Interpretaciones = GenerarInterpretaciones(resultado);

        return resultado;
    }

    private static double Pct(int parte, int total) => total == 0 ? 0 : Math.Round(100.0 * parte / total, 2);

    /// <summary>Pobla las metricas que dependen solo del conjunto de detalles (sirve por granja y consolidado).</summary>
    private static void PoblarMetricasBase(EvaluacionResultado resultado, List<RegistroDetalle> detalles, List<Enfermedad> enfermedades)
    {
        if (detalles.Count == 0) return;

        var consolidaciones = detalles.Select(ConsolidacionAnimal).ToList();
        resultado.ConsolidacionPct = Math.Round(consolidaciones.Average(), 2);

        var conLesion = detalles.Where(d => SumaScores(d) > 0).ToList();
        resultado.PrevalenciaPct = Math.Round(100.0 * conLesion.Count / detalles.Count, 2);
        resultado.AnimalesConLesion = conLesion.Count;

        resultado.Idn = conLesion.Count > 0
            ? Math.Round(conLesion.Select(ConsolidacionAnimal).Average() / 100.0 * 4.0, 2)
            : 0;

        var sev = consolidaciones.Select(SeveridadAnimal).ToList();
        resultado.SeveridadDistribucion = new SeveridadBuckets
        {
            Normal   = sev.Count(s => s == "NORMAL"),
            Leve     = sev.Count(s => s == "LEVE"),
            Moderado = sev.Count(s => s == "MODERADO"),
            Severo   = sev.Count(s => s == "SEVERO"),
        };
        resultado.SeveridadDistribucion.NormalPct   = Pct(resultado.SeveridadDistribucion.Normal,   detalles.Count);
        resultado.SeveridadDistribucion.LevePct     = Pct(resultado.SeveridadDistribucion.Leve,     detalles.Count);
        resultado.SeveridadDistribucion.ModeradoPct = Pct(resultado.SeveridadDistribucion.Moderado, detalles.Count);
        resultado.SeveridadDistribucion.SeveroPct   = Pct(resultado.SeveridadDistribucion.Severo,   detalles.Count);

        resultado.NivelRiesgo = ClasificarRiesgo(resultado.ConsolidacionPct, resultado.PrevalenciaPct,
            resultado.SeveridadDistribucion.SeveroPct);

        resultado.HallazgosPct = new HallazgosSecundarios
        {
            PleuritisSpesPct       = Pct(detalles.Count(d => d.SPES > 0),            detalles.Count),
            AbscesoNoduloPct       = Pct(detalles.Count(d => d.AbscesoNodulo),       detalles.Count),
            PericarditisPct        = Pct(detalles.Count(d => d.Pericarditis),        detalles.Count),
            PericardioEngrosadoPct = Pct(detalles.Count(d => d.PericardioEngrosado), detalles.Count),
            MocoPct                = Pct(detalles.Count(d => d.Moco),                detalles.Count),
        };
        resultado.Hallazgos = enfermedades
            .Where(e => e.TipoCampo != "AGUDA_CRONICA")
            .Select((e, i) => new HallazgoClinico
            {
                Clave = e.Codigo,
                Etiqueta = e.Nombre,
                TipoCampo = e.TipoCampo,
                Color = EnfermedadValorHelper.Color(e.Codigo, i),
                Pct = Pct(detalles.Count(d => EnfermedadValorHelper.Positivo(d, e)), detalles.Count)
            })
            .ToList();

        resultado.AgudaCronicaPct = new AgudaCronicaDist
        {
            AgudaPct     = Pct(detalles.Count(d => d.AgudaCronica == "A"),  detalles.Count),
            CronicaPct   = Pct(detalles.Count(d => d.AgudaCronica == "C"),  detalles.Count),
            AmbasPct     = Pct(detalles.Count(d => d.AgudaCronica == "AC"), detalles.Count),
            SinClasifPct = Pct(detalles.Count(d => string.IsNullOrEmpty(d.AgudaCronica)), detalles.Count),
        };

        resultado.GradosPorLobulo = Lobulos.Select(l =>
        {
            byte[] valores = l.Codigo switch
            {
                "AL" => detalles.Select(d => d.ApicalIzquierdo).ToArray(),
                "CL" => detalles.Select(d => d.CardiacoIzquierdo).ToArray(),
                "DL" => detalles.Select(d => d.DiafragmaticoIzquierdo).ToArray(),
                "AD" => detalles.Select(d => d.ApicalDerecho).ToArray(),
                "CD" => detalles.Select(d => d.CardiacoDerecho).ToArray(),
                "DD" => detalles.Select(d => d.DiafragmaticoDerecho).ToArray(),
                "AC" => detalles.Select(d => d.Accesorio).ToArray(),
                _    => Array.Empty<byte>()
            };
            return new GradoLobulo
            {
                Codigo = l.Codigo, Nombre = l.Nombre, PesoAnatomico = l.Peso,
                G0Pct = Pct(valores.Count(v => v == 0), valores.Length),
                G1Pct = Pct(valores.Count(v => v == 1), valores.Length),
                G2Pct = Pct(valores.Count(v => v == 2), valores.Length),
                G3Pct = Pct(valores.Count(v => v == 3), valores.Length),
                G4Pct = Pct(valores.Count(v => v == 4), valores.Length),
                OportunidadPct = Pct(valores.Count(v => v > 0), valores.Length),
            };
        }).ToList();
    }

    /// <summary>Resumen por granja (KPIs + nivel de riesgo) para ranking/consolidado.</summary>
    private static GranjaCompare ResumenGranja(int id, string codigo, string nombre, List<RegistroDetalle> dt, bool esActual)
    {
        if (dt.Count == 0)
            return new GranjaCompare { GranjaId = id, Codigo = codigo, Nombre = nombre, EsActual = esActual };
        var cons = dt.Select(ConsolidacionAnimal).ToList();
        var consPct = Math.Round(cons.Average(), 2);
        var prev = Math.Round(100.0 * dt.Count(x => SumaScores(x) > 0) / dt.Count, 2);
        var afect = dt.Where(x => SumaScores(x) > 0).Select(ConsolidacionAnimal).ToList();
        var idn = afect.Count == 0 ? 0 : Math.Round(afect.Average() / 100.0 * 4.0, 2);
        var severoPct = Math.Round(100.0 * cons.Count(c => c >= 30) / dt.Count, 2);
        return new GranjaCompare
        {
            GranjaId = id, Codigo = codigo, Nombre = nombre, Animales = dt.Count,
            ConsolidacionPct = consPct, PrevalenciaPct = prev, Idn = idn,
            NivelRiesgo = ClasificarRiesgo(consPct, prev, severoPct), EsActual = esActual,
        };
    }

    /// <summary>Dashboard consolidado de TODAS las granjas en el periodo (vista ejecutiva por defecto).</summary>
    public async Task<EvaluacionResultado> CalcularConsolidadoAsync(DateTime desde, DateTime hasta, CancellationToken ct = default)
    {
        var registros = await _db.Registros.AsNoTracking()
            .Include(r => r.Vacunas)
                .ThenInclude(rv => rv.Vacuna)
            .Include(r => r.Detalles)
                .ThenInclude(d => d.EnfermedadesValores)
            .Include(r => r.Granja)
            .Where(r => r.Estado == "FINALIZADO" && r.GranjaId != null
                     && r.FechaCreacion >= desde && r.FechaCreacion <= hasta)
            .ToListAsync(ct);
        var detalles = registros.SelectMany(r => r.Detalles).ToList();
        var enfermedades = await EnfermedadesReporte(ct);

        var resultado = new EvaluacionResultado
        {
            GranjaId = 0,
            GranjaCodigo = "TODAS",
            GranjaNombre = "Todas las granjas",
            PeriodoDesde = desde,
            PeriodoHasta = hasta,
            TotalEvaluados = detalles.Count,
            TotalRegistros = registros.Count,
        };
        if (detalles.Count == 0) { resultado.SinDatos = true; return resultado; }

        PoblarMetricasBase(resultado, detalles, enfermedades);

        // Ranking por granja (con nivel de riesgo) — alimenta el ranking y la distribucion de riesgo.
        resultado.ComparativoGranjas = registros
            .GroupBy(r => new { r.GranjaId, Codigo = r.Granja!.Codigo, Nombre = r.Granja!.Nombre })
            .Select(g => ResumenGranja(g.Key.GranjaId!.Value, g.Key.Codigo, g.Key.Nombre,
                                       g.SelectMany(r => r.Detalles).ToList(), false))
            .OrderByDescending(x => x.ConsolidacionPct)
            .ToList();

        // Tendencia consolidada (12 meses, todas las granjas)
        var inicioTendencia = new DateTime(hasta.Year, hasta.Month, 1).AddMonths(-11);
        var historicos = await _db.Registros.AsNoTracking()
            .Include(r => r.Vacunas)
                .ThenInclude(rv => rv.Vacuna)
            .Include(r => r.Detalles)
                .ThenInclude(d => d.EnfermedadesValores)
            .Where(r => r.Estado == "FINALIZADO" && r.GranjaId != null && r.FechaCreacion >= inicioTendencia)
            .ToListAsync(ct);
        resultado.Tendencia = Enumerable.Range(0, 12).Select(i =>
        {
            var mes = inicioTendencia.AddMonths(i);
            var sig = mes.AddMonths(1);
            var dt = historicos.Where(r => r.FechaCreacion >= mes && r.FechaCreacion < sig).SelectMany(r => r.Detalles).ToList();
            var punto = new TendenciaPunto
            {
                Etiqueta = mes.ToString("MMM yy", new System.Globalization.CultureInfo("es-ES")),
                Animales = dt.Count,
                ConsolidacionPct = dt.Count == 0 ? 0 : Math.Round(dt.Select(ConsolidacionAnimal).Average(), 2),
                PrevalenciaPct = dt.Count == 0 ? 0 : Math.Round(100.0 * dt.Count(x => SumaScores(x) > 0) / dt.Count, 2),
                Idn = dt.Count(x => SumaScores(x) > 0) == 0 ? 0
                    : Math.Round(dt.Where(x => SumaScores(x) > 0).Select(ConsolidacionAnimal).Average() / 100.0 * 4.0, 2),
                PleuritisSpesPct       = Pct(dt.Count(x => x.SPES > 0),            dt.Count),
                PericarditisPct        = Pct(dt.Count(x => x.Pericarditis),        dt.Count),
                PericardioEngrosadoPct = Pct(dt.Count(x => x.PericardioEngrosado), dt.Count),
                AbscesoNoduloPct       = Pct(dt.Count(x => x.AbscesoNodulo),       dt.Count),
                MocoPct                = Pct(dt.Count(x => x.Moco),                dt.Count),
            };
            PoblarHallazgosTendencia(punto, dt, enfermedades);
            return punto;
        }).ToList();
        resultado.Vacunas = CalcularVacunas(registros, detalles);
        resultado.TendenciaVacunas = CalcularTendenciaVacunas(historicos, inicioTendencia);
        resultado.AnalisisVacunacion = await CalcularAnalisisVacunacionAsync(null, enfermedades, ct);
        resultado.TendenciaHallazgos = CalcularTendenciaHallazgos(resultado.Tendencia, resultado.Hallazgos);

        resultado.Conclusiones = GenerarConclusiones(resultado);
        resultado.Recomendaciones = GenerarRecomendaciones(resultado);
        resultado.Interpretaciones = GenerarInterpretaciones(resultado);
        return resultado;
    }

    public static string ClasificarRiesgo(double consolidacionPct, double prevalenciaPct, double severoPct)
    {
        if (consolidacionPct >= 20 || severoPct >= 15) return "CRITICO";
        if (consolidacionPct >= 10 || prevalenciaPct >= 60 || severoPct >= 5) return "ALTO";
        if (consolidacionPct >= 5  || prevalenciaPct >= 30) return "MEDIO";
        return "BAJO";
    }

    private async Task<List<Enfermedad>> EnfermedadesReporte(CancellationToken ct) =>
        await _db.Enfermedades.AsNoTracking()
            .Where(e => e.Activo && e.TipoCampo != "AGUDA_CRONICA")
            .OrderBy(e => e.Orden).ThenBy(e => e.Nombre)
            .ToListAsync(ct);

    private static void PoblarHallazgosTendencia(
        TendenciaPunto punto,
        List<RegistroDetalle> detalles,
        List<Enfermedad> enfermedades)
    {
        punto.HallazgosPct = enfermedades.ToDictionary(
            e => e.Codigo,
            e => Pct(detalles.Count(d => EnfermedadValorHelper.Positivo(d, e)), detalles.Count));
    }

    private static List<VacunaUso> CalcularVacunas(List<Registro> registros, List<RegistroDetalle> detalles)
    {
        if (detalles.Count == 0) return new();
        return registros
            .SelectMany(r => r.Vacunas.Where(rv => rv.Vacuna != null).Select(rv => new { Registro = r, Vacuna = rv.Vacuna! }))
            .GroupBy(x => new { x.Vacuna.Id, x.Vacuna.Codigo, x.Vacuna.Nombre, x.Vacuna.Laboratorio, x.Vacuna.Orden })
            .Select(g =>
            {
                var regs = g.Select(x => x.Registro).DistinctBy(r => r.Id).ToList();
                var dt = regs.SelectMany(r => r.Detalles).ToList();
                return new VacunaUso
                {
                    Id = g.Key.Id,
                    Codigo = g.Key.Codigo,
                    Nombre = g.Key.Nombre,
                    Laboratorio = g.Key.Laboratorio,
                    Orden = g.Key.Orden,
                    Registros = regs.Count,
                    Animales = dt.Count,
                    CoberturaPct = Pct(dt.Count, detalles.Count),
                    ConsolidacionPct = dt.Count == 0 ? 0 : Math.Round(dt.Select(ConsolidacionAnimal).Average(), 2),
                    PrevalenciaPct = dt.Count == 0 ? 0 : Math.Round(100.0 * dt.Count(x => SumaScores(x) > 0) / dt.Count, 2),
                };
            })
            .OrderByDescending(v => v.Animales)
            .ThenBy(v => v.Orden)
            .ToList();
    }

    private static List<VacunaTendenciaPunto> CalcularTendenciaVacunas(List<Registro> historicos, DateTime inicio)
    {
        return Enumerable.Range(0, 12).Select(i =>
        {
            var mes = inicio.AddMonths(i);
            var sig = mes.AddMonths(1);
            var regs = historicos.Where(r => r.FechaCreacion >= mes && r.FechaCreacion < sig).ToList();
            var vacunados = regs.Where(r => r.UsaVacunas && r.Vacunas.Count > 0).ToList();
            var sinVacuna = regs.Where(r => !r.UsaVacunas || r.Vacunas.Count == 0).ToList();
            var dtVac = vacunados.SelectMany(r => r.Detalles).ToList();
            var dtSin = sinVacuna.SelectMany(r => r.Detalles).ToList();
            var total = dtVac.Count + dtSin.Count;
            // Dosis = por cada registro vacunado, cada animal recibe cada vacuna distinta del registro.
            var dosis = vacunados.Sum(r => r.Detalles.Count * r.Vacunas.Count(rv => rv.Vacuna != null));
            var vacunasDistintas = vacunados
                .SelectMany(r => r.Vacunas.Where(rv => rv.Vacuna != null).Select(rv => rv.VacunaId))
                .Distinct().Count();
            var consVac = dtVac.Count == 0 ? 0 : Math.Round(dtVac.Select(ConsolidacionAnimal).Average(), 2);
            var consSin = dtSin.Count == 0 ? 0 : Math.Round(dtSin.Select(ConsolidacionAnimal).Average(), 2);
            var prevVac = dtVac.Count == 0 ? 0 : Math.Round(100.0 * dtVac.Count(x => SumaScores(x) > 0) / dtVac.Count, 2);
            var prevSin = dtSin.Count == 0 ? 0 : Math.Round(100.0 * dtSin.Count(x => SumaScores(x) > 0) / dtSin.Count, 2);
            return new VacunaTendenciaPunto
            {
                Etiqueta = mes.ToString("MMM yy", new System.Globalization.CultureInfo("es-ES")),
                Animales = total,
                AnimalesVacunados = dtVac.Count,
                AnimalesSinVacuna = dtSin.Count,
                DosisAplicadas = dosis,
                VacunasDistintas = vacunasDistintas,
                CoberturaVacunadosPct = Pct(dtVac.Count, total),
                ConsolidacionVacunadosPct = consVac,
                ConsolidacionSinVacunaPct = consSin,
                PrevalenciaVacunadosPct = prevVac,
                PrevalenciaSinVacunaPct = prevSin,
                DeltaConsolidacionVacunaVsSinVacuna = dtVac.Count > 0 && dtSin.Count > 0 ? Math.Round(consVac - consSin, 2) : 0,
            };
        }).ToList();
    }

    /// <summary>
    /// Analisis antes/despues de vacunacion alineado POR GRANJA (event-study).
    /// Modelo real: cada granja envia ~1 lote de matanza por mes (cada registro es un lote distinto),
    /// asi que la mejoria se mide por granja a lo largo del tiempo, no comparando un lote consigo mismo.
    /// Para cada granja, el primer registro FINALIZADO con vacuna marca el "mes 0" (inicio); cada registro
    /// de la granja se ubica en un offset de meses relativo a ese inicio. Se agregan consolidacion,
    /// prevalencia y hallazgos por offset (linea temporal con marcador en 0) y ANTES (offset&lt;0) vs
    /// DESPUES (offset&gt;=0). granjaId null = todas las granjas (cada una alineada a su propio inicio).
    /// No se limita por periodo: es longitudinal. LotesAnalizados = numero de granjas con inicio de vacunacion.
    /// </summary>
    private async Task<AnalisisVacunacion> CalcularAnalisisVacunacionAsync(int? granjaId, List<Enfermedad> enfermedades, CancellationToken ct)
    {
        var q = _db.Registros.AsNoTracking()
            .Include(r => r.Vacunas)
            .Include(r => r.Detalles)
                .ThenInclude(d => d.EnfermedadesValores)
            .Where(r => r.Estado == "FINALIZADO" && r.GranjaId != null);
        if (granjaId.HasValue) q = q.Where(r => r.GranjaId == granjaId.Value);
        var regs = await q.ToListAsync(ct);

        var res = new AnalisisVacunacion();
        if (regs.Count == 0) return res;

        static int MesIndex(DateTime d) => d.Year * 12 + (d.Month - 1);

        // Agrupar POR GRANJA: cada granja se ancla a su primer mes con vacuna.
        var grupos = regs.GroupBy(r => r.GranjaId);

        var porOffset = new Dictionary<int, List<RegistroDetalle>>();
        foreach (var g in grupos)
        {
            var vacunados = g.Where(r => r.UsaVacunas && r.Vacunas.Count > 0).ToList();
            if (vacunados.Count == 0) continue; // granja sin vacuna: no hay inicio que anclar
            var inicioIdx = MesIndex(vacunados.Min(r => r.FechaCreacion));
            res.LotesAnalizados++; // = granjas analizadas
            foreach (var r in g)
            {
                var off = MesIndex(r.FechaCreacion) - inicioIdx;
                if (!porOffset.TryGetValue(off, out var lst)) { lst = new(); porOffset[off] = lst; }
                lst.AddRange(r.Detalles);
            }
        }
        if (porOffset.Count == 0) return res;

        // Linea temporal: offsets presentes (clamp -6..+6 para legibilidad).
        res.Puntos = porOffset.Keys.Where(o => o >= -6 && o <= 6).OrderBy(o => o).Select(o =>
        {
            var dt = porOffset[o];
            return new PuntoVacunacionRelativo
            {
                OffsetMes = o,
                Etiqueta = o == 0 ? "Inicio" : (o < 0 ? o.ToString() : $"+{o}"),
                Animales = dt.Count,
                ConsolidacionPct = dt.Count == 0 ? 0 : Math.Round(dt.Select(ConsolidacionAnimal).Average(), 2),
                PrevalenciaPct = dt.Count == 0 ? 0 : Math.Round(100.0 * dt.Count(x => SumaScores(x) > 0) / dt.Count, 2),
                HallazgosPct = enfermedades.ToDictionary(
                    e => e.Codigo,
                    e => Pct(dt.Count(d => EnfermedadValorHelper.Positivo(d, e)), dt.Count)),
            };
        }).ToList();

        var antes = porOffset.Where(kv => kv.Key < 0).SelectMany(kv => kv.Value).ToList();
        var despues = porOffset.Where(kv => kv.Key >= 0).SelectMany(kv => kv.Value).ToList();
        res.AnimalesAntes = antes.Count;
        res.AnimalesDespues = despues.Count;
        res.ConsolidacionAntes   = antes.Count   == 0 ? 0 : Math.Round(antes.Select(ConsolidacionAnimal).Average(), 2);
        res.ConsolidacionDespues = despues.Count == 0 ? 0 : Math.Round(despues.Select(ConsolidacionAnimal).Average(), 2);
        res.PrevalenciaAntes     = antes.Count   == 0 ? 0 : Math.Round(100.0 * antes.Count(x => SumaScores(x) > 0) / antes.Count, 2);
        res.PrevalenciaDespues   = despues.Count == 0 ? 0 : Math.Round(100.0 * despues.Count(x => SumaScores(x) > 0) / despues.Count, 2);
        // Mejoria = reduccion relativa de la lesion (positivo = mejoro tras vacunar).
        res.MejoraConsolidacionPct = res.ConsolidacionAntes > 0 ? Math.Round((res.ConsolidacionAntes - res.ConsolidacionDespues) / res.ConsolidacionAntes * 100.0, 1) : 0;
        res.MejoraPrevalenciaPct   = res.PrevalenciaAntes   > 0 ? Math.Round((res.PrevalenciaAntes   - res.PrevalenciaDespues)   / res.PrevalenciaAntes   * 100.0, 1) : 0;
        res.TieneComparacion = antes.Count > 0 && despues.Count > 0;
        return res;
    }

    private const double UMBRAL_PENDIENTE = 0.30; // pp/mes para considerar que sube o baja (no ruido)

    /// <summary>Pendiente de mejora/empeoramiento por hallazgo via minimos cuadrados sobre los meses con datos.</summary>
    private static List<HallazgoTendencia> CalcularTendenciaHallazgos(List<TendenciaPunto> tendencia, List<HallazgoClinico> hallazgos)
    {
        return hallazgos.Select(def =>
        {
            // Solo meses con animales evaluados; x = indice de mes (0..11) para una pendiente temporalmente correcta.
            var pts = new List<(double x, double y)>();
            for (int i = 0; i < tendencia.Count; i++)
                if (tendencia[i].Animales > 0)
                    pts.Add((i, tendencia[i].HallazgosPct.TryGetValue(def.Clave, out var pct) ? pct : 0));

            var h = new HallazgoTendencia
            {
                Clave = def.Clave,
                Etiqueta = def.Etiqueta,
                Color = def.Color,
                MesesConDatos = pts.Count,
            };

            if (pts.Count == 0) { h.Direccion = "SIN_DATOS"; return h; }

            h.InicialPct = Math.Round(pts.First().y, 2);
            h.ActualPct  = Math.Round(pts.Last().y, 2);
            h.DeltaPp    = Math.Round(h.ActualPct - h.InicialPct, 2);

            if (pts.Count < 2) { h.Direccion = "SIN_DATOS"; return h; }

            h.Pendiente = Math.Round(PendienteMinimosCuadrados(pts), 4); // pp por mes

            // Direccion clinica: en enfermedades, bajar = mejora; subir = empeora.
            h.Direccion = h.Pendiente <= -UMBRAL_PENDIENTE ? "MEJORA"
                        : h.Pendiente >=  UMBRAL_PENDIENTE ? "EMPEORA"
                        : "ESTABLE";
            return h;
        }).ToList();
    }

    private static double PendienteMinimosCuadrados(IReadOnlyList<(double x, double y)> pts)
    {
        int n = pts.Count;
        if (n < 2) return 0;
        double mx = pts.Average(p => p.x), my = pts.Average(p => p.y);
        double num = pts.Sum(p => (p.x - mx) * (p.y - my));
        double den = pts.Sum(p => (p.x - mx) * (p.x - mx));
        return den == 0 ? 0 : num / den;
    }

    private static List<string> GenerarConclusiones(EvaluacionResultado r)
    {
        var c = new List<string>();
        c.Add($"De {r.TotalEvaluados} pulmones evaluados en {r.GranjaNombre}, se registra una prevalencia neumonica del {r.PrevalenciaPct:F2}%, una consolidacion pulmonar promedio del {r.ConsolidacionPct:F2}% (area afectada ponderada por Madec) y un Indice de Neumonia (IDN) de {r.Idn:F2}.");

        var diag = r.Idn switch
        {
            < 0.5 => "leve, por debajo del umbral clinico significativo",
            < 1.5 => "moderada, requiere seguimiento",
            < 2.5 => "moderada-severa, requiere intervencion",
            _     => "severa, requiere intervencion inmediata"
        };
        c.Add($"El indice clasifica la condicion como {diag}.");

        if (r.SeveridadDistribucion.SeveroPct > 5)
            c.Add($"Se observa un {r.SeveridadDistribucion.SeveroPct:F1}% de animales con afectacion severa (>30% area), lo cual representa una bandera de alerta productiva por perdida estimada de ganancia diaria de peso (~32 g/dia por punto sobre 10%).");

        if (r.HallazgosPct.PleuritisSpesPct > 30)
            c.Add($"La pleuritis (SPES) se presenta en {r.HallazgosPct.PleuritisSpesPct:F1}% del grupo, sugerente de Actinobacillus pleuropneumoniae (APP) o Glasserella parasuis.");
        if (r.HallazgosPct.PericarditisPct > 20)
            c.Add($"La pericarditis afecta al {r.HallazgosPct.PericarditisPct:F1}% de los animales, hallazgo compatible con enfermedad de Glasser y/o cuadros sistemicos asociados a Streptococcus suis.");
        if (r.HallazgosPct.AbscesoNoduloPct > 2)
            c.Add($"Se detectan abscesos/nodulos en {r.HallazgosPct.AbscesoNoduloPct:F1}% de los pulmones — lesiones especificas de APP en su forma cronica.");

        if (r.AgudaCronicaPct.AgudaPct > 50)
            c.Add($"El {r.AgudaCronicaPct.AgudaPct:F0}% de las lesiones son agudas, lo que sugiere desafio infeccioso en las ultimas semanas previas al sacrificio (semanas 15-22).");
        else if (r.AgudaCronicaPct.CronicaPct > 30)
            c.Add($"Predominan lesiones cronicas ({r.AgudaCronicaPct.CronicaPct:F0}%), indicando exposicion sostenida durante el ciclo productivo.");

        if (r.Comparativo != null)
        {
            var tendCons = r.Comparativo.DeltaConsolidacion;
            var sign = tendCons > 0 ? "AUMENTO" : "REDUCCION";
            c.Add($"Comparado con el periodo anterior ({r.Comparativo.Etiqueta}), se registra un {sign} de {Math.Abs(tendCons):F2} puntos en consolidacion pulmonar.");
        }

        c.Add($"Nivel de riesgo agregado: {r.NivelRiesgo}.");
        return c;
    }

    private static Interpretaciones GenerarInterpretaciones(EvaluacionResultado r)
    {
        var ip = new Interpretaciones();

        // --- Prevalencia ---
        var rangoPrev = r.PrevalenciaPct switch
        {
            < 30 => "un nivel bajo y bien controlado",
            < 50 => "un nivel moderado, dentro de lo esperable para neumonia enzootica",
            < 70 => "un nivel elevado que sugiere un desafio infeccioso activo",
            _    => "un nivel alto que indica un desafio infeccioso importante en el lote"
        };
        ip.Prevalencia = $"El {r.PrevalenciaPct:F2}% de los pulmones evaluados ({r.AnimalesConLesion} de {r.TotalEvaluados}) presentan algun grado de lesion compatible con neumonia, tipicamente asociada a Mycoplasma hyopneumoniae. Esto representa {rangoPrev}. La prevalencia mide la extension del problema en el lote, no su gravedad.";

        // --- Consolidacion ---
        var rangoCons = r.ConsolidacionPct switch
        {
            < 5  => "Se ubica en rango bajo, indicativo de buen control sanitario y bajo impacto productivo.",
            < 10 => "Es un valor moderado que permanece por debajo del umbral critico del 10%, con impacto productivo limitado.",
            < 20 => "Supera el umbral del 10%, con impacto productivo relevante: se estima una perdida cercana a 32 g/dia de ganancia de peso por animal afectado.",
            _    => "Es un valor critico muy por encima del 10%, con perdida productiva significativa y deterioro del bienestar animal."
        };
        ip.Consolidacion = $"La consolidacion pulmonar promedio del {r.ConsolidacionPct:F2}% representa la fraccion de tejido pulmonar no funcional, ponderada por el area anatomica de cada lobulo (metodo Madec). {rangoCons}";

        // --- IDN ---
        var rangoIdn = r.Idn switch
        {
            < 0.5 => "una intensidad leve, por debajo del umbral clinico significativo",
            < 1.5 => "una intensidad leve a moderada que conviene vigilar",
            < 2.5 => "una intensidad moderada que justifica intervencion sanitaria",
            _     => "una intensidad severa que exige intervencion inmediata"
        };
        ip.Idn = $"El Indice de Neumonia (IDN) de {r.Idn:F2} resume la intensidad de las lesiones unicamente en los pulmones afectados, en una escala de 0 a 4. El valor obtenido refleja {rangoIdn}. A diferencia de la prevalencia (cuantos animales), el IDN mide que tan graves son las lesiones presentes.";

        // --- Severidad ---
        var levesMas = r.SeveridadDistribucion.NormalPct + r.SeveridadDistribucion.LevePct;
        var riesgoSev = r.SeveridadDistribucion.ModeradoPct + r.SeveridadDistribucion.SeveroPct;
        ip.Severidad = $"Cada pulmon se clasifica segun el porcentaje de area afectada: Normal (<1%), Leve (1-10%), Moderado (10-30%) y Severo (>30%). El {levesMas:F1}% del lote se mantiene en condicion normal o leve, mientras que el {riesgoSev:F1}% (moderado + severo) concentra el riesgo productivo y debe priorizarse en el seguimiento clinico. " +
            (r.SeveridadDistribucion.SeveroPct >= 5
                ? $"El {r.SeveridadDistribucion.SeveroPct:F1}% severo es una bandera de alerta que requiere atencion directa."
                : "La baja proporcion de casos severos es un indicador favorable.");

        // --- Aguda / Cronica ---
        var ac = r.AgudaCronicaPct;
        string acTxt;
        if (ac.AgudaPct >= ac.CronicaPct && ac.AgudaPct > 0)
            acTxt = $"El predominio de lesiones agudas ({ac.AgudaPct:F0}%) sugiere que el desafio infeccioso ocurre en las ultimas semanas previas al sacrificio (entre la semana 15 y 22 de vida).";
        else if (ac.CronicaPct > 0)
            acTxt = $"El predominio de lesiones cronicas ({ac.CronicaPct:F0}%) indica una exposicion temprana y sostenida durante el ciclo productivo.";
        else
            acTxt = "No se registra una clasificacion temporal predominante en este periodo.";
        if (ac.AmbasPct >= 15)
            acTxt += $" Ademas, el {ac.AmbasPct:F0}% de lesiones mixtas (agudas y cronicas) revela animales con recaidas que vuelven a enfermar al pasar de sitio 2 a sitio 3.";
        ip.AgudaCronica = "La temporalidad de las lesiones orienta el momento del desafio infeccioso. " + acTxt;

        // --- Hallazgos secundarios ---
        var h = r.HallazgosPct;
        var partes = new List<string>();
        if (h.PleuritisSpesPct > 0) partes.Add($"la pleuritis (SPES) en {h.PleuritisSpesPct:F1}% sugiere Actinobacillus pleuropneumoniae (APP) o Glasserella parasuis");
        if (h.PericarditisPct > 0) partes.Add($"la pericarditis en {h.PericarditisPct:F1}% es compatible con enfermedad de Glasser");
        if (h.AbscesoNoduloPct > 0) partes.Add($"los abscesos/nodulos en {h.AbscesoNoduloPct:F1}% son lesiones especificas de APP en su forma cronica");
        if (h.PericardioEngrosadoPct > 0) partes.Add($"el pericardio engrosado en {h.PericardioEngrosadoPct:F1}% acompana cuadros sistemicos");
        ip.Hallazgos = "Los hallazgos secundarios complementan el diagnostico diferencial mas alla de la neumonia: " +
            (partes.Count > 0 ? string.Join("; ", partes) + "." : "no se registran hallazgos secundarios relevantes en este periodo.");

        // --- Grados por lobulo ---
        var lobMax = r.GradosPorLobulo.OrderByDescending(l => l.OportunidadPct).FirstOrDefault();
        var avgG0 = r.GradosPorLobulo.Count > 0 ? r.GradosPorLobulo.Average(l => l.G0Pct) : 0;
        ip.Grados = $"Cada lobulo se califica de 0 (sano) a 4 (consolidacion total) y se pondera segun su area anatomica (Madec). En promedio, el {avgG0:F1}% de las observaciones estan en grado 0 (sin lesion), lo que equivale a un {100 - avgG0:F1}% de oportunidad de reduccion de lesiones. " +
            (lobMax != null
                ? $"El lobulo {lobMax.Nombre} concentra la mayor oportunidad de mejora, con un {lobMax.OportunidadPct:F1}% de observaciones afectadas."
                : "");

        // --- Tendencia ---
        var conDatos = r.Tendencia.Where(t => t.Animales > 0).ToList();
        if (conDatos.Count >= 2)
        {
            var primero = conDatos.First().ConsolidacionPct;
            var ultimo = conDatos.Last().ConsolidacionPct;
            var delta = ultimo - primero;
            string dir = Math.Abs(delta) < 0.5 ? "estable"
                : delta < 0 ? "a la baja" : "al alza";
            string interp = delta < -0.5
                ? "La reduccion sostenida refleja la efectividad de las medidas sanitarias aplicadas y debe mantenerse."
                : delta > 0.5
                    ? "El incremento exige revisar el plan de control, las medicaciones y las condiciones ambientales."
                    : "La estabilidad sugiere un equilibrio que conviene monitorear para detectar cambios tempranos.";
            ip.Tendencia = $"La evolucion de los ultimos meses muestra una tendencia {dir} en la consolidacion pulmonar (de {primero:F1}% a {ultimo:F1}%). {interp}";
        }
        else
        {
            ip.Tendencia = "Aun no hay suficiente historico para establecer una tendencia confiable. Se recomienda generar evaluaciones periodicas para construir la linea de tiempo.";
        }

        // --- Tendencia de hallazgos (mejora / empeora por enfermedad) ---
        var hConDatos = r.TendenciaHallazgos.Where(x => x.Direccion != "SIN_DATOS").ToList();
        if (hConDatos.Count == 0)
        {
            ip.HallazgosTendencia = "Aun no hay suficiente historico mensual para evaluar como evoluciona cada hallazgo. Genera evaluaciones en meses sucesivos para construir la linea de tendencia por enfermedad.";
        }
        else
        {
            var empeoran = hConDatos.Where(x => x.Direccion == "EMPEORA").OrderByDescending(x => x.DeltaPp).ToList();
            var mejoran  = hConDatos.Where(x => x.Direccion == "MEJORA").OrderBy(x => x.DeltaPp).ToList();
            var partesH = new List<string>();
            if (empeoran.Count > 0)
                partesH.Add("al alza " + string.Join(", ", empeoran.Select(x => $"{x.Etiqueta} ({(x.DeltaPp >= 0 ? "+" : "")}{x.DeltaPp:F1} pp)")));
            if (mejoran.Count > 0)
                partesH.Add("a la baja " + string.Join(", ", mejoran.Select(x => $"{x.Etiqueta} ({x.DeltaPp:F1} pp)")));
            string cuerpo = partesH.Count > 0
                ? "Se observan hallazgos " + string.Join("; y ", partesH) + ". "
                : "Los hallazgos se mantienen estables en el periodo analizado. ";
            string cierre = empeoran.Count > 0
                ? "Los hallazgos al alza deben priorizarse: revisar plan sanitario, medicacion y condiciones ambientales asociadas a esas patologias."
                : "La estabilidad o reduccion sostenida indica un buen control; conviene mantener el plan vigente y seguir monitoreando.";
            ip.HallazgosTendencia = "Cada linea sigue el porcentaje de animales afectados por cada hallazgo a lo largo de los meses con datos; la direccion se calcula por regresion lineal (pendiente en puntos porcentuales por mes). " + cuerpo + cierre;
        }

        // --- Uso de vacunas e impacto sanitario ---
        var mesesVac = r.TendenciaVacunas.Where(t => t.Animales > 0).ToList();
        if (r.Vacunas.Count == 0)
        {
            ip.Vacunas = "No hay vacunas asociadas a los registros de este periodo. Al registrar el uso de vacunas por lote, este indicador comparara cobertura, consolidacion y prevalencia entre animales vacunados y no vacunados.";
        }
        else
        {
            var top = r.Vacunas.OrderByDescending(v => v.Animales).First();
            var conComparacion = mesesVac.Where(t => t.AnimalesVacunados > 0 && t.AnimalesSinVacuna > 0).ToList();
            var promedioDelta = conComparacion.Count == 0 ? 0 : conComparacion.Average(t => t.DeltaConsolidacionVacunaVsSinVacuna);
            var ultimo = mesesVac.LastOrDefault(t => t.AnimalesVacunados > 0);
            var lecturaDelta = conComparacion.Count == 0
                ? "Aun no hay meses con grupos vacunados y no vacunados en paralelo para una comparacion directa."
                : promedioDelta < -0.5
                    ? $"En los meses comparables, los vacunados muestran menor consolidacion promedio ({promedioDelta:F1} puntos vs no vacunados), senal compatible con mejora sanitaria."
                    : promedioDelta > 0.5
                        ? $"En los meses comparables, los vacunados muestran mayor consolidacion promedio ({promedioDelta:F1} puntos), lo que exige revisar momento de aplicacion, cobertura, desafio de campo y sesgo por lotes de mayor riesgo."
                        : "En los meses comparables, la diferencia entre vacunados y no vacunados es pequena; conviene acumular mas ciclos para confirmar tendencia.";
            ip.Vacunas = $"El periodo registra uso de {r.Vacunas.Count} vacuna(s), con mayor cobertura para {top.Nombre} ({top.CoberturaPct:F1}% de animales evaluados). " +
                (ultimo != null ? $"La cobertura vacunada mas reciente es {ultimo.CoberturaVacunadosPct:F1}% del mes. " : "") +
                lecturaDelta;
        }

        // --- Analisis antes/despues de vacunacion (alineado por granja) ---
        var av = r.AnalisisVacunacion;
        if (av.LotesAnalizados == 0)
        {
            ip.AnalisisVacunacion = "Aun no hay granjas con inicio de vacunacion registrado. Cuando una granja registre uso de vacuna, este analisis la alinea a su mes de inicio (mes 0) y compara la consolidacion y prevalencia ANTES vs DESPUES de vacunar (cada mes es un lote de matanza distinto), para aislar la mejoria atribuible a la vacuna.";
        }
        else if (!av.TieneComparacion)
        {
            ip.AnalisisVacunacion = $"Se identificaron {av.LotesAnalizados} granja(s) con inicio de vacunacion, pero todavia no hay evaluaciones suficientes antes y despues del inicio para comparar. Se requieren evaluaciones de la granja en meses previos y posteriores a su primera vacuna.";
        }
        else
        {
            string lecturaCons = av.MejoraConsolidacionPct > 0
                ? $"una reduccion del {av.MejoraConsolidacionPct:F1}% en la consolidacion pulmonar (de {av.ConsolidacionAntes:F2}% a {av.ConsolidacionDespues:F2}%)"
                : av.MejoraConsolidacionPct < 0
                    ? $"un aumento del {Math.Abs(av.MejoraConsolidacionPct):F1}% en la consolidacion (de {av.ConsolidacionAntes:F2}% a {av.ConsolidacionDespues:F2}%)"
                    : "una consolidacion sin cambios";
            string lecturaPrev = av.MejoraPrevalenciaPct > 0
                ? $"la prevalencia se redujo {av.MejoraPrevalenciaPct:F1}% (de {av.PrevalenciaAntes:F2}% a {av.PrevalenciaDespues:F2}%)"
                : av.MejoraPrevalenciaPct < 0
                    ? $"la prevalencia aumento {Math.Abs(av.MejoraPrevalenciaPct):F1}% (de {av.PrevalenciaAntes:F2}% a {av.PrevalenciaDespues:F2}%)"
                    : "la prevalencia se mantuvo";
            string cierre = (av.MejoraConsolidacionPct > 0 || av.MejoraPrevalenciaPct > 0)
                ? "La reduccion de lesiones tras el inicio de la vacunacion es compatible con un efecto protector; conviene sostener el plan y acumular mas ciclos para confirmar la tendencia."
                : "No se observa mejoria tras el inicio: revisar momento de aplicacion, cobertura, conservacion de la vacuna (cadena de frio) y el desafio de campo de la granja.";
            ip.AnalisisVacunacion = $"Alineando {av.LotesAnalizados} granja(s) a su mes de inicio de vacunacion (mes 0 = marcador 'Inicio'), y comparando {av.AnimalesAntes} pulmones evaluados ANTES con {av.AnimalesDespues} DESPUES, se observa {lecturaCons} y {lecturaPrev}. {cierre} Este metodo compara cada granja consigo misma (antes vs despues de empezar a vacunar), donde cada mes aporta un lote de matanza distinto.";
        }

        // --- Comparativo periodo anterior ---
        if (r.Comparativo != null)
        {
            var cc = r.Comparativo;
            string sentidoCons = cc.DeltaConsolidacion < 0 ? "disminuyo" : cc.DeltaConsolidacion > 0 ? "aumento" : "se mantuvo";
            string sentidoPrev = cc.DeltaPrevalencia < 0 ? "disminuyo" : cc.DeltaPrevalencia > 0 ? "aumento" : "se mantuvo";
            string valoracion = cc.DeltaConsolidacion < 0
                ? "Es una mejora respecto al periodo anterior."
                : cc.DeltaConsolidacion > 0
                    ? "Representa un retroceso que requiere atencion."
                    : "El indicador se mantiene sin cambios relevantes.";
            ip.Comparativo = $"Respecto al periodo anterior ({cc.Etiqueta}), la consolidacion {sentidoCons} {Math.Abs(cc.DeltaConsolidacion):F2} puntos y la prevalencia {sentidoPrev} {Math.Abs(cc.DeltaPrevalencia):F2} puntos. {valoracion}";
        }
        else
        {
            ip.Comparativo = "No existe un periodo anterior comparable para esta granja. Una vez generadas mas evaluaciones, este apartado mostrara la evolucion entre periodos.";
        }

        // --- Comparativo granjas ---
        if (r.ComparativoGranjas.Count > 1)
        {
            var ordenadas = r.ComparativoGranjas.OrderByDescending(g => g.ConsolidacionPct).ToList();
            var promGrupo = r.ComparativoGranjas.Average(g => g.ConsolidacionPct);
            var idxActual = ordenadas.FindIndex(g => g.EsActual);
            if (idxActual < 0)
            {
                // Vista consolidada: no hay una granja "actual".
                var enRiesgo = r.ComparativoGranjas.Count(g => g.NivelRiesgo is "ALTO" or "CRITICO");
                var peor = ordenadas.First();
                ip.Granjas = $"El consolidado agrupa {r.ComparativoGranjas.Count} granjas con una consolidacion pulmonar promedio del {promGrupo:F2}%. {enRiesgo} de ellas se ubican en nivel ALTO o CRITICO. La granja con mayor consolidacion es {peor.Nombre} ({peor.ConsolidacionPct:F2}%), que deberia priorizarse en la atencion sanitaria.";
            }
            else
            {
                var actual = ordenadas[idxActual];
                string vs = actual.ConsolidacionPct > promGrupo ? "por encima" : "por debajo";
                ip.Granjas = $"En el comparativo entre {r.ComparativoGranjas.Count} granjas del mismo periodo, esta granja ocupa la posicion {idxActual + 1} en consolidacion pulmonar, ubicandose {vs} del promedio del grupo ({promGrupo:F2}%). Una mayor consolidacion relativa indica un mayor desafio sanitario frente a las demas explotaciones.";
            }
        }
        else
        {
            ip.Granjas = "No hay otras granjas con datos en el mismo periodo para comparar.";
        }

        return ip;
    }

    private static List<string> GenerarRecomendaciones(EvaluacionResultado r)
    {
        var rec = new List<string>();

        // Recomendacion baseline para Mycoplasma siempre presente
        rec.Add("Mantener el programa de control de Mycoplasma mediante macrolidos (tilmicosina, tilosina, tiamulina + clortetraciclina) en agua de bebida durante las fases criticas (sitio 2, 4-9 semanas).");

        if (r.HallazgosPct.PleuritisSpesPct > 20 || r.HallazgosPct.AbscesoNoduloPct > 2)
        {
            rec.Add("Implementar plan de vacunacion contra Actinobacillus pleuropneumoniae (APP) y revisar protocolo de medicacion en sitio 2 (amoxicilina 200 ppm, florfenicol 80 ppm).");
        }

        if (r.HallazgosPct.PericarditisPct > 15)
        {
            rec.Add("Evaluar plan de vacunacion contra Glasserella parasuis (enfermedad de Glasser); revisar densidad poblacional, ventilacion y temperatura en maternidad y destete (4-9 semanas).");
        }

        if (r.NivelRiesgo == "CRITICO" || r.NivelRiesgo == "ALTO")
        {
            rec.Add("Auditar condiciones ambientales: niveles de amoniaco, ventilacion, control de temperatura y humedad en instalaciones.");
            rec.Add("Considerar tratamientos inyectables en animales con sintomatologia clinica (tulatromicina + flunixin / ceftiofur + flunixin).");
            rec.Add("Realizar diagnostico diferencial via necropsia y cultivo para identificar agentes primarios (Streptococcus, Pasteurella, Influenza).");
        }
        else
        {
            rec.Add("Mantener seguimiento mensual y conservar el plan sanitario actual.");
        }

        if (r.SeveridadDistribucion.SeveroPct > 10)
            rec.Add("Priorizar evaluacion en mortalidad y sintomatologia clinica para identificar agentes primarios exacerbantes.");

        // Proxima evaluacion sugerida
        var proxima = r.NivelRiesgo switch
        {
            "CRITICO" => "15 dias",
            "ALTO"    => "30 dias",
            "MEDIO"   => "60 dias",
            _         => "90 dias"
        };
        rec.Add($"Proxima evaluacion sugerida: {proxima}.");

        return rec;
    }
}

// ===================== DTOs de resultado =====================

public class EvaluacionResultado
{
    public int GranjaId { get; set; }
    public string GranjaCodigo { get; set; } = "";
    public string GranjaNombre { get; set; } = "";
    public DateTime PeriodoDesde { get; set; }
    public DateTime PeriodoHasta { get; set; }

    public int TotalEvaluados { get; set; }
    public int TotalRegistros { get; set; }
    public int AnimalesConLesion { get; set; }
    public bool SinDatos { get; set; }

    public double PrevalenciaPct { get; set; }
    public double ConsolidacionPct { get; set; }
    public double Idn { get; set; }
    public string NivelRiesgo { get; set; } = "BAJO";

    public SeveridadBuckets SeveridadDistribucion { get; set; } = new();
    public HallazgosSecundarios HallazgosPct { get; set; } = new();
    public List<HallazgoClinico> Hallazgos { get; set; } = new();
    public AgudaCronicaDist AgudaCronicaPct { get; set; } = new();
    public List<GradoLobulo> GradosPorLobulo { get; set; } = new();
    public List<LoteInfo> PorLote { get; set; } = new();
    public List<TendenciaPunto> Tendencia { get; set; } = new();
    public List<HallazgoTendencia> TendenciaHallazgos { get; set; } = new();
    public List<VacunaUso> Vacunas { get; set; } = new();
    public List<VacunaTendenciaPunto> TendenciaVacunas { get; set; } = new();
    public AnalisisVacunacion AnalisisVacunacion { get; set; } = new();
    public ComparativoAnterior? Comparativo { get; set; }
    public List<GranjaCompare> ComparativoGranjas { get; set; } = new();
    public List<string> Conclusiones { get; set; } = new();
    public List<string> Recomendaciones { get; set; } = new();
    public Interpretaciones Interpretaciones { get; set; } = new();
}

/// <summary>Narrativa ejecutiva dinamica para cada grafico/indicador del informe.</summary>
public class Interpretaciones
{
    public string Prevalencia { get; set; } = "";
    public string Consolidacion { get; set; } = "";
    public string Idn { get; set; } = "";
    public string Severidad { get; set; } = "";
    public string AgudaCronica { get; set; } = "";
    public string Hallazgos { get; set; } = "";
    public string Grados { get; set; } = "";
    public string Tendencia { get; set; } = "";
    public string HallazgosTendencia { get; set; } = "";
    public string Vacunas { get; set; } = "";
    public string AnalisisVacunacion { get; set; } = "";
    public string Comparativo { get; set; } = "";
    public string Granjas { get; set; } = "";
}

public class SeveridadBuckets
{
    public int Normal { get; set; }
    public int Leve { get; set; }
    public int Moderado { get; set; }
    public int Severo { get; set; }
    public double NormalPct { get; set; }
    public double LevePct { get; set; }
    public double ModeradoPct { get; set; }
    public double SeveroPct { get; set; }
}

public class HallazgosSecundarios
{
    public double PleuritisSpesPct { get; set; }
    public double AbscesoNoduloPct { get; set; }
    public double PericarditisPct { get; set; }
    public double PericardioEngrosadoPct { get; set; }
    public double MocoPct { get; set; }
}

public class HallazgoClinico
{
    public string Clave { get; set; } = "";
    public string Etiqueta { get; set; } = "";
    public string TipoCampo { get; set; } = "";
    public string Color { get; set; } = "";
    public double Pct { get; set; }
}

public class AgudaCronicaDist
{
    public double AgudaPct { get; set; }
    public double CronicaPct { get; set; }
    public double AmbasPct { get; set; }
    public double SinClasifPct { get; set; }
}

public class GradoLobulo
{
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public double PesoAnatomico { get; set; }
    public double G0Pct { get; set; }
    public double G1Pct { get; set; }
    public double G2Pct { get; set; }
    public double G3Pct { get; set; }
    public double G4Pct { get; set; }
    public double OportunidadPct { get; set; }
}

public class LoteInfo
{
    public string Lote { get; set; } = "";
    public int Animales { get; set; }
    public double ConsolidacionPct { get; set; }
    public double PrevalenciaPct { get; set; }
    public int SpesPositivos { get; set; }
    public int PericarditisPositivos { get; set; }
    public List<HallazgoConteo> Hallazgos { get; set; } = new();
    /// <summary>Nombres de las vacunas aplicadas en este lote (vacio si no se vacuno).</summary>
    public List<string> Vacunas { get; set; } = new();
    /// <summary>Total de dosis aplicadas en el lote (animales x vacunas distintas por registro).</summary>
    public int DosisAplicadas { get; set; }
}

public class HallazgoConteo
{
    public string Clave { get; set; } = "";
    public string Etiqueta { get; set; } = "";
    public int Positivos { get; set; }
}

public class VacunaUso
{
    public int Id { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string? Laboratorio { get; set; }
    public int Orden { get; set; }
    public int Registros { get; set; }
    public int Animales { get; set; }
    public double CoberturaPct { get; set; }
    public double ConsolidacionPct { get; set; }
    public double PrevalenciaPct { get; set; }
}

public class AnalisisVacunacion
{
    public int LotesAnalizados { get; set; }
    public int AnimalesAntes { get; set; }
    public int AnimalesDespues { get; set; }
    public double ConsolidacionAntes { get; set; }
    public double ConsolidacionDespues { get; set; }
    public double PrevalenciaAntes { get; set; }
    public double PrevalenciaDespues { get; set; }
    /// <summary>Reduccion relativa (%) de consolidacion tras el inicio (positivo = mejoria).</summary>
    public double MejoraConsolidacionPct { get; set; }
    public double MejoraPrevalenciaPct { get; set; }
    public bool TieneComparacion { get; set; }
    public List<PuntoVacunacionRelativo> Puntos { get; set; } = new();
}

public class PuntoVacunacionRelativo
{
    /// <summary>Meses relativos al inicio de vacunacion del lote (0 = mes de inicio).</summary>
    public int OffsetMes { get; set; }
    public string Etiqueta { get; set; } = "";
    public int Animales { get; set; }
    public double ConsolidacionPct { get; set; }
    public double PrevalenciaPct { get; set; }
    /// <summary>Prevalencia (% animales afectados) de cada enfermedad en este offset, para ver mejoria por enfermedad tras vacunar.</summary>
    public Dictionary<string, double> HallazgosPct { get; set; } = new();
}

public class VacunaTendenciaPunto
{
    public string Etiqueta { get; set; } = "";
    public int Animales { get; set; }
    public int AnimalesVacunados { get; set; }
    public int AnimalesSinVacuna { get; set; }
    /// <summary>Total de dosis aplicadas en el mes (animales vacunados x vacunas distintas por registro).</summary>
    public int DosisAplicadas { get; set; }
    /// <summary>Cantidad de vacunas distintas utilizadas en el mes.</summary>
    public int VacunasDistintas { get; set; }
    public double CoberturaVacunadosPct { get; set; }
    public double ConsolidacionVacunadosPct { get; set; }
    public double ConsolidacionSinVacunaPct { get; set; }
    public double PrevalenciaVacunadosPct { get; set; }
    public double PrevalenciaSinVacunaPct { get; set; }
    public double DeltaConsolidacionVacunaVsSinVacuna { get; set; }
}

public class TendenciaPunto
{
    public string Etiqueta { get; set; } = "";
    public int Animales { get; set; }
    public double ConsolidacionPct { get; set; }
    public double PrevalenciaPct { get; set; }
    public double Idn { get; set; }
    // Prevalencia mensual por hallazgo (% de animales afectados ese mes)
    public double PleuritisSpesPct { get; set; }
    public double PericarditisPct { get; set; }
    public double PericardioEngrosadoPct { get; set; }
    public double AbscesoNoduloPct { get; set; }
    public double MocoPct { get; set; }
    public Dictionary<string, double> HallazgosPct { get; set; } = new();
}

/// <summary>Tendencia (mejora/empeora) de un hallazgo a lo largo de los meses con datos.</summary>
public class HallazgoTendencia
{
    public string Clave { get; set; } = "";
    public string Etiqueta { get; set; } = "";
    public string Color { get; set; } = "";
    public double InicialPct { get; set; }
    public double ActualPct { get; set; }
    /// <summary>Cambio neto en puntos porcentuales entre el primer y el ultimo mes con datos.</summary>
    public double DeltaPp { get; set; }
    /// <summary>Pendiente de la regresion lineal en pp por mes.</summary>
    public double Pendiente { get; set; }
    /// <summary>MEJORA (baja) | EMPEORA (sube) | ESTABLE | SIN_DATOS.</summary>
    public string Direccion { get; set; } = "ESTABLE";
    public int MesesConDatos { get; set; }
}

public class ComparativoAnterior
{
    public string Etiqueta { get; set; } = "";
    public int Animales { get; set; }
    public double ConsolidacionPctAnterior { get; set; }
    public double PrevalenciaPctAnterior { get; set; }
    public double IdnAnterior { get; set; }
    public double DeltaConsolidacion { get; set; }
    public double DeltaPrevalencia { get; set; }
    public double DeltaIdn { get; set; }
}

public class GranjaCompare
{
    public int GranjaId { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public int Animales { get; set; }
    public double ConsolidacionPct { get; set; }
    public double PrevalenciaPct { get; set; }
    public double Idn { get; set; }
    public string NivelRiesgo { get; set; } = "BAJO";
    public bool EsActual { get; set; }
}
