
using omdbCommon;
using System;

namespace JsonSerialization
{
    public class Class1 {
        HttpServices webService = new HttpServices();

        public void main() {
            var movies = webService.GetMoviesInfo("Bourne", "JSON");
            Console.WriteLine(movies);
        }

    }
}
