using System.ServiceProcess;
//
using Common.Logging;

namespace BankAgentService
{
    public partial class BankAgent : ServiceBase
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Program));
        public BankAgent()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
        }

        protected override void OnStop()
        {
        }
    }
}
