using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using log4net;
using System.IO;
using log4net.Config;
using System.Diagnostics;

namespace Scree.Log
{
    public static class LogProxy
    {
        private static readonly string ConfigDirectory = AppDomain.CurrentDomain.BaseDirectory + @"config\log4net.config";
        private static readonly ILog Logger;
        static LogProxy()
        {
            XmlConfigurator.ConfigureAndWatch(new FileInfo(ConfigDirectory));

            Logger = LogManager.GetLogger("Scree.Log");
        }

        private static void Throw(Exception exception, bool isThrowException)
        {
            if (isThrowException)
            {
                throw exception;
            }
        }

        public static void Debug(string message)
        {
            Logger.Debug(message);
            //string stackInfo = "";//new StackTrace().ToString();
            //Logger.Debug(stackInfo);
        }
        public static void DebugFormat(string format, params object[] args)
        {
            Logger.DebugFormat(format, args);
            //string stackInfo = "";//new StackTrace().ToString();
            //Logger.Debug(stackInfo);
        }
        public static void Debug(Exception exception, bool isThrowException)
        {
            Logger.Debug(exception.ToString(), exception);
            //string stackInfo = "";//new StackTrace().ToString();
            //Logger.Debug(stackInfo);
            Throw(exception, isThrowException);
        }
        public static void Debug(Exception exception, bool isThrowException, string message)
        {
            Logger.Debug(message, exception);
            //string stackInfo = "";//new StackTrace().ToString();
            //Logger.Debug(stackInfo);
            Throw(exception, isThrowException);
        }
        public static void DebugFormat(Exception exception, bool isThrowException, string format, params object[] args)
        {
            Logger.Debug(string.Format(format, args), exception);
            //string stackInfo = "";//new StackTrace().ToString();
            //Logger.Debug(stackInfo);
            Throw(exception, isThrowException);
        }

        public static void Error(string message)
        {
            Logger.Error(message);
            //string stackInfo = "";//new StackTrace().ToString();
            //Logger.Error(stackInfo);
        }
        public static void ErrorFormat(string format, params object[] args)
        {
            Logger.ErrorFormat(format, args);
            //string stackInfo = "";//new StackTrace().ToString();
            //Logger.Error(stackInfo);
        }
        public static void Error(string message, bool isThrowException)
        {
            Logger.Error(message);
            //string stackInfo = "";//new StackTrace().ToString();
            //Logger.Error(stackInfo);
            Throw(new Exception(message), isThrowException);
        }
        public static void ErrorFormat(bool isThrowException, string format, params object[] args)
        {
            string message = string.Format(format, args);
            Logger.Error(message);
            //string stackInfo = "";//new StackTrace().ToString();
            //Logger.Error(stackInfo);
            Throw(new Exception(message), isThrowException);
        }
        public static void Error(Exception exception, bool isThrowException)
        {
            Logger.Error(exception.ToString(), exception);
            //string stackInfo = "";//new StackTrace().ToString();
            //Logger.Error(stackInfo);
            Throw(exception, isThrowException);
        }
        public static void Error(Exception exception, bool isThrowException, string message)
        {
            Logger.Error(message, exception);
            //string stackInfo = "";//new StackTrace().ToString();
            //Logger.Error(stackInfo);
            Throw(exception, isThrowException);
        }
        public static void ErrorFormat(Exception exception, bool isThrowException, string format, params object[] args)
        {
            Logger.Error(string.Format(format, args), exception);
            //string stackInfo = "";//new StackTrace().ToString();
            //Logger.Error(stackInfo);
            Throw(exception, isThrowException);
        }

