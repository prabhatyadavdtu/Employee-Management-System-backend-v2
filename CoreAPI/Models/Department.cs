using System.ComponentModel.DataAnnotations;

namespace CoreAPI.Models
{
    public class Department
    {
        public int DepartmentId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public int? ManagerId { get; set; }

        public decimal? Budget { get; set; }

        public bool? IsActive { get; set; } = true; // default

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public User? Manager { get; set; }
        public ICollection<Employee> Employees { get; set; } = new List<Employee>();
    }

    public class DepartmentCreateRequest
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public int? ManagerId { get; set; }

        public decimal? Budget { get; set; }
    }

    public class DepartmentUpdateRequest
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public int? ManagerId { get; set; }

        public decimal? Budget { get; set; }
    }

    public class DepartmentResponse
    {
        public int DepartmentId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? ManagerId { get; set; }
        public string? ManagerName { get; set; }
        public decimal? Budget { get; set; }
        public int EmployeeCount { get; set; }
        public bool? IsActive { get; set; } 
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class DepartmentDetailResponse : DepartmentResponse
    {
        public List<EmployeeSummaryResponse> Employees { get; set; } = new List<EmployeeSummaryResponse>();
    }
}
