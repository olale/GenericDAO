using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Configuration;
using System.Threading;

namespace Support.SqlServer {
    public class ThreadSpecificConnectionFactory: IConnectionFactory {
        private static string DbName() {
            return string.Format("Test_{0}",Thread.CurrentThread.ManagedThreadId);
        }
        private static string GetConnectionString() {
            return String.Format("Server={0};Database={1};User Id={2};Password={3};",ConfigurationManager.AppSettings["Server"],
                                                                                         DbName(),
                                                                                         ConfigurationManager.AppSettings["UserId"],
                                                                                         ConfigurationManager.AppSettings["Password"]);
        }

        public SqlConnection GetSqlConnection() {
            return new SqlConnection(GetConnectionString());
        }


        public string GetDbName() {
            return DbName();
        }
    }
}
