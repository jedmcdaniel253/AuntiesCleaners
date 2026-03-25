using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace AuntiesCleaners.Client.Models;

[Table("miscellaneous_entries")]
public class MiscellaneousEntry : BaseModel
{
    [PrimaryKey("id")] public Guid Id { get; set; }
    [Column("worker_id")] public Guid WorkerId { get; set; }
    [Column("house_id")] public Guid? HouseId { get; set; }
    [Column("entry_date")] public DateTime EntryDate { get; set; }
    [Column("description")] public string Description { get; set; } = string.Empty;
    [Column("charge_amount")] public decimal ChargeAmount { get; set; }
    [Column("pay_amount")] public decimal PayAmount { get; set; }
    [Column("created_by")] public Guid CreatedBy { get; set; }
    [Column("created_at")] public DateTime CreatedAt { get; set; }
}
