namespace CoreAPI.Models
{
    public class DashboardStats
    {
        public int TotalEmployees { get; set; }
        public int ActiveEmployees { get; set; }
        public int TotalDepartments { get; set; }
        public decimal TotalSalaryBudget { get; set; }
        public int NewHiresThisMonth { get; set; }
        public List<DepartmentStats> DepartmentStats { get; set; } = new List<DepartmentStats>();
        public List<RecentActivity> RecentActivities { get; set; } = new List<RecentActivity>();
    }

    public class DepartmentStats
    {
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public int EmployeeCount { get; set; }
        public decimal TotalSalary { get; set; }
        public decimal Budget { get; set; }
        public decimal BudgetUtilization { get; set; }
    }

    public class RecentActivity
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty; // "hire", "department_change", "salary_update", etc.
        public string Description { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    public class EmployeeSearchRequest
    {
        public string? SearchTerm { get; set; }
        public int? DepartmentId { get; set; }
        public bool? IsActive { get; set; }
        public string? Position { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class PaginatedResponse<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }
}
