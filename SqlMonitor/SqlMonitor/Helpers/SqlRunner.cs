using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlMonitor.Helpers
{
    public static class SqlRunner
    {
        public static readonly string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnectionString"].ConnectionString;
        public static void ExecuteSqlNonQuery(string query)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();

                using (var command = new SqlCommand(query, conn))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        public static int ExecuteSqlScalar(string query)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();

                using (var command = new SqlCommand(query, conn))
                {
                    var result = command.ExecuteScalar();
                    return (int)result;
                }
            }
        }

    }
}
