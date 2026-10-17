using KimLongEcoStay.CoreBusiness.Models;

namespace KimLongEcoStay.UseCases.ViewRoomScreen
{
    public interface IViewRoomUseCase
    {
        Product? Execute(int id);
    }
}
