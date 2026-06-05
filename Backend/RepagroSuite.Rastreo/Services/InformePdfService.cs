using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Rastreo.Api.Models;
using SkiaSharp;

namespace Rastreo.Api.Services;

/// <summary>
/// Genera el PDF ejecutivo de evaluacion pulmonar: portada + resumen ejecutivo + KPIs +
/// graficos vectoriales (donut/lineas/barras) + analisis detallado + conclusiones +
/// recomendaciones + anexo. SIN imagenes de pulmones, todo data-driven.
/// </summary>
public class InformePdfService
{
    // Paleta corporativa Trust & Authority
    private static readonly string TealPrimary = "#0f766e";
    private static readonly string TealDark    = "#115e59";
    private static readonly string AccentBlue  = "#0369a1";
    private static readonly string Slate900    = "#0f172a";
    private static readonly string Slate700    = "#334155";
    private static readonly string Slate500    = "#64748b";
    private static readonly string Slate300    = "#cbd5e1";
    private static readonly string Slate100    = "#f1f5f9";
    private static readonly string Slate50     = "#f8fafc";

    private static readonly string OkGreen   = "#059669";
    private static readonly string WarnAmber = "#d97706";
    private static readonly string DangerRed = "#dc2626";

    // Severidad
    private static readonly string SevNormal   = "#10b981";
    private static readonly string SevLeve     = "#fbbf24";
    private static readonly string SevModerado = "#f97316";
    private static readonly string SevSevero   = "#dc2626";

    public byte[] Generar(InformeEvaluacion meta, EvaluacionResultado r, Granja granja)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var doc = Document.Create(c =>
        {
            // ===== PORTADA =====
            c.Page(p =>
            {
                p.Size(PageSizes.A4);
                p.Margin(0);
                p.DefaultTextStyle(t => t.FontFamily("Segoe UI").FontColor(Slate900));
                p.Content().Element(e => Portada(e, meta, r, granja));
            });

            // ===== RESTO DE PAGINAS =====
            c.Page(p =>
            {
                p.Size(PageSizes.A4);
                p.Margin(40);
                p.DefaultTextStyle(t => t.FontFamily("Segoe UI").FontSize(10).FontColor(Slate900));
                p.Header().Element(e => HeaderPagina(e, meta, granja));
                p.Footer().Element(e => FooterPagina(e));
                p.Content().Element(e => ContenidoPrincipal(e, meta, r));
            });
        });

