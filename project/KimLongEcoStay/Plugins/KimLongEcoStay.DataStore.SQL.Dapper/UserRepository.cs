using System.Threading.Tasks;
using KimLongEcoStay.CoreBusiness.Models;
using KimLongEcoStay.UseCases.PluginInterfaces.DataStore;

namespace KimLongEcoStay.DataStore.SQL.Dapper
{
    public class UserRepository : IUserRepository
    {
        private readonly IDataAccess _dataAccess;

        public UserRepository(IDataAccess dataAccess)
        {
            _dataAccess = dataAccess;
        }

        public async Task<AppUser?> AuthenticateAsync(string username, string password)
        {
            try
            {
                const string sql = "SELECT * FROM dbo.AppUsers WHERE Username = @Username AND PasswordHash = @Password AND IsActive = 1";
                var user = await _dataAccess.QuerySingleAsync<AppUser, dynamic>(sql, new { Username = username, Password = password });
                if (user != null) return user;
            }
            catch
            {
                // SQL down fallback
            }

            // Fallback demo credentials
            if (username == "admin" && (password == "admin123" || password == "admin"))
                return new AppUser { Id = 1, Username = "admin", FullName = "Trần Thị Thu (CEO)", Role = "Admin", IsActive = true };
            if (username == "letan" && (password == "letan123" || password == "letan"))
                return new AppUser { Id = 2, Username = "letan", FullName = "Lễ Tân Kim Long", Role = "Staff", IsActive = true };
            if (username == "khach" && (password == "khach123" || password == "khach"))
                return new AppUser { Id = 3, Username = "khach", FullName = "Nguyễn Văn An", Role = "Customer", IsActive = true };

            return null;
        }

        public async Task<AppUser?> GetByUsernameAsync(string username)
        {
            try
            {
                const string sql = "SELECT * FROM dbo.AppUsers WHERE Username = @Username AND IsActive = 1";
                var user = await _dataAccess.QuerySingleAsync<AppUser, dynamic>(sql, new { Username = username });
                if (user != null) return user;
            }
            catch
            {
                // SQL down fallback
            }

            if (username == "admin")
                return new AppUser { Id = 1, Username = "admin", FullName = "Trần Thị Thu (CEO)", Role = "Admin", IsActive = true };
            if (username == "letan")
                return new AppUser { Id = 2, Username = "letan", FullName = "Lễ Tân Kim Long", Role = "Staff", IsActive = true };
            if (username == "khach")
                return new AppUser { Id = 3, Username = "khach", FullName = "Nguyễn Văn An", Role = "Customer", IsActive = true };

            return null;
        }
    }
}
