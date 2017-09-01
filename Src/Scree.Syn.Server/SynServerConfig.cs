using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Scree.Log;
using System.Xml.Linq;
using System.Threading;

namespace Scree.Syn.Server
{
    internal static class SynServerConfig
    {
        private static readonly string ConfigDirectory = AppDomain.CurrentDomain.BaseDirectory + @"config\synserver.config";

        public static string ApplicationName
        {
            get
            {
                return _applicationName;
            }
        }
        private static readonly string _applicationName;

        public static int Port
        {
            get
            {
                return _port;
            }
        }
        private static readonly int _port;

        public static int TimeLimit
        {
            get
            {
                return _timeLimit;
            }
        }
        private static readonly int _timeLimit;

        public static int ClearInterval
        {
            get
            {
                return _clearInterval;
            }
        }
        private static readonly int _clearInterval;

        static SynServerConfig()
        {
            try
            {
                XElement root = XElement.Load(ConfigDirectory);
                _applicationName = root.Element("ApplicationName").Value;
                int.TryParse(root.Element("Port").Value, out _port);
                int.TryParse(root.Element("TimeLimit").Value, out _timeLimit);
                int.TryParse(root.Element("ClearInterval").Value, out _clearInterval);
            }
            catch (Exception ex)
            {
                LogProxy.Error(ex, false);
            }
        }
    }
}
