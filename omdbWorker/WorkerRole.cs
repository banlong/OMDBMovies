using Microsoft.Azure;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using omdbCommon;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;

namespace omdbWorker
{
    public class WorkerRole : RoleEntryPoint
    {
        
        private CloudBlobContainer imagesBlobContainer;
        private MoviesContext db;
        private QueueClient Client;
        ManualResetEvent CompletedEvent = new ManualResetEvent(false);
        

        public override void Run(){
            Trace.WriteLine("WKR >>> Starting processing of messages");         
            BrokeredMessage msg = null;
            //Receiving message in Azure Queue
            while (true){
                try{
                   
                    msg = Client.Receive();
                    if (msg != null){
                        ProcessQueueMessage(msg);                     
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

            //Connect to the database 
            var dbConnString = CloudConfigurationManager.GetSetting("omdbDbConnectionString");
            db = new MoviesContext(dbConnString);

            //Create the queue if it does not exist already
            Trace.TraceInformation("WKR >>> Creating images queue");
            string connectionString = CloudConfigurationManager.GetSetting("Microsoft.ServiceBus.ConnectionString");
            var namespaceManager = NamespaceManager.CreateFromConnectionString(connectionString);
            if (!namespaceManager.QueueExists(AppConfiguration.QueueName)) {
                namespaceManager.CreateQueue(AppConfiguration.QueueName);
            }

            // Initialize the connection to Service Bus Queue
            Client = QueueClient.CreateFromConnectionString(connectionString, AppConfiguration.QueueName);
            
            // Open storage account using credentials from .cscfg file.
            var storageAccount = CloudStorageAccount.Parse
                (RoleEnvironment.GetConfigurationSettingValue("StorageConnectionString"));

            Trace.TraceInformation("WKR >>> Creating images blob container");
            var blobClient = storageAccount.CreateCloudBlobClient();
            imagesBlobContainer = blobClient.GetContainerReference(AppConfiguration.BlobContainerName);
            if (imagesBlobContainer.CreateIfNotExists()){
                // Enable public access on the newly created "images" container.
                imagesBlobContainer.SetPermissions(
                    new BlobContainerPermissions
                    {
                        PublicAccess = BlobContainerPublicAccessType.Blob
                    });
            }

            Trace.TraceInformation("WKR >>> Storage initialized");
            return base.OnStart();
        }

        public override void OnStop()
        {
            // Close the connection to Service Bus Queue
            Client.Close();
            CompletedEvent.Set();
            base.OnStop();
        }

        //PROCESS A MESSAGE
        private void ProcessQueueMessage(BrokeredMessage msg){
            Trace.TraceInformation("WKR >>> Processing queue message {0}", msg);

            //CHECK: recognize the app id? fail- abandon this message
            string appId = msg.GetBody<string>();
            if (appId != AppConfiguration.ApplicationId)
            {
                msg.Abandon();
                return;
            }

            //PROCESS: base on action value
            string act = msg.Properties["Action"].ToString();
            if (act == "DeleteAll")
            {
                DeleteAllContainerBlobs();
                msg.Complete();
                return;
            }
            else if (act == "Delete") {
                string imageUrl = msg.Properties["ImageUrl"].ToString();
                string thumbUrl = msg.Properties["ThumbURL"].ToString();
                DeleteMovieImageBlobs(imageUrl, thumbUrl);
                msg.Complete();
                return;
            }

            //GET MESSAGE CONTENT (Create)
            string imdbId = msg.Properties["imdbId"].ToString();
            string remoteFileUrl = msg.Properties["Poster"].ToString();
            var movieId = int.Parse(msg.Properties["MovieId"].ToString());

            

            //CHECK: IF no image URL, or no MovieId in the sql database, fail- delete this message
            Movie m = db.Movies.Find(movieId);          
            if (remoteFileUrl == "" | remoteFileUrl == null | m == null)
            {
                msg.Complete();
                return;
            }

            //GET REFERENCE TO THE ITEM IN DATABASE
            var imdbIdList = db.Movies.AsQueryable();
            imdbIdList = imdbIdList.Where(a => a.imdbID == imdbId);
            int itemCount = imdbIdList.Count();

            //CHECK - if no imdbId found, , fail- delete this message
            if (itemCount == 0){
                msg.Complete();
                return;
            }
            //CHECK - if there is more than one imdbId found (ie duplicate movies info), delete duplicate records
            else if (itemCount > 1)
            {
                //remove duplicate item
                Trace.TraceInformation("WKR >>> Duplicate movies detected");
                db.Movies.Remove(m);
                db.SaveChanges();
                msg.Complete();
                Trace.TraceInformation("WKR >>> Redundant item removed, message was deleted.");
                return;
            }
            else if (itemCount == 1) {
                //DEFINE BLOB NAME
                Uri blobUri = new Uri(remoteFileUrl);
                string blobName = blobUri.Segments[blobUri.Segments.Length - 1];
                CloudBlockBlob posterBlob = imagesBlobContainer.GetBlockBlobReference(blobName);             
                posterBlob.Properties.ContentType = "image/jpeg";

                //DEFINE THUMB NAME
                string thumbName = Path.GetFileNameWithoutExtension(posterBlob.Name) + "thumb.jpg";
                CloudBlockBlob thumbBlob = imagesBlobContainer.GetBlockBlobReference(thumbName);
                thumbBlob.Properties.ContentType = "image/jpeg";
                

                //DOWNLOAD IMAGE FILE
                Trace.TraceInformation("WKR >>> Downloads poster from remote web site");
                var mStream = GetImageStream(remoteFileUrl);
                
                //UPLOAD IMAGE TO BLOB, THEN DELETE FILE
                using (Stream stream = posterBlob.OpenWrite()){
                    stream.Write(mStream.ToArray(), 0,Convert.ToInt32(mStream.Length));
                    stream.Flush();
                }
                
                //CREATE THUMBBLOB FROM POSTERBLOB
                Trace.TraceInformation("WKR >>> Generates thumbnail in blob {0}", thumbName);
                using (Stream input = mStream, output = thumbBlob.OpenWrite())
                {
                    ConvertImageToThumbnailJPG(input, output);
                    thumbBlob.Properties.ContentType = "image/jpeg";
                }
                
                
                //UPDATE THE URL IN SQL DB
                var movie = (from mv in db.Movies
                             where mv.imdbID == imdbId
                             select mv).Single();

                movie.ImageURL = posterBlob.Uri.ToString();
                movie.ThumbnailURL = thumbBlob.Uri.ToString();
                db.SaveChanges();
                msg.Complete();
                mStream.Dispose();
                mStream = null;
            }
        }

        //DELETE ALL BLOBS IN IMAGE CONTAINERS
        private void DeleteAllContainerBlobs() {
            Trace.TraceInformation("WKR >>> Starts deleting container's blobs");
            var blobs = imagesBlobContainer.ListBlobs();
            if (blobs == null || blobs.Count() == 0) { return; }
            foreach (IListBlobItem blob in blobs) {
                string blobName = blob.Uri.Segments[blob.Uri.Segments.Length - 1];
                CloudBlockBlob thisBlob = imagesBlobContainer.GetBlockBlobReference(blobName);
                Trace.TraceInformation("WKR >>> Delete blob: {0}", blobName);
                thisBlob.DeleteIfExists();
            }
            Trace.TraceInformation("WKR >>> Complete deleting");
        }

        //DELETE A THUMNAIL AND IMAGE BLOBS IN IMAGE CONTAINERS
        private void DeleteMovieImageBlobs(string imageURL, string thumbURL){
            //No image blob
            if (imageURL == "") return;

            //Get the blob's names from URL string
            Uri imageUri = new Uri(imageURL);
            string imageName = imageUri.Segments[imageUri.Segments.Length - 1];
            Uri thumbUri = new Uri(thumbURL);
            string thumbName = thumbUri.Segments[thumbUri.Segments.Length - 1];

            //Delete images
            Trace.TraceInformation("WKR >>> Start deleting movie's blobs");
            CloudBlockBlob imageBlob = imagesBlobContainer.GetBlockBlobReference(imageName);
            CloudBlockBlob thumbBlob = imagesBlobContainer.GetBlockBlobReference(thumbName);
            imageBlob.DeleteIfExistsAsync();
            thumbBlob.DeleteIfExistsAsync();
            Trace.TraceInformation("WKR >>> Complete deleting");
        }

        //CONVERT AN IMAGE INTO A THUMBNAIL
        public void ConvertImageToThumbnailJPG(Stream input, Stream output)
        {
            int thumbnailsize = 80;
            int width;
            int height;
            var originalImage = new Bitmap(input);

            if (originalImage.Width > originalImage.Height)
            {
                width = thumbnailsize;
                height = thumbnailsize * originalImage.Height / originalImage.Width;
            }
            else
            {
                height = thumbnailsize;
                width = thumbnailsize * originalImage.Width / originalImage.Height;
            }

            Bitmap thumbnailImage = null;
            try
            {
                thumbnailImage = new Bitmap(width, height);

                using (Graphics graphics = Graphics.FromImage(thumbnailImage))
                {
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    graphics.DrawImage(originalImage, 0, 0, width, height);
                }

                thumbnailImage.Save(output, ImageFormat.Jpeg);
            }
            finally
            {
                if (thumbnailImage != null)
                {
                    thumbnailImage.Dispose();
                }
            }
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
    }
}