        public static void Fatal(string message)
        {
            Logger.Fatal(message);
            //string stackInfo = "";//new StackTrace().ToString();
            //Logger.Fatal(stackInfo);
        }
        public static void FatalFormat(string format, params object[] args)
        {
            Logger.FatalFormat(format, args);
            //string stackInfo = "";//new StackTrace().ToString();
            //Logger.Fatal(stackInfo);
        }
        public static void Fatal(string message, bool isThrowException)
        {
            Logger.Fatal(message);
            //string stackInfo = "";//new StackTrace().ToString();
            //Logger.Fatal(stackInfo);
            Throw(new Exception(message), isThrowException);
        }
        public static void FatalFormat(bool isThrowException, string format, params object[] args)
        {
            string message = string.Format(format, args);
            Logger.Fatal(message);
            //string stackInfo = "";//new StackTrace().ToString();
            //Logger.Fatal(stackInfo);
            Throw(new Exception(message), isThrowException);
        }
        public static void Fatal(Exception exception, bool isThrowException)
        {
            Logger.Fatal(exception.ToString(), exception);
            //string stackInfo = "";//new StackTrace().ToString();
            //Logger.Fatal(stackInfo);
            Throw(exception, isThrowException);
        }
        public static void Fatal(Exception exception, bool isThrowException, string message)
        {
            Logger.Fatal(message, exception);
            //string stackInfo = "";//new StackTrace().ToString();
            //Logger.Fatal(stackInfo);
            Throw(exception, isThrowException);
        }
        public static void FatalFormat(Exception exception, bool isThrowException, string format, params object[] args)
        {
            Logger.Fatal(string.Format(format, args), exception);
            //string stackInfo = "";//new StackTrace().ToString();
            //Logger.Fatal(stackInfo);
            Throw(exception, isThrowException);
        }

        public static void Info(string message)
        {
            Logger.Info(message);
            //string stackInfo = "";//new StackTrace().ToString();
            //Logger.Info(stackInfo);
        }
        public static void InfoFormat(string format, params object[] args)
        {
            Logger.InfoFormat(format, args);
            //string stackInfo = "";//new StackTrace().ToString();
            //Logger.Info(stackInfo);
        }
        public static void Info(Exception exception, bool isThrowException)
        {
            Logger.Info(exception.ToString(), exception);
            //string stackInfo = "";//new StackTrace().ToString();
            //Logger.Info(stackInfo);
            Throw(exception, isThrowException);
        }
        public static void Info(Exception exception, bool isThrowException, string message)
        {
            Logger.Info(message, exception);
            //string stackInfo = "";//new StackTrace().ToString();
            //Logger.Info(stackInfo);
            Throw(exception, isThrowException);
        }
        public static void InfoFormat(Exception exception, bool isThrowException, string format, params object[] args)
        {
            Logger.Info(string.Format(format, args), exception);
            //string stackInfo = "";//new StackTrace().ToString();
            //Logger.Info(stackInfo);
            Throw(exception, isThrowException);
        }

        public static void Warn(string message)
        {
            Logger.Warn(message);
            //string stackInfo = "";//new StackTrace().ToString();
            //Logger.Warn(stackInfo);
        }
        public static void WarnFormat(string format, params object[] args)
        {
            Logger.WarnFormat(format, args);
            //string stackInfo = "";//new StackTrace().ToString();
            //Logger.Warn(stackInfo);
        }
        public static void Warn(Exception exception, bool isThrowException)
        {
            Logger.Warn(exception.ToString(), exception);
            //string stackInfo = "";//new StackTrace().ToString();
            //Logger.Warn(stackInfo);
            Throw(exception, isThrowException);
        }
        public static void Warn(Exception exception, bool isThrowException, string message)
        {
            Logger.Warn(message, exception);
            //string stackInfo = "";//new StackTrace().ToString();
            //Logger.Warn(stackInfo);
            Throw(exception, isThrowException);
        }
        public static void WarnFormat(Exception exception, bool isThrowException, string format, params object[] args)
        {
            Logger.Warn(string.Format(format, args), exception);
            //string stackInfo = "";//new StackTrace().ToString();
            //Logger.Warn(stackInfo);
            Throw(exception, isThrowException);
        }

    }
}
