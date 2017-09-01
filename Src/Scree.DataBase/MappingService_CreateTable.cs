using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data;
using System.Data.Common;
using System.Xml;
using System.Xml.Linq;
using Scree.Log;
using System.Reflection;
using Scree.Common;
using Scree.Attributes;
using System.IO;

namespace Scree.DataBase
{
    public static partial class MappingService
    {
        private const string DATATYPEERROR = "{0} data type is unallowed";
        private const string SQLFORTABLEEXISTS = "SELECT 1 FROM SYSOBJECTS WITH(NOLOCK) WHERE NAME='{0}' AND TYPE='U'";
        private static readonly string TableCreatedDirectory = AppDomain.CurrentDomain.BaseDirectory + @"log\TableCreated.config";

        private static Type INTTYPE = typeof(int);
        private static Type BOOLTYPE = typeof(bool);
        private static Type STRINGTYPE = typeof(string);
        private static Type DATETIMETYPE = typeof(DateTime);
        private static Type DECIMALTYPE = typeof(decimal);
        private static Type LONGTYPE = typeof(long);
        private static Type BYTESTYPE = typeof(byte[]);

        private static void AutoCreateTable()
        {
            try
            {
                XElement root = null;
                if (File.Exists(TableCreatedDirectory))
                {
                    root = XElement.Load(TableCreatedDirectory);
                }
                //IEnumerable<XElement> assemblies = root.Elements("Assembly");

                string assemblyPath = Tools.GetAssemblyPath();

                Assembly assembly;
                foreach (string assemblyName in Assemblies.Keys)
                {
                    assembly = Assembly.LoadFrom(assemblyPath + assemblyName + ".dll");
                    Type[] types = assembly.GetTypes();
                    CreateTable(types, root);
                }
            }
            catch (Exception ex)
            {
                LogProxy.Error(ex, false);
            }
        }

        private static void CreateTable(Type[] types, XElement root)
        {
            foreach (Type type in types)
            {
                if (!type.IsClass)
                {
                    continue;
                }

                object[] dtAttributes = type.GetCustomAttributes(typeof(DataTableAttribute), true);
                if (dtAttributes == null || dtAttributes.Length == 0)
                {
                    continue;
                }

                if (root != null)
                {
                    var table = root.Descendants("Assembly")
                         .Where(a => a.Attribute("name").Value == type.Assembly.FullName)
                         .Select(a => a.Descendants("Type")
                             .Where(t => t.Attribute("name").Value == type.FullName));
                    if (table.Count() >= 0)
                    {
                        continue;
                    }
                }

                CreateTable(type);
            }
        }

        private static void CreateTable(Type type)
        {
            try
            {
                string tableName = GetTableName(type);
                string sqlForTableExists = string.Format(SQLFORTABLEEXISTS, tableName);

                Queue<string> aliases = GetAlias(type);

                IDbOperate dbOperate;
                bool isTableExists;
                foreach (string alias in aliases)
                {
                    try
                    {
                        dbOperate = DbProxy.Create(alias, type);

                        isTableExists = IsTableExists(sqlForTableExists, dbOperate);
                        if (isTableExists)
                        {
                            continue;
                        }

                        CreateTable(tableName, dbOperate, type.GetProperties());
                    }
                    catch (Exception ex)
                    {
                        LogProxy.Error(ex, false);
                    }
                }
            }
            catch (Exception ex)
            {
                LogProxy.Error(ex, false);
            }
        }

        private static void CreateTable(string tableName, IDbOperate dbOperate, PropertyInfo[] propertys)
        {
            StringBuilder sql = new StringBuilder("CREATE TABLE [");

            sql.Append(tableName);
            sql.Append("] ( ");
          propertys =  propertys.OrderByDescending(p => p.DeclaringType.ToString() == "Scree.Persister.SRO").ToArray();
            foreach (PropertyInfo property in propertys)
            {
                object[] dataTypeAttributes = property.GetCustomAttributes(typeof(DataTypeAttribute), false);
                DataTypeAttribute dataTypeAttribute =null;
                if (dataTypeAttributes != null && dataTypeAttributes.Length > 0)
                {
                    dataTypeAttribute = dataTypeAttributes[0] as DataTypeAttribute;
                }

                if (dataTypeAttribute != null && dataTypeAttribute.IsLoad == false && dataTypeAttribute.IsSave == false)
                {
                    //判断是否映射
                    continue;
                }

                sql.Append(" [" + property.Name + "] ");

                CreateTableTypeHandle(property.PropertyType, dataTypeAttribute, ref sql);
            }
            sql.Append(" ) ");

            try
            {
                dbOperate.ExecuteNonQuery(sql.ToString(), null);
            }
            catch (Exception ex)
            {
                LogProxy.Error(ex, false);
            }
        }


