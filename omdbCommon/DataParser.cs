
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Xml;
using System.Web.Script.Serialization;

namespace omdbCommon
{
    public interface IMovieParser {
        List<Movie> Parse(HttpWebResponse httpWebResponse);
    }

    public class XmlMovieParser : IMovieParser  {

        public List<Movie> Parse(HttpWebResponse webResponse){
            List<Movie> ret = new List<Movie>();
            XmlDocument doc = new XmlDocument();
            doc.Load(webResponse.GetResponseStream());

            XmlNodeList nodes = doc.DocumentElement.SelectNodes("result");

            foreach (XmlNode node in nodes)
            {
                ret.Add(new Movie()
                {
                    Title = DataHelper.GetAttributeValue(node, "Title"),
                    Year = DataHelper.GetAttributeValue(node, "Year"),
                    imdbID = DataHelper.GetAttributeValue(node, "imdbID"),
                    Poster = DataHelper.GetAttributeValue(node, "Poster"),
                    Type = DataHelper.GetOMDBObjType(DataHelper.GetAttributeValue(node, "Type"))
                });
            }

            return ret;
        }
    }

    public class JSONMovieParser : IMovieParser {
        public List<Movie> Parse(HttpWebResponse webResponse)
        {
            List<Movie> ret = new List<Movie>();
            using (var reader = new StreamReader(webResponse.GetResponseStream()))
            {
                JavaScriptSerializer js = new JavaScriptSerializer();
                var data = js.Deserialize<dynamic>(reader.ReadToEnd());
                foreach (var dataObj in data["Search"])
                {
                    Movie newMovie = new Movie();
                    newMovie.Title = dataObj["Title"];
                    newMovie.Year = dataObj["Year"];
                    newMovie.imdbID = dataObj["imdbID"];
                    newMovie.Poster = dataObj["Poster"];
                    newMovie.Type = DataHelper.GetOMDBObjType(dataObj["Type"]);
                    ret.Add(newMovie);
                }
            }

            return ret;
        }
    }

    public static class DataHelper {
        public static string GetAttributeValue(XmlNode node, string attributeName)
        {
            return node.Attributes[attributeName] != null ? node.Attributes[attributeName].Value : string.Empty;
        }

        public static Type GetOMDBObjType(string type)
        {
            switch (type)
            {
                case "movie":
                    return Type.Movie;
                case "series":
                    return Type.Series;
                case "game":
                    return Type.Games;
                default:
                    return Type.Episode;
            }
        }
    }
}