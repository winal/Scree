using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Scree.Log;
using System.Collections;

namespace Scree.Common
{
    public static class TimeStampService
    {
        private static int LastStamp;
        private static DateTime LastDate = DateTime.Now.Date;
        private static object LockStampObj = new object();
        private static object LockIdFormatObj = new object();

        private const string IDFORMATSTRINGISNULL = "{0} : Id format string is null";
        private const string GETONEIDIDFORMATSTRINGISNULL = "GetOneId() {0} : Id format string is null";

        private static Dictionary<string, string> IdFormat = new Dictionary<string, string>();

        private static Tuple<string, string> GetStamp(DateTime dt)
        {
            int stamp = (int)dt.TimeOfDay.TotalMilliseconds;

            lock (LockStampObj)
            {
                if (LastDate < dt.Date)
                {
                    LastStamp = 0;
                }

                if (stamp > LastStamp)
                {
                    LastStamp = stamp;
                }
                else
                {
                    stamp = ++LastStamp;
                }

                LastDate = dt.Date;
            }

            return new Tuple<string, string>(Convert.ToString(stamp, 16).ToLower().PadLeft(7, '0'), stamp.ToString().PadLeft(8, '0'));
        }


        /// <summary>
        /// 本方法用于自定义对象的 Id，如果不自定义，系统默认Id为Guid
        /// format 说明
        /// <para>{0} ：当前时间</para>
        /// <para>{1} ：唯一时间戳（近似当天总毫秒数，内部保证不重复），十六进制表示，总长7位，不足左补零</para>
        /// <para>{2} ：同上，以十进制表示，总长8位，不足左补零</para>
        /// <para>{0}和{1}或者{0}和{2}结合可确保当前机器生成唯一 Id，不确保不同机器生成相同 Id，{1}和{2}不必同时引用</para>
        /// <para>{3} ：app.config中配置的AppId（在分布式环境下，每个应用程序应该确保配置完全独立的AppId），与上述配合可生成全局唯一 Id</para>
        /// <para>{4}以后可以通过 GetOneId&lt;T&gt;(params string[] args) 传入值 </para>
        /// format示例：us-{0:yyMMdd}{1}-{3} 或 d{3}{0:yyMMdd}{1}
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="format"></param>
        public static void RegisterIdFormat<T>(string format)
        {
            if (string.IsNullOrEmpty(format))
            {
                LogProxy.Fatal(string.Format(IDFORMATSTRINGISNULL, typeof(T).FullName), true);
            }

            lock (LockIdFormatObj)
            {
                IdFormat[typeof(T).FullName] = format;
            }
        }
        public static string GetOneId<T>()
        {
            return GetOneId<T>(null);
        }
        public static string GetOneId<T>(params object[] args)
        {
            //{0}时间
            DateTime dt = DateTime.Now;
            //{1}当天唯一时间戳
            Tuple<string, string> stamp = GetStamp(dt);

            string idFormat;
            bool isGetted = IdFormat.TryGetValue(typeof(T).FullName, out idFormat);

            if (isGetted)
            {
                Queue queue = new Queue();
                queue.Enqueue(dt);
                queue.Enqueue(stamp.Item1);
                queue.Enqueue(stamp.Item2);
                queue.Enqueue(AppService.AppId);
                if (args != null)
                {
                    foreach (object o in args)
                    {
                        queue.Enqueue(o);
                    }
                }
                return string.Format(idFormat, queue.ToArray());
            }

            LogProxy.Error(string.Format(GETONEIDIDFORMATSTRINGISNULL, typeof(T).FullName), true);

            return string.Empty;
        }

    }
}
