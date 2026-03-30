using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace AuntiesCleaners.Client.Models;

[Table("houses")]
public class House : BaseModel
{
    [PrimaryKey("id")] public Guid Id { get; set; }
    [Column("name")] public string Name { get; set; } = string.Empty;
    [Column("is_multiple_houses")] public bool IsMultipleHouses { get; set; }
    [Column("is_active")] public bool IsActive { get; set; } = true;
    [Column("created_at")] public DateTime CreatedAt { get; set; }
    [Column("owner_id")] public Guid? OwnerId { get; set; }
}
