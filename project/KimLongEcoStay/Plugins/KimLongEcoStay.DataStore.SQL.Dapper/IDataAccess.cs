using System.Collections.Generic;
using System.Threading.Tasks;

namespace KimLongEcoStay.DataStore.SQL.Dapper
{
    public interface IDataAccess
    {
        List<T> Query<T, U>(string sql, U parameters);
        Task<List<T>> QueryAsync<T, U>(string sql, U parameters);
        T? QuerySingle<T, U>(string sql, U parameters);
        Task<T?> QuerySingleAsync<T, U>(string sql, U parameters);
        int ExecuteCommand<T>(string sql, T parameters);
        Task<int> ExecuteCommandAsync<T>(string sql, T parameters);
        string ConnectionString { get; }
    }
}