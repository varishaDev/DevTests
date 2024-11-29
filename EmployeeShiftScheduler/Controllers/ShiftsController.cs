
using EmployeeShiftScheduler.Interfaces;
using Microsoft.AspNetCore.Mvc;



namespace EmployeeShiftScheduler.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class ShiftsController : ControllerBase
    {
        private readonly IDatabaseService _databaseService;

        public ShiftsController(IDatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        [HttpPost("IngestShifts")]
        public IActionResult IngestShifts()
        {
            string result = _databaseService.InsertShiftsFromJsonFiles();

            return Ok(new { message = result });
        }
    }
}
