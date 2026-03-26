using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using Newtonsoft.Json;

namespace AuntiesCleaners.Client.Models;

[Table("turnover_events")]
public class TurnoverEvent : BaseModel
{
    [PrimaryKey("id")] public Guid Id { get; set; }
    [Column("house_id")] public Guid HouseId { get; set; }
    [Column("event_date")] public DateTime EventDate { get; set; }
    [Column("is_checkout")] public bool IsCheckout { get; set; }
    [Column("is_checkin")] public bool IsCheckin { get; set; }
    [Column("notes")] public string? Notes { get; set; }
    [Column("created_by")] public Guid CreatedBy { get; set; }
    [Column("created_at")] public DateTime CreatedAt { get; set; }

    [JsonIgnore]
    public bool IsFlip => IsCheckout && IsCheckin;

    [JsonIgnore]
    public string DisplayLabel => IsFlip ? "Flip" : IsCheckout ? "Out" : "In";
}
