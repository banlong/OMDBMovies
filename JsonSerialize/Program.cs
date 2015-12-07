using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Web.Script.Serialization;

namespace JsonSerialize
{
    class Program
    {
        static void Main(string[] args) {

            //METHOD 1
            string imdbApiStr = "http://www.omdbapi.com/?s={0}&r={1}";
            string title = "Resident Evil";
            string searchTitle = title.ToLower().Replace(" ", "%20");
            string queryURL = String.Format(imdbApiStr, searchTitle, "json");

            MovieList dcl = DataContractJsonSerializerParse(queryURL);
            dcl.DisplayList();

            
            MovieList jsl = NewtonJsonParse(queryURL);
            jsl.DisplayList();

            MovieList jvl = JavaScriptSerializerParse(queryURL);
            jvl.DisplayList();

            Console.ReadKey();
        }

        static MovieList DataContractJsonSerializerParse(string url) {
            //Get response either in XML/JSON
            HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(url);
            httpRequest.Timeout = 10000;     // 10 secs
            HttpWebResponse webResponse = (HttpWebResponse)httpRequest.GetResponse();
            Stream ms = webResponse.GetResponseStream();

            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(MovieList));
            MovieList ml = (MovieList)serializer.ReadObject(ms);
            return ml;

        }


        static MovieList NewtonJsonParse(string url) {
            var json = JsonConvert.DeserializeObject<MovieList>(new WebClient().DownloadString(url));
            return json;
        }

        static MovieList JavaScriptSerializerParse(string url)
        {
            //Get response either in XML/JSON
            HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(url);
            httpRequest.Timeout = 10000;     // 10 secs
            HttpWebResponse webResponse = (HttpWebResponse)httpRequest.GetResponse();
            Stream ms = webResponse.GetResponseStream();

            using (var reader = new StreamReader(ms)){
                JavaScriptSerializer js = new JavaScriptSerializer();
                var data = js.Deserialize<MovieList>(reader.ReadToEnd());
                return data;
            }

            
        }

    }
}
