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
    public sealed class MappingService : ServiceBase, IMappingService
    {
        private const string ASSEMBLYNAMEISNULL = "Assembly name is null";
        private const string MAPPINGSNAMEISNULL = "Mapping name is null";
        private const string ALIASISNULL = "Alias is null";
        private static readonly string ConfigDirectory = AppDomain.CurrentDomain.BaseDirectory + @"config\mapping.config";
        private static Dictionary<string, AssemblyConfig> Assemblies = new Dictionary<string, AssemblyConfig>(29);
        private static Dictionary<string, Mapping> Mappings = new Dictionary<string, Mapping>(197);

        public bool IsAutoCreateTable
        {
            get
            {
                return _isAutoCreateTable;
            }
        }
        private static bool _isAutoCreateTable;

        public Mapping[] GetMappings()
        {
            if (Mappings == null)
            {
                return new Mapping[0];
            }

            return Mappings.Select<KeyValuePair<string, Mapping>, Mapping>(x => x.Value.Clone()).ToArray();
        }

        public AssemblyConfig[] GetAssemblies()
        {
            if (Assemblies == null)
            {
                return new AssemblyConfig[0];
            }

            return Assemblies.Select<KeyValuePair<string, AssemblyConfig>, AssemblyConfig>(x => x.Value.Clone()).ToArray();
        }

        public override bool Init()
        {
            return true;
        }

        static MappingService()
        {
            StorageService.Init();

            try
            {
                XElement root = XElement.Load(ConfigDirectory);

                _isAutoCreateTable = string.Equals(root.Attribute("autocreatetable") == null ? "false" : root.Attribute("autocreatetable").Value, "true", StringComparison.OrdinalIgnoreCase);

                IEnumerable<XElement> assemblies = root.Elements("Assembly");
                LoadAssemblies(assemblies);
            }
            catch (Exception ex)
            {
                LogProxy.Fatal(ex, true);
            }
        }

        private static void LoadAssemblies(IEnumerable<XElement> assemblies)
        {
            AssemblyConfig assembly;
            foreach (XElement el in assemblies)
            {
                try
                {
                    assembly = AssemblyConfigBuilder(el);
                    if (string.IsNullOrEmpty(assembly.Name))
                    {
                        LogProxy.Warn(ASSEMBLYNAMEISNULL);
                        continue;
                    }

                    Assemblies[assembly.Name] = assembly;
                    LoadMappings(el.Elements("Type"), assembly);
                }
                catch (Exception ex)
                {
                    LogProxy.Error(ex, false);
                }
            }
        }

        private static void LoadMappings(IEnumerable<XElement> mappings, AssemblyConfig assembly)
        {
            Mapping mapping;
            foreach (XElement el in mappings)
            {
                try
                {
                    mapping = MappingBuilder(el, assembly);
                    if (string.IsNullOrEmpty(mapping.Name))
                    {
                        LogProxy.Warn(MAPPINGSNAMEISNULL);
                        continue;
                    }

                    Mappings[mapping.Name] = mapping;
                }
                catch (Exception ex)
                {
                    LogProxy.Error(ex, false);
                }
            }
        }

        private static AssemblyConfig AssemblyConfigBuilder(XElement el)
        {
            AssemblyConfig assembly = new AssemblyConfig(el.Attribute("name").Value);
            assembly.DefaultStorageContext = el.Attribute("default") == null ? null : el.Attribute("default").Value;
            return assembly;
        }

        private static Mapping MappingBuilder(XElement el, AssemblyConfig assembly)
        {
            Mapping mapping = new Mapping(el.Attribute("name").Value, assembly);
            mapping.DefaultStorageContext = el.Attribute("default") == null ? null : el.Attribute("default").Value;

            IEnumerable<XElement> storageContextList = el.Elements("StorageContext");
            Dictionary<string, string> storageContexts = LoadStorageContexts(storageContextList);
            mapping.StorageContexts = storageContexts;

            return mapping;
        }

        private static Dictionary<string, string> LoadStorageContexts(IEnumerable<XElement> storageContextList)
        {
            Dictionary<string, string> storageContexts = new Dictionary<string, string>();

            string name;
            foreach (XElement el in storageContextList)
            {
                try
                {
                    name = el.Attribute("name").Value;
                    if (string.IsNullOrEmpty(name))
                    {
                        LogProxy.Warn(ALIASISNULL);
                        continue;
                    }

                    storageContexts[name] = el.Value;
                }
                catch (Exception ex)
                {
                    LogProxy.Error(ex, false);
                }
            }
            return storageContexts;
        }

        internal static AssemblyConfig GetAssemblyConfig(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            if (Assemblies == null || Assemblies.Count == 0)
            {
                return null;
            }

            if (Assemblies.ContainsKey(name) == false)
            {
                return null;
            }

            return Assemblies[name];
        }

        private static Mapping GetMapping(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            if (Mappings == null || Mappings.Count == 0)
            {
                return null;
            }

            if (Mappings.ContainsKey(name) == false)
            {
                return null;
            }

            return Mappings[name];
        }

        private static StorageContext GetStorageContext(string alias, string name, string assemblyName)
        {
            Mapping mapping = GetMapping(name);
            if (mapping == null)
            {
                AssemblyConfig assemblyConfig = GetAssemblyConfig(assemblyName);
                if (assemblyConfig == null)
                {
                    return StorageService.DefaultStorageContext;
                }

                return StorageService.GetStorageContext(assemblyConfig.DefaultStorageContext);
            }

            string context = mapping.GetStorageContext(alias);
            if (string.IsNullOrEmpty(context))
            {
                context = mapping.DefaultStorageContext;

                if (string.IsNullOrEmpty(context))
                {
                    context = mapping.Assembly.DefaultStorageContext;
                }
            }

            return StorageService.GetStorageContext(context);
        }

        internal static StorageContext GetStorageContext(string alias, Type type)
        {
            return GetStorageContext(alias, type.FullName, type.Assembly.FullName);
        }
    }
}
