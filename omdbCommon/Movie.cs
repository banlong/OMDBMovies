﻿using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace omdbCommon {
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
        public Type? Type { get; set; }

        [DataMember(Name = "Year", IsRequired = true)]
        public string Year { get; set; }

        [DataMember(Name = "imdbID", IsRequired = true)]
        public string imdbID { get; set; }

        [DataMember]
        public string Poster { get; set; }

        [StringLength(1000)]
        [DisplayName("Full-size Image")]
        [DataMember(Name = "ImageURL")]
        public string ImageURL { get; set; }

        [StringLength(1000)]
        [DisplayName("Thumbnail")]
        [DataMember(Name = "ThumbnailURL")]
        public string ThumbnailURL { get; set; }

    }

    [DataContract]
    public class MovieList
    {
        [DataMember]
        public List<Movie> Search { get; set; }
    }


    //MOVIE TYPES
    public enum Type
    {
        Movie,
        Series,
        Episode,
        Games
    }

    //MESSAGE TYPES
    public enum Action {
        Create,
        Delete,
        DeleteAll
    }

    //Using this data struct to transmit data in a search
    public class SubmitData{
        public SubmitData(){
            Title = "";
            Type = "";
            Year = "";
        }

        public string Title { get; set; }
        public string Type { get; set; }
        public string Year { get; set; }
    }


}
