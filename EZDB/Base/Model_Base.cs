using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Authentication.ExtendedProtection;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Security.Cryptography;
using EZDB.DBAccess;
using EZDB.Utilities;

namespace EZDB.Base
{
    public class Model_Base : INotifyPropertyChanged
    {
        protected string ModelLastError = string.Empty;
        protected string ModelLastErrorSql = string.Empty;

        // use the "Cargo_" prefix to identify extra properties within your model that you DOES NOT match a column in the database table
        // similar to "Cargo_", "Model_" is used as a prefix for properties within these base classes so that they are not detected as part of the derivative's property list
        protected internal List<string> ListReservedPropertyPrefixes { get { return new List<string>() { "Model_", "Cargo_" }; } }

        public Model_Base() { }

        /// <summary>
        /// Event used for data binding
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        // virtual ==  overridable, propertyName will become the name of the prop or method that calls this "NotifyPropertChanged" because of [CallMemberName]
        protected virtual void NotifyPropertyChanged([CallerMemberName] string propertyName = "") 
            => PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));


        /// <summary>
        /// Infers the current model's primary key
        /// </summary>
        /// <returns></returns>
        public virtual PropertyInfo PrimaryKeyProperty()
        {
            PropertyInfo propInfo = null;

            // assume that the first found 'ID' is the primary key... it's standard convention
            foreach (PropertyInfo p in Model_AllProperties)
            {
                if (p.Name.ToLower().Left(3) == "id_"
                    || p.Name.Right(2) == "ID"
                    || p.Name.Right(3) == "_id")
                {
                    propInfo = p;
                    break;
                }
            }
            return propInfo;
        }

        public string Model_EntityName
        {
            get
            {
                return GetType().Name;
            }
        }

        public List<PropertyInfo> Model_AllProperties
        {
            get
            {
                var lstProps = new List<PropertyInfo>();

                lstProps.AddRange(GetType().GetProperties());

                return lstProps;
            }
        }

        internal List<PropertyInfo> Model_DataProperties
        {
            get
            {
                var lstProps = new List<PropertyInfo>();

                foreach (PropertyInfo p in GetType().GetProperties())
                    if (IsDataProperty(p))
                        lstProps.Add(p);

                return lstProps;
            }
            
        }

        protected internal bool IsDataProperty(PropertyInfo p)
        {
            foreach (string prefix in ListReservedPropertyPrefixes)
                if (p.Name.StartsWith(prefix) || p.CanWrite == false)
                    return false;

            return true;
        }

        /// <summary>
        /// Provides access to a property based on the name of the property.
        /// </summary>
        /// <param name="name">The string name of the property, such as "LastName"</param></param>
        /// <returns></returns>
        public PropertyInfo PropertyByName(string name)
        {
            foreach (PropertyInfo p in Model_AllProperties)
            {
                if (p.Name.ToLower() == name.ToLower())
                    return p;
            }
            return null;
        }

        internal List<PropertyInfo> Model_DataPropertiesWithoutPKorRefs
        {
            get
            {
                var lstProps = new List<PropertyInfo>();
                foreach (PropertyInfo p in GetType().GetProperties())
                {
                    if (p.Equals(PrimaryKeyProperty()) == false 
                        && IsDataProperty(p) == true 
                        && !(p.Name.ToLower().Left(4) == "ref_"))
                        lstProps.Add(p);
                }

                return lstProps;
            }
        }

        internal List<PropertyInfo> Model_DataPropertiesWithoutRefs
        {
            get
            {
                var lstProps = new List<PropertyInfo>();

                foreach (PropertyInfo p in GetType().GetProperties())
                {
                    if (IsDataProperty(p) == true && !(p.Name.ToLower().Left(4) == "ref_"))
                        lstProps.Add(p);
                }

                return lstProps;
            }
        }

        protected void SafePropertyAssignment(PropertyInfo oP, object oValue, object mdl = null)
        {
            if (oValue != null)
            {
                if (oValue.GetType().ToString() == "System.DBNull")
                {
                    switch (true)
                    {
                        case object _ when oP.PropertyType.Equals(typeof(string)):
                        case object _ when oP.PropertyType.Equals(typeof(char)):
                            {
                                oValue = "";
                                break;
                            }

                        case object _ when oP.PropertyType.Equals(typeof(decimal)):
                            {
                                oValue = 0M;
                                break;
                            }

                        case object _ when oP.PropertyType.Equals(typeof(double)):
                            {
                                oValue = 0.0D;
                                break;
                            }

                        case object _ when oP.PropertyType.Equals(typeof(uint)):
                        case object _ when oP.PropertyType.Equals(typeof(ulong)):
                            {
                                oValue = 0;
                                break;
                            }

                        case object _ when oP.PropertyType.Equals(typeof(int)):
                            {
                                oValue = Convert.ToInt32(0);
                                break;
                            }

                        case object _ when oP.PropertyType.Equals(typeof(long)):
                            {
                                oValue = Convert.ToInt64(0);
                                break;
                            }

                        case object _ when oP.PropertyType.Equals(typeof(short)):
                            {
                                oValue = Convert.ToInt16(0);
                                break;
                            }

                        case object _ when oP.PropertyType.Equals(typeof(ushort)):
                            {
                                oValue = Convert.ToUInt16(0);
                                break;
                            }

                        case object _ when oP.PropertyType.Equals(typeof(float)):
                            {
                                oValue = Convert.ToSingle(0);
                                break;
                            }

                        case object _ when oP.PropertyType.Equals(typeof(sbyte)):
                            {
                                oValue = Convert.ToSByte(0);
                                break;
                            }

                        case object _ when oP.PropertyType.Equals(typeof(byte)):
                            {
                                oValue = Convert.ToByte(0);
                                break;
                            }

                        case object _ when oP.PropertyType.Equals(typeof(DateTime)):
                            {
                                // oValue = DateTime.MinValue  'uh, so this screws up data binding with dtp controls
                                // need to use the real min date for sql
                                oValue = new SQLGenerator().SQLMinDate;
                                break;
                            }

                        case object _ when oP.PropertyType.Equals(typeof(bool)):
                            {
                                oValue = false;
                                break;
                            }

                        case object _ when oP.PropertyType.Equals(typeof(byte[])):
                            {
                                oValue = null;
                                break;
                            }

                        case object _ when oP.PropertyType.Equals(typeof(object)):
                            {
                                oValue = null;
                                break;
                            }

                        case object _ when oP.PropertyType.Equals(typeof(Guid)):
                            {
                                oValue = new Guid();
                                break;
                            }
                    }
                }

                if (mdl == null)
                    oP.SetValue(this, oValue, null);
                else
                    oP.SetValue(mdl, oValue, null);
            }
        }
        protected internal DBAccess.DBAccess GetDB()
        {
            return new DBAccess.DBAccess(DBSettings.ConnectionString);
        }
    }

    public class Model_RecordOverrides
    {
        public string EntityName = string.Empty;
        public string SQLPrimaryKeyName = string.Empty;
        public bool PrimaryKeyNotIdentity = false;
    }
}
