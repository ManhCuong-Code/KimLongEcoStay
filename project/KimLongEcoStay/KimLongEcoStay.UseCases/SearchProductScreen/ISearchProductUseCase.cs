using KimLongEcoStay.CoreBusiness.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace KimLongEcoStay.UseCases.SearchProductScreen
{
    public interface ISearchProductUseCase
    {
        IEnumerable<Product> Execute(string? filter = null);
    }
}
