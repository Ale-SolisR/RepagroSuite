namespace RepagroSuite.Application.Features.Settings.DTOs;

public class SystemSettingDto
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string? DefaultValue { get; set; }
    public string? Description { get; set; }
    public string? Module { get; set; }
    public string? DataType { get; set; }
    public bool IsReadOnly { get; set; }
    public bool IsEncrypted { get; set; }
}

public class UpdateSettingDto
{
    public string? Value { get; set; }
}

public class UpdateSettingsBulkDto
{
    public Dictionary<string, string?> Settings { get; set; } = [];
}

public class TestEmailDto
{
    public string ToEmail { get; set; } = string.Empty;
}
