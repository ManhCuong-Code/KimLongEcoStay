using System;
using System.Threading.Tasks;

namespace KimLongEcoStay.UseCases.PluginInterfaces.StateStore
{
    public interface IShoppingCartStateStore : IStateStore
    {
        Task<int> GetItemsCount();
        void UpdateLineItemsCount();
        void UpdateProductQuantity();
    }
}
