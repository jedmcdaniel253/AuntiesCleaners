using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace AuntiesCleaners.Client.Models;

[Table("workers")]
public class Worker : BaseModel
{
    [PrimaryKey("id")] public Guid Id { get; set; }
    [Column("name")] public string Name { get; set; } = string.Empty;
    [Column("phone")] public string? Phone { get; set; }
    [Column("is_active")] public bool IsActive { get; set; } = true;
    [Column("created_at")] public DateTime CreatedAt { get; set; }
}
