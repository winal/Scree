using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Scree.Log;
using System.Collections;
using System.Xml.Linq;

namespace Scree.Common
{
    public static class AppService
    {
        private static readonly string ConfigDirectory = AppDomain.CurrentDomain.BaseDirectory + @"config\app.config";

        public static string AppId
        {
            get
            {
                return _appId;
            }
        }
        private static readonly string _appId = "";

        static AppService()
        {
            try
            {
                XElement root = XElement.Load(ConfigDirectory);
                if (root != null)
                {
                    XElement localLock = root.Element("AppId");
                    if (localLock != null)
                    {
                        _appId = localLock.Value ?? "";
                    }
                }
            }
            catch (Exception ex)
            {
                LogProxy.Error(ex, false);
            }
        }

    }
}
