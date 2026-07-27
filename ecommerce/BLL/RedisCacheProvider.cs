using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL
{
    public static class RedisCacheProvider
    {
        private static readonly Lazy<ConnectionMultiplexer> _connection = new Lazy<ConnectionMultiplexer>(() =>
        {
            var connString = System.Configuration.ConfigurationManager.AppSettings["Redis:ConnectionString"];
            return ConnectionMultiplexer.Connect(connString);
        });

        public static IDatabase Db => _connection.Value.GetDatabase();
    }
}