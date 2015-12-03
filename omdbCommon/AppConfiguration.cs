using Microsoft.Azure;
using System.Collections.Generic;

namespace omdbCommon
{
    //STORE COMMON DEFINITION INFO OF THE PROJECT
    public static class AppConfiguration {

        //Containers to store image and thumbnail
        public const string BlobContainerName = "images";

        //Application name, using as identity in sending and receiving Azure messages
        public const string ApplicationId = "omdbWeb";

        //API string to get the poster from source site
        public static string imdbApiStr = "http://www.omdbapi.com/?s={0}&r={1}";

    }

    //STORE DEFINITION OF THE CONNECTION STRINGS IN THE APP
    //These connection string value is changed automatically base on the environment that users choose Development(Local) or 
    //Productions(Cloud). No Connection string setup is required in Web.config because this class will get conn string from 
    //ServiceConfiguration.Local.cscfg & ServiceConfiguration.Local.cscfg
    public static class ConnectionStrings{

        public static Dictionary<string, string> GetConnStrs(){
            Dictionary<string, string> cons = new Dictionary<string, string>();
            string diagnosticsConnString = CloudConfigurationManager.GetSetting("Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString");
            string busConnString = CloudConfigurationManager.GetSetting("Microsoft.ServiceBus.ConnectionString");
            string storageConnString = CloudConfigurationManager.GetSetting("StorageConnectionString");
            string sqlConnString = CloudConfigurationManager.GetSetting("MoviesContextConnectionString");
            cons.Add("DiagnosticsConnString", diagnosticsConnString);
            cons.Add("BusConnString", busConnString);
            cons.Add("StorageConnString", storageConnString);
            cons.Add("SqlConnString", sqlConnString);

            //In order to prevent the case that both Production & Development share the same SB queue,
            //application will create 2 queues for each case
            //(will be no messages consuming race between production and development).
            string queueName = CloudConfigurationManager.GetSetting("QueueName");
            cons.Add("QueueName", queueName);

            return cons;
        }
    }

}
    
