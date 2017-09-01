using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data;
using System.Data.Common;
using System.Xml;
using System.Xml.Linq;
using Scree.Core.IoC;

namespace Scree.DataBase
{
    public class AssemblyConfig : ICloneable
    {
        public string Name { get; internal set; }
        public string DefaultStorageContext { get; internal set; }
        internal AssemblyConfig(string name)
        {
            this.Name = name;
        }

        public AssemblyConfig Clone()
        {
            AssemblyConfig ac = new AssemblyConfig(this.Name);
            ac.DefaultStorageContext = this.DefaultStorageContext;

            return ac;
        }

        object ICloneable.Clone()
        {
            return this.Clone();
        }
    }

    public class Mapping : ICloneable
    {
        public string Name { get; internal set; }
        public string AssemblyName { get; internal set; }
        public string DefaultStorageContext { get; internal set; }
        public Dictionary<string, string> StorageContexts { get; internal set; }
        public AssemblyConfig Assembly { get; internal set; }

        public bool IsStorageContextExist(string alias)
        {
            if (string.IsNullOrEmpty(alias))
            {
                return false;
            }

            if (StorageContexts == null || StorageContexts.Count == 0)
            {
                return false;
            }

            return StorageContexts.ContainsKey(alias);
        }
        public string GetStorageContext(string alias)
        {
            if (IsStorageContextExist(alias))
            {
                return StorageContexts[alias];
            }

            return null;
        }

        internal Mapping(string name, AssemblyConfig assembly)
        {
            this.Name = name;
            this.Assembly = assembly;
            this.AssemblyName = assembly.Name;
        }

        public Mapping Clone()
        {
            Mapping m = new Mapping(this.Name, this.Assembly.Clone());
            m.DefaultStorageContext = this.DefaultStorageContext;

            if (this.StorageContexts != null)
            {
                Dictionary<string, string> storageContexts = new Dictionary<string, string>(this.StorageContexts.Count);
                foreach (var v in this.StorageContexts)
                {
                    storageContexts.Add(v.Key, v.Value);
                }
                m.StorageContexts = storageContexts;
            }

            return m;
        }

        object ICloneable.Clone()
        {
            return this.Clone();
        }
    }

    public interface IMappingService : IService
    {
        bool IsAutoCreateTable { get; }

        Mapping[] GetMappings();

        AssemblyConfig[] GetAssemblies();
    }
}
