using System;
using System.Collections.Generic;
using System.Text;
using KimLongEcoStay.CoreBusiness.Models;

namespace KimLongEcoStay.UseCases.PluginInterfaces.DataStore
{
    public interface IProductRepository
    {
        IEnumerable<Product> GetProducts(string? filter = null);
        Product? GetProduct(int id);
    }
}
