using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace AuntiesCleaners.Client.Models;

[Table("work_entries")]
public class WorkEntry : BaseModel
{
    [PrimaryKey("id")] public Guid Id { get; set; }
    [Column("worker_id")] public Guid WorkerId { get; set; }
    [Column("house_id")] public Guid HouseId { get; set; }
    [Column("work_category")] public string WorkCategoryValue { get; set; } = string.Empty;
    [Column("entry_date")] public DateTime EntryDate { get; set; }
    [Column("hours_billed")] public decimal? HoursBilled { get; set; }
    [Column("number_of_loads")] public int? NumberOfLoads { get; set; }
    [Column("created_by")] public Guid CreatedBy { get; set; }
    [Column("created_at")] public DateTime CreatedAt { get; set; }
}
