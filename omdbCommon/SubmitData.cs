namespace omdbCommon{

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