using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using Scree.Log;

namespace Scree.SynServerService
{
    partial class Start : ServiceBase
    {
        public Start()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            LogProxy.Info("同步服务端启动开始");
            Scree.Core.IoC.ServiceRoot.Init();
            LogProxy.Info("同步服务端启动结束");
            // TODO: 在此处添加代码以启动服务。
        }

        protected override void OnStop()
        {
            LogProxy.Info("同步服务端停止");
            // TODO: 在此处添加代码以执行停止服务所需的关闭操作。
        }
    }
}
