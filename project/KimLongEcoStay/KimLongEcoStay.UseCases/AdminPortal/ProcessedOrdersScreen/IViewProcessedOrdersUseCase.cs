using System.Collections.Generic;
using KimLongEcoStay.CoreBusiness.Models;

namespace KimLongEcoStay.UseCases.AdminPortal.ProcessedOrdersScreen
{
    public interface IViewProcessedOrdersUseCase
    {
        IEnumerable<Order> Execute();
    }
}