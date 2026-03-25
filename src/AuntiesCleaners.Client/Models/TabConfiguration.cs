using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace AuntiesCleaners.Client.Models;

[Table("tab_configurations")]
public class TabConfiguration : BaseModel
{
    [PrimaryKey("id")] public Guid Id { get; set; }
    [Column("user_profile_id")] public Guid UserProfileId { get; set; }
    [Column("tab_name")] public string TabName { get; set; } = string.Empty;
    [Column("display_order")] public int DisplayOrder { get; set; }
    [Column("is_visible")] public bool IsVisible { get; set; } = true;
}
