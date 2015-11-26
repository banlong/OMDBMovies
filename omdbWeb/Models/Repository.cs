using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using omdbCommon;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;

namespace omdbWeb.Models
{


    public class Repository
    {
        private Dictionary<string, IMovieParser> _parsers = new Dictionary<string, IMovieParser>();
        private MoviesContext db = new MoviesContext();

        public Repository()
        {
            _parsers.Add("xml", new XmlMovieParser());
            _parsers.Add("json", new JSONMovieParser());
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
            return _parsers[protocol.ToLower()].Parse(webResponse);

        }

        public void AppendToAzueQueue(List<Movie> movies, string queueName){
            string appId = "omdbWeb";
            string connectionString = CloudConfigurationManager.GetSetting("Microsoft.ServiceBus.ConnectionString");
            QueueClient Client = QueueClient.CreateFromConnectionString(connectionString, queueName);

            foreach (Movie m in movies){
                // Create message, passing a string message for the body.
                BrokeredMessage message = new BrokeredMessage(appId);

                // Set some additional custom app-specific properties.
                message.Properties["imdbId"] = m.imdbID;
                message.Properties["Poster"] = m.Poster;


                // Send message to the queue.
                Client.Send(message);

                
            }
        }
    }
}