using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace AuntiesCleaners.Client.Models;

[Table("mow_list_items")]
public class MowListItem : BaseModel
{
    [PrimaryKey("id")] public Guid Id { get; set; }
    [Column("house_id")] public Guid HouseId { get; set; }
    [Column("needs_mowing")] public bool NeedsMowing { get; set; }
    [Column("updated_at")] public DateTime UpdatedAt { get; set; }
}
