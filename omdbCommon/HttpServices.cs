using System;
using System.Collections.Generic;
using System.Net;

namespace omdbCommon
{
    public class HttpServices {
        private Dictionary<string, IMovieParser> parsers;

        public HttpServices() {
            parsers = new Dictionary<string, IMovieParser>();
            parsers.Add("xml", new XmlMovieParser());
            parsers.Add("json", new JSONMovieParser());
        }

        public List<Movie> GetMovies(string title, string protocol)
        {
            string searchTitle = title.ToLower().Replace(" ", "%20");
            string queryURL = String.Format(AppConfiguration.imdbApiStr, searchTitle, protocol.ToLower());


            //Get response either in XML/JSON
            HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(queryURL);
            httpRequest.Timeout = 10000;     // 10 secs
            HttpWebResponse webResponse = (HttpWebResponse)httpRequest.GetResponse();

            //Parse data
            return parsers[protocol.ToLower()].Parse(webResponse);

        }
    }
}
