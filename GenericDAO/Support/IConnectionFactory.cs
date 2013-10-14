using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.SqlClient;

namespace Support {
    public interface IConnectionFactory {
        SqlConnection GetSqlConnection();
        string GetDbName();
    }
}
