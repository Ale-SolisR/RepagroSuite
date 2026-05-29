namespace RepagroSuite.Application.Common.Interfaces;

public class TicketPdfLine
{
    public string InternalCode { get; set; } = string.Empty;
    public string? TypeName { get; set; }
    public string? Description { get; set; }
    public string? SerialNumber { get; set; }
    public string? Condition { get; set; }
}

public class TicketPdfSignature
{
    public string Label { get; set; } = string.Empty;
    public string? SignerName { get; set; }
    public string ImageBase64 { get; set; } = string.Empty;
    public string SignedAt { get; set; } = string.Empty;
}

public class TicketPdfModel
{
    public string TicketNumber { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
    public string IssuedAt { get; set; } = string.Empty;
    public string? EmployeeName { get; set; }
    public string? ResponsibleName { get; set; }
    public string? Accessories { get; set; }
    public string? Notes { get; set; }
    public List<TicketPdfLine> Lines { get; set; } = [];
    public List<TicketPdfSignature> Signatures { get; set; } = [];
    public List<string> PhotosBase64 { get; set; } = [];
}

public interface IPdfGenerator
{
    /// <summary>Genera el PDF de una boleta TI. Devuelve los bytes (para hash + almacenamiento).</summary>
    byte[] GenerateTicketPdf(TicketPdfModel model);
}
