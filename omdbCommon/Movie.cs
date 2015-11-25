using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace omdbCommon
{
    public class Movie{
        public int MovieId { get; set; }

        [StringLength(100)]
        public string Title { get; set; }

        public Type? Type { get; set; }

        [StringLength(4)]
        public string Year { get; set; }

        public string imdbID { get; set; }

        public string Poster { get; set; }

        [StringLength(1000)]
        [DisplayName("Full-size Image")]
        public string ImageURL { get; set; }

        [StringLength(1000)]
        [DisplayName("Thumbnail")]
        public string ThumbnailURL { get; set; }

    }

    public enum Type
    {
        Movie,
        Series,
        Episode
    }

}
