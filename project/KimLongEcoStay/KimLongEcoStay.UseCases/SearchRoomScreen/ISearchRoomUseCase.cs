using KimLongEcoStay.CoreBusiness.Models;
using System.Collections.Generic;

namespace KimLongEcoStay.UseCases.SearchRoomScreen
{
    public interface ISearchRoomUseCase
    {
        IEnumerable<Product> Execute(string? filter = null);
    }
}
