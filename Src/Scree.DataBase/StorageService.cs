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
using Scree.Core.IoC;

namespace Scree.DataBase
{
    internal class StorageContext
    {
        internal string Name { get; set; }
        internal string DataSource { get; set; }
        internal string Catalog { get; set; }
        internal string UId { get; set; }
        internal string Pwd { get; set; }
        internal string Timeout { get; set; }
        internal int MinPoolSize { get; set; }
        internal int MaxPoolSize { get; set; }
        internal bool Enabled { get; set; }
        internal StorageContext(string name)
        {
            this.Name = name;
        }
    }

    internal sealed class StorageService
    {
        private const string STORAGECONTEXTNAMEISNULL = "StorageContext name is null";
        private const string STORAGECONTEXTISNULL = "StorageContext:{0} is null";
        private const string STORAGECONTEXTSISNULL = "StorageContexts is null";
        private const string DEFAULTSTORAGECONTEXT = "current";
        private static readonly string ConfigDirectory = AppDomain.CurrentDomain.BaseDirectory + @"config\storage.config";
        private static Dictionary<string, StorageContext> StorageContexts = new Dictionary<string, StorageContext>(29);

        internal static void Init()
        {
        }

        static StorageService()
        {
            try
            {
                XElement root = XElement.Load(ConfigDirectory);
                IEnumerable<XElement> storageContexts = root.Elements("StorageContext");
                LoadStorageContexts(storageContexts);
            }
            catch (Exception ex)
            {
                LogProxy.Fatal(ex, true);
            }
        }

        private static void LoadStorageContexts(IEnumerable<XElement> storageContexts)
        {
            StorageContext storageContext;
            bool isFirst = true;
            foreach (XElement el in storageContexts)
            {
                storageContext = BuilderStorageContext(el);
                if (string.IsNullOrEmpty(storageContext.Name))
                {
                    LogProxy.Warn(STORAGECONTEXTNAMEISNULL);
                    continue;
                }

                StorageContexts[storageContext.Name] = storageContext;

                if (isFirst)
                {
                    DefaultStorageContext = storageContext;
                    isFirst = false;
                }
                else if (string.Equals(storageContext.Name, DEFAULTSTORAGECONTEXT, StringComparison.OrdinalIgnoreCase))
                {
                    DefaultStorageContext = storageContext;
                }
            }
        }

        private static StorageContext BuilderStorageContext(XElement el)
        {
            StorageContext storageContext = new StorageContext(el.Attribute("name").Value);
            storageContext.DataSource = el.Element("DataSource").Value;
            storageContext.Catalog = el.Element("Catalog").Value;
            storageContext.UId = el.Element("UId").Value;
            storageContext.Pwd = el.Element("Pwd").Value;
            storageContext.Timeout = el.Element("Timeout").Value;
            storageContext.MinPoolSize = int.Parse(el.Element("MinPoolSize").Value);
            storageContext.MaxPoolSize = int.Parse(el.Element("MaxPoolSize").Value);
            storageContext.Enabled = bool.Parse(el.Element("Enabled").Value);
            return storageContext;
        }

        internal static StorageContext GetStorageContext(string name)
        {
            if (StorageContexts == null || StorageContexts.Count == 0)
            {
                LogProxy.Error(STORAGECONTEXTSISNULL, true);
                return null;
            }

            if (string.IsNullOrEmpty(name))
            {
                return DefaultStorageContext;
            }

            if (StorageContexts.ContainsKey(name) == false)
            {
                LogProxy.ErrorFormat(true, STORAGECONTEXTISNULL, name);
                return null;
            }

            return StorageContexts[name];
        }

        internal static StorageContext DefaultStorageContext { get; private set; }

    }
}
