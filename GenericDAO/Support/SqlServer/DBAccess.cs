using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;


namespace Support.SqlServer {

    public abstract class DBAccess {
        private static IConnectionFactory ConnectionFactory {
            get;
            set;
        }

        public static bool IsTest {get {return ConfigurationManager.AppSettings["Test"]=="True"; }}

        static DBAccess() {
            ConnectionFactory=new AppSettingConnectionFactory() as IConnectionFactory;
        }

        #region "declarations"

        [ThreadStatic]
        private static SqlTransaction _sqlTransaction;

        #endregion

        [ThreadStatic]
        protected static SqlConnection _sqlConnection;

        #region "Functions/Sub"

        public static SqlConnection GetSqlConnection() {
            if(_sqlConnection==null) {
                _sqlConnection=ConnectionFactory.GetSqlConnection();
            }
            if(_sqlConnection.State==ConnectionState.Closed) {
                _sqlConnection.Open();
            }
            return _sqlConnection;
        }


        public static void CloseConnection() {
            if(_sqlTransaction==null) // Do not close connection if there is a transaction.
            {
                if(_sqlConnection!=null) {
                    if(_sqlConnection.State!=ConnectionState.Closed) {
                        _sqlConnection.Close();
                    }
                }
            }
        }

        public static SqlCommand GetCommand(string commandText) {
            return new SqlCommand() {
                CommandType=CommandType.StoredProcedure,
                CommandText=commandText,
                Connection=GetSqlConnection(),
                Transaction=_sqlTransaction,
                CommandTimeout=6000
            };
        }

        #region "Transactions"
        /*
         * BeginTransaction must always be followed by CommitTransaction or, if something goes wrong, RollbackTransaction.
         * Example usage:
        /* 
            try
            {
                TransactioToken token = this.GetDA().BeginTransaction("MyTransaction");
                this.GetDA().SomeOperation(token);
                this.GetDA().CommitTransaction();
            }
            catch (System.Exception)
            {
                this.GetDA().RollbackTransaction();
                throw;
            }
        */


        public static TransactionToken BeginTransaction(string transactionName) {
            if(_sqlTransaction!=null) {
                throw new InvalidOperationException("A transaction is already started.");
            } else {
                // To start a transaction, a connection must be open.
                _sqlTransaction=GetSqlConnection().BeginTransaction(IsolationLevel.Serializable,ForceStringToMax32Characters(transactionName)); // SQL Server requires that this string is no more than 32 characters.
                return new TransactionToken();
            }
        }

        private static string ForceStringToMax32Characters(string s) {
            return s.Length>32?s.Substring(0,
                                               32):s;
        }

        // Use RequireTransaction to force all callers of a method to call BeginTransction before using it.
        public void RequireTransaction(TransactionToken token) {
            if(!TransactionIsStarted()||token==null) {
                throw new InvalidOperationException("You must call BeginTransaction before calling this method");
            }
        }

        private static bool TransactionIsStarted() {
            return (_sqlTransaction!=null);
        }

        public static void CommitTransaction() {
            if(_sqlTransaction==null) {
                throw new InvalidOperationException("Can not commit because no transaction is started.");
            }

            _sqlTransaction.Commit();
            _sqlTransaction.Dispose();
            _sqlTransaction=null;

            ResetIsolationLevel();

            CloseConnection();
        }

        private static void ResetIsolationLevel() {
            using(SqlTransaction transaction=_sqlConnection.BeginTransaction(IsolationLevel.ReadCommitted,
                                                                               "Set Isolation Level")) {
                transaction.Commit();
            }
        }

        public static void RollbackTransaction() {
            if(_sqlTransaction!=null) {
                _sqlTransaction.Rollback();
                _sqlTransaction.Dispose();
                _sqlTransaction=null;

                ResetIsolationLevel();

                CloseConnection();
            }
        }

        #endregion

        #endregion

    }

    // The idea behind TransactionToken is this:
    // If a method Foo() should always be run within a transaction, this can be guaranteed by adding a call to RequireTransaction() at the start of the method.
    // RequireTransaction() takes one argument, a TransactionToken. A TransactionToken can only be created by calling BeginTransaction().
    // Foo() does not have to start the transaction, it can simply take a TransactionToken as a parameter, thus making the caller responsible of starting the transaction.
    public class TransactionToken {
        internal TransactionToken() {
        } // Constructor is internal so that no one outside this file can create a TransactionToken.
    }

}
