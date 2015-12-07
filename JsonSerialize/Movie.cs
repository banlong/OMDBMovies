using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace JsonSerialize
{
    //MOVIE CLASS
    [DataContract]
    public class Movie{

        public Movie() {
            Title = "";
            Year = "";
            imdbID = "";
            Poster = "";
            ImageURL = "";
            ThumbnailURL = "";
        }
        [DataMember(Name = "MovieId", IsRequired = false)]
        public int MovieId { get; set; }

        [DataMember(Name = "Title", IsRequired = true)]
        public string Title { get; set; }

        [DataMember(Name = "Type", IsRequired = true)]
        public string Type { get; set; }

        [DataMember(Name = "Year", IsRequired = true)]
        public string Year { get; set; }

        [DataMember(Name = "imdbID", IsRequired = true)]
        public string imdbID { get; set; }

        [DataMember]
        public string Poster { get; set; }

        
        [DisplayName("Full-size Image")]
        [DataMember(Name = "ImageURL")]
        public string ImageURL { get; set; }

        [DisplayName("Thumbnail")]
        [DataMember(Name = "ThumbnailURL")]
        public string ThumbnailURL { get; set; }

    }

    [DataContract]
    public class MovieList
    {
        [DataMember]
        public List<Movie> Search { get; set; }

        public void DisplayList()
        {
            foreach (Movie mv in Search) {
                Trace.TraceInformation(mv.Title);
                Trace.TraceInformation(mv.Year);
            }
            Trace.TraceInformation("-------------------------------------------");
        }
    }
}