        return doc.GeneratePdf();
    }

    // =============================================================
    // PORTADA
    // =============================================================
    private void Portada(IContainer container, InformeEvaluacion meta, EvaluacionResultado r, Granja granja)
    {
        container.Background(Slate50).Column(col =>
        {
            // Banner superior con gradiente (PNG via SkiaSharp)
            col.Item().Height(170).Layers(layers =>
            {
                layers.PrimaryLayer().Background(TealDark);
                layers.Layer().Image(RenderBannerPng(1240, 340)).FitUnproportionally();
                layers.Layer().PaddingHorizontal(40).PaddingTop(30).Row(banner =>
                {
                    banner.RelativeItem().Column(c =>
                    {
                        c.Item().Text("REPAGRO").FontSize(11).Bold().FontColor("#a7f3d0").LetterSpacing(0.3f);
                        c.Item().PaddingTop(48).Text("INFORME EJECUTIVO").FontSize(11).Bold().FontColor("#ccfbf1").LetterSpacing(0.4f);
                        c.Item().PaddingTop(2).Text("Evaluacion Pulmonar").FontSize(28).Bold().FontColor(Colors.White);
                    });
                    // Logo principal directo sobre el banner (sin fondo).
                    var logo = BrandAssets.LogoRepagro();
                    if (logo != null)
                        banner.ConstantItem(190).AlignTop().Image(logo).FitWidth();
                });
            });

            // Bloque granja
            col.Item().PaddingHorizontal(40).PaddingTop(28).Column(c =>
            {
                c.Item().Text(granja.Nombre).FontSize(22).Bold().FontColor(Slate900);
                c.Item().PaddingTop(2).Text($"Codigo: {granja.Codigo}").FontSize(11).FontColor(Slate500);
                if (!string.IsNullOrWhiteSpace(granja.Propietario))
                    c.Item().Text($"Propietario / Cliente: {granja.Propietario}").FontSize(11).FontColor(Slate700);
                if (!string.IsNullOrWhiteSpace(granja.Ubicacion))
                    c.Item().Text($"Ubicacion: {granja.Ubicacion}").FontSize(11).FontColor(Slate700);
            });

            // Tarjeta meta
            col.Item().PaddingHorizontal(40).PaddingTop(22).Background(Colors.White)
                .Border(1).BorderColor(Slate300).Padding(20).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("PERIODO EVALUADO").FontSize(8).Bold().FontColor(Slate500).LetterSpacing(0.2f);
                    c.Item().PaddingTop(2).Text(meta.PeriodoEtiqueta ?? $"{r.PeriodoDesde:dd MMM} - {r.PeriodoHasta:dd MMM yyyy}").FontSize(13).Bold().FontColor(Slate900);
                });
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("CONSECUTIVO").FontSize(8).Bold().FontColor(Slate500).LetterSpacing(0.2f);
                    c.Item().PaddingTop(2).Text(meta.Consecutivo).FontSize(13).Bold().FontColor(Slate900);
                });
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("RESPONSABLE TECNICO").FontSize(8).Bold().FontColor(Slate500).LetterSpacing(0.2f);
                    c.Item().PaddingTop(2).Text(string.IsNullOrEmpty(meta.ResponsableTecnico) ? "-" : meta.ResponsableTecnico).FontSize(13).Bold().FontColor(Slate900);
                });
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("GENERADO").FontSize(8).Bold().FontColor(Slate500).LetterSpacing(0.2f);
                    c.Item().PaddingTop(2).Text(meta.FechaGeneracion.ToLocalTime().ToString("dd MMM yyyy HH:mm")).FontSize(13).Bold().FontColor(Slate900);
                });
            });

            // Tarjeta riesgo grande
            col.Item().PaddingHorizontal(40).PaddingTop(22).Element(e =>
            {
                var (bgRiesgo, fgRiesgo) = ColoresRiesgo(r.NivelRiesgo);
                e.Background(bgRiesgo).Padding(22).Column(c =>
                {
                    c.Item().Row(row =>
                    {
                        row.RelativeItem().Column(cc =>
                        {
                            cc.Item().Text("NIVEL DE RIESGO AGREGADO").FontSize(9).Bold().FontColor(fgRiesgo).LetterSpacing(0.3f);
                            cc.Item().PaddingTop(4).Text(r.NivelRiesgo).FontSize(36).Bold().FontColor(fgRiesgo);
                        });
                        row.ConstantItem(220).Column(cc =>
                        {
                            cc.Item().Element(e2 => Kpi(e2, "Total evaluados", r.TotalEvaluados.ToString(), fgRiesgo, transparent: true));
                            cc.Item().PaddingTop(4).Element(e2 => Kpi(e2, "Consolidacion promedio", $"{r.ConsolidacionPct:F2}%", fgRiesgo, transparent: true));
                            cc.Item().PaddingTop(4).Element(e2 => Kpi(e2, "Indice de Neumonia (IDN)", r.Idn.ToString("F2"), fgRiesgo, transparent: true));
                        });
                    });
                });
            });

            col.Item().PaddingHorizontal(40).PaddingTop(40).PaddingBottom(40).Text(t =>
            {
                t.AlignCenter();
                t.Span("Documento confidencial · Generado por RASTREO · ").FontSize(8).FontColor(Slate500);
                t.Span("REPAGRO").FontSize(8).Bold().FontColor(Slate700);
            });
        });
    }

    // =============================================================
    // HEADER / FOOTER de paginas internas
    // =============================================================
    private void HeaderPagina(IContainer container, InformeEvaluacion meta, Granja granja)
    {
        container.PaddingBottom(8).BorderBottom(1).BorderColor(Slate300).Row(r =>
        {
            r.RelativeItem().AlignMiddle().Column(c =>
            {
                c.Item().Text("INFORME DE EVALUACION PULMONAR").FontSize(8).Bold().FontColor(TealPrimary).LetterSpacing(0.3f);
                c.Item().Text($"{granja.Nombre} · {meta.PeriodoEtiqueta} · {meta.Consecutivo}").FontSize(9).FontColor(Slate700);
            });
            // Logo arriba a la derecha en paginas internas.
            var logoH = BrandAssets.LogoRepagro();
            if (logoH != null) r.ConstantItem(95).AlignRight().AlignMiddle().Image(logoH).FitWidth();
        });
    }

    private void FooterPagina(IContainer container)
    {
        var logo = BrandAssets.LogoRepagro();
        container.PaddingTop(8).BorderTop(1).BorderColor(Slate300).Row(r =>
        {
            // Logo a la izquierda.
            if (logo != null) r.ConstantItem(58).AlignMiddle().Image(logo).FitWidth();
            r.RelativeItem().PaddingLeft(logo != null ? 8 : 0).AlignMiddle().Text(t =>
            {
                t.Span("Generado por ").FontSize(7).FontColor(Slate500);
                t.Span("RASTREO").FontSize(7).Bold().FontColor(TealPrimary);
                t.Span($" · {DateTime.Now:dd MMM yyyy HH:mm}").FontSize(7).FontColor(Slate500);
            });
            // Paginacion numerada. En ConstantItem alineada a la izquierda: asi el total
            // (p.ej. "11") nunca se recorta contra el borde (la medicion dinamica de QuestPDF
            // pierde digitos al alinear a la derecha contra el margen).
            r.ConstantItem(64).AlignMiddle().Text(t =>
            {
                t.DefaultTextStyle(s => s.FontSize(7).FontColor(Slate500));
                t.Span("Pagina ");
                t.CurrentPageNumber();
                t.Span(" / ");
                t.TotalPages();
            });
        });
    }

    // =============================================================
    // CONTENIDO PRINCIPAL
    // =============================================================
    private void ContenidoPrincipal(IContainer container, InformeEvaluacion meta, EvaluacionResultado r)
    {
        container.PaddingVertical(10).Column(col =>
        {
            // ===== 1. Resumen ejecutivo =====
            col.Item().Element(e => SeccionTitulo(e, "1. Resumen Ejecutivo"));
            col.Item().PaddingTop(6).Element(e => ResumenEjecutivo(e, r));

            // ===== 2. Indicadores clave =====
            col.Item().PaddingTop(20).Element(e => SeccionTitulo(e, "2. Indicadores Clave"));
            col.Item().PaddingTop(8).Element(e => KpiGrid(e, r));
            col.Item().Element(e => LecturaIndicadores(e, r));

            // ===== 3. Distribucion de severidad + Aguda/Cronica =====
            col.Item().PaddingTop(20).Element(e => SeccionTitulo(e, "3. Distribucion de Severidad y Temporalidad"));
            col.Item().PaddingTop(8).Row(row =>
            {
                row.RelativeItem().Element(e => DonutSeveridad(e, r));
                row.ConstantItem(16);
                row.RelativeItem().Element(e => DonutAgudaCronica(e, r));
            });
            col.Item().Element(e => Explicacion(e, r.Interpretaciones.Severidad, "LECTURA · SEVERIDAD"));
            col.Item().Element(e => Explicacion(e, r.Interpretaciones.AgudaCronica, "LECTURA · LESIONES AGUDAS / CRONICAS"));

            // ===== 4. Hallazgos secundarios =====
            col.Item().PaddingTop(20).Element(e => SeccionTitulo(e, "4. Hallazgos Clinicos Secundarios"));
            col.Item().PaddingTop(8).Element(e => BarrasHallazgos(e, r));
            col.Item().Element(e => Explicacion(e, r.Interpretaciones.Hallazgos));

            // ===== 5. Distribucion de grados por lobulo =====
            col.Item().PageBreak();
            col.Item().Element(e => SeccionTitulo(e, "5. Distribucion de Grados por Lobulo"));
            col.Item().PaddingTop(8).Element(e => TablaGradosLobulo(e, r));
            col.Item().Element(e => Explicacion(e, r.Interpretaciones.Grados));

            // ===== 6. Comparativo periodo anterior =====
            if (r.Comparativo != null)
            {
                col.Item().PaddingTop(20).Element(e => SeccionTitulo(e, "6. Comparativo con Periodo Anterior"));
                col.Item().PaddingTop(8).Element(e => TablaComparativo(e, r));
                col.Item().Element(e => Explicacion(e, r.Interpretaciones.Comparativo));
            }

            // ===== 7. Tendencia historica =====
            if (r.Tendencia.Any(t => t.Animales > 0))
            {
                col.Item().PaddingTop(20).Element(e => SeccionTitulo(e, "7. Tendencia Historica (12 meses)"));
                col.Item().PaddingTop(8).Element(e => GraficoLineaTendencia(e, r));
                col.Item().Element(e => Explicacion(e, r.Interpretaciones.Tendencia));

                // 7.1 Tendencia por hallazgo (mejora / empeora con direccion calculada por regresion)
                col.Item().PaddingTop(16).Text("7.1 Tendencia de Hallazgos Clinicos").FontSize(11).Bold().FontColor(Slate900);
                if (r.Tendencia.Count(t => t.Animales > 0) >= 2)
                {
                    col.Item().PaddingTop(8).Background(Colors.White).Border(1).BorderColor(Slate300).Padding(12).Column(cc =>
                    {
                        cc.Item().Text("Evolucion mensual del % de animales afectados por hallazgo").FontSize(10).Bold().FontColor(Slate900);
                        cc.Item().PaddingTop(10).Height(210).Image(RenderTendenciaHallazgosPng(r, 1100, 420)).FitArea();
                    });
                }
                col.Item().PaddingTop(8).Element(e => TablaTendenciaHallazgos(e, r));
                col.Item().Element(e => Explicacion(e, r.Interpretaciones.HallazgosTendencia));
            }

            // ===== 7.2 Vacunas e impacto sanitario =====
            if (r.Vacunas.Count > 0 || r.TendenciaVacunas.Any(t => t.Animales > 0))
            {
                col.Item().PaddingTop(16).Text("7.2 Uso de Vacunas e Impacto Sanitario").FontSize(11).Bold().FontColor(Slate900);
                col.Item().PaddingTop(8).Background(Colors.White).Border(1).BorderColor(Slate300).Padding(12).Column(cc =>
                {
                    cc.Item().Text("Cobertura vacunal vs consolidacion pulmonar").FontSize(10).Bold().FontColor(Slate900);
                    cc.Item().PaddingTop(10).Height(210).Image(RenderVacunasPng(r, 1100, 420)).FitArea();
                });
                if (r.Vacunas.Count > 0)
                    col.Item().PaddingTop(8).Element(e => TablaVacunas(e, r));
                col.Item().Element(e => Explicacion(e, r.Interpretaciones.Vacunas, "LECTURA · VACUNAS"));
            }

            // ===== 7.3 Mejoria por vacunacion (antes vs despues, alineado por lote) =====
            if (r.AnalisisVacunacion.LotesAnalizados > 0)
            {
                col.Item().PaddingTop(16).Text("7.3 Mejoría por Vacunación (antes vs después)").FontSize(11).Bold().FontColor(Slate900);
                col.Item().PaddingTop(8).Background(Colors.White).Border(1).BorderColor(Slate300).Padding(12).Column(cc =>
                {
                    cc.Item().Text($"Lotes alineados a su mes de inicio de vacunación ({r.AnalisisVacunacion.LotesAnalizados} lote(s); 0 = inicio)").FontSize(10).Bold().FontColor(Slate900);
                    cc.Item().PaddingTop(10).Height(210).Image(RenderAnalisisVacunacionPng(r, 1100, 420)).FitArea();
                });
                if (r.AnalisisVacunacion.TieneComparacion)
                    col.Item().PaddingTop(8).Element(e => TablaAnalisisVacunacion(e, r));
                col.Item().Element(e => Explicacion(e, r.Interpretaciones.AnalisisVacunacion, "LECTURA · VACUNACIÓN ANTES/DESPUÉS"));
            }

            // ===== 8. Comparativo entre granjas =====
            if (r.ComparativoGranjas.Count > 1)
            {
                col.Item().PageBreak();
                col.Item().Element(e => SeccionTitulo(e, "8. Comparativo entre Granjas (mismo periodo)"));
                col.Item().PaddingTop(8).Element(e => BarrasComparativoGranjas(e, r));
                col.Item().Element(e => Explicacion(e, r.Interpretaciones.Granjas));
            }

            // ===== 9. Detalle por lote =====
            if (r.PorLote.Count > 0)
            {
                col.Item().PaddingTop(20).Element(e => SeccionTitulo(e, "9. Resultados por Lote"));
                col.Item().PaddingTop(8).Element(e => TablaPorLote(e, r));
            }

            // ===== 10. Conclusiones =====
            col.Item().PageBreak();
            col.Item().Element(e => SeccionTitulo(e, "10. Conclusiones Tecnicas"));
            col.Item().PaddingTop(8).Element(e => ListaTexto(e, r.Conclusiones, TealPrimary));

            // ===== 11. Recomendaciones =====
            col.Item().PaddingTop(20).Element(e => SeccionTitulo(e, "11. Recomendaciones"));
            col.Item().PaddingTop(8).Element(e => ListaTexto(e, r.Recomendaciones, AccentBlue));

            // ===== 12. Anexo de datos =====
            col.Item().PaddingTop(20).Element(e => SeccionTitulo(e, "12. Anexo de Datos · Auditoria"));
            col.Item().PaddingTop(8).Element(e => AnexoDatos(e, meta, r));
        });
    }

    // =============================================================
    // COMPONENTES DE SECCION
    // =============================================================
    private void SeccionTitulo(IContainer container, string texto)
    {
        container.BorderLeft(3).BorderColor(TealPrimary).PaddingLeft(10).Column(c =>
        {
            c.Item().Text(texto).FontSize(13).Bold().FontColor(Slate900);
        });
    }

    /// <summary>Callout de interpretacion clinica. El texto envuelve automaticamente (sin desborde).</summary>
    private void Explicacion(IContainer container, string texto, string titulo = "LECTURA CLINICA")
    {
        container.PaddingTop(8).Background("#f0f7f6").BorderLeft(3).BorderColor(TealPrimary).Padding(11).Column(col =>
        {
            col.Item().Text(titulo).FontSize(7.5f).Bold().FontColor(TealPrimary).LetterSpacing(0.5f);
            col.Item().PaddingTop(3).Text(texto).FontSize(9.5f).FontColor(Slate700).LineHeight(1.45f);
        });
    }

    /// <summary>Lectura detallada de los 3 indicadores principales (prevalencia, consolidacion, IDN).</summary>
    private void LecturaIndicadores(IContainer container, EvaluacionResultado r)
    {
        container.PaddingTop(8).Background("#f0f7f6").BorderLeft(3).BorderColor(TealPrimary).Padding(11).Column(col =>
        {
            col.Item().Text("LECTURA DE INDICADORES").FontSize(7.5f).Bold().FontColor(TealPrimary).LetterSpacing(0.5f);
            col.Item().PaddingTop(5).Element(e => PuntoLectura(e, "Prevalencia", r.Interpretaciones.Prevalencia));
            col.Item().PaddingTop(5).Element(e => PuntoLectura(e, "Consolidacion", r.Interpretaciones.Consolidacion));
            col.Item().PaddingTop(5).Element(e => PuntoLectura(e, "IDN", r.Interpretaciones.Idn));
        });
    }

    private void PuntoLectura(IContainer container, string etiqueta, string texto)
    {
        container.Text(t =>
        {
            t.Span($"{etiqueta}: ").FontSize(9.5f).Bold().FontColor(Slate900);
            t.Span(texto).FontSize(9.5f).FontColor(Slate700).LineHeight(1.4f);
        });
    }

    private void ResumenEjecutivo(IContainer container, EvaluacionResultado r)
    {
        container.Background(Slate50).Border(1).BorderColor(Slate300).Padding(14).Column(col =>
        {
            col.Item().Text(t =>
            {
                t.Span("De los ").FontSize(10).FontColor(Slate700);
                t.Span($"{r.TotalEvaluados} pulmones evaluados").FontSize(10).Bold().FontColor(Slate900);
                t.Span(" en este periodo, ").FontSize(10).FontColor(Slate700);
                t.Span($"{r.PrevalenciaPct:F2}%").FontSize(10).Bold().FontColor(TealPrimary);
                t.Span(" presentan algun grado de lesion neumonica, con un area consolidada promedio del ").FontSize(10).FontColor(Slate700);
                t.Span($"{r.ConsolidacionPct:F2}%").FontSize(10).Bold().FontColor(TealPrimary);
                t.Span(" e Indice de Neumonia (IDN) de ").FontSize(10).FontColor(Slate700);
                t.Span($"{r.Idn:F2}").FontSize(10).Bold().FontColor(TealPrimary);
                t.Span(".").FontSize(10).FontColor(Slate700);
            });
            col.Item().PaddingTop(8).Row(r2 =>
            {
                var (bg, fg) = ColoresRiesgo(r.NivelRiesgo);
                r2.AutoItem().Background(bg).PaddingHorizontal(10).PaddingVertical(4).Text($"Nivel de riesgo: {r.NivelRiesgo}").FontSize(9).Bold().FontColor(fg);
                r2.RelativeItem();
                if (r.Comparativo != null)
                {
                    var simbolo = r.Comparativo.DeltaConsolidacion > 0 ? "↑" : "↓";
                    var color = r.Comparativo.DeltaConsolidacion > 0 ? DangerRed : OkGreen;
                    r2.AutoItem().Text(t =>
                    {
                        t.Span("Δ vs anterior: ").FontSize(9).FontColor(Slate500);
                        t.Span($"{simbolo} {Math.Abs(r.Comparativo.DeltaConsolidacion):F2} pts").FontSize(9).Bold().FontColor(color);
                    });
                }
            });
        });
    }

    private void KpiGrid(IContainer container, EvaluacionResultado r)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Element(e => Kpi(e, "Total Evaluados", r.TotalEvaluados.ToString(), TealPrimary));
                row.ConstantItem(8);
                row.RelativeItem().Element(e => Kpi(e, "Prevalencia Neumonica", $"{r.PrevalenciaPct:F2}%", AccentBlue));
                row.ConstantItem(8);
                row.RelativeItem().Element(e => Kpi(e, "Consolidacion Promedio", $"{r.ConsolidacionPct:F2}%", TealDark));
                row.ConstantItem(8);
                row.RelativeItem().Element(e => Kpi(e, "Indice de Neumonia", r.Idn.ToString("F2"), AccentBlue));
            });
            col.Item().PaddingTop(8).Row(row =>
            {
                row.RelativeItem().Element(e => Kpi(e, "% Normal", $"{r.SeveridadDistribucion.NormalPct:F1}%", SevNormal));
                row.ConstantItem(8);
                row.RelativeItem().Element(e => Kpi(e, "% Leve", $"{r.SeveridadDistribucion.LevePct:F1}%", SevLeve));
                row.ConstantItem(8);
                row.RelativeItem().Element(e => Kpi(e, "% Moderado", $"{r.SeveridadDistribucion.ModeradoPct:F1}%", SevModerado));
                row.ConstantItem(8);
                row.RelativeItem().Element(e => Kpi(e, "% Severo", $"{r.SeveridadDistribucion.SeveroPct:F1}%", SevSevero));
            });
        });
    }

    private void Kpi(IContainer container, string label, string value, string color, bool transparent = false)
    {
        IContainer c = transparent
            ? container.Background("#ffffff").Border(1).BorderColor("#e2e8f0").Padding(10)
            : container.Background(Colors.White).Border(1).BorderColor(Slate300).Padding(10);
        c.Column(col =>
        {
            col.Item().Text(label).FontSize(8).Bold().FontColor(Slate500).LetterSpacing(0.2f);
            col.Item().PaddingTop(2).Text(value).FontSize(18).Bold().FontColor(color);
        });
    }

    // =============================================================
    // CHARTS — DONUT (SkiaSharp Canvas)
    // =============================================================
    private void DonutSeveridad(IContainer container, EvaluacionResultado r)
    {
        var data = new[]
        {
            (label: "Normal",   pct: r.SeveridadDistribucion.NormalPct,   color: SevNormal,   cnt: r.SeveridadDistribucion.Normal),
            (label: "Leve",     pct: r.SeveridadDistribucion.LevePct,     color: SevLeve,     cnt: r.SeveridadDistribucion.Leve),
            (label: "Moderado", pct: r.SeveridadDistribucion.ModeradoPct, color: SevModerado, cnt: r.SeveridadDistribucion.Moderado),
            (label: "Severo",   pct: r.SeveridadDistribucion.SeveroPct,   color: SevSevero,   cnt: r.SeveridadDistribucion.Severo),
        };
        ChartDonut(container, "Distribucion por severidad clinica", data, r.TotalEvaluados);
    }

    private void DonutAgudaCronica(IContainer container, EvaluacionResultado r)
    {
        var a = r.AgudaCronicaPct;
        var data = new[]
        {
            (label: "Aguda",       pct: a.AgudaPct,      color: "#ef4444", cnt: 0),
            (label: "Cronica",     pct: a.CronicaPct,    color: "#a855f7", cnt: 0),
            (label: "Ambas (AC)",  pct: a.AmbasPct,      color: "#f97316", cnt: 0),
            (label: "Sin clasif.", pct: a.SinClasifPct,  color: Slate300,  cnt: 0),
        };
        ChartDonut(container, "Lesiones agudas vs cronicas", data, 100);
    }

    private void ChartDonut(IContainer container, string titulo, (string label, double pct, string color, int cnt)[] data, int totalShown)
    {
        container.Background(Colors.White).Border(1).BorderColor(Slate300).Padding(12).Column(col =>
        {
            col.Item().Text(titulo).FontSize(10).Bold().FontColor(Slate900);
            col.Item().PaddingTop(10).Row(row =>
            {
                row.ConstantItem(140).Height(140).Image(RenderDonutPng(data, totalShown, 300)).FitArea();

                row.RelativeItem().PaddingLeft(10).Column(legend =>
                {
                    foreach (var slice in data)
                    {
                        legend.Item().PaddingVertical(2).Row(rr =>
                        {
                            rr.ConstantItem(10).Height(10).Background(slice.color);
                            rr.ConstantItem(8);
                            rr.RelativeItem().Text(slice.label).FontSize(9).FontColor(Slate700);
                            rr.AutoItem().Text($"{slice.pct:F1}%").FontSize(9).Bold().FontColor(Slate900);
                        });
                    }
                });
            });
        });
    }

    // =============================================================
    // BARRAS HORIZONTALES (DSL puro, sin Canvas)
    // =============================================================
    private void BarrasHallazgos(IContainer container, EvaluacionResultado r)
    {
        var items = r.Hallazgos
            .Select(h => (h.Etiqueta, h.Pct, string.IsNullOrWhiteSpace(h.Color) ? AccentBlue : h.Color))
            .ToArray();
        BarrasHorizontales(container, "Prevalencia de hallazgos clinicos (% del grupo)", items);
    }

    private void BarrasComparativoGranjas(IContainer container, EvaluacionResultado r)
    {
        var items = r.ComparativoGranjas
            .Select(g => ($"{g.Codigo} {g.Nombre}", g.ConsolidacionPct, g.EsActual ? TealPrimary : Slate500))
            .ToArray();
        BarrasHorizontales(container, "Consolidacion pulmonar (%) por granja", items);
    }

    private void BarrasHorizontales(IContainer container, string titulo, (string label, double pct, string color)[] items)
    {
        container.Background(Colors.White).Border(1).BorderColor(Slate300).Padding(12).Column(col =>
        {
            col.Item().Text(titulo).FontSize(10).Bold().FontColor(Slate900);
            col.Item().PaddingTop(10).Column(stack =>
            {
                var max = Math.Max(1.0, items.Max(i => i.pct));
                foreach (var (label, pct, color) in items)
                {
                    var ratio = Math.Clamp(pct / max, 0.0, 1.0);
                    stack.Item().PaddingVertical(3).Row(r =>
                    {
                        r.ConstantItem(135).AlignMiddle().PaddingRight(6).Text(label).FontSize(9).FontColor(Slate700);
                        r.RelativeItem().AlignMiddle().Background(Slate100).Height(18).Row(bar =>
                        {
                            // porcion llena (minimo visible si pct>0)
                            var llena = pct > 0.01 ? (float)Math.Max(ratio, 0.02) : 0f;
                            if (llena > 0)
                                bar.RelativeItem(llena).Background(color);
                            if (llena < 1f)
                                bar.RelativeItem(1f - llena);
                        });
                        r.ConstantItem(12); // separador para que el valor nunca toque el borde
                        r.ConstantItem(46).AlignMiddle().AlignRight().Text($"{pct:F1}%").FontSize(9).Bold().FontColor(Slate900);
                    });
                }
            });
        });
    }

    // =============================================================
    // TABLA: GRADOS POR LOBULO
    // =============================================================
    private void TablaGradosLobulo(IContainer container, EvaluacionResultado r)
    {
        container.Border(1).BorderColor(Slate300).Table(t =>
        {
            t.ColumnsDefinition(c =>
            {
                c.RelativeColumn(2.2f);
                c.ConstantColumn(50);
                c.ConstantColumn(50);
                c.ConstantColumn(50);
                c.ConstantColumn(50);
                c.ConstantColumn(50);
                c.ConstantColumn(50);
                c.ConstantColumn(60);
            });

            t.Header(h =>
            {
                CeldaHead(h.Cell(), "LOBULO");
                CeldaHead(h.Cell(), "PESO");
                CeldaHead(h.Cell(), "G0");
                CeldaHead(h.Cell(), "G1");
                CeldaHead(h.Cell(), "G2");
                CeldaHead(h.Cell(), "G3");
                CeldaHead(h.Cell(), "G4");
                CeldaHead(h.Cell(), "% AFECT.");
            });

            int i = 0;
            foreach (var l in r.GradosPorLobulo)
            {
                var bg = i++ % 2 == 0 ? "#ffffff" : Slate50;
                Celda(t.Cell(), bg, l.Nombre, bold: true, align: Align.Left);
                Celda(t.Cell(), bg, $"{l.PesoAnatomico:F0}%");
                Celda(t.Cell(), bg, $"{l.G0Pct:F1}%");
                Celda(t.Cell(), bg, $"{l.G1Pct:F1}%");
                Celda(t.Cell(), bg, $"{l.G2Pct:F1}%");
                Celda(t.Cell(), bg, $"{l.G3Pct:F1}%");
                Celda(t.Cell(), bg, $"{l.G4Pct:F1}%");
                Celda(t.Cell(), bg, $"{l.OportunidadPct:F1}%", bold: true, color: l.OportunidadPct > 30 ? DangerRed : (l.OportunidadPct > 15 ? WarnAmber : OkGreen));
            }
        });
    }

    private enum Align { Left, Center, Right }

    private void CeldaHead(IContainer cell, string txt)
    {
        cell.Background(TealPrimary).PaddingVertical(6).PaddingHorizontal(8).Text(txt).FontSize(8).Bold().FontColor(Colors.White).LetterSpacing(0.2f);
    }

    private void Celda(IContainer cell, string bg, string txt, bool bold = false, string? color = null, Align align = Align.Center)
    {
        var text = cell.Background(bg).PaddingVertical(5).PaddingHorizontal(8).Text(txt).FontSize(9).FontColor(color ?? Slate700);
        if (bold) text.Bold();
        if (align == Align.Left) text.AlignLeft();
        else if (align == Align.Right) text.AlignRight();
        else text.AlignCenter();
    }

    // =============================================================
    // TABLA: COMPARATIVO
    // =============================================================
    private void TablaComparativo(IContainer container, EvaluacionResultado r)
    {
        var c = r.Comparativo!;
        container.Border(1).BorderColor(Slate300).Table(t =>
        {
            t.ColumnsDefinition(d =>
            {
                d.RelativeColumn(2);
                d.RelativeColumn();
                d.RelativeColumn();
                d.RelativeColumn();
            });
            t.Header(h =>
            {
                CeldaHead(h.Cell(), "METRICA");
                CeldaHead(h.Cell(), "ANTERIOR");
                CeldaHead(h.Cell(), "ACTUAL");
                CeldaHead(h.Cell(), "Δ DELTA");
            });
            Fila(t, "Prevalencia (%)",     $"{c.PrevalenciaPctAnterior:F2}",   $"{r.PrevalenciaPct:F2}",   FormatDelta(c.DeltaPrevalencia));
            Fila(t, "Consolidacion (%)",   $"{c.ConsolidacionPctAnterior:F2}", $"{r.ConsolidacionPct:F2}", FormatDelta(c.DeltaConsolidacion));
            Fila(t, "IDN (0-4)",           $"{c.IdnAnterior:F2}",              $"{r.Idn:F2}",              FormatDelta(c.DeltaIdn));
            Fila(t, "Animales evaluados",  c.Animales.ToString(),              r.TotalEvaluados.ToString(),"=");
        });
    }

    private void Fila(TableDescriptor t, string label, string ant, string act, string delta)
    {
        Celda(t.Cell(), Colors.White, label, bold: true, align: Align.Left);
        Celda(t.Cell(), Colors.White, ant);
        Celda(t.Cell(), Colors.White, act, bold: true);
        var deltaColor = delta.StartsWith("↓") ? OkGreen : delta.StartsWith("↑") ? DangerRed : Slate500;
        Celda(t.Cell(), Colors.White, delta, bold: true, color: deltaColor);
    }

    private static string FormatDelta(double v, bool invertir = false)
    {
        if (Math.Abs(v) < 0.01) return "=";
        // invertir=true significa que aumento es malo (mas neumonia)
        var arrow = v > 0 ? "↑" : "↓";
        return $"{arrow} {Math.Abs(v):F2}";
    }

    // =============================================================
    // LINEA TENDENCIA (Canvas SkiaSharp)
    // =============================================================
    private void GraficoLineaTendencia(IContainer container, EvaluacionResultado r)
    {
        container.Background(Colors.White).Border(1).BorderColor(Slate300).Padding(12).Column(col =>
        {
            col.Item().Text("Evolucion mensual de consolidacion pulmonar (%)").FontSize(10).Bold().FontColor(Slate900);
            col.Item().PaddingTop(10).Height(170).Image(RenderLineaPng(r.Tendencia, 1100, 340)).FitArea();
        });
    }

    // =============================================================
    // TABLA: TENDENCIA DE HALLAZGOS (mejora / empeora por enfermedad)
    // =============================================================
    private void TablaTendenciaHallazgos(IContainer container, EvaluacionResultado r)
    {
        container.Border(1).BorderColor(Slate300).Table(t =>
        {
            t.ColumnsDefinition(c =>
            {
                c.RelativeColumn(2.4f);
                c.ConstantColumn(68);
                c.ConstantColumn(68);
                c.ConstantColumn(64);
                c.RelativeColumn(1.4f);
            });
            t.Header(h =>
            {
                CeldaHead(h.Cell(), "HALLAZGO");
                CeldaHead(h.Cell(), "INICIAL");
                CeldaHead(h.Cell(), "ACTUAL");
                CeldaHead(h.Cell(), "Δ PP");
                CeldaHead(h.Cell(), "TENDENCIA");
            });

            int i = 0;
            foreach (var h in r.TendenciaHallazgos)
            {
                var bg = i++ % 2 == 0 ? "#ffffff" : Slate50;
                bool sinDatos = h.Direccion == "SIN_DATOS";
                var (txt, color) = h.Direccion switch
                {
                    "MEJORA"  => ("↓ Mejora", OkGreen),
                    "EMPEORA" => ("↑ Empeora", DangerRed),
                    "ESTABLE" => ("= Estable", Slate500),
                    _         => ("Sin datos", Slate500),
                };
                var delta = sinDatos ? "—" : $"{(h.DeltaPp > 0 ? "+" : "")}{h.DeltaPp:F1}";
                Celda(t.Cell(), bg, h.Etiqueta, bold: true, align: Align.Left);
                Celda(t.Cell(), bg, sinDatos ? "—" : $"{h.InicialPct:F1}%");
                Celda(t.Cell(), bg, sinDatos ? "—" : $"{h.ActualPct:F1}%", bold: true);
                Celda(t.Cell(), bg, delta, bold: true, color: color);
                Celda(t.Cell(), bg, txt, bold: true, color: color);
            }
        });
    }

    private void TablaVacunas(IContainer container, EvaluacionResultado r)
    {
        container.Border(1).BorderColor(Slate300).Table(t =>
        {
            t.ColumnsDefinition(c =>
            {
                c.RelativeColumn(2.2f);
                c.ConstantColumn(70);
                c.ConstantColumn(76);
                c.ConstantColumn(76);
                c.ConstantColumn(76);
            });
            t.Header(h =>
            {
                CeldaHead(h.Cell(), "VACUNA");
                CeldaHead(h.Cell(), "ANIMALES");
                CeldaHead(h.Cell(), "COBERTURA");
                CeldaHead(h.Cell(), "CONSOLID.");
                CeldaHead(h.Cell(), "PREVAL.");
            });

            int i = 0;
            foreach (var v in r.Vacunas.OrderByDescending(v => v.Animales).ThenBy(v => v.Orden))
            {
                var bg = i++ % 2 == 0 ? "#ffffff" : Slate50;
                var nombre = string.IsNullOrWhiteSpace(v.Laboratorio)
                    ? $"{v.Codigo} - {v.Nombre}"
                    : $"{v.Codigo} - {v.Nombre} ({v.Laboratorio})";
                Celda(t.Cell(), bg, nombre, bold: true, align: Align.Left);
                Celda(t.Cell(), bg, v.Animales.ToString());
                Celda(t.Cell(), bg, $"{v.CoberturaPct:F1}%", bold: true, color: TealPrimary);
                Celda(t.Cell(), bg, $"{v.ConsolidacionPct:F2}%", bold: true,
                    color: v.ConsolidacionPct >= 10 ? DangerRed : (v.ConsolidacionPct >= 5 ? WarnAmber : OkGreen));
                Celda(t.Cell(), bg, $"{v.PrevalenciaPct:F1}%");
            }
        });
    }

    private void TablaAnalisisVacunacion(IContainer container, EvaluacionResultado r)
    {
        var a = r.AnalisisVacunacion;
        container.Border(1).BorderColor(Slate300).Table(t =>
        {
            t.ColumnsDefinition(c =>
            {
                c.RelativeColumn(2.2f);
                c.ConstantColumn(90);
                c.ConstantColumn(90);
                c.ConstantColumn(110);
            });
            t.Header(h =>
            {
                CeldaHead(h.Cell(), "MÉTRICA");
                CeldaHead(h.Cell(), "ANTES");
                CeldaHead(h.Cell(), "DESPUÉS");
                CeldaHead(h.Cell(), "MEJORÍA");
            });

            void Fila(string bg, string metrica, double antes, double despues, double mejora)
            {
                Celda(t.Cell(), bg, metrica, bold: true, align: Align.Left);
                Celda(t.Cell(), bg, $"{antes:F2}%");
                Celda(t.Cell(), bg, $"{despues:F2}%");
                Celda(t.Cell(), bg, $"{(mejora > 0 ? "▼ " : mejora < 0 ? "▲ " : "")}{Math.Abs(mejora):F1}%",
                    bold: true, color: mejora > 0 ? OkGreen : mejora < 0 ? DangerRed : WarnAmber);
            }
            Fila("#ffffff", "Consolidación pulmonar", a.ConsolidacionAntes, a.ConsolidacionDespues, a.MejoraConsolidacionPct);
            Fila(Slate50, "Prevalencia neumónica", a.PrevalenciaAntes, a.PrevalenciaDespues, a.MejoraPrevalenciaPct);
        });
    }

    // =============================================================
    // TABLA POR LOTE
    // =============================================================
    private void TablaPorLote(IContainer container, EvaluacionResultado r)
    {
        var hallazgos = r.Hallazgos;
        // Ancho dinamico por hallazgo: el catalogo de enfermedades es variable, asi que ajustamos
        // el ancho de columna para que la tabla siempre quepa en la pagina (~515pt utiles) y la
        // columna LOTE (relativa) conserve espacio. Evita el desborde "conflicting size constraints".
        var hw = (float)Math.Clamp((515.0 - 70 - 160) / Math.Max(1, hallazgos.Count), 24, 54);
        container.Border(1).BorderColor(Slate300).Table(t =>
        {
            t.ColumnsDefinition(c =>
            {
                c.RelativeColumn(2);   // LOTE — toma el espacio restante
                c.ConstantColumn(48);  // animales
                c.ConstantColumn(56);  // prevalencia
                c.ConstantColumn(56);  // consolidacion
                foreach (var _ in hallazgos) c.ConstantColumn(hw);
            });
            t.Header(h =>
            {
                CeldaHead(h.Cell(), "LOTE");
                CeldaHead(h.Cell(), "ANIMALES");
                CeldaHead(h.Cell(), "PREVALENCIA");
                CeldaHead(h.Cell(), "CONSOLID. %");
                foreach (var hallazgo in hallazgos)
                    CeldaHead(h.Cell(), hallazgo.Etiqueta);
            });
            int i = 0;
            foreach (var l in r.PorLote)
            {
                var bg = i++ % 2 == 0 ? "#ffffff" : Slate50;
                Celda(t.Cell(), bg, l.Lote, bold: true, align: Align.Left);
                Celda(t.Cell(), bg, l.Animales.ToString());
                Celda(t.Cell(), bg, $"{l.PrevalenciaPct:F1}%");
                Celda(t.Cell(), bg, $"{l.ConsolidacionPct:F2}%", bold: true,
                    color: l.ConsolidacionPct >= 10 ? DangerRed : (l.ConsolidacionPct >= 5 ? WarnAmber : OkGreen));
                foreach (var hallazgo in hallazgos)
                    Celda(t.Cell(), bg, (l.Hallazgos.FirstOrDefault(x => x.Clave == hallazgo.Clave)?.Positivos ?? 0).ToString());
            }
        });
    }

    // =============================================================
    // LISTA DE TEXTO (conclusiones / recomendaciones)
    // =============================================================
    private void ListaTexto(IContainer container, List<string> items, string bulletColor)
    {
        container.Column(col =>
        {
            foreach (var item in items)
            {
                col.Item().PaddingBottom(6).Row(r =>
                {
                    r.ConstantItem(14).PaddingTop(5).Element(e => e.Width(6).Height(6).Background(bulletColor));
                    r.RelativeItem().Text(item).FontSize(10).FontColor(Slate700).LineHeight(1.4f);
                });
            }
        });
    }

    // =============================================================
    // ANEXO DE DATOS
    // =============================================================
    private void AnexoDatos(IContainer container, InformeEvaluacion meta, EvaluacionResultado r)
    {
        container.Background(Slate50).Border(1).BorderColor(Slate300).Padding(12).Column(col =>
        {
            col.Item().Text("Trazabilidad de calculo").FontSize(10).Bold().FontColor(Slate900);
            col.Item().PaddingTop(6).Text(t =>
            {
                t.Span("Identificador unico: ").FontSize(9).FontColor(Slate500);
                t.Span(meta.Id.ToString()).FontSize(9).Bold().FontColor(Slate700);
            });
            col.Item().Text(t =>
            {
                t.Span("Consecutivo: ").FontSize(9).FontColor(Slate500);
                t.Span(meta.Consecutivo).FontSize(9).Bold().FontColor(Slate700);
            });
            col.Item().Text(t =>
            {
                t.Span("Periodo: ").FontSize(9).FontColor(Slate500);
                t.Span($"{r.PeriodoDesde:yyyy-MM-dd} a {r.PeriodoHasta:yyyy-MM-dd}").FontSize(9).Bold().FontColor(Slate700);
            });
            col.Item().Text(t =>
            {
                t.Span("Registros considerados: ").FontSize(9).FontColor(Slate500);
                t.Span($"{r.TotalRegistros} registros, {r.TotalEvaluados} animales (solo FINALIZADOS)").FontSize(9).Bold().FontColor(Slate700);
            });
            col.Item().Text(t =>
            {
                t.Span("Modelo de ponderacion: ").FontSize(9).FontColor(Slate500);
                t.Span("Madec/Christensen - pesos anatomicos (AL 5%, CL 6%, DL 32%, AD 11%, CD 10%, DD 25%, AC 11%)").FontSize(9).Bold().FontColor(Slate700);
            });
            col.Item().PaddingTop(8).Text("Este informe puede auditarse cruzando los registros del periodo en el modulo Registros con el snapshot persistido.")
                .FontSize(8).FontColor(Slate500).Italic();
        });
    }

    // =============================================================
    // RENDER DE CHARTS A PNG (SkiaSharp 3.x)
    // =============================================================
    private static byte[] RenderPng(int width, int height, bool transparente, Action<SKCanvas> draw)
    {
        var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        var canvas = surface.Canvas;
        canvas.Clear(transparente ? SKColors.Transparent : SKColors.White);
        draw(canvas);
        canvas.Flush();
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private static SKColor C(string hex) => SKColor.Parse(hex);

    private static void Texto(SKCanvas canvas, string txt, float x, float y, float size, string color, SKTextAlign align, bool bold = false)
    {
        using var font = new SKFont(SKTypeface.FromFamilyName("Arial", bold ? SKFontStyle.Bold : SKFontStyle.Normal), size);
        using var paint = new SKPaint { Color = C(color), IsAntialias = true };
        canvas.DrawText(txt, x, y, align, font, paint);
    }

    private byte[] RenderBannerPng(int w, int h)
    {
        return RenderPng(w, h, false, canvas =>
        {
            using var paint = new SKPaint
            {
                IsAntialias = true,
                Shader = SKShader.CreateLinearGradient(
                    new SKPoint(0, 0), new SKPoint(w, h),
                    new[] { C("#115e59"), C("#0f766e"), C("#134e4a") },
                    new[] { 0f, 0.55f, 1f },
                    SKShaderTileMode.Clamp)
            };
            canvas.DrawRect(0, 0, w, h, paint);
            // sutil patron de circulos decorativos
            using var deco = new SKPaint { Color = C("#ffffff").WithAlpha(12), IsAntialias = true };
            canvas.DrawCircle(w * 0.85f, h * 0.2f, h * 0.5f, deco);
            canvas.DrawCircle(w * 0.95f, h * 0.9f, h * 0.35f, deco);
        });
    }

    private byte[] RenderDonutPng((string label, double pct, string color, int cnt)[] data, int totalShown, int px)
    {
        return RenderPng(px, px, true, canvas =>
        {
            var cx = px / 2f;
            var cy = px / 2f;
            var outerR = px / 2f - 8f;
            var innerR = outerR * 0.62f;
            var rect = new SKRect(cx - outerR, cy - outerR, cx + outerR, cy + outerR);

            var total = data.Sum(d => d.pct);
            if (total < 0.01) total = 100;

            float startAngle = -90f;
            foreach (var slice in data)
            {
                if (slice.pct < 0.01) continue;
                var sweep = (float)(slice.pct / total * 360.0);
                using var paint = new SKPaint { Color = C(slice.color), IsAntialias = true, Style = SKPaintStyle.Fill };
                using var path = new SKPath();
                path.MoveTo(cx, cy);
                path.ArcTo(rect, startAngle, sweep, false);
                path.Close();
                canvas.DrawPath(path, paint);
                startAngle += sweep;
            }

            using var hole = new SKPaint { Color = SKColors.White, IsAntialias = true };
            canvas.DrawCircle(cx, cy, innerR, hole);

            Texto(canvas, totalShown.ToString(), cx, cy + px * 0.03f, px * 0.16f, "#0f172a", SKTextAlign.Center, bold: true);
            Texto(canvas, "TOTAL", cx, cy + px * 0.13f, px * 0.055f, "#64748b", SKTextAlign.Center);
        });
    }

    private byte[] RenderLineaPng(List<TendenciaPunto> data, int w, int h)
    {
        return RenderPng(w, h, true, canvas =>
        {
            if (data.Count == 0) return;
            float padL = 70f, padR = 24f, padT = 24f, padB = 56f;
            var gw = w - padL - padR;
            var gh = h - padT - padB;
            var max = Math.Max(5.0, data.Max(d => d.ConsolidacionPct));
            max = Math.Ceiling(max / 5.0) * 5.0;

            using var grid = new SKPaint { Color = C("#e2e8f0"), StrokeWidth = 1.4f, IsAntialias = true };
            for (int i = 0; i <= 4; i++)
            {
                var y = padT + gh * i / 4;
                canvas.DrawLine(padL, y, padL + gw, y, grid);
                var v = max - max * i / 4;
                Texto(canvas, $"{v:F0}%", padL - 10, y + 8, 22f, "#94a3b8", SKTextAlign.Right);
            }

            float X(int i) => padL + (data.Count > 1 ? gw * i / (data.Count - 1) : gw / 2);
            float Y(double v) => padT + gh * (1 - (float)(v / max));

            using var pathLine = new SKPath();
            using var pathArea = new SKPath();
            for (int i = 0; i < data.Count; i++)
            {
                var x = X(i);
                var y = Y(data[i].ConsolidacionPct);
                if (i == 0) { pathLine.MoveTo(x, y); pathArea.MoveTo(x, padT + gh); pathArea.LineTo(x, y); }
                else { pathLine.LineTo(x, y); pathArea.LineTo(x, y); }
            }
            pathArea.LineTo(X(data.Count - 1), padT + gh);
            pathArea.Close();

            using var areaPaint = new SKPaint
            {
                IsAntialias = true,
                Shader = SKShader.CreateLinearGradient(
                    new SKPoint(0, padT), new SKPoint(0, padT + gh),
                    new[] { C("#0f766e").WithAlpha(70), C("#0f766e").WithAlpha(0) },
                    SKShaderTileMode.Clamp)
            };
            canvas.DrawPath(pathArea, areaPaint);

            using var line = new SKPaint
            {
                Color = C("#0f766e"), StrokeWidth = 4f, IsAntialias = true,
                Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeJoin = SKStrokeJoin.Round
            };
            canvas.DrawPath(pathLine, line);

            using var dot = new SKPaint { Color = C("#0f766e"), IsAntialias = true };
            using var dotEdge = new SKPaint { Color = SKColors.White, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 3f };
            for (int i = 0; i < data.Count; i++)
            {
                var x = X(i);
                var y = Y(data[i].ConsolidacionPct);
                canvas.DrawCircle(x, y, 6f, dot);
                canvas.DrawCircle(x, y, 6f, dotEdge);
                Texto(canvas, data[i].Etiqueta, x, padT + gh + 26, 19f, "#64748b", SKTextAlign.Center);
                if (data[i].Animales > 0)
                    Texto(canvas, $"{data[i].ConsolidacionPct:F1}", x, y - 14, 20f, "#0f172a", SKTextAlign.Center, bold: true);
            }
        });
    }

    // Multi-linea: evolucion de cada hallazgo a lo largo de los meses con datos.
    private byte[] RenderTendenciaHallazgosPng(EvaluacionResultado r, int w, int h)
    {
        var defs = r.TendenciaHallazgos
            .Select(h => (h.Clave, h.Etiqueta, h.Color))
            .ToArray();
        var data = r.Tendencia;
        var idxConDatos = Enumerable.Range(0, data.Count).Where(i => data[i].Animales > 0).ToList();

        return RenderPng(w, h, true, canvas =>
        {
            if (idxConDatos.Count == 0) return;
            float padL = 64f, padR = 24f, padT = 22f, padB = 96f;
            var gw = w - padL - padR;
            var gh = h - padT - padB;

            var max = 10.0;
            foreach (var d in defs)
                foreach (var i in idxConDatos)
                    max = Math.Max(max, data[i].HallazgosPct.TryGetValue(d.Clave, out var v) ? v : 0);
            max = Math.Ceiling(max / 10.0) * 10.0;

            using var grid = new SKPaint { Color = C("#e2e8f0"), StrokeWidth = 1.4f, IsAntialias = true };
            for (int g = 0; g <= 4; g++)
            {
                var y = padT + gh * g / 4;
                canvas.DrawLine(padL, y, padL + gw, y, grid);
                Texto(canvas, $"{max - max * g / 4:F0}%", padL - 10, y + 8, 20f, "#94a3b8", SKTextAlign.Right);
            }

            float X(int i) => padL + (data.Count > 1 ? gw * i / (data.Count - 1) : gw / 2);
            float Y(double v) => padT + gh * (1 - (float)(v / max));

            // etiquetas de mes (solo meses con datos para no saturar)
            foreach (var i in idxConDatos)
                Texto(canvas, data[i].Etiqueta, X(i), padT + gh + 24, 18f, "#64748b", SKTextAlign.Center);

            // una linea por hallazgo
            foreach (var d in defs)
            {
                using var line = new SKPaint
                {
                    Color = C(d.Color), StrokeWidth = 3.4f, IsAntialias = true,
                    Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeJoin = SKStrokeJoin.Round
                };
                using var path = new SKPath();
                bool first = true;
                foreach (var i in idxConDatos)
                {
                    var pct = data[i].HallazgosPct.TryGetValue(d.Clave, out var v) ? v : 0;
                    var x = X(i); var y = Y(pct);
                    if (first) { path.MoveTo(x, y); first = false; } else path.LineTo(x, y);
                }
                canvas.DrawPath(path, line);
                using var dot = new SKPaint { Color = C(d.Color), IsAntialias = true };
                foreach (var i in idxConDatos)
                {
                    var pct = data[i].HallazgosPct.TryGetValue(d.Clave, out var v) ? v : 0;
                    canvas.DrawCircle(X(i), Y(pct), 4.5f, dot);
                }
            }

            // leyenda en 2 filas
            float lx = padL, ly = padT + gh + 52;
            int col = 0;
            foreach (var d in defs)
            {
                using var sw = new SKPaint { Color = C(d.Color), IsAntialias = true };
                canvas.DrawRect(lx, ly - 11, 16, 16, sw);
                Texto(canvas, d.Etiqueta, lx + 22, ly + 2, 19f, "#475569", SKTextAlign.Left, bold: true);
                lx += 230;
                if (++col == 3) { col = 0; lx = padL; ly += 28; }
            }
        });
    }

    private byte[] RenderVacunasPng(EvaluacionResultado r, int w, int h)
    {
        var data = r.TendenciaVacunas;
        var idx = Enumerable.Range(0, data.Count).Where(i => data[i].Animales > 0).ToList();

        return RenderPng(w, h, true, canvas =>
        {
            if (idx.Count == 0) return;
            float padL = 68f, padR = 36f, padT = 22f, padB = 86f;
            var gw = w - padL - padR;
            var gh = h - padT - padB;
            var max = 100.0;

            using var grid = new SKPaint { Color = C("#e2e8f0"), StrokeWidth = 1.4f, IsAntialias = true };
            for (int g = 0; g <= 4; g++)
            {
                var y = padT + gh * g / 4;
                canvas.DrawLine(padL, y, padL + gw, y, grid);
                Texto(canvas, $"{max - max * g / 4:F0}%", padL - 10, y + 8, 20f, "#94a3b8", SKTextAlign.Right);
            }

            float X(int i) => padL + (data.Count > 1 ? gw * i / (data.Count - 1) : gw / 2);
            float Y(double v) => padT + gh * (1 - (float)(Math.Clamp(v, 0, max) / max));

            void Serie(Func<VacunaTendenciaPunto, double> valor, string color, bool dashed = false)
            {
                using var line = new SKPaint
                {
                    Color = C(color), StrokeWidth = 3.6f, IsAntialias = true,
                    Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeJoin = SKStrokeJoin.Round,
                    PathEffect = dashed ? SKPathEffect.CreateDash(new[] { 12f, 8f }, 0) : null
                };
                using var path = new SKPath();
                bool first = true;
                foreach (var i in idx)
                {
                    var x = X(i);
                    var y = Y(valor(data[i]));
                    if (first) { path.MoveTo(x, y); first = false; } else path.LineTo(x, y);
                }
                canvas.DrawPath(path, line);
                using var dot = new SKPaint { Color = C(color), IsAntialias = true };
                foreach (var i in idx)
                    canvas.DrawCircle(X(i), Y(valor(data[i])), 4.6f, dot);
            }

            Serie(t => t.CoberturaVacunadosPct, "#0f766e");
            Serie(t => t.ConsolidacionVacunadosPct, "#2563eb");
            Serie(t => t.ConsolidacionSinVacunaPct, "#dc2626", dashed: true);

            foreach (var i in idx)
                Texto(canvas, data[i].Etiqueta, X(i), padT + gh + 25, 18f, "#64748b", SKTextAlign.Center);

            var leyenda = new[]
            {
                ("Cobertura vacunal", "#0f766e"),
                ("Consolidacion vacunados", "#2563eb"),
                ("Consolidacion sin vacuna", "#dc2626")
            };
            float lx = padL, ly = padT + gh + 58;
            foreach (var item in leyenda)
            {
                using var sw = new SKPaint { Color = C(item.Item2), IsAntialias = true };
                canvas.DrawRect(lx, ly - 11, 16, 16, sw);
                Texto(canvas, item.Item1, lx + 22, ly + 2, 19f, "#475569", SKTextAlign.Left, bold: true);
                lx += 310;
            }
        });
    }

    private byte[] RenderAnalisisVacunacionPng(EvaluacionResultado r, int w, int h)
    {
        var data = r.AnalisisVacunacion.Puntos;
        return RenderPng(w, h, true, canvas =>
        {
            if (data.Count == 0) return;
            float padL = 68f, padR = 36f, padT = 30f, padB = 86f;
            var gw = w - padL - padR;
            var gh = h - padT - padB;
            var maxVal = data.SelectMany(p => new[] { p.ConsolidacionPct, p.PrevalenciaPct }).DefaultIfEmpty(0).Max();
            var max = Math.Max(10.0, Math.Ceiling(maxVal / 10.0) * 10.0);

            using var grid = new SKPaint { Color = C("#e2e8f0"), StrokeWidth = 1.4f, IsAntialias = true };
            for (int g = 0; g <= 4; g++)
            {
                var y = padT + gh * g / 4;
                canvas.DrawLine(padL, y, padL + gw, y, grid);
                Texto(canvas, $"{max - max * g / 4:F0}%", padL - 10, y + 8, 20f, "#94a3b8", SKTextAlign.Right);
            }

            float X(int i) => padL + (data.Count > 1 ? gw * i / (data.Count - 1) : gw / 2);
            float Y(double v) => padT + gh * (1 - (float)(Math.Clamp(v, 0, max) / max));

            // Marcador vertical en el inicio de vacunacion (offset 0)
            var idxInicio = data.FindIndex(p => p.OffsetMes == 0);
            if (idxInicio >= 0)
            {
                using var mk = new SKPaint
                {
                    Color = C("#0f766e"), StrokeWidth = 2.4f, IsAntialias = true,
                    Style = SKPaintStyle.Stroke, PathEffect = SKPathEffect.CreateDash(new[] { 10f, 7f }, 0)
                };
                var xm = X(idxInicio);
                canvas.DrawLine(xm, padT, xm, padT + gh, mk);
                Texto(canvas, "Inicio vacunacion", xm, padT - 10, 18f, "#0f766e", SKTextAlign.Center, bold: true);
            }

            void Serie(Func<PuntoVacunacionRelativo, double> valor, string color)
            {
                using var line = new SKPaint
                {
                    Color = C(color), StrokeWidth = 3.6f, IsAntialias = true,
                    Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeJoin = SKStrokeJoin.Round
                };
                using var path = new SKPath();
                for (int i = 0; i < data.Count; i++)
                {
                    var x = X(i); var y = Y(valor(data[i]));
                    if (i == 0) path.MoveTo(x, y); else path.LineTo(x, y);
                }
                canvas.DrawPath(path, line);
                using var dot = new SKPaint { Color = C(color), IsAntialias = true };
                for (int i = 0; i < data.Count; i++) canvas.DrawCircle(X(i), Y(valor(data[i])), 4.6f, dot);
            }

            Serie(p => p.ConsolidacionPct, "#2563eb");
            Serie(p => p.PrevalenciaPct, "#ea580c");

            for (int i = 0; i < data.Count; i++)
                Texto(canvas, data[i].Etiqueta, X(i), padT + gh + 25, 18f, "#64748b", SKTextAlign.Center);

            var leyenda = new[] { ("Consolidacion %", "#2563eb"), ("Prevalencia %", "#ea580c") };
            float lx = padL, ly = padT + gh + 58;
            foreach (var item in leyenda)
            {
                using var sw = new SKPaint { Color = C(item.Item2), IsAntialias = true };
                canvas.DrawRect(lx, ly - 11, 16, 16, sw);
                Texto(canvas, item.Item1, lx + 22, ly + 2, 19f, "#475569", SKTextAlign.Left, bold: true);
                lx += 310;
            }
        });
    }

    // =============================================================
    // UTILS
    // =============================================================
    private static (string bg, string fg) ColoresRiesgo(string nivel) => nivel switch
    {
        "CRITICO" => ("#fef2f2", "#991b1b"),
        "ALTO"    => ("#fff7ed", "#9a3412"),
        "MEDIO"   => ("#fffbeb", "#854d0e"),
        _         => ("#f0fdf4", "#166534")
    };
}
