using KimLongEcoStay.UseCases.PluginInterfaces.StateStore;
using KimLongEcoStay.UseCases.PluginInterfaces.UI;
using System.Threading.Tasks;

namespace KimLongEcoStay.StateStore.DI
{
    public class ShoppingCartStateStore : StateStoreBase, IShoppingCartStateStore
    {
        private readonly IShoppingCart shoppingCart;
        public ShoppingCartStateStore(IShoppingCart shoppingCart)
        {
            this.shoppingCart = shoppingCart;
        }
        public async Task<int> GetItemsCount()
        {
            var order = await shoppingCart.GetOrderAsync();
            if (order != null && order.LineItems != null && order.LineItems.Count > 0)
                return order.LineItems.Count;

            return 0;
        }
        public void UpdateLineItemsCount()
        {
            base.BroadCastStateChange();
        }
        public void UpdateProductQuantity()
        {
            base.BroadCastStateChange();
        }
    }
}
