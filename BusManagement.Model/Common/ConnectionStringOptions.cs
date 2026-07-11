using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusManagement.Model.Common
{
    public class ConnectionStringOptions
    {
        public string BusManagementConnection { get; set; }
        public string EzConnection { get; set; }
        public string AgentConnection { get; set; }
        public string WorkingConnection { get; set; }
    }
}
