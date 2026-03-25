using AuntiesCleaners.Client.Models;

namespace AuntiesCleaners.Client.Services;

public interface IReceiptService
{
    Task<List<Receipt>> GetByDateRangeAsync(DateTime from, DateTime to);
    Task<List<Receipt>> GetByWorkerAsync(Guid workerId);
    Task<Receipt> CreateAsync(Receipt receipt, byte[] photoData, string fileName);
    Task DeleteAsync(Guid receiptId);
}
