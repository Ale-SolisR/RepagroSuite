using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RepagroSuite.Application.Common.Interfaces;
using RepagroSuite.Domain.Common;
using RepagroSuite.Infrastructure.Data;

namespace RepagroSuite.Infrastructure.Services;

/// <summary>
/// Consecutivo seguro: incrementa la fila (tipo, año) tomando lock exclusivo de fila durante
/// la transacción actual; si no existe la crea. El UPDATE serializa los accesos concurrentes,
/// el índice único evita duplicados. Debe correr dentro de la transacción de la boleta.
/// </summary>
public class SequenceGenerator : ISequenceGenerator
{
    private readonly ApplicationDbContext _context;

    public SequenceGenerator(ApplicationDbContext context) => _context = context;

    public async Task<string> NextTicketNumberAsync(string typeCode, CancellationToken cancellationToken = default)
    {
        var year = BusinessClock.Now.Year;

        const string sql = @"
SET NOCOUNT ON;
UPDATE TI_ConsecutivosDocumento SET UltimoNumero = UltimoNumero + 1
 WHERE CodigoTipo = @code AND Anio = @year AND EliminadoLogico = 0;
IF @@ROWCOUNT = 0
    INSERT INTO TI_ConsecutivosDocumento (Id, CodigoTipo, Anio, Prefijo, UltimoNumero, CreadoEn, EliminadoLogico)
    VALUES (NEWID(), @code, @year, 'TI', 1, SYSUTCDATETIME(), 0);
SELECT CAST(UltimoNumero AS bigint) AS Value FROM TI_ConsecutivosDocumento
 WHERE CodigoTipo = @code AND Anio = @year AND EliminadoLogico = 0;";

        var result = await _context.Database
            .SqlQueryRaw<long>(sql, new SqlParameter("@code", typeCode), new SqlParameter("@year", year))
            .ToListAsync(cancellationToken);

        var next = result.First();
        return $"TI-{typeCode}-{year}-{next:D6}";
    }
}
