using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KimLongEcoStay.CoreBusiness.Models;
using KimLongEcoStay.UseCases.PluginInterfaces.DataStore;

namespace KimLongEcoStay.UseCases.AdminPortal.FinancialManagementScreen
{
    public interface IFinancialManagementUseCase
    {
        Task<FinancialDashboardData> GetDashboardDataAsync(string filterPeriod = "THIS_MONTH", DateTime? customFrom = null, DateTime? customTo = null);
        Task<IEnumerable<Expense>> GetExpensesAsync(DateTime? fromDate = null, DateTime? toDate = null, int? categoryId = null, string? status = null);
        Task<Expense?> GetExpenseByIdAsync(int id);
        Task<int> AddExpenseAsync(Expense expense);
        Task<bool> UpdateExpenseAsync(Expense expense);
        Task<bool> DeleteExpenseAsync(int id);
        Task<IEnumerable<ExpenseCategory>> GetExpenseCategoriesAsync();
        Task<IEnumerable<FinancialTransaction>> GetTransactionsAsync(DateTime? fromDate = null, DateTime? toDate = null, string? type = null);
        Task<int> AddTransactionAsync(FinancialTransaction transaction);
    }

    public class FinancialManagementUseCase : IFinancialManagementUseCase
    {
        private readonly IFinancialRepository _financialRepository;

        public FinancialManagementUseCase(IFinancialRepository financialRepository)
        {
            _financialRepository = financialRepository;
        }

        public async Task<FinancialDashboardData> GetDashboardDataAsync(string filterPeriod = "THIS_MONTH", DateTime? customFrom = null, DateTime? customTo = null)
        {
            DateTime now = DateTime.Today;
            DateTime fromDate;
            DateTime toDate;

            switch (filterPeriod.ToUpperInvariant())
            {
                case "TODAY":
                    fromDate = now;
                    toDate = now.AddDays(1).AddSeconds(-1);
                    break;
                case "LAST_7_DAYS":
                    fromDate = now.AddDays(-6);
                    toDate = now.AddDays(1).AddSeconds(-1);
                    break;
                case "LAST_MONTH":
                    var firstDayOfLastMonth = new DateTime(now.Year, now.Month, 1).AddMonths(-1);
                    fromDate = firstDayOfLastMonth;
                    toDate = firstDayOfLastMonth.AddMonths(1).AddSeconds(-1);
                    break;
                case "THIS_QUARTER":
                    int currentQuarter = (now.Month - 1) / 3 + 1;
                    fromDate = new DateTime(now.Year, (currentQuarter - 1) * 3 + 1, 1);
                    toDate = fromDate.AddMonths(3).AddSeconds(-1);
                    break;
                case "THIS_YEAR":
                    fromDate = new DateTime(now.Year, 1, 1);
                    toDate = new DateTime(now.Year, 12, 31, 23, 59, 59);
                    break;
                case "CUSTOM":
                    fromDate = customFrom ?? new DateTime(now.Year, now.Month, 1);
                    toDate = customTo ?? now.AddDays(1).AddSeconds(-1);
                    break;
                case "THIS_MONTH":
                default:
                    fromDate = new DateTime(now.Year, now.Month, 1);
                    toDate = fromDate.AddMonths(1).AddSeconds(-1);
                    break;
            }

            return await _financialRepository.GetDashboardDataAsync(fromDate, toDate, filterPeriod);
        }

        public async Task<IEnumerable<Expense>> GetExpensesAsync(DateTime? fromDate = null, DateTime? toDate = null, int? categoryId = null, string? status = null)
        {
            return await _financialRepository.GetExpensesAsync(fromDate, toDate, categoryId, status);
        }

        public async Task<Expense?> GetExpenseByIdAsync(int id)
        {
            return await _financialRepository.GetExpenseByIdAsync(id);
        }

        public async Task<int> AddExpenseAsync(Expense expense)
        {
            if (string.IsNullOrWhiteSpace(expense.VoucherCode))
            {
                expense.VoucherCode = $"PC-{DateTime.Today:yyyyMM}-{(new Random().Next(100, 999))}";
            }
            return await _financialRepository.AddExpenseAsync(expense);
        }

        public async Task<bool> UpdateExpenseAsync(Expense expense)
        {
            return await _financialRepository.UpdateExpenseAsync(expense);
        }

        public async Task<bool> DeleteExpenseAsync(int id)
        {
            return await _financialRepository.DeleteExpenseAsync(id);
        }

        public async Task<IEnumerable<ExpenseCategory>> GetExpenseCategoriesAsync()
        {
            return await _financialRepository.GetExpenseCategoriesAsync();
        }

        public async Task<IEnumerable<FinancialTransaction>> GetTransactionsAsync(DateTime? fromDate = null, DateTime? toDate = null, string? type = null)
        {
            return await _financialRepository.GetTransactionsAsync(fromDate, toDate, type);
        }

        public async Task<int> AddTransactionAsync(FinancialTransaction transaction)
        {
            if (string.IsNullOrWhiteSpace(transaction.TransactionCode))
            {
                transaction.TransactionCode = $"TX-{DateTime.Now:yyyyMMddHHmmss}";
            }
            return await _financialRepository.AddTransactionAsync(transaction);
        }
    }
}
