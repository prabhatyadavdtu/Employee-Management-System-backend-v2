using CoreAPI.DataBaseContext;
using CoreAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoreAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class DepartmentController : ControllerBase
    {
        private readonly APIDBContext _context;

        public DepartmentController(APIDBContext context)
        {
            _context = context;
        }
        // GET: api/department
        [HttpGet]
        public async Task<ActionResult<List<DepartmentResponse>>> GetDepartments()
        {
            var departments = await _context.Departments
                .Where(d => (bool)d.IsActive) // 👈 only active departments
                .Include(d => d.Manager)
                .GroupJoin(
                    _context.Employees.Where(e => e.IsActive),
                    d => d.DepartmentId,
                    e => e.DepartmentId,
                    (d, employees) => new DepartmentResponse
                    {
                        DepartmentId = d.DepartmentId,
                        Name = d.Name,
                        Description = d.Description,
                        ManagerId = d.ManagerId,
                        ManagerName = d.Manager != null
                            ? d.Manager.FirstName + " " + d.Manager.LastName
                            : null,
                        Budget = d.Budget,
                        EmployeeCount = employees.Count(),
                        IsActive = d.IsActive,
                        CreatedAt = d.CreatedAt,
                        UpdatedAt = d.UpdatedAt
                    }
                )
                .ToListAsync();

            return Ok(departments);
        }

        // GET: api/department/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<DepartmentResponse>> GetDepartment(int id)
        {
            var department = await _context.Departments.FirstOrDefaultAsync(d => d.DepartmentId == id);

            if (department == null)
            {
                return NotFound(new { message = "Department not found" });
            }

            var response = new DepartmentResponse
            {
                DepartmentId = department.DepartmentId,
                Name = department.Name,
                Description = department.Description,
                ManagerId = department.ManagerId,
                ManagerName = null,
                Budget = department.Budget,
                EmployeeCount = 0,
                CreatedAt = department.CreatedAt,
                UpdatedAt = department.UpdatedAt
            };

            return Ok(response);
        }

        // POST: api/department
        [HttpPost]
        public async Task<ActionResult<DepartmentResponse>> CreateDepartment([FromBody] DepartmentCreateRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var department = await _context.Departments
                .FirstOrDefaultAsync(d => d.Name == request.Name);

            if (department != null)
            {
                return BadRequest(new { message = "Department already exist with this name." });
            }

            // Manager validation removed for simplicity

            var new_department = new Department
            {
                Name = request.Name,
                Description = request.Description,
                ManagerId = request.ManagerId,
                Budget = request.Budget
            };

            _context.Departments.Add(new_department);
            await _context.SaveChangesAsync();

            var response = new DepartmentResponse
            {
                DepartmentId = new_department.DepartmentId,
                Name = new_department.Name,
                Description = new_department.Description,
                ManagerId = new_department.ManagerId,
                Budget = new_department.Budget,
                EmployeeCount = 0,
                CreatedAt = new_department.CreatedAt,
                UpdatedAt = new_department.UpdatedAt
            };

            return CreatedAtAction(nameof(GetDepartment), new { id = new_department.DepartmentId }, response);
        }

        // PUT: api/department/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<DepartmentResponse>> UpdateDepartment(int id, [FromBody] DepartmentUpdateRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var department = await _context.Departments.FindAsync(id);
            if (department == null)
            {
                return NotFound(new { message = "Department not found" });
            }

            // Manager validation removed for simplicity

            department.Name = request.Name;
            department.Description = request.Description;
            department.ManagerId = request.ManagerId;
            department.Budget = request.Budget;
            department.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var response = new DepartmentResponse
            {
                DepartmentId = department.DepartmentId,
                Name = department.Name,
                Description = department.Description,
                ManagerId = department.ManagerId,
                Budget = department.Budget,
                EmployeeCount = 0,
                CreatedAt = department.CreatedAt,
                UpdatedAt = department.UpdatedAt
            };

            return Ok(response);
        }

        // DELETE: api/department/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteDepartment(int id)
        {            
            var department = await _context.Departments
                .Include(d => d.Employees.Where(e => e.IsActive))
                .FirstOrDefaultAsync(d => d.DepartmentId == id);

            if (department == null)
            {
                return NotFound(new { message = "Department not found" });
            }

            if (department.Employees.Any())
            {
                return BadRequest(new { message = "Cannot delete department with active employees" });
            }

            department.IsActive = false;
            //_context.Departments.Remove(department);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Department deleted successfully" });
        }

        // GET: api/department
        //[HttpGet]
        //public async Task<ActionResult<List<DepartmentResponse>>> GetDepartments()
        //{
        //    var departments = await _context.Departments
        //        .Include(d => d.Manager)
        //        .Include(d => d.Employees.Where(e => e.IsActive))
        //        .Select(d => new DepartmentResponse
        //        {
        //            DepartmentId = d.DepartmentId,
        //            Name = d.Name,
        //            Description = d.Description,
        //            ManagerId = d.ManagerId,
        //            ManagerName = d.Manager != null ? $"{d.Manager.FirstName} {d.Manager.LastName}" : null,
        //            Budget = d.Budget,
        //            EmployeeCount = d.Employees.Count,
        //            CreatedAt = d.CreatedAt,
        //            UpdatedAt = d.UpdatedAt
        //        })
        //        .ToListAsync();

        //    return Ok(departments);
        //}

        //// GET: api/department/{id}
        //[HttpGet("{id}")]
        //public async Task<ActionResult<DepartmentDetailResponse>> GetDepartment(int id)
        //{
        //    var department = await _context.Departments
        //        .Include(d => d.Manager)
        //        .Include(d => d.Employees.Where(e => e.IsActive))
        //        .ThenInclude(e => e.User)
        //        .FirstOrDefaultAsync(d => d.DepartmentId == id);

        //    if (department == null)
        //    {
        //        return NotFound(new { message = "Department not found" });
        //    }

        //    var response = new DepartmentDetailResponse
        //    {
        //        DepartmentId = department.DepartmentId,
        //        Name = department.Name,
        //        Description = department.Description,
        //        ManagerId = department.ManagerId,
        //        ManagerName = department.Manager != null ? $"{department.Manager.FirstName} {department.Manager.LastName}" : null,
        //        Budget = department.Budget,
        //        EmployeeCount = department.Employees.Count,
        //        CreatedAt = department.CreatedAt,
        //        UpdatedAt = department.UpdatedAt,
        //        Employees = department.Employees.Select(e => new EmployeeSummaryResponse
        //        {
        //            EmployeeId = e.EmployeeId,
        //            FullName = $"{e.User.FirstName} {e.User.LastName}",
        //            Email = e.User.Email,
        //            Position = e.Position,
        //            DepartmentName = department.Name,
        //            IsActive = e.IsActive
        //        }).ToList()
        //    };

        //    return Ok(response);
        //}

        //// POST: api/department
        //[HttpPost]
        //public async Task<ActionResult<DepartmentResponse>> CreateDepartment([FromBody] DepartmentCreateRequest request)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return BadRequest(ModelState);
        //    }

        //    // Check if manager exists if provided
        //    if (request.ManagerId.HasValue)
        //    {
        //        var manager = await _context.Users.FindAsync(request.ManagerId.Value);
        //        if (manager == null)
        //        {
        //            return BadRequest(new { message = "Manager not found" });
        //        }
        //    }

        //    var department = new Department
        //    {
        //        Name = request.Name,
        //        Description = request.Description,
        //        ManagerId = request.ManagerId,
        //        Budget = request.Budget
        //    };

        //    _context.Departments.Add(department);
        //    await _context.SaveChangesAsync();

        //    var response = new DepartmentResponse
        //    {
        //        DepartmentId = department.DepartmentId,
        //        Name = department.Name,
        //        Description = department.Description,
        //        ManagerId = department.ManagerId,
        //        Budget = department.Budget,
        //        EmployeeCount = 0,
        //        CreatedAt = department.CreatedAt,
        //        UpdatedAt = department.UpdatedAt
        //    };

        //    return CreatedAtAction(nameof(GetDepartment), new { id = department.DepartmentId }, response);
        //}

        //// PUT: api/department/{id}
        //[HttpPut("{id}")]
        //public async Task<ActionResult<DepartmentResponse>> UpdateDepartment(int id, [FromBody] DepartmentUpdateRequest request)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return BadRequest(ModelState);
        //    }

        //    var department = await _context.Departments.FindAsync(id);
        //    if (department == null)
        //    {
        //        return NotFound(new { message = "Department not found" });
        //    }

        //    // Check if manager exists if provided
        //    if (request.ManagerId.HasValue)
        //    {
        //        var manager = await _context.Users.FindAsync(request.ManagerId.Value);
        //        if (manager == null)
        //        {
        //            return BadRequest(new { message = "Manager not found" });
        //        }
        //    }

        //    department.Name = request.Name;
        //    department.Description = request.Description;
        //    department.ManagerId = request.ManagerId;
        //    department.Budget = request.Budget;
        //    department.UpdatedAt = DateTime.UtcNow;

        //    await _context.SaveChangesAsync();

        //    var response = new DepartmentResponse
        //    {
        //        DepartmentId = department.DepartmentId,
        //        Name = department.Name,
        //        Description = department.Description,
        //        ManagerId = department.ManagerId,
        //        Budget = department.Budget,
        //        EmployeeCount = await _context.Employees.CountAsync(e => e.DepartmentId == id && e.IsActive),
        //        CreatedAt = department.CreatedAt,
        //        UpdatedAt = department.UpdatedAt
        //    };

        //    return Ok(response);
        //}

        //// DELETE: api/department/{id}
        //[HttpDelete("{id}")]
        //public async Task<ActionResult> DeleteDepartment(int id)
        //{
        //    var department = await _context.Departments
        //        .Include(d => d.Employees.Where(e => e.IsActive))
        //        .FirstOrDefaultAsync(d => d.DepartmentId == id);

        //    if (department == null)
        //    {
        //        return NotFound(new { message = "Department not found" });
        //    }

        //    if (department.Employees.Any())
        //    {
        //        return BadRequest(new { message = "Cannot delete department with active employees" });
        //    }

        //    _context.Departments.Remove(department);
        //    await _context.SaveChangesAsync();

        //    return Ok(new { message = "Department deleted successfully" });
        //}

        //// GET: api/department/{id}/employees
        //[HttpGet("{id}/employees")]
        //public async Task<ActionResult<List<EmployeeSummaryResponse>>> GetDepartmentEmployees(int id)
        //{
        //    var department = await _context.Departments.FindAsync(id);
        //    if (department == null)
        //    {
        //        return NotFound(new { message = "Department not found" });
        //    }

        //    var employees = await _context.Employees
        //        .Include(e => e.User)
        //        .Where(e => e.DepartmentId == id && e.IsActive)
        //        .Select(e => new EmployeeSummaryResponse
        //        {
        //            EmployeeId = e.EmployeeId,
        //            FullName = $"{e.User.FirstName} {e.User.LastName}",
        //            Email = e.User.Email,
        //            Position = e.Position,
        //            DepartmentName = department.Name,
        //            IsActive = e.IsActive
        //        })
        //        .ToListAsync();

        //    return Ok(employees);
        //}

        //// GET: api/department/stats
        //[HttpGet("stats")]
        //public async Task<ActionResult<List<DepartmentStats>>> GetDepartmentStats()
        //{
        //    var stats = await _context.Departments
        //        .Include(d => d.Employees.Where(e => e.IsActive))
        //        .Select(d => new DepartmentStats
        //        {
        //            DepartmentId = d.DepartmentId,
        //            DepartmentName = d.Name,
        //            EmployeeCount = d.Employees.Count,
        //            TotalSalary = d.Employees.Sum(e => e.Salary ?? 0),
        //            Budget = d.Budget ?? 0,
        //            BudgetUtilization = d.Budget > 0 ? (d.Employees.Sum(e => e.Salary ?? 0) / d.Budget.Value) * 100 : 0
        //        })
        //        .ToListAsync();

        //    return Ok(stats);
        //}
    }
}
