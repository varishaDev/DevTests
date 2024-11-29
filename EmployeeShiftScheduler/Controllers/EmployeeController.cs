
using EmployeeShiftScheduler.Interfaces;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class EmployeeController : ControllerBase
{
    private readonly IDatabaseService _databaseService;

    public EmployeeController(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    [HttpPost("IngestEmployees")]
    public IActionResult IngestEmployees()
    {
        string filePath = "DataSource/employees.json";
        string result = _databaseService.InsertEmployeesFromJson(filePath);
        return Ok(result);
    }

    [HttpGet("GetEmployees")]
    public IActionResult GetEmployees()
    {
        var employees = _databaseService.GetEmployees();

        if (employees == null || !employees.Any())
        {
            return NotFound(new { message = "No employees found." });
        }

        return new JsonResult(new { success = true, data = employees });
    }
}
