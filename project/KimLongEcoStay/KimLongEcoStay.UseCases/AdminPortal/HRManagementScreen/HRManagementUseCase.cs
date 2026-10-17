using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KimLongEcoStay.CoreBusiness.Models;
using KimLongEcoStay.UseCases.PluginInterfaces.DataStore;

namespace KimLongEcoStay.UseCases.AdminPortal.HRManagementScreen
{
    public interface IHRManagementUseCase
    {
        Task<IEnumerable<Employee>> GetEmployeesAsync(string? search = null, int? departmentId = null, string? status = null, bool includeDeleted = false);
        Task<Employee?> GetEmployeeByIdAsync(int id);
        Task<int> AddEmployeeAsync(Employee employee);
        Task<bool> UpdateEmployeeAsync(Employee employee);
        Task<bool> SoftDeleteEmployeeAsync(int id);
        Task<IEnumerable<Department>> GetDepartmentsAsync();
        Task<IEnumerable<Position>> GetPositionsAsync(int? departmentId = null);
        Task<IEnumerable<EmployeeContract>> GetContractsAsync(int? employeeId = null, string? status = null);
        Task<int> AddContractAsync(EmployeeContract contract);
        Task<bool> UpdateContractAsync(EmployeeContract contract);
    }

    public class HRManagementUseCase : IHRManagementUseCase
    {
        private readonly IEmployeeRepository _employeeRepository;

        public HRManagementUseCase(IEmployeeRepository employeeRepository)
        {
            _employeeRepository = employeeRepository;
        }

        public async Task<IEnumerable<Employee>> GetEmployeesAsync(string? search = null, int? departmentId = null, string? status = null, bool includeDeleted = false)
        {
            return await _employeeRepository.GetEmployeesAsync(search, departmentId, status, includeDeleted);
        }

        public async Task<Employee?> GetEmployeeByIdAsync(int id)
        {
            return await _employeeRepository.GetEmployeeByIdAsync(id);
        }

        public async Task<int> AddEmployeeAsync(Employee employee)
        {
            // Tự sinh mã nếu chưa có
            if (string.IsNullOrWhiteSpace(employee.EmployeeCode))
            {
                var existing = await _employeeRepository.GetEmployeesAsync(includeDeleted: true);
                int count = 0;
                foreach (var _ in existing) count++;
                employee.EmployeeCode = $"KLE-NV{(count + 1):D3}";
            }

            int newId = await _employeeRepository.AddEmployeeAsync(employee);
            if (newId > 0)
            {
                // Tự động tạo hợp đồng tương ứng
                var contract = new EmployeeContract
                {
                    ContractCode = $"HD-{employee.EmployeeCode}",
                    EmployeeId = newId,
                    ContractType = employee.ContractType,
                    StartDate = employee.ContractStartDate,
                    EndDate = employee.ContractEndDate,
                    BasicSalary = employee.BaseSalary,
                    Allowance = employee.Allowance,
                    Status = "Còn hiệu lực",
                    SignedDate = employee.ContractStartDate,
                    Notes = $"Hợp đồng khởi tạo tự động cho {employee.FullName}"
                };
                await _employeeRepository.AddContractAsync(contract);
            }
            return newId;
        }

        public async Task<bool> UpdateEmployeeAsync(Employee employee)
        {
            return await _employeeRepository.UpdateEmployeeAsync(employee);
        }

        public async Task<bool> SoftDeleteEmployeeAsync(int id)
        {
            return await _employeeRepository.DeleteEmployeeAsync(id, softDelete: true);
        }

        public async Task<IEnumerable<Department>> GetDepartmentsAsync()
        {
            return await _employeeRepository.GetDepartmentsAsync();
        }

        public async Task<IEnumerable<Position>> GetPositionsAsync(int? departmentId = null)
        {
            return await _employeeRepository.GetPositionsAsync(departmentId);
        }

        public async Task<IEnumerable<EmployeeContract>> GetContractsAsync(int? employeeId = null, string? status = null)
        {
            return await _employeeRepository.GetContractsAsync(employeeId, status);
        }

        public async Task<int> AddContractAsync(EmployeeContract contract)
        {
            return await _employeeRepository.AddContractAsync(contract);
        }

        public async Task<bool> UpdateContractAsync(EmployeeContract contract)
        {
            return await _employeeRepository.UpdateContractAsync(contract);
        }
    }
}
