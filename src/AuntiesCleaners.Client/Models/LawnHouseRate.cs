using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace AuntiesCleaners.Client.Models;

[Table("lawn_house_rates")]
public class LawnHouseRate : BaseModel
{
    [PrimaryKey("id")] public Guid Id { get; set; }
    [Column("house_id")] public Guid HouseId { get; set; }
    [Column("worker_id")] public Guid? WorkerId { get; set; }
    [Column("rate_charged")] public decimal RateCharged { get; set; }
    [Column("rate_paid")] public decimal RatePaid { get; set; }
    [Column("updated_at")] public DateTime UpdatedAt { get; set; }
}
