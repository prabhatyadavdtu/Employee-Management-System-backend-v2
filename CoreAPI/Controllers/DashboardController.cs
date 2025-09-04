using CoreAPI.DataBaseContext;
using CoreAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoreAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly APIDBContext _context;

        public DashboardController(APIDBContext context)
        {
            _context = context;
        }

        // GET: api/dashboard/stats
        [HttpGet("stats")]
        public async Task<ActionResult<DashboardStats>> GetDashboardStats()
        {
            var totalEmployees = await _context.Employees.CountAsync(e => e.IsActive);
            var activeEmployees = await _context.Employees.CountAsync(e => e.IsActive);
            var totalDepartments = await _context.Departments.CountAsync();
            var totalSalaryBudget = await _context.Employees.Where(e => e.IsActive).SumAsync(e => e.Salary ?? 0);
            var newHiresThisMonth = await _context.Employees.CountAsync(e =>
                e.HireDate.Month == DateTime.UtcNow.Month &&
                e.HireDate.Year == DateTime.UtcNow.Year);

            // Department statistics
            var departmentStats = await _context.Departments
                .Include(d => d.Employees.Where(e => e.IsActive))
                .Select(d => new DepartmentStats
                {
                    DepartmentId = d.DepartmentId,
                    DepartmentName = d.Name,
                    EmployeeCount = d.Employees.Count,
                    TotalSalary = d.Employees.Sum(e => e.Salary ?? 0),
                    Budget = d.Budget ?? 0,
                    BudgetUtilization = d.Budget > 0 ? (d.Employees.Sum(e => e.Salary ?? 0) / d.Budget.Value) * 100 : 0
                })
                .ToListAsync();

            // Recent activities (simplified - in a real app, you'd have an activity log table)
            var recentActivities = await _context.Employees
                .Include(e => e.User)
                .Where(e => e.CreatedAt >= DateTime.UtcNow.AddDays(-30))
                .OrderByDescending(e => e.CreatedAt)
                .Take(10)
                .Select(e => new RecentActivity
                {
                    Id = e.EmployeeId,
                    Type = "hire",
                    Description = $"New employee {e.User.FirstName} {e.User.LastName} hired as {e.Position}",
                    EmployeeName = $"{e.User.FirstName} {e.User.LastName}",
                    Timestamp = e.CreatedAt
                })
                .ToListAsync();

            var stats = new DashboardStats
            {
                TotalEmployees = totalEmployees,
                ActiveEmployees = activeEmployees,
                TotalDepartments = totalDepartments,
                TotalSalaryBudget = totalSalaryBudget,
                NewHiresThisMonth = newHiresThisMonth,
                DepartmentStats = departmentStats,
                RecentActivities = recentActivities
            };

            return Ok(stats);
        }

        // GET: api/dashboard/employees-by-department
        [HttpGet("employees-by-department")]
        public async Task<ActionResult<List<object>>> GetEmployeesByDepartment()
        {
            var data = await _context.Departments
                .Include(d => d.Employees.Where(e => e.IsActive))
                .Select(d => new
                {
                    DepartmentName = d.Name,
                    EmployeeCount = d.Employees.Count,
                    TotalSalary = d.Employees.Sum(e => e.Salary ?? 0)
                })
                .ToListAsync();

            return Ok(data);
        }

        // GET: api/dashboard/salary-distribution
        [HttpGet("salary-distribution")]
        public async Task<ActionResult<object>> GetSalaryDistribution()
        {
            var salaries = await _context.Employees
                .Where(e => e.IsActive && e.Salary.HasValue)
                .Select(e => e.Salary.Value)
                .ToListAsync();

            if (!salaries.Any())
            {
                return Ok(new
                {
                    MinSalary = 0,
                    MaxSalary = 0,
                    AverageSalary = 0,
                    MedianSalary = 0,
                    SalaryRanges = new List<object>()
                });
            }

            var minSalary = salaries.Min();
            var maxSalary = salaries.Max();
            var avgSalary = salaries.Average();
            var medianSalary = salaries.OrderBy(s => s).Skip(salaries.Count / 2).First();

            // Create salary ranges
            var rangeSize = (maxSalary - minSalary) / 5;
            var salaryRanges = new List<object>();

            for (int i = 0; i < 5; i++)
            {
                var rangeStart = minSalary + (i * rangeSize);
                var rangeEnd = rangeStart + rangeSize;
                var count = salaries.Count(s => s >= rangeStart && s < rangeEnd);

                salaryRanges.Add(new
                {
                    Range = $"{rangeStart:C0} - {rangeEnd:C0}",
                    Count = count,
                    Percentage = (double)count / salaries.Count * 100
                });
            }

            return Ok(new
            {
                MinSalary = minSalary,
                MaxSalary = maxSalary,
                AverageSalary = avgSalary,
                MedianSalary = medianSalary,
                SalaryRanges = salaryRanges
            });
        }

        // GET: api/dashboard/hiring-trends
        [HttpGet("hiring-trends")]
        public async Task<ActionResult<List<object>>> GetHiringTrends()
        {
            var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);
            var hiringData = await _context.Employees
                .Where(e => e.HireDate >= sixMonthsAgo)
                .GroupBy(e => new { e.HireDate.Year, e.HireDate.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    MonthName = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMMM"),
                    HireCount = g.Count()
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToListAsync();

            return Ok(hiringData);
        }

        // GET: api/dashboard/top-departments
        [HttpGet("top-departments")]
        public async Task<ActionResult<List<object>>> GetTopDepartments()
        {
            var topDepartments = await _context.Departments
                .Include(d => d.Employees.Where(e => e.IsActive))
                .Select(d => new
                {
                    DepartmentName = d.Name,
                    EmployeeCount = d.Employees.Count,
                    TotalSalary = d.Employees.Sum(e => e.Salary ?? 0),
                    AverageSalary = d.Employees.Any() ? d.Employees.Average(e => e.Salary ?? 0) : 0,
                    BudgetUtilization = d.Budget > 0 ? (d.Employees.Sum(e => e.Salary ?? 0) / d.Budget.Value) * 100 : 0
                })
                .OrderByDescending(d => d.EmployeeCount)
                .Take(5)
                .ToListAsync();

            return Ok(topDepartments);
        }

        // GET: api/dashboard/employee-growth
        [HttpGet("employee-growth")]
        public async Task<ActionResult<List<object>>> GetEmployeeGrowth()
        {
            var growthData = await _context.Employees
                .GroupBy(e => new { e.CreatedAt.Year, e.CreatedAt.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    MonthName = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMMM"),
                    NewEmployees = g.Count(),
                    CumulativeEmployees = _context.Employees.Count(e =>
                        e.CreatedAt.Year < g.Key.Year ||
                        (e.CreatedAt.Year == g.Key.Year && e.CreatedAt.Month <= g.Key.Month))
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToListAsync();

            return Ok(growthData);
        }
    }
}
