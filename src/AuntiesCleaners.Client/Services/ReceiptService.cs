using AuntiesCleaners.Client.Models;
using static Supabase.Postgrest.Constants;

namespace AuntiesCleaners.Client.Services;

public class ReceiptService : IReceiptService
{
    private readonly ISupabaseClientService _supabase;
    private readonly IAuthService _auth;
    private const string StorageBucket = "receipts";

    public ReceiptService(ISupabaseClientService supabase, IAuthService auth)
    {
        _supabase = supabase;
        _auth = auth;
    }

    public async Task<List<Receipt>> GetByDateRangeAsync(DateTime from, DateTime to)
    {
        var response = await _supabase.Client
            .From<Receipt>()
            .Filter("receipt_date", Operator.GreaterThanOrEqual, from.Date.ToString("yyyy-MM-dd"))
            .Filter("receipt_date", Operator.LessThanOrEqual, to.Date.ToString("yyyy-MM-dd"))
            .Get();
        return response.Models;
    }

    public async Task<List<Receipt>> GetByWorkerAsync(Guid workerId)
    {
        var response = await _supabase.Client
            .From<Receipt>()
            .Filter("worker_id", Operator.Equals, workerId.ToString())
            .Order("receipt_date", Ordering.Descending)
            .Get();
        return response.Models;
    }

    public async Task<Receipt> CreateAsync(Receipt receipt, byte[] photoData, string fileName)
    {
        var profileId = await _auth.GetCurrentUserProfileIdAsync();
        if (profileId == null)
            throw new InvalidOperationException("User is not authenticated.");

        var storagePath = $"{receipt.WorkerId}/{Guid.NewGuid()}_{fileName}";
        await _supabase.Client.Storage
            .From(StorageBucket)
            .Upload(photoData, storagePath);

        var publicUrl = _supabase.Client.Storage
            .From(StorageBucket)
            .GetPublicUrl(storagePath);

        receipt.PhotoUrl = publicUrl;
        receipt.CreatedBy = profileId.Value;
        receipt.CreatedAt = DateTime.UtcNow;

        var response = await _supabase.Client.From<Receipt>().Insert(receipt);
        return response.Models.First();
    }

    public async Task DeleteAsync(Guid receiptId)
    {
        await _supabase.Client
            .From<Receipt>()
            .Filter("id", Operator.Equals, receiptId.ToString())
            .Delete();
    }
}
