using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using omdbCommon;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Net;

namespace omdbWeb.Models
{


    public class Repository
    {
        private Dictionary<string, IMovieParser> parsers = new Dictionary<string, IMovieParser>();
        private MoviesContext db = new MoviesContext();

        public Repository()
        {
            parsers.Add("xml", new XmlMovieParser());
            parsers.Add("json", new JSONMovieParser());
        }

        public List<Movie> GetMovies(string title, string protocol)
        {
            string searchTitle = title.ToLower().Replace(" ", "%20");
            string queryURL = String.Format(ConfigurationManager.AppSettings["OMDBApiUrl"], searchTitle, protocol.ToLower());


            //Get response either in XML/JSON
            HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(queryURL);
            httpRequest.Timeout = 10000;     // 10 secs
            HttpWebResponse webResponse = (HttpWebResponse)httpRequest.GetResponse();

            //Parse data
            return parsers[protocol.ToLower()].Parse(webResponse);

        }

        public void SendMessages(string act, List<Movie> movies, string queueName)
        {

            string connectionString = CloudConfigurationManager.GetSetting("Microsoft.ServiceBus.ConnectionString");
            QueueClient Client = QueueClient.CreateFromConnectionString(connectionString, queueName);

            foreach (Movie m in movies)
            {
                Trace.TraceInformation("Created queue message for AdId {0}", m.MovieId);

                // Create message, passing a string message for the body.
                BrokeredMessage message = new BrokeredMessage(AppConfiguration.ApplicationId);

                // Set some additional custom app-specific properties.
                message.Properties["Action"] = act;
                message.Properties["imdbId"] = m.imdbID;
                message.Properties["Poster"] = m.Poster;
                message.Properties["MovieId"] = m.MovieId;


                // Send message to the queue.
                Client.Send(message);


            }
        }

        public void SendDeleteMessages(omdbCommon.Action act, string queueName) {

            string connectionString = CloudConfigurationManager.GetSetting("Microsoft.ServiceBus.ConnectionString");
            QueueClient Client = QueueClient.CreateFromConnectionString(connectionString, queueName);
            Trace.TraceInformation("Created deleting request message");

            // Create message, passing a string message for the body.
            BrokeredMessage message = new BrokeredMessage(AppConfiguration.ApplicationId);

            // Set some additional custom app-specific properties.
            message.Properties["Action"] = act.ToString();

            // Send message to the queue.
            Client.Send(message);

        }
    }
}