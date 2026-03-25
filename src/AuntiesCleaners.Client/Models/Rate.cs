using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace AuntiesCleaners.Client.Models;

[Table("rates")]
public class Rate : BaseModel
{
    [PrimaryKey("id")] public Guid Id { get; set; }
    [Column("work_category")] public string WorkCategoryValue { get; set; } = string.Empty;
    [Column("worker_id")] public Guid? WorkerId { get; set; }
    [Column("rate_charged")] public decimal RateCharged { get; set; }
    [Column("rate_paid")] public decimal RatePaid { get; set; }
    [Column("updated_at")] public DateTime UpdatedAt { get; set; }
}
