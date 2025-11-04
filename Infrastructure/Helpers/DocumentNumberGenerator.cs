using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Helpers;

public static class DocumentNumberGenerator
{
    public static async Task<string> GenerateReceiptNumberAsync(DataContext dbContext, int centerId, int year, int month, CancellationToken ct = default)
    {
        // CTR-{CenterId}-{YYYY}{MM}-{Seq6}
        var seq = await dbContext.Payments
            .AsNoTracking()
            .Where(p => p.CenterId == centerId && p.Year == year && p.Month == month)
            .LongCountAsync(ct) + 1;
        return $"CTR-{centerId}-{year}{month:00}-{seq:000000}";
    }
}


