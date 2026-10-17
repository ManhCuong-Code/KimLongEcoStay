using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KimLongEcoStay.CoreBusiness.Models;

namespace KimLongEcoStay.UseCases.PluginInterfaces.DataStore
{
    public interface IFinancialRepository
    {
        Task<FinancialDashboardData> GetDashboardDataAsync(DateTime fromDate, DateTime toDate, string filterPeriod = "THIS_MONTH");
        Task<IEnumerable<Expense>> GetExpensesAsync(DateTime? fromDate = null, DateTime? toDate = null, int? categoryId = null, string? status = null);
        Task<Expense?> GetExpenseByIdAsync(int id);
        Task<int> AddExpenseAsync(Expense expense);
        Task<bool> UpdateExpenseAsync(Expense expense);
        Task<bool> DeleteExpenseAsync(int id);

        Task<IEnumerable<ExpenseCategory>> GetExpenseCategoriesAsync();
        Task<IEnumerable<FinancialTransaction>> GetTransactionsAsync(DateTime? fromDate = null, DateTime? toDate = null, string? type = null);
        Task<int> AddTransactionAsync(FinancialTransaction transaction);
    }
}
