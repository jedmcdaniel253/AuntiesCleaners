using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace AuntiesCleaners.Client.Models;

[Table("owner")]
public class Owner : BaseModel
{
    [PrimaryKey("id")] public Guid Id { get; set; }
    [Column("name")] public string Name { get; set; } = string.Empty;
    [Column("email")] public string Email { get; set; } = string.Empty;
    [Column("phone")] public string Phone { get; set; } = string.Empty;
    [Column("updated_at")] public DateTime UpdatedAt { get; set; }
}
