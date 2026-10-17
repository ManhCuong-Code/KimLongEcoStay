using System;
using System.Collections.Generic;

namespace KimLongEcoStay.CoreBusiness.Models
{
    public class FinancialKPISummary
    {
        public decimal TotalRevenue { get; set; }
        public decimal TotalExpense { get; set; }
        public decimal EstimatedProfit => TotalRevenue - TotalExpense;
        public double ProfitMargin => TotalRevenue > 0 ? (double)(EstimatedProfit / TotalRevenue * 100) : 0;

        public decimal CurrentMonthRevenue { get; set; }
        public decimal CurrentMonthExpense { get; set; }
        public decimal CurrentMonthProfit => CurrentMonthRevenue - CurrentMonthExpense;

        public int IncomeTransactionCount { get; set; }
        public int ExpenseTransactionCount { get; set; }

        public decimal ActualCashCollected { get; set; } // Tiền thực thu
        public decimal ActualCashPaid { get; set; }      // Tiền thực chi
        public decimal TotalReceivables { get; set; }    // Khoản phải thu (50% tiền phòng còn lại chưa thanh toán)
        public decimal TotalPayables { get; set; }       // Khoản phải trả (Chi phí pending, lương chưa chi)
    }

    public class TimelinePoint
    {
        public string Label { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
        public decimal Expense { get; set; }
        public decimal Profit => Revenue - Expense;
    }

    public class CategoryBreakdownItem
    {
        public string CategoryName { get; set; } = string.Empty;
        public string CategoryCode { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public double Percentage { get; set; }
        public string ColorHex { get; set; } = "#10B981";
    }

    public class RevenueSourceBreakdownItem
    {
        public string SourceName { get; set; } = string.Empty;
        public string SourceType { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public double Percentage { get; set; }
        public string ColorHex { get; set; } = "#0D9488";
    }

    public class FinancialDashboardData
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string FilterPeriod { get; set; } = "THIS_MONTH";

        public FinancialKPISummary KPI { get; set; } = new();
        public List<TimelinePoint> Timeline { get; set; } = new();
        public List<CategoryBreakdownItem> ExpenseBreakdown { get; set; } = new();
        public List<RevenueSourceBreakdownItem> RevenueSources { get; set; } = new();
        public List<FinancialTransaction> RecentTransactions { get; set; } = new();
    }
}
