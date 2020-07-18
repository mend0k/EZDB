using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using EZDB.Base;
using EZDB.DBAccess;

namespace EZDB.Utilities
{
    internal class SQLGenerator
    {
        public readonly DateTime SQLMinDate = new DateTime(1753, 1, 1);
        public readonly DateTime SQLMaxDate = new DateTime(9999, 12, 31);

        private Model_Base _mdl;
        public SQLGenerator() { }
        public SQLGenerator(Model_Base mdl) 
        { 
            _mdl = mdl;
        }

        internal string CreateSqlDeleteStatement()
            => $"Delete from {_mdl.Model_EntityName} where {_mdl.PrimaryKeyProperty().Name} = {PropertyToValidValue(_mdl, _mdl.PrimaryKeyProperty())}";

        internal string CreateSqlSelectByWOB(string sWhere = "", string sOrderBy = "")
        {
            string sSelectPhrase = $"Select {CreateSqlSelectPhrase()} "; 
            //TODO: string sJoinPhrase = $"{GetRelatedJoinClauses(sSelectPhrase)} " ;
            string sFromPhrase = $"From {_mdl.Model_EntityName} ";

            if (sWhere.Length > 0)
                sWhere = $"Where {sWhere} "; 


            if (sOrderBy.Length > 0)
                sOrderBy = $"Order By {sOrderBy}";

            return sSelectPhrase + sFromPhrase + sWhere + sOrderBy;
        }

        internal string CreateSqlUpdateStatement()
        {
            string sClause = string.Empty;

            foreach (PropertyInfo p in GetAppropriatePropListForInsertOrUpdate(_mdl))
                sClause = AppendCommaSepratedString(sClause, $"[{p.Name}] = {PropertyToValidValue(_mdl,p)}");

            return $"Update {_mdl.Model_EntityName} Set {sClause} Where {_mdl.Model_EntityName}.{_mdl.PrimaryKeyProperty().Name} = {PropertyToValidValue(_mdl, _mdl.PrimaryKeyProperty())}";

        }

        internal string CreateSqlInsertStatement()
        {
            string sColumns = string.Empty;
            string sValues = string.Empty;

            foreach (PropertyInfo p in GetAppropriatePropListForInsertOrUpdate(_mdl))
            {
                sColumns = AppendCommaSepratedString(sColumns, $"[{p.Name}]");
                sValues = AppendCommaSepratedString(sValues, PropertyToValidValue(_mdl,p));
            }

            // "Select scope identity" required to retrieve and populate the current model instance ID_* property with its new ID from the database
            return $"Insert into {_mdl.Model_EntityName} ({sColumns}) Values ({sValues}) ; SELECT SCOPE_IDENTITY();";
        }

        internal string CreateSqlSelectByPrimaryKey(long pkId)
        {
            var sSelectPhrase = $"Select {CreateSqlSelectPhrase()} ";
            //TODO: string sJoinPhrase = $"{GetRelatedJoinClauses(sSelectPhrase)} " ;
            var sFromPhrase = $"From {_mdl.Model_EntityName} ";

            var sWhere = $"Where {FullyQualifiedSqlPkName()} = {pkId}";

            return sSelectPhrase + sFromPhrase + sWhere;
        }

        internal string CreateSqlSelectPhrase()
        {
            string sReturn = string.Empty;

            foreach (PropertyInfo p in _mdl.Model_DataProperties)
            {
                if (sReturn.Length > 0)
                    sReturn += $", {FullyQualifiedSqlName(p)}";
                else
                    sReturn = FullyQualifiedSqlName(p);
            }

            return sReturn;
        }

        private string FullyQualifiedSqlName(PropertyInfo p)
        {
            const string REF_PREFIX = "ref_";

            if (p.Name.ToLower().StartsWith(REF_PREFIX))
                return DeriveRefColumnName(p);
            else
                return $"{_mdl.Model_EntityName}.[{p.Name}]";
        }

        private string DeriveRefColumnName(PropertyInfo p)
        {
            // Convention: Related_ForeignTableName_ForeignColumnName
            var sReturn = string.Empty;
            var sName = p.Name;
            string[] splits = sName.Split("_");

            if (splits.Count() > 2)
            {
                string sForeignTableName = splits[1];
                string sForeignColumnName = sName.Right(sName.Length - ("ref_" + sForeignTableName + "_").Length);
                sReturn = $"{sForeignTableName}.[{sForeignColumnName}] as Ref_{sForeignTableName}_{sForeignColumnName}";
            }

            return sReturn;
        }

