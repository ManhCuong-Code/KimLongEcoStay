using KimLongEcoStay.CoreBusiness.Models;

namespace KimLongEcoStay.UseCases.AdminPortal.ProcessOrderScreen
{
    public interface IProcessOrderUseCase
    {
        bool Execute(int orderId, string adminUserName);
    }
}