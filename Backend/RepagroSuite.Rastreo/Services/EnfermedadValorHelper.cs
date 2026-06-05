using Rastreo.Api.Models;

namespace Rastreo.Api.Services;

public static class EnfermedadValorHelper
{
    private static readonly Dictionary<string, string> Colors = new()
    {
        ["SPES"] = "#0369a1",
        ["PERICARDITIS"] = "#dc2626",
        ["PERICARDIO_ENGROSADO"] = "#a855f7",
        ["ABSCESO_NODULO"] = "#ea580c",
        ["MOCO"] = "#0891b2",
    };

    public static string Color(string codigo, int index)
    {
        if (Colors.TryGetValue(codigo, out var color)) return color;
        var palette = new[] { "#0f766e", "#7c3aed", "#db2777", "#ca8a04", "#16a34a", "#2563eb", "#9333ea", "#be123c" };
        return palette[Math.Abs(index) % palette.Length];
    }

    public static EnfermedadValor Valor(RegistroDetalle d, Enfermedad e)
    {
        var dyn = d.EnfermedadesValores.FirstOrDefault(v => v.EnfermedadId == e.Id);
        if (dyn != null)
        {
            return new EnfermedadValor(
                dyn.ValorNumero,
                dyn.ValorBooleano,
                dyn.ValorTexto
            );
        }

        return e.Codigo switch
        {
            "SPES" => new EnfermedadValor(d.SPES, null, null),
            "ABSCESO_NODULO" => new EnfermedadValor(null, d.AbscesoNodulo, null),
            "PERICARDIO_ENGROSADO" => new EnfermedadValor(null, d.PericardioEngrosado, null),
            "PERICARDITIS" => new EnfermedadValor(null, d.Pericarditis, null),
            "AGUDA_CRONICA" => new EnfermedadValor(null, null, d.AgudaCronica),
            "MOCO" => new EnfermedadValor(null, d.Moco, null),
            _ => new EnfermedadValor(null, e.TipoCampo == "BOOL" ? false : null, null)
        };
    }

    public static string Texto(RegistroDetalle d, Enfermedad e)
    {
        var v = Valor(d, e);
        return e.TipoCampo switch
        {
            "SCORE_0_4" => (v.Numero ?? 0).ToString(),
            "BOOL" => (v.Booleano ?? false) ? "1" : "0",
            "AGUDA_CRONICA" => string.IsNullOrWhiteSpace(v.Texto) ? "" : v.Texto!,
            _ => ""
        };
    }

    public static bool Positivo(RegistroDetalle d, Enfermedad e)
    {
        var v = Valor(d, e);
        return e.TipoCampo switch
        {
            "SCORE_0_4" => (v.Numero ?? 0) > 0,
            "BOOL" => v.Booleano ?? false,
            "AGUDA_CRONICA" => !string.IsNullOrWhiteSpace(v.Texto),
            _ => false
        };
    }
}

public record EnfermedadValor(byte? Numero, bool? Booleano, string? Texto);