        private List<PropertyInfo> GetAppropriatePropListForInsertOrUpdate(dynamic model)
        {
            return model.Model_Overrides.PrimaryKeyNotIdentity == false
                // table pk is identity so we don't include
                ? model.Model_DataPropertiesWithoutPKorRefs
                // include pk as it is not the identity
                : model.Model_DataPropertiesWithoutRefs;
        }

        private string AppendCommaSepratedString(string sValue, string sAppend)
        {
            return sValue.Length == 0
                ? sAppend
                : $"{sValue}, {sAppend}";
        }

        private string FullyQualifiedSqlPkName()
        {
            var p = _mdl.PrimaryKeyProperty();

            return p == null
                ? ""
                : FullyQualifiedSqlName(p);
        }

        private string FullyQulifiedSqlName(PropertyInfo p)
        {
            return $"{_mdl.Model_EntityName}.{p.Name}";
        }

        private string PropertyToValidValue(dynamic model,PropertyInfo p)
        {
            var oValue = p.GetValue(model, null);
            Type tp = p.PropertyType;

            string strValue;
            
            // NOTE: we use invariant culture in order to format certain data the way that sql server expects, 
            // regardless of where the .net code is running.  
            switch (true)
            {
                case object _ when tp.Equals(typeof(string)):
                case object _ when tp.Equals(typeof(char)):
                    {
                        strValue = oValue != null
                            ? string.Format("'{0}'", oValue.ToString().Replace("'", "''"))
                            : "NULL";
                        break;
                    }
                case object _ when tp.Equals(typeof(DateTime)):
                    {
                        if (!(oValue == null))
                        {
                            // manage for sql datetime limits
                            if ((DateTime)oValue < SQLMinDate)
                                oValue = SQLMinDate;
                            if ((DateTime)oValue > SQLMaxDate)
                                oValue = SQLMaxDate;

                            {
                                // read answer from here: http://stackoverflow.com/questions/8816712/what-is-the-culture-neutral-dateformat-for-sql-server as to why this format is used
                                string sFormat = "yyyyMMdd HH:mm:ss";

                                DateTime dt = (DateTime)oValue;

                                strValue = $"'{dt.ToString(sFormat)}'";

                            }
                        }
                        else
                            strValue = "NULL";
                        break;
                    }
                case object _ when tp.Equals(typeof(decimal)):
                    {
                        decimal dec = (decimal)oValue;
                        strValue = oValue != null
                                ? dec.ToString(CultureInfo.InvariantCulture)
                                : "NULL";
                        break;
                    }
                case object _ when tp.Equals(typeof(double)):
                    {
                        double dbl = (double)oValue;
                        strValue = oValue != null
                                ? dbl.ToString(CultureInfo.InvariantCulture)
                                : "NULL";
                        break;
                    }
                case object _ when tp.Equals(typeof(float)):
                    {
                        float flt = (float)oValue;
                        strValue = oValue != null
                                ? flt.ToString(CultureInfo.InvariantCulture)
                                : "NULL";
                        break;
                    }
                case object _ when tp.Equals(typeof(long)):
                case object _ when tp.Equals(typeof(int)):
                case object _ when tp.Equals(typeof(short)):
                case object _ when tp.Equals(typeof(byte)):
                case object _ when tp.Equals(typeof(sbyte)):
                case object _ when tp.Equals(typeof(ushort)):
                case object _ when tp.Equals(typeof(uint)):
                case object _ when tp.Equals(typeof(ulong)):
                    {
                        switch (true)
                        {
                            case object _ when oValue == null:
                                strValue = "NULL";
                                break;

                            default:
                                strValue = oValue.ToString();
                                break;
                        }

                        break;
                    }
                case object _ when tp.Equals(typeof(bool)):
                    {
                        if (oValue != null)
                        {
                            strValue = (bool)oValue == true
                                ? "1"
                                : "0";
                        }
                        else
                            strValue = "NULL";
                        break;
                    }
                case object _ when tp.GetTypeInfo().Name == "Byte[]":
                    {
                        strValue = oValue != null
                            ? "0x" + BitConverter.ToString((byte[])oValue).Replace("-", "")
                            : "NULL";
                        break;
                    }
                case object _ when tp.Equals(typeof(System.Guid)):
                    {
                        strValue = oValue != null
                            ? string.Format("'{0}'", oValue.ToString())
                            : "NULL";
                        break;
                    }
                default:
                    {
                        throw new Exception("Type " + tp.Name + " cannot be passed to the database");
                    }
            }
            return strValue;
        }

        
    }
}
