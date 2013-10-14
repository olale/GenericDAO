using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Configuration;
using Npgsql;

namespace Support.NpgSql
{
	public abstract class DBAccess
	{
	
		[ThreadStatic]
		protected static NpgsqlConnection _sqlConnection;

		private static NpgsqlConnection GetSqlConnection ()
		{
			if (_sqlConnection == null) {
				_sqlConnection = new NpgsqlConnection (String.Format ("Server={0};Database={1};User Id={2};Password={3};",
				                                                  ConfigurationManager.AppSettings ["Server"],
				                                                  ConfigurationManager.AppSettings ["Database"],
				                                                  ConfigurationManager.AppSettings ["UserId"],
				                                                  ConfigurationManager.AppSettings ["Password"]));
			}
			if (_sqlConnection.State == ConnectionState.Closed) {
				_sqlConnection.Open ();
			}
			return _sqlConnection;
		}

		public static void CloseConnection ()
		{
         
			if (_sqlConnection != null) {
				if (_sqlConnection.State != ConnectionState.Closed) {
					_sqlConnection.Close ();
				}
			}
         
		}

		public static NpgsqlCommand GetCommand (string commandText)
		{
			return new NpgsqlCommand () {
				CommandType=CommandType.StoredProcedure,
				CommandText=commandText,
				Connection=GetSqlConnection(),
				CommandTimeout=6000
			};
		}
	}
}