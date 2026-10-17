using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KimLongEcoStay.CoreBusiness.Models;
using KimLongEcoStay.UseCases.PluginInterfaces.DataStore;

namespace KimLongEcoStay.UseCases.AdminPortal.PayrollManagementScreen
{
    public interface IPayrollManagementUseCase
    {
        Task<IEnumerable<Payroll>> GetPayrollsAsync(int? month = null, int? year = null, int? departmentId = null, int? employeeId = null, string? status = null);
        Task<Payroll?> GetPayrollByIdAsync(int id);
        Task<int> AddPayrollAsync(Payroll payroll);
        Task<bool> UpdatePayrollAsync(Payroll payroll);
        Task<bool> DeletePayrollAsync(int id);
        Task<int> GenerateMonthlyPayrollAsync(int month, int year, string createdBy);
        Task<bool> ApprovePayrollAsync(int id, string approvedBy);
        Task<bool> PayPayrollAsync(int id, string paymentMethod);
    }

    public class PayrollManagementUseCase : IPayrollManagementUseCase
    {
        private readonly IPayrollRepository _payrollRepository;
        private readonly IFinancialRepository _financialRepository;

        public PayrollManagementUseCase(IPayrollRepository payrollRepository, IFinancialRepository financialRepository)
        {
            _payrollRepository = payrollRepository;
            _financialRepository = financialRepository;
        }

        public async Task<IEnumerable<Payroll>> GetPayrollsAsync(int? month = null, int? year = null, int? departmentId = null, int? employeeId = null, string? status = null)
        {
            return await _payrollRepository.GetPayrollsAsync(month, year, departmentId, employeeId, status);
        }

        public async Task<Payroll?> GetPayrollByIdAsync(int id)
        {
            return await _payrollRepository.GetPayrollByIdAsync(id);
        }

        public async Task<int> AddPayrollAsync(Payroll payroll)
        {
            payroll.CalculateTotals();
            return await _payrollRepository.AddPayrollAsync(payroll);
        }

        public async Task<bool> UpdatePayrollAsync(Payroll payroll)
        {
            payroll.CalculateTotals();
            payroll.UpdatedAt = DateTime.UtcNow;
            return await _payrollRepository.UpdatePayrollAsync(payroll);
        }

        public async Task<bool> DeletePayrollAsync(int id)
        {
            return await _payrollRepository.DeletePayrollAsync(id);
        }

        public async Task<int> GenerateMonthlyPayrollAsync(int month, int year, string createdBy)
        {
            return await _payrollRepository.GenerateMonthlyPayrollAsync(month, year, createdBy);
        }

        public async Task<bool> ApprovePayrollAsync(int id, string approvedBy)
        {
            return await _payrollRepository.ApprovePayrollAsync(id, approvedBy);
        }

        public async Task<bool> PayPayrollAsync(int id, string paymentMethod)
        {
            var payroll = await _payrollRepository.GetPayrollByIdAsync(id);
            if (payroll == null) return false;

            bool success = await _payrollRepository.PayPayrollAsync(id, paymentMethod);
            if (success)
            {
                // Tự động tạo Phiếu chi lương liên kết (tránh tính trùng)
                var categories = await _financialRepository.GetExpenseCategoriesAsync();
                int luongCatId = 1;
                foreach (var cat in categories)
                {
                    if (cat.Code == "LUONG") { luongCatId = cat.Id; break; }
                }

                var expense = new Expense
                {
                    VoucherCode = $"PC-SAL-{payroll.Month:D2}{payroll.Year}-{payroll.EmployeeCode}",
                    ExpenseDate = DateTime.Today,
                    CategoryId = luongCatId,
                    Title = $"Chi trả lương tháng {payroll.Month}/{payroll.Year} - {payroll.EmployeeName} ({payroll.EmployeeCode})",
                    Amount = payroll.NetIncome,
                    PayeeName = $"{payroll.EmployeeName} ({payroll.EmployeeCode})",
                    PaymentMethod = paymentMethod,
                    Status = "PAID",
                    CreatedBy = payroll.ApprovedBy ?? "Kế toán trưởng",
                    ApprovedBy = "Ban Giám Đốc",
                    PayrollId = payroll.Id,
                    Notes = $"Thanh toán theo bảng lương {payroll.PayrollCode}"
                };

                await _financialRepository.AddExpenseAsync(expense);
            }
            return success;
        }
    }
}
