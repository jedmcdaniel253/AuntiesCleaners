using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace AuntiesCleaners.Client.Models;

[Table("user_profiles")]
public class UserProfile : BaseModel
{
    [PrimaryKey("id")] public Guid Id { get; set; }
    [Column("auth_user_id")] public Guid AuthUserId { get; set; }
    [Column("worker_id")] public Guid? WorkerId { get; set; }
    [Column("name")] public string Name { get; set; } = string.Empty;
    [Column("email")] public string Email { get; set; } = string.Empty;
    [Column("role")] public string RoleValue { get; set; } = "Worker";
    [Column("is_active")] public bool IsActive { get; set; } = true;
    [Column("created_at")] public DateTime CreatedAt { get; set; }
}
