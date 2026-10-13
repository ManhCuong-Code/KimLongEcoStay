using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace KimLongEcoStay.DataStore.SQL.Dapper
{
    public class DataAccess : IDataAccess
    {
        private readonly string _connectionString;

        public string ConnectionString => _connectionString;

        public DataAccess(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("KimLongEcoStayConnection")
                ?? "Server=localhost\\SQLEXPRESS;Database=KimLongEcoStayDB;Trusted_Connection=True;TrustServerCertificate=True;";
        }

        public DataAccess(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public List<T> Query<T, U>(string sql, U parameters)
        {
            using IDbConnection connection = new SqlConnection(_connectionString);
            return connection.Query<T>(sql, parameters).ToList();
        }

        public async Task<List<T>> QueryAsync<T, U>(string sql, U parameters)
        {
            using IDbConnection connection = new SqlConnection(_connectionString);
            var rows = await connection.QueryAsync<T>(sql, parameters);
            return rows.ToList();
        }

        public T? QuerySingle<T, U>(string sql, U parameters)
        {
            using IDbConnection connection = new SqlConnection(_connectionString);
            return connection.QuerySingleOrDefault<T>(sql, parameters);
        }

        public async Task<T?> QuerySingleAsync<T, U>(string sql, U parameters)
        {
            using IDbConnection connection = new SqlConnection(_connectionString);
            return await connection.QuerySingleOrDefaultAsync<T>(sql, parameters);
        }

        public int ExecuteCommand<T>(string sql, T parameters)
        {
            using IDbConnection connection = new SqlConnection(_connectionString);
            return connection.Execute(sql, parameters);
        }

        public async Task<int> ExecuteCommandAsync<T>(string sql, T parameters)
        {
            using IDbConnection connection = new SqlConnection(_connectionString);
            return await connection.ExecuteAsync(sql, parameters);
        }
    }
}