using System.Data.Entity;

namespace omdbCommon
{
    
    public class MoviesContext : DbContext{

        public MoviesContext() : base("name=MoviesContext"){
        }


        public MoviesContext(string connString) : base(connString){
        }

        public DbSet<Movie> Movies { get; set; }
    }

}
