using System.Collections.Generic;
using KimLongEcoStay.CoreBusiness.Models;

namespace KimLongEcoStay.UseCases.AdminPortal.OutstandingOrdersScreen
{
    public interface IViewOutstandingOrdersUseCase
    {
        IEnumerable<Order> Execute();
    }
}