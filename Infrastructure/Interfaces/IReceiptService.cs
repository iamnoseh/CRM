namespace Infrastructure.Interfaces;

public interface IReceiptService
{
    // format: "html" | "pdf"
    Task<(string receiptNumber, string url)> GenerateOrGetReceiptAsync(int paymentId, string format = "html", CancellationToken ct = default);
}
