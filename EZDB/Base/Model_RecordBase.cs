using EZDB.Utilities;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace EZDB.Base
{
    public class Model_RecordBase : Model_Base
    {
        public Model_RecordOverrides Model_Overrides = new Model_RecordOverrides();
        public Model_RecordBase() : base() { }

        #region Basic CRUD Methods
        /// <summary>
        /// Inserts the current model instance as a record into the database.
        /// </summary>
        /// <returns>Boolean representing insert result</returns>
        public virtual bool AddRecord()
        {
            DBAccess.DBAccess db = GetDB();
            var sql = new SQLGenerator(this).CreateSqlInsertStatement();
            var bResult = false;
            try
            {
                // I use type of "long" for to account for sql server failures
                // as sql unexpected failure will cause server saves to jump 1000?
                // see: https://stackoverflow.com/questions/14162648/sql-server-2012-column-identity-increment-jumping-from-6-to-1000-on-7th-entry/14162761#14162761
                long newRecordIdLong = db.ExecuteSqlScalar(sql);
                int newRecordIdInt = (int)(newRecordIdLong);


                if (newRecordIdLong > 0)
                {
                    if (PrimaryKeyProperty().PropertyType.Equals(typeof(int)))
                        PrimaryKeyProperty().SetValue(this, newRecordIdInt);
                    else
                        PrimaryKeyProperty().SetValue(this, newRecordIdLong);
                    bResult = true;
                }
                else
                {
                    PrimaryKeyProperty().SetValue(this, -1);
                    bResult = false;
                }

                if (!bResult)
                    throw new Exception("Record could not be added because no new ID came back from database.");
            }
            catch (Exception ex)
            {
                if (ex.Data.Contains("Function"))
                    ex.Data.Add("Function", "Model_RecordBase.AddRecord");
                if (ex.Data.Contains("FailureDescription"))
                    ex.Data.Add("FailureDescription", $"Failed adding {Model_EntityName} record.");

                OnExceptionEvent(ex, sql);
            }
            return bResult;
        }

        /// <summary>
        /// Loads all records for the specific model type.
        /// </summary>
        /// <param name="sOrderBy">Column to order the return list by</param>
        /// <returns>A collection of models</returns>
        public virtual Collection<dynamic> SelectAll(string sOrderBy = "")
            => SelectWhereOrderBy(sOrderBy: sOrderBy);

        /// <summary>
        /// Returns an arbitrary set of model records matching the where clause. 
        /// </summary>
        /// <param name="sWhere">Sql where statement. Do not include the "WHERE =" keyword and operator.</param>
        /// <returns>A collection of models</returns>
        public Collection<dynamic> SelectWhereOrderBy(string sWhere = "", string sOrderBy = "")
        {
            Collection<dynamic> lstReturn = null;
            try
            {
                DBAccess.DBAccess db = GetDB();
                var sql = new SQLGenerator(this).CreateSqlSelectByWOB(sWhere, sOrderBy);

                DataTable dt = db.ExecuteSql(sql);

                lstReturn = DataTableToCollection(dt);
            }
            catch (Exception ex)
            {
                if (ex.Data.Contains("Function"))
                    ex.Data.Add("Function", "Model_RecordBase.SelectWhereOrderBy");
                if (ex.Data.Contains("sWhere"))
                    ex.Data.Add("sWhere", sWhere);
                if (ex.Data.Contains("sOrderBy"))
                    ex.Data.Add("sOrderBy", sOrderBy);

                OnExceptionEvent(ex, $"Where = {sWhere}; OrderBy= {sOrderBy}");
            }
            return lstReturn;
        }

        /// <summary>
        /// Updates the table for the corresponding model.
        /// </summary>
        /// <typeparam name="T">The entity model type</typeparam>
        /// <param name="mdl">The model instance that will be updated.</param>
        /// <returns></returns>
        public bool UpdateRecord()
        {
            DBAccess.DBAccess db = GetDB();
            string sql = new SQLGenerator(this).CreateSqlUpdateStatement();
            var bResult = false;

            try
            {
                bResult = db.ExecuteSqlMethod(sql);

                if (!bResult)
                {
                    ModelLastError = "No record was updated.";
                    throw new Exception(ModelLastError);
                }
            }
            catch (Exception ex)
            {
                if (ex.Data.Contains("Function"))
                    ex.Data.Add("Function", "Model_RecordBase.UpdateRecord");
                if (ex.Data.Contains("FailureDescription"))
                    ex.Data.Add("FailureDescription", $"Failed adding {Model_EntityName} record.");

                OnExceptionEvent(ex, sql);
            }

            return bResult;
        }

        /// <summary>
        /// Delete the current model instance record from database
        /// </summary>
        /// <returns>Boolean representing delete result</returns>
        public bool DeleteRecord()
        {
            var bResult = false;
            DBAccess.DBAccess db = GetDB();
            var sql = new SQLGenerator(this).CreateSqlDeleteStatement();

            try
            {
                // TODO: Delete any child records first 

                bResult = db.ExecuteSqlMethod(sql);

                if (!bResult)
                {
                    ModelLastError = "No record was deleted.";
                    throw new Exception(ModelLastError);
                }
            }
            catch (Exception ex)
            {
                if (ex.Data.Contains("Function"))
                    ex.Data.Add("Function", "Model_RecordBase.DeleteRecord");
                if (ex.Data.Contains("FailureDescription"))
                    ex.Data.Add("FailureDescription", $"Failed adding {Model_EntityName} record.");

                OnExceptionEvent(ex, sql);
            }
            return bResult;
        }

        #endregion

        #region Support Methods
        protected virtual void OnExceptionEvent(Exception ex, string sFailureSql)
        {
            // prioritize our custom error message
            ModelLastError = ex.Data["FailureDescription"] != null
                ? ModelLastError = (string)ex.Data["FailureDescription"]
                : ModelLastError = ex.Message;

            if (ex.Data["Function"] != null)
                ModelLastError += $"Method: {(string)ex.Data["Function"]}";
            if (ex.InnerException != null)
                ModelLastError += $"Inner Exception: {ex.InnerException.Message}";

        }

        private Collection<dynamic> DataTableToCollection(DataTable dt)
        {
            var colReturn = new Collection<dynamic>();
            dynamic ModelMaster = Activator.CreateInstance(this.GetType());

            if (dt != null)
                foreach (DataRow dr in dt.Rows)
                {
                    Model_RecordBase mdl = ModelMaster.MemberwiseClone();
                    mdl.ModelPopulate(dr);
                    colReturn.Add(mdl);
                }

            return colReturn;
        }

        /// <summary>
        /// Populates the current or passed model from the passed datarow.
        /// </summary>
        /// <param name="dr">Datarow containing the data to add to model.</param>
        /// <param name="mdl">The model to populate. If not provided, it will use the current model instance by default.</param>
        public void ModelPopulate(DataRow dr, Model_RecordBase mdl = null)
        {
            foreach (PropertyInfo p in Model_DataProperties)
                if (p.CanRead && p.CanWrite)
                    SafePropertyAssignment(p, dr[p.Name], mdl);

        }

        
        #endregion
    }
}
