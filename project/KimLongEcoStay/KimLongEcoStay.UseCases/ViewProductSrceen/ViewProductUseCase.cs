using System;
using System.Collections.Generic;
using System.Text;
using KimLongEcoStay.CoreBusiness.Models;
using KimLongEcoStay.UseCases.PluginInterfaces.DataStore;
using KimLongEcoStay.UseCases.ViewProductSrceen.Interfaces;

namespace KimLongEcoStay.UseCases.ViewProductSrceen
{
    public class ViewProductUseCase : IViewProductUseCase
    {
        private readonly IProductRepository productRepository;
        public ViewProductUseCase(IProductRepository productRepository)
        {
            this.productRepository = productRepository;
        }
        public Product? Execute(int id)
        {
            return productRepository.GetProduct(id);
        }
    }
}
