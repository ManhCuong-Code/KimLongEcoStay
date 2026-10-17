using System.Collections.Generic;
using System.Threading.Tasks;
using KimLongEcoStay.CoreBusiness.Models;

namespace KimLongEcoStay.UseCases.PluginInterfaces.DataStore
{
    public interface IEmployeeRepository
    {
        Task<IEnumerable<Employee>> GetEmployeesAsync(string? search = null, int? departmentId = null, string? status = null, bool includeDeleted = false);
        Task<Employee?> GetEmployeeByIdAsync(int id);
        Task<Employee?> GetEmployeeByCodeAsync(string code);
        Task<int> AddEmployeeAsync(Employee employee);
        Task<bool> UpdateEmployeeAsync(Employee employee);
        Task<bool> DeleteEmployeeAsync(int id, bool softDelete = true);

        Task<IEnumerable<Department>> GetDepartmentsAsync();
        Task<IEnumerable<Position>> GetPositionsAsync(int? departmentId = null);
        Task<IEnumerable<EmployeeContract>> GetContractsAsync(int? employeeId = null, string? status = null);
        Task<int> AddContractAsync(EmployeeContract contract);
        Task<bool> UpdateContractAsync(EmployeeContract contract);
    }
}
