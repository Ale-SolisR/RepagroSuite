namespace Rastreo.Api.Dtos;

public record LoginRequest(string Correo, string Password, bool Forzar = false);
public record LoginResponse(string Token, string Correo, string? Nombre, string Rol, int Uid, DateTime Expira);
