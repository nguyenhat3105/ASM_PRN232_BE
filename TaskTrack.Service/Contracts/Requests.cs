using System.ComponentModel.DataAnnotations;

namespace TaskTrack.Service.Contracts;

public sealed class DepartmentRequest
{
    [Required, StringLength(100)] public string DepartmentName { get; set; } = "";
    [Required, StringLength(300)] public string DepartmentDescription { get; set; } = "";
}

public sealed class ProjectRequest : IValidatableObject
{
    [Required, StringLength(200)] public string ProjectName { get; set; } = "";
    public string? Description { get; set; }
    [Required] public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    [Range(0, 3)] public short Status { get; set; }
    [Range(1, int.MaxValue)] public int DepartmentId { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (StartDate.HasValue && EndDate < StartDate)
            yield return new ValidationResult("End date must not be before start date.", [nameof(EndDate)]);
    }
}

public sealed class TaskRequest
{
    [Required, StringLength(300)] public string Title { get; set; } = "";
    public string? Description { get; set; }
    [Range(0, 3)] public short Status { get; set; }
    [Range(0, 3)] public short Priority { get; set; } = 1;
    public DateOnly? DueDate { get; set; }
    [Range(1, int.MaxValue)] public int ProjectId { get; set; }
    public int[] TagIDs { get; set; } = [];
}

public sealed class TagRequest
{
    [Required, StringLength(50)] public string TagName { get; set; } = "";
    [RegularExpression("^#[0-9a-fA-F]{6}$", ErrorMessage = "Use a six-digit hex color, for example #3B82F6.")]
    public string? Color { get; set; }
}

public sealed class TaskFilter
{
    public string? Title { get; set; }
    [Range(0, 3)] public short? Status { get; set; }
    [Range(0, 3)] public short? Priority { get; set; }
    [Range(1, int.MaxValue)] public int? ProjectId { get; set; }
    [Range(1, int.MaxValue)] public int? TagId { get; set; }
    public bool? Overdue { get; set; }
}

public sealed class ProjectFilter
{
    public string? Name { get; set; }
    [Range(0, 3)] public short? Status { get; set; }
    [Range(1, int.MaxValue)] public int? DepartmentId { get; set; }
}

public sealed class StatusRequest
{
    [Required, Range(0, 3)] public short? Status { get; set; }
}
