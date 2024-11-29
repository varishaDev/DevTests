using Microsoft.AspNetCore.Mvc;
using EmployeeShiftScheduler.Interfaces;

namespace EmployeeShiftScheduler.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ScheduleController : ControllerBase
    {
        private readonly IDatabaseService _databaseService;

        public ScheduleController(IDatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        [HttpPost("GenerateAndStoreSchedule")]
        public IActionResult GenerateAndStoreSchedule()
        {
            string result = _databaseService.GenerateAndStoreSchedule();
            return Ok(result);
        }

        [HttpGet("GetSchedule")]
        public IActionResult GetSchedule()
        {
            try
            {
                var schedules = _databaseService.GetSchedule();
                return Ok(schedules); 
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred.", error = ex.Message });
            }
        }
    }
}
