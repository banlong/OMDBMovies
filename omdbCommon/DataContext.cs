using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace omdbCommon {
    public class DataContext{

        //Connect to the database 
        private string dbConnString;
        private MoviesContext db;
        public DbSet<Movie> Movies;

        public DataContext(Dictionary<string, string> cons)
        {
            dbConnString = cons["SqlConnString"];
            db = new MoviesContext(dbConnString);
            Movies = db.Movies;

        }

        public Movie GetMovie(int id)
        {
            return Movies.Find(id);
        }

        public bool MovieExist(int id)
        {
            Movie m = Movies.Find(id);
            return (m != null);
        }

        public int Count(string imdbId) { 
            //GET REFERENCE TO THE ITEM IN DATABASE
            var imdbIdList = Movies.AsQueryable();
            imdbIdList = imdbIdList.Where(a => a.imdbID == imdbId);
            return imdbIdList.Count();
        }

        public int Count(int movieId)
        {
            var imdbIdList = Movies.AsQueryable();
            imdbIdList = imdbIdList.Where(a => a.MovieId == movieId);
            return imdbIdList.Count();
        }

        public void RemoveDuplicate(int movieId)
        {
            Trace.TraceInformation("WKR >>> Duplicate movies detected");
            Movie m = GetMovie(movieId);
            Movies.Remove(m);
            db.SaveChanges();
            Trace.TraceInformation("WKR >>> Redundant item removed");
        }


        public void UpdateImageURL(Dictionary<string, string> urls, string imdbId)
        {
            //UPDATE THE URL IN SQL DB
            var movie = (from mv in Movies
                         where mv.imdbID == imdbId
                         select mv).Single();

            movie.ImageURL = urls["imageURL"];
            movie.ThumbnailURL = urls["thumbURL"];
            db.SaveChanges();
            Trace.TraceInformation("WKR >>> URLs updated");
        }

        public void ExecuteSqlCommand(string sql)
        {
            db.Database.ExecuteSqlCommand(sql);
            db.SaveChanges();
        }

        public async Task AddRangeAsync(List<Movie> mvs)
        {
            db.Movies.AddRange(mvs);
            await db.SaveChangesAsync();
        }

        public async Task<List<Movie>> ToListAsync()
        {
            List<Movie> ret = await db.Movies.ToListAsync();
            return ret;
        }

        public async Task<Movie> FindAsync(int? id)
        {
            return await db.Movies.FindAsync(id); ;
        }

        public async Task AddAsync(Movie m)
        {
            Movies.Add(m);
            await db.SaveChangesAsync();
        }

        public async Task SetEntityState(Movie m, EntityState state)
        {
            db.Entry(m).State = state;
            await db.SaveChangesAsync();
        }

        public async Task SaveChangesAsync()
        {
            await db.SaveChangesAsync();
        }

        public void Remove(Movie m)
        {
            db.Movies.Remove(m);
        }

        public void Dispose()
        {
            db.Dispose();
        }

        public List<Movie> Search(SubmitData message){
            //get input values from client
            string title = (message.Title == null) ? "" : message.Title.Trim();
            string year = (message.Year == null) ? "" : message.Year.Trim();
            string type = (message.Type == null) ? "" : message.Type.Trim();

            List<Movie> retResult = new List<Movie>();
            var sType = GetMovieType(type);
            using (var context = new MoviesContext())
            {
                var query =
                    from movie in context.Movies
                    where (title == "" | movie.Title.Contains(title)) &&
                          (year == "" | movie.Year == year) &&
                          (type == "" | movie.Type == sType)
                    select movie;

                foreach (var m in query)
                {
                    retResult.Add(m);
                }
            }


            return retResult;

        }

        private Type GetMovieType(string type)
        {
            if (type.ToLower() == "movie")
            {
                return Type.Movie;
            }
            else if (type.ToLower() == "games")
            {
                return Type.Games;
            }
            else if (type.ToLower() == "episodes")
            {
                return Type.Episode;
            }
            else
            {
                return Type.Series;
            }
        }
    }
}

