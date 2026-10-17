using KimLongEcoStay.CoreBusiness.Models;

namespace KimLongEcoStay.CoreBusiness.Services
{
    public interface IOrderService
    {
        bool ValidateCreateOrder(Order order);
        bool ValidateCustomerInformation(string? name, string? address, string? city, string? province, string? country);
        bool ValidateProcessOrder(Order order);
        bool ValidateUpdateOrder(Order order);
    }
}