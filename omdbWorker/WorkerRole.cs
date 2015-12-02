using Microsoft.Azure;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using omdbCommon;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;

namespace omdbWorker
{
    public class WorkerRole : RoleEntryPoint
    {
        
        ManualResetEvent CompletedEvent = new ManualResetEvent(false);
        AzureConnector azureSub;
        DataContext dc;

        public override void Run(){
            Trace.WriteLine("WKR >>> Starting processing of messages");         
            BrokeredMessage msg = null;
            //Receiving message in Azure Queue
            while (true){
                try{
                   
                    msg = azureSub.QClient.Receive();
                    if (msg != null){
                        ProcessQueueMessage(msg);
                        Trace.TraceWarning("Process messages {0}'", msg.Label);
                    } else {
                        Thread.Sleep(1000);
                    }
                }catch (StorageException e){
                    Trace.TraceError("WKR >>> Deleting poison queue item: '{0}'", msg.Label);
                }
            }
        }

        public override bool OnStart(){
            //Set the maximum number of concurrent connections 
            ServicePointManager.DefaultConnectionLimit = 12;

            //start setup of Azure & Data Context
            azureSub = new AzureConnector(GetConnStrs());
            Trace.TraceInformation("WKR >>> Storage initialized");

            dc = new DataContext(GetConnStrs());
            Trace.TraceInformation("WKR >>> Database Context established");
            return base.OnStart();
        }

        public override void OnStop()
        {
            // Close the connection to Service Bus Queue
            // Client.Close();
            CompletedEvent.Set();
            base.OnStop();
        }

        //PROCESS A MESSAGE
        private void ProcessQueueMessage(BrokeredMessage msg){
            Trace.TraceInformation("WKR >>> Processing queue message {0}", msg);

            //CHECK: recognize the app id? fail- abandon this message
            if (!isMessageFromWebApp(msg)) { return; }

            //PROCESS: base on action value
            string act = msg.Properties["Action"].ToString();
            if (act == "DeleteAll")
            {
                azureSub.DeleteAllBlobs();
                msg.Complete();
                return;
            }
            else if (act == "Delete") {
                string imageUrl = msg.Properties["ImageUrl"].ToString();
                string thumbUrl = msg.Properties["ThumbURL"].ToString();
                azureSub.DeleteMovieImageBlobs(imageUrl, thumbUrl);
                msg.Complete();
                Trace.TraceInformation("WKR >>> Blob {0} deleted", msg);
                return;
            }

            //GET MESSAGE CONTENT (Create)
            string imdbId = msg.Properties["imdbId"].ToString();
            string remoteFileUrl = msg.Properties["Poster"].ToString();
            var movieId = int.Parse(msg.Properties["MovieId"].ToString());

            //CHECK: no image URL?
            if (remoteFileUrl == "" | remoteFileUrl == null){
                msg.Complete();
                return;
            }

            //CHECK: SQL record for the movieId exist?
            if (dc.Count(movieId) < 1) {
                msg.Complete();
                return;
            }

            //CHECK - if no imdbId found, , fail- delete this message
            int imdbCount = dc.Count(imdbId);
            if (imdbCount < 1){
                msg.Complete();
                return;
            }

            //CHECK - if there is more than one imdbId found (ie duplicate movies info), delete duplicate records
            else if (imdbCount > 1){
                //remove duplicate item
                dc.RemoveDuplicate(movieId);
                msg.Complete();
                Trace.TraceInformation("WKR >>> Duplicate message was deleted.");
                return;
            } else if (imdbCount == 1) {
                                              
                //DOWNLOAD IMAGE FILE
                Trace.TraceInformation("WKR >>> Downloads poster from remote web site");
                var mStream = GetImageStream(remoteFileUrl);

                //DEFINE BLOB NAME
                Uri blobUri = new Uri(remoteFileUrl);
                string blobName = blobUri.Segments[blobUri.Segments.Length - 1];
                var urls = azureSub.SaveImagesToContainer(mStream, blobName);


                //UPDATE THE URL IN SQL DB
                dc.UpdateImageURL(urls, imdbId);
                msg.Complete();
                Trace.TraceInformation("WKR >>> Message process completed");
                mStream.Dispose();
                mStream = null;
            }
        }

        //CHECK WETHER MESSAGE FROM WEB APP
        private bool isMessageFromWebApp(BrokeredMessage msg) {
            string appId = msg.GetBody<string>();
            if (appId != AppConfiguration.ApplicationId){
                msg.Abandon();
                return false;
            }

            return true;
        }

        //SAVE REMOTE IMAGE LOCALLY
        //In this project: omdbMovies\csx\Debug\roles\omdbWorker\approot
        public void saveImage(string file_name, string url) {
            byte[] content;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            WebResponse response = request.GetResponse();

            Stream stream = response.GetResponseStream();

            using (BinaryReader br = new BinaryReader(stream)) {
                content = br.ReadBytes(500000);
                br.Close();
            }
            response.Close();

            FileStream fs = new FileStream(file_name, FileMode.Create);
            BinaryWriter bw = new BinaryWriter(fs);
            try {
                bw.Write(content);
            }finally{
                fs.Close();
                bw.Close();
            }
        }

        //CREATE MEMORYSTREAM OF THE REMOTE IMAGE(base on URL)
        public MemoryStream GetImageStream(string url) {
            
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            WebResponse response = request.GetResponse();
            Stream stream = response.GetResponseStream();
            MemoryStream memStream = new MemoryStream();
            //Read Response Stream into a Memory Stream
            try{ 
                byte[] block = new byte[0x1000]; // blocks of 4K.
                while (true){
                    int bytesRead = stream.Read(block, 0, block.Length);
                    if (bytesRead == 0) return memStream;
                    memStream.Write(block, 0, bytesRead);
                }
            } finally {
                stream.Close();
            }
        }

        public Dictionary<string, string> GetConnStrs() {
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

;