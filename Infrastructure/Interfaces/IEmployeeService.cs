using Domain.DTOs.User.Employee;
using Domain.Filters;
using Domain.Responses;

namespace Infrastructure.Interfaces;

public interface IEmployeeService
{
    Task<PaginationResponse<List<GetEmployeeDto>>> GetEmployeesAsync(EmployeeFilter filter);
    Task<Response<GetEmployeeDto>> GetEmployeeAsync(int employeeId);
    Task<Response<string>> CreateEmployeeAsync(CreateEmployeeDto request);
    Task<Response<string>> UpdateEmployeeAsync(UpdateEmployeeDto request);
    Task<Response<string>> DeleteEmployeeAsync(int employeeId);
    Task<Response<List<ManagerSelectDto>>> GetManagersForSelectAsync();
}