using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Scree.Log;
using System.Xml.Linq;
using System.Threading;

namespace Scree.Syn.Client
{
    internal static class SynClientCommConfig
    {
        private static readonly string ConfigDirectory = AppDomain.CurrentDomain.BaseDirectory + @"config\synclient.config";

        public static string ServerURI
        {
            get
            {
                return _serverURI;
            }
        }
        private static string _serverURI;

        public static int PostDueTime
        {
            get
            {
                return _postDueTime;
            }
        }
        private static int _postDueTime;

        public static int PostPeriod
        {
            get
            {
                return _postPeriod;
            }
        }
        private static int _postPeriod;

        public static int GetDueTime
        {
            get
            {
                return _getDueTime;
            }
        }
        private static int _getDueTime;

        public static int GetPeriod
        {
            get
            {
                return _getPeriod;
            }
        }
        private static int _getPeriod;

        public static int PostTryInterval
        {
            get
            {
                return _postTryInterval;
            }
        }
        private static int _postTryInterval;

        public static int PostTryMultiple
        {
            get
            {
                return _postTryMultiple;
            }
        }
        private static int _postTryMultiple;

        public static int PostTrys
        {
            get
            {
                return _postTrys;
            }
        }
        private static int _postTrys;

        public static int ClearInterval
        {
            get
            {
                return _clearInterval;
            }
        }
        private static int _clearInterval;

        internal static void Init()
        {
            try
            {
                XElement root = XElement.Load(ConfigDirectory);
                XElement localLock = root.Element("Comm");
                _serverURI = localLock.Element("ServerURI").Value;
                int.TryParse(localLock.Element("PostDueTime").Value, out _postDueTime);
                int.TryParse(localLock.Element("PostPeriod").Value, out _postPeriod);
                int.TryParse(localLock.Element("GetDueTime").Value, out _getDueTime);
                int.TryParse(localLock.Element("GetPeriod").Value, out _getPeriod);
                int.TryParse(localLock.Element("PostTryInterval").Value, out _postTryInterval);
                int.TryParse(localLock.Element("PostTryMultiple").Value, out _postTryMultiple);
                int.TryParse(localLock.Element("PostTrys").Value, out _postTrys);
                int.TryParse(localLock.Element("ClearInterval").Value, out _clearInterval);
            }
            catch (Exception ex)
            {
                LogProxy.Error(ex, false);
            }
        }
    }

    internal sealed class SynClientConfig
    {
        private const string SYNCLIENTTYPENAMEISNULL = "Syn client type name is null";

        private static readonly string ConfigDirectory = AppDomain.CurrentDomain.BaseDirectory + @"config\synclient.config";
        private static readonly Dictionary<string, SynClientConfig> MySynClientConfig = new Dictionary<string, SynClientConfig>(29);

        public string Name { get; set; }
        public bool IsLazy { get; set; }
        public bool IsBan { get; set; }

        private SynClientConfig(string name, bool isBan, bool isLazy)
        {
            this.Name = name;
            this.IsBan = isBan;
            this.IsLazy = isLazy;
        }

        internal static void Init()
        {
            try
            {
                XElement root = XElement.Load(ConfigDirectory);

                IEnumerable<XElement> excludedTypes = root.Elements("Excluded").Elements();
                IEnumerable<XElement> synTypes = root.Elements("Syn").Elements();
                LoadSynClientConfig(excludedTypes, synTypes);
            }
            catch (Exception ex)
            {
                LogProxy.Error(ex, false);
            }
        }

        private static void LoadSynClientConfig(IEnumerable<XElement> excludedTypes, IEnumerable<XElement> synTypes)
        {
            SynClientConfig config;
            foreach (XElement el in excludedTypes)
            {
                try
                {
                    string name = el.Attribute("name").Value;
                    if (string.IsNullOrEmpty(name))
                    {
                        LogProxy.Warn(SYNCLIENTTYPENAMEISNULL);
                        continue;
                    }

                    if (MySynClientConfig.ContainsKey(name))
                    {
                        continue;
                    }

                    config = new SynClientConfig(name, true, false);
                    MySynClientConfig[name] = config;
                }
                catch (Exception ex)
                {
                    LogProxy.Error(ex, false);
                }
            }

            foreach (XElement el in synTypes)
            {
                try
                {
                    string name = el.Attribute("name").Value;
                    if (string.IsNullOrEmpty(name))
                    {
                        LogProxy.Warn(SYNCLIENTTYPENAMEISNULL);
                        continue;
                    }

                    if (MySynClientConfig.ContainsKey(name))
                    {
                        continue;
                    }

                    bool isLazy = false;
                    if (el.Attribute("islazy") != null)
                    {
                        bool.TryParse(el.Attribute("islazy").Value.Trim(), out isLazy);
                    }

                    config = new SynClientConfig(name, false, isLazy);
                    MySynClientConfig[name] = config;
                }
                catch (Exception ex)
                {
                    LogProxy.Error(ex, false);
                }
            }

        }

        public static bool IsExist(string name)
        {
            return MySynClientConfig.ContainsKey(name);
        }

        //public static bool IsExist<T>()
        //{
        //    return IsExist(typeof(T).FullName);
        //}

        public static SynClientConfig Get(string name)
        {
            if (IsExist(name))
            {
                return MySynClientConfig[name];
            }
            return null;
        }

        public static SynClientConfig Get<T>()
        {
            return Get(typeof(T).FullName);
        }

    }
}
