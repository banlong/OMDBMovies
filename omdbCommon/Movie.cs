using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace omdbCommon {
    //MOVIE CLASS
    public class Movie{

        public Movie() {
            Title = "";
            Year = "";
            imdbID = "";
            Poster = "";
            ImageURL = "";
            ThumbnailURL = "";
        }
        [Required]
        public int MovieId { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public Type? Type { get; set; }

        [Required]
        public string Year { get; set; }

        [Required]
        public string imdbID { get; set; }

        public string Poster { get; set; }

        [StringLength(1000)]
        [DisplayName("Full-size Image")]
        public string ImageURL { get; set; }

        [StringLength(1000)]
        [DisplayName("Thumbnail")]
        public string ThumbnailURL { get; set; }

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
