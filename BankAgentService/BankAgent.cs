using System.ServiceProcess;
//
using Common.Logging;
using System.Collections.Generic;
using ALOLAsync;
using System.Collections.Specialized;
using System.Linq;
using System;
using System.Threading;

namespace BankAgentService
{
    public partial class BankAgent : ServiceBase
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(BankAgent));
        IList<SimpleServer> listSimpleServer;
        public BankAgent()
        {
            InitializeComponent();
            this.listSimpleServer = new List<SimpleServer>();
            try
            {
                if (this.listSimpleServer.Count > 0)
                {
                    this.listSimpleServer.Clear();
                }
                NameValueCollection appSettingKeys = System.Configuration.ConfigurationManager.AppSettings;
                IEnumerable<string> keys = appSettingKeys.AllKeys.Where(k => k.IndexOf("SimpleServer") > -1);//.First();
                foreach (string str in keys)
                {
                    string[] setting = appSettingKeys[str].Split(':');
                    if (setting.Length != 5)
                    {
                        log.Error("此key:" + str + "的appSettings(5個)設定錯誤 => parameters:" + setting.Length);
                        continue;
                    };
                    int listenPort = Convert.ToInt32(setting[0]);
                    int maxBacklog = Convert.ToInt32(setting[1]);
                    string xmlNodeName = setting[2];
                    int sendTimeout = Convert.ToInt32(setting[3]);
                    int receiveTimeout = Convert.ToInt32(setting[4]);
                    SimpleServer s1 = new SimpleServer(listenPort, maxBacklog, xmlNodeName, sendTimeout, receiveTimeout);
                    listSimpleServer.Add(s1);
                }
            }
            catch (Exception ex)
            {
                log.Error("[BankAgent]Constructor Error: " + ex.StackTrace);
            }
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                log.Debug("開始啟動服務...");
                foreach (SimpleServer server in this.listSimpleServer)
                {
                    SimpleServer tmp = server;
                    ThreadPool.QueueUserWorkItem((object o) =>
                    {
                        SimpleServer server1 = o as SimpleServer;
                        server1.Start();
                    }, tmp);
                }
            }
            catch (Exception ex)
            {
                log.Error("[BankAgent][OnStart] Error:" + ex.StackTrace);
            }
        }

        protected override void OnStop()
        {
            try
            {
                log.Debug("開始關閉服務...");
                foreach (SimpleServer s in this.listSimpleServer)
                {
                    s.Stop();
                }
            }
            catch (Exception ex)
            {
                log.Error("[BankAgent][OnStop] Error:" + ex.StackTrace);
            }
        }
    }
}
