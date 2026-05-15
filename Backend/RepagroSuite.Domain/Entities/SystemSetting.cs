using RepagroSuite.Domain.Common;

namespace RepagroSuite.Domain.Entities;

public class SystemSetting : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string? DefaultValue { get; set; }
    public string? Description { get; set; }
    public string? Module { get; set; }
    public string? DataType { get; set; }
    public bool IsEncrypted { get; set; } = false;
    public bool IsReadOnly { get; set; } = false;
}
