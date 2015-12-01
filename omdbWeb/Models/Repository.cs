using Microsoft.Azure;
using Microsoft.ServiceBus.Messaging;
using omdbCommon;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net;

namespace omdbWeb.Models
{


    public class Repository
    {
        private Dictionary<string, IMovieParser> parsers = new Dictionary<string, IMovieParser>();
        private MoviesContext db = new MoviesContext();
       

        public Repository() {
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
                Trace.TraceInformation("WER >>> Created queue message for movieId {0}", m.MovieId);

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

        public void SendDeleteMessages(omdbCommon.Action act, string queueName, string blobUrl = "", string thumbUrl = ""){

            string connectionString = CloudConfigurationManager.GetSetting("Microsoft.ServiceBus.ConnectionString");
            QueueClient Client = QueueClient.CreateFromConnectionString(connectionString, queueName);
            Trace.TraceInformation("WER >>> Created deleting request message");

            // Create message, passing a string message for the body.
            BrokeredMessage message = new BrokeredMessage(AppConfiguration.ApplicationId);

            // Set some additional custom app-specific properties.
            message.Properties["Action"] = act.ToString();
            message.Properties["ImageUrl"] = blobUrl;
            message.Properties["ThumbURL"] = thumbUrl;

            // Send message to the queue.
            Client.Send(message);

        }

        public List<Movie> Search(SubmitData message)
        {
            //get input values from client
            string title = (message.Title == null) ? "" : message.Title.Trim();
            string year = (message.Year == null) ? "" : message.Year.Trim();
            string type = (message.Type == null) ? "" : message.Type.Trim();

            List<Movie> retResult = new List<Movie>();
            var sType = GetMovieType(type);
            using (var context = new MoviesContext()){
                var query =
                    from movie in context.Movies
                    where (title == "" | movie.Title.Contains(title)) &&
                          (year == "" | movie.Year == year ) &&
                          (type =="" | movie.Type == sType)
                    select movie;

                foreach (var m in query) {
                    retResult.Add(m);
                }
            }


            return retResult;

        }

        private omdbCommon.Type GetMovieType(string type) {
            if (type.ToLower() == "movie")
            {
                return omdbCommon.Type.Movie;
            }
            else if (type.ToLower() == "games")
            {
                return omdbCommon.Type.Games;
            }
            else if (type.ToLower() == "episodes")
            {
                return omdbCommon.Type.Episode;
            }
            else {
                return omdbCommon.Type.Series;
            }
        }
    }
}