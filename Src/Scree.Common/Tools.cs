using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Security;
using System.Security.Cryptography;
using System.IO;
using Scree.Log;
using System.Net;
using System.Drawing;

namespace Scree.Common
{
    public static class Tools
    {
        public static string GetAssemblyPath()
        {
            if (string.IsNullOrEmpty(_assemblyPath))
            {
                if (System.Web.HttpContext.Current == null)
                {//Windows应用程序
                    _assemblyPath = AppDomain.CurrentDomain.BaseDirectory;
                }
                else
                {
                    _assemblyPath = AppDomain.CurrentDomain.BaseDirectory + "bin\\";
                }
            }

            return _assemblyPath;
        }
        private static string _assemblyPath;


        public static string GetBaseDirectory()
        {
            if (string.IsNullOrEmpty(_baseDirectory))
            {
                _baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            }

            return _baseDirectory;
        }
        private static string _baseDirectory;

        public static string MD5(string val)
        {
            if (string.IsNullOrEmpty(val))
            {
                return "";
            }
            return FormsAuthentication.HashPasswordForStoringInConfigFile(val, "MD5");
        }

        /// <summary>  
        /// 获取主机域名  
        /// </summary>  
        /// <returns></returns>  
        public static string GetHostName()
        {
            if (string.IsNullOrEmpty(CurrentHostName))
            {
                lock (LockObj)
                {
                    if (string.IsNullOrEmpty(CurrentHostName))
                    {
                        string hostName = Dns.GetHostName();
                        if (!string.IsNullOrEmpty(hostName) && hostName.Length > 32)
                        {
                            hostName = hostName.Substring(hostName.Length - 32);
                        }

                        CurrentHostName = hostName;
                    }
                }
            }

            return CurrentHostName;
        }
        private static object LockObj = new object();
        private static string CurrentHostName;
    }
}
