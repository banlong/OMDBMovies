using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace omdbCommon {

    //PROVIDE DATA SERVICES
    public class DataProvider{

        private string dbConnString;
        private MoviesContext db;
        public DbSet<Movie> Movies;


        public DataProvider(Dictionary<string, string> cons) {
            //Create movie context
            dbConnString = cons["SqlConnString"];
            db = new MoviesContext(dbConnString);
            Movies = db.Movies;

        }


        //GET MOVIE BASE ON PRIMARY ID
        public Movie GetMovie(int id) { 
            return Movies.Find(id);
        }


        //CHECK MOVIE EXISTANCE BASE ON PRIMARY ID
        public bool MovieExist(int id){
            Movie m = Movies.Find(id);
            return (m != null);
        }

        //COUNT THE NUMBER OF MOVIES IN DATABASE BASED ON IMDB ID
        public int Count(string imdbId) { 
            var imdbIdList = Movies.AsQueryable();
            imdbIdList = imdbIdList.Where(a => a.imdbID == imdbId);
            return imdbIdList.Count();
        }

        //COUNT THE NUMBER OF MOVIES IN DATABASE BASED ON MOVIES ID
        public int Count(int movieId) {
            var imdbIdList = Movies.AsQueryable();
            imdbIdList = imdbIdList.Where(a => a.MovieId == movieId);
            return imdbIdList.Count();
        }

        //REMOVE DUPLICATED DATA IN DB
        public void RemoveDuplicate(int movieId){
            Trace.TraceInformation("WKR >>> Duplicate movies detected");
            Movie m = GetMovie(movieId);
            Movies.Remove(m);
            db.SaveChanges();
            Trace.TraceInformation("WKR >>> Redundant item removed");
        }

        //UPDATE URLS IN DB
        public void UpdateImageURL(Dictionary<string, string> urls, string imdbId){
            //UPDATE THE URL IN SQL DB
            var movie = (from mv in Movies
                         where mv.imdbID == imdbId
                         select mv).Single();

            movie.ImageURL = urls["imageURL"];
            movie.ThumbnailURL = urls["thumbURL"];
            db.SaveChanges();
            Trace.TraceInformation("WKR >>> URLs updated");
        }

        //EXECUTE SQL COMMAND
        public void ExecuteSqlCommand(string sql) {
            db.Database.ExecuteSqlCommand(sql);
            db.SaveChanges();
        }

        //ADD MOVIES TO DATA
        public async Task AddRangeAsync(List<Movie> mvs) {
            db.Movies.AddRange(mvs);
            await db.SaveChangesAsync();
        }

        //RETURN LIST OF MOVIES
        public async Task<List<Movie>> ToListAsync(){
            List<Movie> ret = await db.Movies.ToListAsync();
            return ret;
        }

        //GET MOVIE BASE ON PRIMARY ID
        public async Task<Movie> FindAsync(int? id){
            return await db.Movies.FindAsync(id); ;
        }

        //GET MOVIES TO DB
        public async Task AddAsync(Movie m){
            Movies.Add(m);
            await db.SaveChangesAsync();
        }

        //SET ENTITY STATE
        public async Task SetEntityState(Movie m, EntityState state){
            db.Entry(m).State = state;
            await db.SaveChangesAsync();
        }

        //SAVE CHANGES
        public async Task SaveChangesAsync(){
            await db.SaveChangesAsync();
        }

        //REMOVE A MOVIE FROM DB
        public void Remove(Movie m){
            db.Movies.Remove(m);
        }


        public void Dispose() {
            db.Dispose();
        }

        //SEARCH MOVIES BASE ON COMBO CONDITION OF TITLE, YEAR, & TYPE
        public List<Movie> Search(SubmitData message){
            //get input values from client
            string title = (message.Title == null) ? "" : message.Title.Trim();
            string year = (message.Year == null) ? "" : message.Year.Trim();
            string type = (message.Type == null) ? "" : message.Type.Trim();

            List<Movie> retResult = new List<Movie>();
            //get movie type
            var sType = GetMovieType(type);

            //LINQ query to get result
            using (var context = new MoviesContext()){
                //all matched movies --> query
                var query =
                    from movie in context.Movies
                    where (title == "" | movie.Title.Contains(title)) &&
                          (year == "" | movie.Year == year) &&
                          (type == "" | movie.Type == sType)
                    select movie;

                //add movies to the return list
                foreach (var m in query){
                    retResult.Add(m);
                }
            }


            return retResult;

        }

        //GET MOVIE TYPE
        private Type GetMovieType(string type) {
            if (type.ToLower() == "movie"){
                return Type.Movie;
            } else if (type.ToLower() == "games") {
                return Type.Games;
            } else if (type.ToLower() == "episodes"){
                return Type.Episode;
            } else {
                return Type.Series;
            }
        }
    }
}

