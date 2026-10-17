using System.Collections.Generic;
using System.Threading.Tasks;
using KimLongEcoStay.CoreBusiness.Models;

namespace KimLongEcoStay.UseCases.PluginInterfaces.DataStore
{
    public interface IPayrollRepository
    {
        Task<IEnumerable<Payroll>> GetPayrollsAsync(int? month = null, int? year = null, int? departmentId = null, int? employeeId = null, string? status = null);
        Task<Payroll?> GetPayrollByIdAsync(int id);
        Task<Payroll?> GetPayrollByCodeAsync(string code);
        Task<int> AddPayrollAsync(Payroll payroll);
        Task<bool> UpdatePayrollAsync(Payroll payroll);
        Task<bool> DeletePayrollAsync(int id);
        Task<int> GenerateMonthlyPayrollAsync(int month, int year, string createdBy);
        Task<bool> ApprovePayrollAsync(int id, string approvedBy);
        Task<bool> PayPayrollAsync(int id, string paymentMethod);
    }
}
