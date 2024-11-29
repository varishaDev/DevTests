
using Microsoft.Data.SqlClient;
using EmployeeShiftScheduler.Interfaces;
using EmployeeShiftScheduler.Models;
using Newtonsoft.Json;

namespace EmployeeShiftScheduler.DatabaseService
{
    public class DatabaseService : IDatabaseService
    {

        private readonly string _connectionString;
        private readonly string _shiftsDataFolderPath;
        public DatabaseService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _shiftsDataFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "DataSource");
        }

        public string InsertEmployeesFromJson(string filePath)
        {
            var employeesJson = File.ReadAllText(filePath);
            var employees = JsonConvert.DeserializeObject<List<Employee>>(employeesJson);

            int employeesAdded = 0;

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                foreach (var employee in employees)
                {
                    int monday = employee.Availability.Monday ? 1 : 0;
                    int tuesday = employee.Availability.Tuesday ? 1 : 0;
                    int wednesday = employee.Availability.Wednesday ? 1 : 0;
                    int thursday = employee.Availability.Thursday ? 1 : 0;

                    string insertQuery = @"
                INSERT INTO employee (Id, Name, Monday, Tuesday, Wednesday, Thursday)
                VALUES (@Id, @Name, @Monday, @Tuesday, @Wednesday, @Thursday)";

                    using (SqlCommand insertCommand = new SqlCommand(insertQuery, connection))
                    {
                        insertCommand.Parameters.AddWithValue("@Id", employee.Id);
                        insertCommand.Parameters.AddWithValue("@Name", employee.Name);
                        insertCommand.Parameters.AddWithValue("@Monday", monday);
                        insertCommand.Parameters.AddWithValue("@Tuesday", tuesday);
                        insertCommand.Parameters.AddWithValue("@Wednesday", wednesday);
                        insertCommand.Parameters.AddWithValue("@Thursday", thursday);

                        insertCommand.ExecuteNonQuery();
                        employeesAdded++; 
                    }
                }
            }

            return $"{employeesAdded} employee(s) added.";
        }


        public List<Employee> GetEmployees()
        {
            var employees = new List<Employee>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string selectQuery = "SELECT Id, Name, Monday, Tuesday, Wednesday, Thursday FROM employee";

                using (SqlCommand selectCommand = new SqlCommand(selectQuery, connection))
                using (SqlDataReader reader = selectCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var employee = new Employee
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            Name = reader.GetString(reader.GetOrdinal("Name")),
                            Availability = new Availability
                            {
                                Monday = reader.GetBoolean(reader.GetOrdinal("Monday")),
                                Tuesday = reader.GetBoolean(reader.GetOrdinal("Tuesday")),
                                Wednesday = reader.GetBoolean(reader.GetOrdinal("Wednesday")),
                                Thursday = reader.GetBoolean(reader.GetOrdinal("Thursday"))
                            }
                        };

                        employees.Add(employee);
                    }
                }
            }

            return employees;
        }


        public List<Shifts> GetShifts()
        {
            var shifts = new List<Shifts>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string query = "SELECT * FROM shifts";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            shifts.Add(new Shifts
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("id")),
                                ScheduledWorkDay = reader.GetString(reader.GetOrdinal("scheduledWorkDay"))
                            });
                        }
                    }
                }
            }
            return shifts;
        }

        public string InsertShiftsFromJsonFiles()
        {
            var jsonFiles = Directory.GetFiles(_shiftsDataFolderPath, "shifts*.json")
                                  .ToList();

            int shiftsAdded = 0;

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                // Loop through each JSON file and process it one by one
                foreach (var filePath in jsonFiles)
                {
                    var shiftsJson = File.ReadAllText(filePath);

                    var shifts = JsonConvert.DeserializeObject<List<Shifts>>(shiftsJson);

                    foreach (var shift in shifts)
                    {
                        string insertQuery = @"
                        INSERT INTO Shifts (ScheduledWorkDay)
                        VALUES (@ScheduledWorkDay)";

                        using (SqlCommand command = new SqlCommand(insertQuery, connection))
                        {
                            command.Parameters.AddWithValue("@ScheduledWorkDay", shift.ScheduledWorkDay);
                            command.ExecuteNonQuery();
                            shiftsAdded++;
                        }
                    }

                    Console.WriteLine($"Successfully processed file: {filePath}");
                }
            }

            return $"{shiftsAdded} shifts were successfully added from {jsonFiles.Count} JSON files.";
        }

        public string GenerateAndStoreSchedule()
        {
            int schedulesCreated = 0;

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                var employees = GetEmployees();  
                var shifts = GetShifts();  

                // Generate a schedule and store it in the Schedule table
                foreach (var employee in employees)
                {
                    // Get the employee's available days
                    var availableDays = new List<string>();

                    if (employee.Availability.Monday) availableDays.Add("Monday");
                    if (employee.Availability.Tuesday) availableDays.Add("Tuesday");
                    if (employee.Availability.Wednesday) availableDays.Add("Wednesday");
                    if (employee.Availability.Thursday) availableDays.Add("Thursday");

                    // If the employee has available days, assign shifts
                    if (availableDays.Count > 0)
                    {
                        foreach (var day in availableDays)
                        {
                            // Select a shift for the day
                            var shift = shifts.OrderBy(s => Guid.NewGuid()).FirstOrDefault();  // Randomly select a shift

                            if (shift != null)
                            {
                                string query = @"
                            INSERT INTO Schedule (EmployeeId, ScheduledWorkDay, ShiftId)
                            VALUES (@EmployeeId, @ScheduledWorkDay, @ShiftId)";

                                using (SqlCommand command = new SqlCommand(query, connection))
                                {
                                    command.Parameters.AddWithValue("@EmployeeId", employee.Id);
                                    command.Parameters.AddWithValue("@ScheduledWorkDay", day);
                                    command.Parameters.AddWithValue("@ShiftId", shift.Id);

                                    command.ExecuteNonQuery();
                                    schedulesCreated++;
                                }
                            }
                        }
                    }
                }
            }

            return $"{schedulesCreated} schedules were successfully generated and stored.";
        }

        public List<EmployeeSchedule> GetSchedule()
        {
            var schedules = new List<EmployeeSchedule>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                // Define the SQL query to fetch data
                var query = @"
                SELECT 
                    e.Name AS EmployeeName,
                    STRING_AGG(s.ScheduledWorkDay, ', ') AS ShiftDays,
                    COUNT(s.ShiftId) AS ShiftCount
                FROM 
                    Employee e
                LEFT JOIN 
                    Schedule s ON e.Id = s.EmployeeId
                GROUP BY 
                    e.Id, e.Name
                ORDER BY 
                    e.Name;";

                using (SqlCommand command = new SqlCommand(query, connection))
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var schedule = new EmployeeSchedule
                        {
                            EmployeeName = reader["EmployeeName"].ToString(),
                            ShiftDays = reader["ShiftDays"]?.ToString(),
                            ShiftCount = Convert.ToInt32(reader["ShiftCount"])
                        };
                        schedules.Add(schedule);
                    }
                }
            }

            return schedules;
        }
    
    }
}
