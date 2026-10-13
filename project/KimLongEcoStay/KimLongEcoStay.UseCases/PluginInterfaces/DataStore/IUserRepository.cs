using System.Threading.Tasks;
using KimLongEcoStay.CoreBusiness.Models;

namespace KimLongEcoStay.UseCases.PluginInterfaces.DataStore
{
    public interface IUserRepository
    {
        Task<AppUser?> AuthenticateAsync(string username, string password);
        Task<AppUser?> GetByUsernameAsync(string username);
    }
}
