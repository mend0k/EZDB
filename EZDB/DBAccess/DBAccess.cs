using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace EZDB.DBAccess
{
    public class DBAccess
    {
        private string _connectionString = "";
        private SqlCommand _sqlCommand = null;
        private SqlTransaction _sqlTransaction = null;
        public DBAccess(string connectionString)
            => _connectionString = connectionString;

        public bool ExecuteSqlMethod(string sqlCommandText)
            => ExecuteCommandMethod(sqlCommandText, null, CommandType.Text);

        public DataTable ExecuteSql(string sqlCommandText)
            => DefaultDataSetTable(GetDataSet(sqlCommandText, null, CommandType.Text));

        public long ExecuteSqlScalar(string sqlCommandText) 
            => ExecuteCommandScalar(sqlCommandText, null, CommandType.Text);

        public long ExecuteSqlScalar(string sqlCommandText, List<SqlParameter> sqlParameters)
            => ExecuteCommandScalar(sqlCommandText, sqlParameters, CommandType.Text);

        // can be used for stored procs
        private DataSet GetDataSet(string sqlCommandText, List<SqlParameter> sqlParameters, CommandType commandType)
        {
            var dsResult = new DataSet();
            SqlConnection sqlConnection = null;

            try
            {
                sqlConnection = GetOpenedConnection;

                _sqlCommand = new SqlCommand(sqlCommandText, sqlConnection, _sqlTransaction);
                _sqlCommand.CommandTimeout = 600;
                _sqlCommand.CommandType = commandType;
                if (sqlParameters != null)
                    _sqlCommand.Parameters.AddRange(sqlParameters.ToArray());

                var sqlDataAdapter = new SqlDataAdapter(_sqlCommand);
                sqlDataAdapter.Fill(dsResult);

                sqlConnection.Close();

            }
            catch (Exception ex)
            {
                var sb = new StringBuilder();
                string paramData = "";

                if (sqlParameters != null)
                    foreach (SqlParameter sp in sqlParameters)
                        paramData += $"Param: {sp.ParameterName}; Value: {sp.Value}";

                sb.AppendLine();
                sb.AppendLine("Function: DbAccess.GetDataSet");
                sb.AppendLine();
                sb.AppendLine($"CommandText: {sqlCommandText}");
                sb.AppendLine();
                sb.AppendLine($"CommandType: {commandType}");
                sb.AppendLine();

                ex.Data.Add("DbAccess Exception Info", sb.ToString());
                ex.Data.Add("SqlUsed", sqlCommandText);

                throw new Exception(ex.InnerException.Message, ex);
            }
            finally
            {
                if (sqlConnection.State == ConnectionState.Open)
                    sqlConnection.Close();
                DisposeSqlCommand();
            }

            return dsResult;
        }


        private bool ExecuteCommandMethod(string sqlCommandText, List<SqlParameter> sqlParameters, CommandType commandType)
        {
            var bResult = true;
            SqlConnection sqlConnection = null;

            try
            {
                sqlConnection = GetOpenedConnection;
                // harmless to pass in a null _sqlTrans
                _sqlCommand = new SqlCommand(sqlCommandText, sqlConnection, _sqlTransaction);
                _sqlCommand.CommandTimeout = 600;
                _sqlCommand.CommandType = commandType;

                if (sqlParameters != null)
                    _sqlCommand.Parameters.AddRange(sqlParameters.ToArray());

                bResult = _sqlCommand.ExecuteNonQuery() >= 0;

                sqlConnection.Close();
            }
            catch (Exception ex)
            {
                var sb = new StringBuilder();
                string paramData = "";

                if (sqlParameters != null)
                    foreach (SqlParameter sp in sqlParameters)
                        paramData += $"Param: {sp.ParameterName}; Value: {sp.Value}";

                sb.AppendLine();
                sb.AppendLine("Function: DbAccess.ExecuteSqlMethod");
                sb.AppendLine();
                sb.AppendLine($"CommandText: {sqlCommandText}");
                sb.AppendLine();
                sb.AppendLine($"CommandType: {commandType}");
                sb.AppendLine();

                ex.Data.Add("DbAccess Exception Info", sb.ToString());
                ex.Data.Add("SqlUsed", sqlCommandText);

                throw new Exception(ex.Message, ex);
            }
            finally
            {
                if (sqlConnection.State == ConnectionState.Open)
                    sqlConnection.Close();
                DisposeSqlCommand();
            }

            return bResult;
        }
        private long ExecuteCommandScalar(string sqlCommandText, List<SqlParameter> sqlParameters, CommandType commandType)
        {
            long nResult = 0;
            SqlConnection sqlConnection = null;

            try
            {
                sqlConnection = GetOpenedConnection;
                // harmless to pass in a null _sqlTrans
                _sqlCommand = new SqlCommand(sqlCommandText, sqlConnection, _sqlTransaction);
                _sqlCommand.CommandTimeout = 600;
                _sqlCommand.CommandType = commandType;

                if (sqlParameters != null)
                    _sqlCommand.Parameters.AddRange(sqlParameters.ToArray());

                nResult = Convert.ToInt64(_sqlCommand.ExecuteScalar());

                sqlConnection.Close();
            }
            catch (Exception ex)
            {
                var sb = new StringBuilder();
                string paramData = "";

                if (sqlParameters != null)
                    foreach (SqlParameter sp in sqlParameters)
                        paramData += $"Param: {sp.ParameterName}; Value: {sp.Value}";

                sb.AppendLine();
                sb.AppendLine("Function: DbAccess.ExecuteCommandScalar");
                sb.AppendLine();
                sb.AppendLine($"CommandText: {sqlCommandText}");
                sb.AppendLine();
                sb.AppendLine($"CommandType: {commandType}");
                sb.AppendLine();

                ex.Data.Add("DbAccess Exception Info", sb.ToString());
                ex.Data.Add("SqlUsed", sqlCommandText);

                throw new Exception(ex.Message, ex);
            }
            finally
            {
                if (sqlConnection.State == ConnectionState.Open)
                    sqlConnection.Close();
                DisposeSqlCommand();
            }

            return nResult;
        }

        private SqlConnection GetOpenedConnection
        {
            get
            {
                var sqlConnection = new SqlConnection(_connectionString);
                sqlConnection.Open();
                return sqlConnection;
            }
        }

        private DataTable DefaultDataSetTable(DataSet ds)
            => ds.Tables.Count > 0
                ? ds.Tables[0]
                : null;

        private void DisposeSqlCommand()
        {
            _sqlCommand.Parameters.Clear();
            _sqlCommand.Dispose();
            _sqlCommand = null;
        }
    }
}
