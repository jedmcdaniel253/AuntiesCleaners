using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace AuntiesCleaners.Client.Models;

[Table("receipts")]
public class Receipt : BaseModel
{
    [PrimaryKey("id")] public Guid Id { get; set; }
    [Column("worker_id")] public Guid WorkerId { get; set; }
    [Column("receipt_date")] public DateTime ReceiptDate { get; set; }
    [Column("business_name")] public string BusinessName { get; set; } = string.Empty;
    [Column("amount")] public decimal Amount { get; set; }
    [Column("is_reimbursable")] public bool IsReimbursable { get; set; } = true;
    [Column("photo_url")] public string PhotoUrl { get; set; } = string.Empty;
    [Column("created_by")] public Guid CreatedBy { get; set; }
    [Column("created_at")] public DateTime CreatedAt { get; set; }
}
