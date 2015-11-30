namespace omdbWeb.Models
{
    public class SubmitData{
        public SubmitData()
        {
            Title = "";
            Type = "";
            Year = "";
        }
        public string Title { get; set; }
        public string Type { get; set; }
        public string Year { get; set; }
    }
}