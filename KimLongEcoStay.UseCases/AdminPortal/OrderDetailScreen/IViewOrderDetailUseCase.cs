using KimLongEcoStay.CoreBusiness.Models;

namespace KimLongEcoStay.UseCases.AdminPortal.OrderDetailScreen
{
    public interface IViewOrderDetailUseCase
    {
        Order? Execute(int orderId);
    }
}