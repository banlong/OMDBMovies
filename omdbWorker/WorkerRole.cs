using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using omdbCommon;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;

namespace omdbWorker
{
    public class WorkerRole : RoleEntryPoint {
        
        ManualResetEvent CompletedEvent = new ManualResetEvent(false);
        //AzureConnector provides all services of Azure SB Messages/Blobs
        AzureServiceProvider azureServiceProvider;

        //DataText provides ability to manupulate data base
        DataProvider dataProvider;

        

        public override void Run(){
            Trace.WriteLine("WKR >>> Starting processing of messages");         
            BrokeredMessage msg = null;
            //Receiving message in Azure Queue
            while (true){
                try{
                   
                    msg = azureServiceProvider.QClient.Receive();
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
            var cons = ConnectionStrings.GetConnStrs();
            azureServiceProvider = new AzureServiceProvider(cons);
            Trace.TraceInformation("WKR >>> Storage initialized");

            dataProvider = new DataProvider(cons);
            Trace.TraceInformation("WKR >>> Database Context established");
            return base.OnStart();
        }

        public override void OnStop()
        {
            // Close the connection to Service Bus Queue
            azureServiceProvider.QClient.Close();
            CompletedEvent.Set();
            base.OnStop();
        }

        //PROCESS A MESSAGE
        private void ProcessQueueMessage(BrokeredMessage msg){
            Trace.TraceInformation("WKR >>> Processing queue message {0}", msg);

            //CHECK: recognize the app id? fail- abandon this message
            if (!isMessageFromWebApp(msg)) { return; }

            //PROCESS: base on action value
            //Delete all records in database
            string act = msg.Properties["Action"].ToString();
            if (act == "DeleteAll")
            {
                azureServiceProvider.DeleteAllBlobs();
                msg.Complete();
                return;
            }
            //Delete a record
            else if (act == "Delete") {
                string imageUrl = msg.Properties["ImageUrl"].ToString();
                string thumbUrl = msg.Properties["ThumbURL"].ToString();
                azureServiceProvider.DeleteMovieImageBlobs(imageUrl, thumbUrl);
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
            if (dataProvider.Count(movieId) < 1) {
                msg.Complete();
                return;
            }

            //CHECK - if no imdbId found, , fail- delete this message
            int imdbCount = dataProvider.Count(imdbId);
            if (imdbCount < 1){
                msg.Complete();
                return;
            }

            //CHECK - if there is more than one imdbId found (ie duplicate movies info), delete duplicate records
            else if (imdbCount > 1){
                //remove duplicate item
                dataProvider.RemoveDuplicate(movieId);
                msg.Complete();
                Trace.TraceInformation("WKR >>> Duplicate message was deleted.");
                return;
            } else if (imdbCount == 1) {

                //DOWNLOAD IMAGE FILE
                //HttpSevice provides service of download, save, convert image to stream
                HttpServices httpHelper = new HttpServices();

                Trace.TraceInformation("WKR >>> Downloads poster from remote web site");
                var mStream = httpHelper.GetImageStream(remoteFileUrl);

                //DEFINE BLOB NAME
                Uri blobUri = new Uri(remoteFileUrl);
                string blobName = blobUri.Segments[blobUri.Segments.Length - 1];
                var urls = azureServiceProvider.SaveImagesToContainer(mStream, blobName);


                //UPDATE THE URL IN SQL DB
                dataProvider.UpdateImageURL(urls, imdbId);
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

        
    }
}