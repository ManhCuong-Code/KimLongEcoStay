using KimLongEcoStay.CoreBusiness.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace KimLongEcoStay.UseCases.ViewProductSrceen.Interfaces
{
    public interface IViewProductUseCase
    {
        Product? Execute(int id);
    }
}
