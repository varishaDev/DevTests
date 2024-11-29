using EmployeeShiftScheduler.Models;

namespace EmployeeShiftScheduler.Interfaces
{
    public interface IDatabaseService
    {
        List<Employee> GetEmployees();
        string InsertEmployeesFromJson(string filePath);
        string InsertShiftsFromJsonFiles();

        string GenerateAndStoreSchedule();
        List<EmployeeSchedule> GetSchedule();
    }
}
