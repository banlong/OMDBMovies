using Microsoft.Azure;
using System.Collections.Generic;

namespace omdbWeb.Models
{
    public static class Connection {
        public static Dictionary<string, string> GetConnStrs() {
            Dictionary<string, string> cons = new Dictionary<string, string>();
            string diagnosticsConnString = CloudConfigurationManager.GetSetting("Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString");
            string busConnString = CloudConfigurationManager.GetSetting("Microsoft.ServiceBus.ConnectionString");
            string storageConnString = CloudConfigurationManager.GetSetting("StorageConnectionString");
            string sqlConnString = CloudConfigurationManager.GetSetting("MoviesContextConnectionString");
            cons.Add("DiagnosticsConnString", diagnosticsConnString);
            cons.Add("BusConnString", busConnString);
            cons.Add("StorageConnString", storageConnString);
            cons.Add("SqlConnString", sqlConnString);

            return cons;
        }
    }
}