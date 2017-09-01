using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Scree.Log;

namespace Scree.Lock.Server
{
    internal static class LockServerConfig
    {
        private static readonly string ConfigDirectory = AppDomain.CurrentDomain.BaseDirectory + @"config\lockserver.config";

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

        public static int Expire
        {
            get
            {
                return _expire;
            }
        }
        private static readonly int _expire;

        public static int TryInterval
        {
            get
            {
                return _tryInterval;
            }
        }
        private static readonly int _tryInterval;

        public static int Trys
        {
            get
            {
                return _trys;
            }
        }
        private static readonly int _trys;

        public static int ClearInterval
        {
            get
            {
                return _clearInterval;
            }
        }
        private static readonly int _clearInterval;

        static LockServerConfig()
        {
            try
            {
                XElement root = XElement.Load(ConfigDirectory);
                _applicationName = root.Element("ApplicationName").Value;
                int.TryParse(root.Element("Port").Value, out _port);
                int.TryParse(root.Element("Expire").Value, out _expire);
                int.TryParse(root.Element("TryInterval").Value, out _tryInterval);
                int.TryParse(root.Element("Trys").Value, out _trys);
                int.TryParse(root.Element("ClearInterval").Value, out _clearInterval);
            }
            catch (Exception ex)
            {
                LogProxy.Error(ex, false);
            }
        }
    }

    #region local lock 
    //public static class LocalLockConfig
    //{
    //    private static readonly string ConfigDirectory = AppDomain.CurrentDomain.BaseDirectory + @"config\lock.config";

    //    public static int Expire
    //    {
    //        get
    //        {
    //            return _expire;
    //        }
    //    }
    //    private static readonly int _expire;

    //    public static int TryInterval
    //    {
    //        get
    //        {
    //            return _tryInterval;
    //        }
    //    }
    //    private static readonly int _tryInterval;

    //    public static int Trys
    //    {
    //        get
    //        {
    //            return _trys;
    //        }
    //    }
    //    private static readonly int _trys;

    //    static LocalLockConfig()
    //    {
    //        try
    //        {
    //            XElement root = XElement.Load(ConfigDirectory);
    //            XElement localLock = root.Element("LocalLock");
    //            int.TryParse(localLock.Element("Expire").Value, out _expire);
    //            int.TryParse(localLock.Element("TryInterval").Value, out _tryInterval);
    //            int.TryParse(localLock.Element("Trys").Value, out _trys);
    //        }
    //        catch (Exception ex)
    //        {
    //            LogProxy.Error(ex, false);
    //        }
    //    }
    //}
    #endregion

}
