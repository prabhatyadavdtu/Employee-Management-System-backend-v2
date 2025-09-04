using System.ComponentModel.DataAnnotations;

namespace CoreAPI.Models
{
    public class Employee
    {
        public int EmployeeId { get; set; }

        public int UserId { get; set; }

        public int? DepartmentId { get; set; }

        [Required]
        [StringLength(100)]
        public string Position { get; set; } = string.Empty;

        [Required]
        public DateTime HireDate { get; set; }

        public decimal? Salary { get; set; }

        [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(255)]
        public string? Address { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public User User { get; set; } = new();
        public Department? Department { get; set; }
    }

    public class EmployeeCreateRequest
    {
        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Company { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Position { get; set; } = string.Empty;

        [Required]
        public DateTime HireDate { get; set; }

        public decimal? Salary { get; set; }

        [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(255)]
        public string? Address { get; set; }

        public int? DepartmentId { get; set; }
    }

    public class EmployeeUpdateRequest
    {
        [Required]
        [StringLength(100)]
        public string Position { get; set; } = string.Empty;

        public decimal? Salary { get; set; }

        [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(255)]
        public string? Address { get; set; }

        public int? DepartmentId { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class EmployeeResponse
    {
        public int EmployeeId { get; set; }
        public int UserId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Company { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public DateTime HireDate { get; set; }
        public decimal? Salary { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public bool IsActive { get; set; }
        public int? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class EmployeeSummaryResponse
    {
        public int EmployeeId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string? DepartmentName { get; set; }
        public bool IsActive { get; set; }
    }

    public class EmployeeDetailResponse : EmployeeResponse
    {
        public DepartmentResponse? Department { get; set; }
    }
}
