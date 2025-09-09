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
    public class EmployeeController : ControllerBase
    {
        private readonly APIDBContext _context;

        public EmployeeController(APIDBContext context)
        {
            _context = context;
        }

        // GET: api/employee
        [HttpGet]
        public async Task<ActionResult<PaginatedResponse<EmployeeResponse>>> GetEmployees([FromQuery] EmployeeSearchRequest request)
        
        {
            var query = _context.Employees
                .Include(e => e.User)
                .Include(e => e.Department)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                query = query.Where(e =>
                    e.User.FirstName.Contains(request.SearchTerm) ||
                    e.User.LastName.Contains(request.SearchTerm) ||
                    e.User.Email.Contains(request.SearchTerm) ||
                    e.Position.Contains(request.SearchTerm));
            }

            if (request.DepartmentId.HasValue)
            {
                query = query.Where(e => e.DepartmentId == request.DepartmentId.Value);
            }

            if (request.IsActive.HasValue)
            {
                query = query.Where(e => e.IsActive == request.IsActive.Value);
            }

            if (!string.IsNullOrEmpty(request.Position))
            {
                query = query.Where(e => e.Position == request.Position);
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

            var employees = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(e => new EmployeeResponse
                {
                    EmployeeId = e.EmployeeId,
                    UserId = e.UserId,
                    FirstName = e.User.FirstName,
                    LastName = e.User.LastName,
                    Email = e.User.Email,
                    Company = e.User.Company,
                    Position = e.Position,
                    HireDate = e.HireDate,
                    Salary = e.Salary,
                    Phone = e.Phone,
                    Address = e.Address,
                    IsActive = e.IsActive,
                    DepartmentId = e.DepartmentId,
                    DepartmentName = e.Department != null ? e.Department.Name : null,
                    CreatedAt = e.CreatedAt,
                    UpdatedAt = e.UpdatedAt
                })
                .ToListAsync();

            var response = new PaginatedResponse<EmployeeResponse>
            {
                Items = employees,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalPages = totalPages,
                HasNextPage = request.Page < totalPages,
                HasPreviousPage = request.Page > 1
            };

            return Ok(response);
        }

        // GET: api/employee/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<EmployeeDetailResponse>> GetEmployee(int id)
        {
            var employee = await _context.Employees
                .Include(e => e.User)
                .Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.EmployeeId == id);

            if (employee == null)
            {
                return NotFound(new { message = "Employee not found" });
            }

            var response = new EmployeeDetailResponse
            {
                EmployeeId = employee.EmployeeId,
                UserId = employee.UserId,
                FirstName = employee.User.FirstName,
                LastName = employee.User.LastName,
                Email = employee.User.Email,
                Company = employee.User.Company,
                Position = employee.Position,
                HireDate = employee.HireDate,
                Salary = employee.Salary,
                Phone = employee.Phone,
                Address = employee.Address,
                IsActive = employee.IsActive,
                DepartmentId = employee.DepartmentId,
                DepartmentName = employee.Department?.Name,
                CreatedAt = employee.CreatedAt,
                UpdatedAt = employee.UpdatedAt,
                Department = employee.Department != null ? new DepartmentResponse
                {
                    DepartmentId = employee.Department.DepartmentId,
                    Name = employee.Department.Name,
                    Description = employee.Department.Description,
                    ManagerId = employee.Department.ManagerId,
                    Budget = employee.Department.Budget,
                    EmployeeCount = await _context.Employees.CountAsync(e => e.DepartmentId == employee.DepartmentId && e.IsActive),
                    CreatedAt = employee.Department.CreatedAt,
                    UpdatedAt = employee.Department.UpdatedAt
                } : null
            };

            return Ok(response);
        }

        // POST: api/employee
        [HttpPost]
        public async Task<ActionResult<EmployeeResponse>> CreateEmployee([FromBody] EmployeeCreateRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // Check if email already exists
                if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                {
                    return BadRequest(new { message = "Email already exists" });
                }

                // Check if department exists if provided
                if (request.DepartmentId.HasValue)
                {
                    var department = await _context.Departments.FindAsync(request.DepartmentId.Value);
                    if (department == null)
                    {
                        return BadRequest(new { message = "Department not found" });
                    }
                }

                // Create user
                var user = new User
                {
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Email = request.Email,
                    Company = request.Company,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("temp123"),
                    Role = "User"
                };

                // Create employee (EF Core will handle the relationship)
                var employee = new Employee
                {
                    User = user, // Set navigation property instead of UserId
                    DepartmentId = request.DepartmentId,
                    Position = request.Position,
                    HireDate = request.HireDate,
                    Salary = request.Salary,
                    Phone = request.Phone,
                    Address = request.Address
                };

                _context.Users.Add(user);
                _context.Employees.Add(employee);

                // Single SaveChanges - EF Core handles the transaction internally
                await _context.SaveChangesAsync();

                var response = new EmployeeResponse
                {
                    EmployeeId = employee.EmployeeId,
                    UserId = employee.UserId,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    Company = user.Company,
                    Position = employee.Position,
                    HireDate = employee.HireDate,
                    Salary = employee.Salary,
                    Phone = employee.Phone,
                    Address = employee.Address,
                    IsActive = employee.IsActive,
                    DepartmentId = employee.DepartmentId,
                    CreatedAt = employee.CreatedAt,
                    UpdatedAt = employee.UpdatedAt
                };

                return CreatedAtAction(nameof(GetEmployee), new { id = employee.EmployeeId }, response);
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("unique") == true)
            {
                return BadRequest(new { message = "Email already exists" });
            }
        }

        // PUT: api/employee/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<EmployeeResponse>> UpdateEmployee(int id, [FromBody] EmployeeUpdateRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var employee = await _context.Employees
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.EmployeeId == id);

            if (employee == null)
            {
                return NotFound(new { message = "Employee not found" });
            }

            // Check if department exists if provided
            if (request.DepartmentId.HasValue)
            {
                var department = await _context.Departments.FindAsync(request.DepartmentId.Value);
                if (department == null)
                {
                    return BadRequest(new { message = "Department not found" });
                }
            }

            employee.Position = request.Position;
            employee.Salary = request.Salary;
            employee.Phone = request.Phone;
            employee.Address = request.Address;
            employee.DepartmentId = request.DepartmentId;
            employee.IsActive = request.IsActive;
            employee.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var response = new EmployeeResponse
            {
                EmployeeId = employee.EmployeeId,
                UserId = employee.UserId,
                FirstName = employee.User.FirstName,
                LastName = employee.User.LastName,
                Email = employee.User.Email,
                Company = employee.User.Company,
                Position = employee.Position,
                HireDate = employee.HireDate,
                Salary = employee.Salary,
                Phone = employee.Phone,
                Address = employee.Address,
                IsActive = employee.IsActive,
                DepartmentId = employee.DepartmentId,
                CreatedAt = employee.CreatedAt,
                UpdatedAt = employee.UpdatedAt
            };

            return Ok(response);
        }

        // DELETE: api/employee/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteEmployee(int id)
        {
            var employee = await _context.Employees
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.EmployeeId == id);

            if (employee == null)
            {
                return NotFound(new { message = "Employee not found" });
            }

            // Soft delete - mark as inactive
            employee.IsActive = false;
            employee.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Employee deactivated successfully" });
        }

        // GET: api/employee/search
        [HttpGet("search")]
        public async Task<ActionResult<List<EmployeeSummaryResponse>>> SearchEmployees([FromQuery] string term)
        {
            if (string.IsNullOrEmpty(term))
            {
                return BadRequest(new { message = "Search term is required" });
            }

            var employees = await _context.Employees
                .Include(e => e.User)
                .Include(e => e.Department)
                .Where(e => e.IsActive && (
                    e.User.FirstName.Contains(term) ||
                    e.User.LastName.Contains(term) ||
                    e.User.Email.Contains(term) ||
                    e.Position.Contains(term)))
                .Select(e => new EmployeeSummaryResponse
                {
                    EmployeeId = e.EmployeeId,
                    FullName = $"{e.User.FirstName} {e.User.LastName}",
                    Email = e.User.Email,
                    Position = e.Position,
                    DepartmentName = e.Department != null ? e.Department.Name : null,
                    IsActive = e.IsActive
                })
                .Take(10)
                .ToListAsync();

            return Ok(employees);
        }

        // GET: api/employee/positions
        [HttpGet("positions")]
        public async Task<ActionResult<List<string>>> GetPositions()
        {
            var positions = await _context.Employees
                .Where(e => e.IsActive)
                .Select(e => e.Position)
                .Distinct()
                .OrderBy(p => p)
                .ToListAsync();

            return Ok(positions);
        }

        // GET: api/employee/stats
        [HttpGet("stats")]
        public async Task<ActionResult<object>> GetEmployeeStats()
        {
            var totalEmployees = await _context.Employees.CountAsync(e => e.IsActive);
            var totalSalary = await _context.Employees.Where(e => e.IsActive).SumAsync(e => e.Salary ?? 0);
            var avgSalary = await _context.Employees.Where(e => e.IsActive && e.Salary.HasValue).AverageAsync(e => e.Salary.Value);
            var newHiresThisMonth = await _context.Employees.CountAsync(e => e.HireDate.Month == DateTime.UtcNow.Month && e.HireDate.Year == DateTime.UtcNow.Year);

            var stats = new
            {
                TotalEmployees = totalEmployees,
                TotalSalary = totalSalary,
                AverageSalary = avgSalary,
                NewHiresThisMonth = newHiresThisMonth
            };

            return Ok(stats);
        }
    }
}