        private static Queue<string> GetAlias(Type type)
        {
            Queue<string> queue = new Queue<string>();

            Mapping mapping;
            if (Mappings.ContainsKey(type.FullName) == false)
            {
                queue.Enqueue(null);
                return queue;
            }

            mapping = Mappings[type.FullName];
            Dictionary<string, string> storageContexts = mapping.StorageContexts;

            queue.Enqueue(mapping.DefaultStorageContext);

            if (storageContexts == null || storageContexts.Count == 0)
            {
                return queue;
            }

            foreach (string alias in storageContexts.Keys)
            {
                if (queue.Contains(alias))
                {
                    continue;
                }

                queue.Enqueue(alias);
            }

            return queue;
        }

        public static string GetTableName(Type type)
        {
            return type.Name;
        }

        private static bool IsTableExists(string sqlForTableExists, IDbOperate dbOperate)
        {
            try
            {
                return dbOperate.IsExist(sqlForTableExists);
            }
            catch (Exception ex)
            {
                LogProxy.Error(ex, false);
                return false;
            }
        }

        private static void CreateTableTypeHandle(Type pType, DataTypeAttribute dataTypeAttribute, ref StringBuilder sql)
        {
            if (pType == INTTYPE)
            {
                sql.Append(" int ");
                CreateTableCommSetting(dataTypeAttribute, ref sql);
            }
            else if (pType == BOOLTYPE)
            {
                sql.Append(" bit ");
                CreateTableCommSetting(dataTypeAttribute, ref sql);
            }
            else if (pType == STRINGTYPE)
            {
                CreateTableWithAttributeOfString(dataTypeAttribute, ref sql);
            }
            else if (pType == DATETIMETYPE)
            {
                sql.Append(" datetime ");
                CreateTableCommSetting(dataTypeAttribute, ref sql);
            }
            else if (pType == DECIMALTYPE)
            {
                sql.Append(" decimal ");
                CreateTableWithAttributeOfDecimal(dataTypeAttribute, ref sql);
            }
            else if (pType.IsEnum)
            {
                sql.Append(" int ");
                CreateTableCommSetting(dataTypeAttribute, ref sql);
            }
            else if (pType == LONGTYPE)
            {
                sql.Append(" bigint ");
                CreateTableCommSetting(dataTypeAttribute, ref sql);
            }
            else if (pType == BYTESTYPE)
            {
                sql.Append(" image ");
                CreateTableCommSetting(dataTypeAttribute, ref sql);
            }
            else
            {
                LogProxy.ErrorFormat(true, DATATYPEERROR, pType.ToString());
            }
        }

        private static void CreateTableWithAttributeOfDecimal(DataTypeAttribute dataTypeAttribute, ref StringBuilder sql)
        {
            DecimalDataTypeAttribute decimalDataTypeAttribute = new DecimalDataTypeAttribute();
            if (dataTypeAttribute != null)
            {
                decimalDataTypeAttribute.IsNullable = dataTypeAttribute.IsNullable;
            }

            sql.Append(" (");
            sql.Append(decimalDataTypeAttribute.Precision);
            sql.Append(", ");
            sql.Append(decimalDataTypeAttribute.DecimalDigits);
            sql.Append(") ");

            CreateTableCommSetting(decimalDataTypeAttribute, ref sql);
        }

        private static void CreateTableWithAttributeOfString(DataTypeAttribute dataTypeAttribute, ref StringBuilder sql)
        {
            StringDataTypeAttribute stringDataTypeAttribute = new StringDataTypeAttribute();
            if (dataTypeAttribute != null)
            {
                stringDataTypeAttribute.IsNullable = dataTypeAttribute.IsNullable;
            }

            if (stringDataTypeAttribute.Type == StringType.NVarchar)
            {
                sql.Append(" NVARCHAR(");
                sql.Append(stringDataTypeAttribute.IsMaxLength ? "MAX" : stringDataTypeAttribute.Length.ToString());
                sql.Append(") ");
            }
            else
            {
                sql.Append(" NTEXT ");
            }

            CreateTableCommSetting(stringDataTypeAttribute, ref sql);
        }

        private static void CreateTableCommSetting(DataTypeAttribute dataTypeAttribute, ref StringBuilder sql)
        {
            sql.Append(dataTypeAttribute == null || dataTypeAttribute.IsNullable ? " NULL," : " NOT NULL,");
        }

    }
}
