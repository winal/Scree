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
using Scree.DataBase;
using Scree.Core.IoC;

namespace Scree.Persister
{
    internal static class DbTableService
    {
        private static IMappingService MappingService
        {
            get
            {
                return ServiceRoot.GetService<IMappingService>();
            }
        }

        private const string DATATYPEERROR = "{0} data type is unallowed";
        private const string SQLFORTABLEEXISTS = "SELECT 1 FROM SYSOBJECTS WITH(NOLOCK) WHERE NAME='{0}' AND TYPE IN ('U','V')";
        private static readonly string TableCreatedDirectory = AppDomain.CurrentDomain.BaseDirectory + @"config\TableCreated.config";

        private static Type INTTYPE = typeof(int);
        private static Type BOOLTYPE = typeof(bool);
        private static Type STRINGTYPE = typeof(string);
        private static Type DATETIMETYPE = typeof(DateTime);
        private static Type DECIMALTYPE = typeof(decimal);
        private static Type LONGTYPE = typeof(long);
        private static Type BYTESTYPE = typeof(byte[]);

        private static Mapping[] Mappings;
        private static AssemblyConfig[] Assemblies;

        internal static bool Init()
        {
            if (!MappingService.IsInitialized)
            {
                return false;
            }

            try
            {
                if (MappingService.IsAutoCreateTable)
                {
                    Assemblies = MappingService.GetAssemblies();
                    Mappings = MappingService.GetMappings();

                    AutoCreateTable();

                    Assemblies = null;
                    Mappings = null;
                }
            }
            catch (Exception ex)
            {
                LogProxy.Error(ex, false);
            }

            return true;
        }

        private static void AutoCreateTable()
        {
            try
            {
                XElement root = null;
                if (File.Exists(TableCreatedDirectory))
                {
                    root = XElement.Load(TableCreatedDirectory);
                }
                else
                {
                    root = new XElement("Assemblies");
                    root.Save(TableCreatedDirectory);
                }

                string assemblyPath = Tools.GetAssemblyPath();
                var name = Assemblies.Select<AssemblyConfig, string>(x => x.Name);

                Assembly assembly;
                foreach (string assemblyName in name)
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

                var table = root.Descendants("Assembly")
                    .Where(a => a.Attribute("name").Value == type.Assembly.FullName.Split(',')[0])
                    .Descendants("Type")
                    .Where(t => t.Attribute("name").Value == type.FullName);
                if (table.Count() > 0)
                {
                    continue;
                }

                CreateTable(type);

                TableCreated(type, root);
            }
        }

        private static void TableCreated(Type type, XElement root)
        {
            string assemblyName = type.Assembly.FullName.Split(',')[0];

            if (root.Descendants("Assembly")
                 .Where(a => a.Attribute("name").Value == assemblyName).Count() == 0)
            {
                root.Add(new XElement("Assembly", new XAttribute("name", assemblyName)));
            }

            root.Descendants("Assembly")
                  .Where(a => a.Attribute("name").Value == assemblyName).FirstOrDefault()
                  .Add(new XElement("Type", new XAttribute("name", type.FullName)));

            root.Save(TableCreatedDirectory);
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

                        CreateTable(type, tableName, dbOperate);
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

        private static void CreateTable(Type type, string tableName, IDbOperate dbOperate)
        {
            StringBuilder sql = new StringBuilder("CREATE TABLE [");
            StringBuilder sql2 = new StringBuilder();

            sql.Append(tableName);
            sql.Append("] ( ");

            var propertys = type.GetProperties()
                .OrderByDescending(p => p.DeclaringType == typeof(SRO))
                .OrderBy(p => p.DeclaringType == type);

            foreach (PropertyInfo property in propertys)
            {
                object[] dataTypeAttributes = property.GetCustomAttributes(typeof(DataTypeAttribute), false);
                DataTypeAttribute dataTypeAttribute = null;
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
            sql.Append(" )");

            sql2.Append("CREATE CLUSTERED INDEX [ix-CreatedDate] ON [dbo].[");
            sql2.Append(tableName);
            sql2.Append("] ( [CreatedDate] DESC ");
            sql2.Append(")WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY] ");

            sql2.Append("CREATE UNIQUE NONCLUSTERED INDEX [ix-Id] ON [dbo].[");
            sql2.Append(tableName);
            sql2.Append("] ( [Id] ASC ");
            sql2.Append(")WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY] ");

            try
            {
                dbOperate.ExecuteNonQuery(sql.ToString(), null);
                dbOperate.ExecuteNonQuery(sql2.ToString(), null);
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
            if (Mappings.Any<Mapping>(x => string.Equals(x.Name, type.FullName, StringComparison.OrdinalIgnoreCase)) == false)
            {
                queue.Enqueue(null);
                return queue;
            }

            mapping = Mappings.First(x => x.Name == type.FullName);
            Dictionary<string, string> storageContexts = mapping.StorageContexts;

            queue.Enqueue(null);

            if (storageContexts == null || storageContexts.Count == 0)
            {
                return queue;
            }

            var aliases = storageContexts
                .Where(x => x.Value != mapping.DefaultStorageContext)
                .GroupBy<KeyValuePair<string, string>, string>(x => x.Value)
                .Select(x => x.First().Key).ToArray();

            foreach (string alias in aliases)
            {
                if (queue.Contains(alias))
                {
                    continue;
                }

                queue.Enqueue(alias);
            }

            return queue;
        }

        private static string GetTableName(Type type)
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
            DecimalDataTypeAttribute decimalDataTypeAttribute;
            if (dataTypeAttribute == null || !(dataTypeAttribute is DecimalDataTypeAttribute))
            {
                decimalDataTypeAttribute = new DecimalDataTypeAttribute();
            }
            else
            {
                decimalDataTypeAttribute = dataTypeAttribute as DecimalDataTypeAttribute;
            }
            //if (dataTypeAttribute != null)
            //{
            //    decimalDataTypeAttribute.IsNullable = dataTypeAttribute.IsNullable;
            //}

            sql.Append(" (");
            sql.Append(decimalDataTypeAttribute.Precision);
            sql.Append(", ");
            sql.Append(decimalDataTypeAttribute.DecimalDigits);
            sql.Append(") ");

            CreateTableCommSetting(decimalDataTypeAttribute, ref sql);
        }

        private static void CreateTableWithAttributeOfString(DataTypeAttribute dataTypeAttribute, ref StringBuilder sql)
        {
            StringDataTypeAttribute stringDataTypeAttribute;
            if (dataTypeAttribute == null || !(dataTypeAttribute is StringDataTypeAttribute))
            {
                stringDataTypeAttribute = new StringDataTypeAttribute();
            }
            else
            {
                stringDataTypeAttribute = dataTypeAttribute as StringDataTypeAttribute;
            }
            //if (dataTypeAttribute != null)
            //{
            //    stringDataTypeAttribute.IsNullable = dataTypeAttribute.IsNullable;
            //}

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
