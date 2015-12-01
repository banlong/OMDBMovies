
using Microsoft.Azure;
using System.Data.Entity;

namespace omdbCommon
{
    
    public class MoviesContext : DbContext{

        public MoviesContext() : base(CloudConfigurationManager.GetSetting("MoviesContextConnectionString")){ }

        
        public MoviesContext(string connString) : base(connString){}

        public DbSet<Movie> Movies { get; set; }
    }

}
