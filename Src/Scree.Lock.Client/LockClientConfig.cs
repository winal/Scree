using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Scree.Log;
using System.Xml.Linq;
using System.Threading;

namespace Scree.Syn.Client
{
    internal static class LockClientConfig
    {
        private static readonly string ConfigDirectory = AppDomain.CurrentDomain.BaseDirectory + @"config\lockclient.config";

        public static string ServerURI
        {
            get
            {
                return _serverURI;
            }
        }
        private static readonly string _serverURI;

        static LockClientConfig()
        {
            try
            {
                XElement root = XElement.Load(ConfigDirectory);
                _serverURI = root.Element("ServerURI").Value;
            }
            catch (Exception ex)
            {
                LogProxy.Error(ex, false);
            }
        }
    }
}
