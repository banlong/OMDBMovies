﻿using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace omdbCommon
{

    //THIS CLASS PROVIDES USERS ABILITIES TO INTERACTIVE WITH THE AZURE ACCOUNT
    //Users can create queue, add/delete blobs, send/receive messages
    public class AzureServiceProvider{
        
        private string busConnString, storageConnString;
        private CloudStorageAccount storageAccount;
        public QueueClient QClient;
        private CloudBlobContainer container;
        private CloudBlobClient blobClient;
        

        //CONSTRUCTOR
        public AzureServiceProvider(Dictionary<string, string> cons) {
            
            busConnString = cons["BusConnString"];
            storageConnString = cons["StorageConnString"];
            storageAccount = CloudStorageAccount.Parse(storageConnString);
            blobClient = storageAccount.CreateCloudBlobClient();
            container = blobClient.GetContainerReference(AppConfiguration.BlobContainerName);
            CreateQueue(cons["QueueName"]);
            CreateContainer();
        }

        //SEND MESSAGE TO QUEUE - REQUEST to creat thumbnail & poster
        public void SendMessages(string act, List<Movie> movies){

            foreach (Movie m in movies){
                // Create message, passing a string message for the body.
                BrokeredMessage message = new BrokeredMessage(AppConfiguration.ApplicationId);

                // Set some additional custom app-specific properties.
                message.Properties["Action"] = act;
                message.Properties["imdbId"] = m.imdbID;
                message.Properties["Poster"] = m.Poster;
                message.Properties["MovieId"] = m.MovieId;


                // Send message to the queue.
                QClient.Send(message);
                
            }
        }

        //SEND MESSAGE TO QUEUE - REQUEST to delete thumbnails & posters
        public void SendDeleteMessages(Action act, string blobUrl = "", string thumbUrl = ""){
            // Create message, passing a string message for the body.
            BrokeredMessage message = new BrokeredMessage(AppConfiguration.ApplicationId);

            // Set some additional custom app-specific properties.
            message.Properties["Action"] = act.ToString();
            message.Properties["ImageUrl"] = blobUrl;
            message.Properties["ThumbURL"] = thumbUrl;

            // Send message to the queue.
            QClient.Send(message);
        }

        //GET QUEUE REFERENCE
        private void CreateQueue(string queueName){
            //Create the queue if it does not exist already
            Trace.TraceInformation("{0}Initializing queue reference", TraceInfo.ShortTime);
            var namespaceManager = NamespaceManager.CreateFromConnectionString(busConnString);
            if (!namespaceManager.QueueExists(queueName))
            {
                namespaceManager.CreateQueue(queueName);
                Trace.TraceInformation("{0}Queue created", TraceInfo.ShortTime);
            }

            //Set the client of the queue
            QClient = QueueClient.CreateFromConnectionString(busConnString, queueName);
            Trace.TraceInformation("{0}Queue reference created", TraceInfo.ShortTime);

        }

        //CREAT THE CONTAINER FOR IMAGE
        private void CreateContainer() { 
            Trace.TraceInformation("{0}Initializing images blob container", TraceInfo.ShortTime);

            if (container.CreateIfNotExists()) {
                // Enable public access on the newly created "images" container.
                container.SetPermissions(
                    new BlobContainerPermissions
                    {
                        PublicAccess = BlobContainerPublicAccessType.Blob
                    });
                Trace.TraceInformation("{0}Container created", TraceInfo.ShortTime);
            }

            Trace.TraceInformation("{0}Container initialized", TraceInfo.ShortTime);
        }

        //CREATE IMAGE BLOBS FROM INPUT STREAM
        public Dictionary<string, string> SaveImagesToContainer(MemoryStream mStream, string posterName) {
            //DEFINE BLOB
            CloudBlockBlob posterBlob = container.GetBlockBlobReference(posterName);
            posterBlob.Properties.ContentType = "image/jpeg";

            //DEFINE THUMB NAME
            string thumbName = Path.GetFileNameWithoutExtension(posterBlob.Name) + "thumb.jpg";
            CloudBlockBlob thumbBlob = container.GetBlockBlobReference(thumbName);
            thumbBlob.Properties.ContentType = "image/jpeg";


            //CREATE POSTER IMAGE
            using (Stream stream = posterBlob.OpenWrite())
            {
                stream.Write(mStream.ToArray(), 0, Convert.ToInt32(mStream.Length));
                stream.Flush();
            }

            //CREATE THUMBBLOB FROM POSTERBLOB
            using (Stream input = mStream, output = thumbBlob.OpenWrite())
            {
                ConvertImageToThumbnailJPG(input, output);
                thumbBlob.Properties.ContentType = "image/jpeg";
            }

            Dictionary<string, string> url = new Dictionary<string, string>();
            url.Add("thumbURL", thumbBlob.Uri.ToString());
            url.Add("imageURL", posterBlob.Uri.ToString());

            return url;
        }

        //CONVERT AN IMAGE INTO A THUMBNAIL
        private void ConvertImageToThumbnailJPG(Stream input, Stream output){
            int thumbnailsize = 80;
            int width;
            int height;
            var originalImage = new Bitmap(input);

            if (originalImage.Width > originalImage.Height){
                width = thumbnailsize;
                height = thumbnailsize * originalImage.Height / originalImage.Width;
            }else {
                height = thumbnailsize;
                width = thumbnailsize * originalImage.Width / originalImage.Height;
            }

            Bitmap thumbnailImage = null;
            try {
                thumbnailImage = new Bitmap(width, height);

                using (Graphics graphics = Graphics.FromImage(thumbnailImage)) {
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    graphics.DrawImage(originalImage, 0, 0, width, height);
                }

                thumbnailImage.Save(output, ImageFormat.Jpeg);
            } finally {
                if (thumbnailImage != null) {
                    thumbnailImage.Dispose();
                }
            }
        }


        //DELETE ALL BLOBS IN IMAGE CONTAINERS
        public void DeleteAllBlobs(){
            Trace.TraceInformation("WKR >>> Starts deleting container's blobs");
            var blobs = container.ListBlobs();
            if (blobs == null || blobs.Count() == 0) { return; }
            foreach (IListBlobItem blob in blobs) {
                string blobName = blob.Uri.Segments[blob.Uri.Segments.Length - 1];
                CloudBlockBlob thisBlob = container.GetBlockBlobReference(blobName);
                thisBlob.DeleteIfExists();
                Trace.TraceInformation("WKR >>> Delete blob: {0}", blobName);
            }
            Trace.TraceInformation("WKR >>> Complete deleting");
        }


        //DELETE A THUMNAIL AND IMAGE BLOBS IN IMAGE CONTAINERS
        public void DeleteMovieImageBlobs(string imageURL, string thumbURL){
            //No image blob
            if (imageURL == "") return;

            //Get the blob's names from URL string
            Uri imageUri = new Uri(imageURL);
            string imageName = imageUri.Segments[imageUri.Segments.Length - 1];
            Uri thumbUri = new Uri(thumbURL);
            string thumbName = thumbUri.Segments[thumbUri.Segments.Length - 1];

            //Delete images
            CloudBlockBlob imageBlob = container.GetBlockBlobReference(imageName);
            CloudBlockBlob thumbBlob = container.GetBlockBlobReference(thumbName);
            Trace.TraceInformation(TraceInfo.ShortTime + "WKR >>> Deleting " + imageName);
            imageBlob.DeleteIfExistsAsync();
            Trace.TraceInformation(TraceInfo.ShortTime + "WKR >>> Deleting " + thumbName);
            thumbBlob.DeleteIfExistsAsync();
            Trace.TraceInformation("Complete deleting");
        }
    }

    
}
