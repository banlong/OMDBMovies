using System.Data.Entity;
using System.Threading.Tasks;
using System.Net;
using System.Web.Mvc;
using omdbCommon;
using omdbWeb.Models;
using System.Diagnostics;
using System.Linq;

namespace omdbWeb.Controllers
{
    public class MoviesController : Controller
    {
        private MoviesContext db = new MoviesContext();
        private Repository repo = new Repository();

        // GET: Movies/Search
        public ActionResult Search(){
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Search(SubmitData message)
        {
            if (message.Title == null & message.Type == null & message.Year == null) {
                //return full list if there is no input
                
                return View(await db.Movies.ToListAsync());
                
            } else {
                var ret = repo.Search(message);
                if (ret.Count == 0){
                  ModelState.AddModelError("NotFound", "Movie not found");
                }
                return View(ret);
            }
        }

        // GET: Movies/DeleteAll
        public async Task<ActionResult> DeleteAll() {
            //create SQL entry
            db.Database.ExecuteSqlCommand("TRUNCATE TABLE Movies");
            db.SaveChanges();
            repo.SendDeleteMessages(Action.DeleteAll, AppConfiguration.QueueName);
            return View("Index", await db.Movies.ToListAsync());
        }

        
        // GET: Movies/Populate
        public ActionResult Populate()
        {
            return View();
        }

        // Post: Movies/Populate
        [HttpPost]
        public async Task<ActionResult> Populate(string SearchString, string Protocol)
        {
            //get data from omdbapi.com
            var movies = repo.GetMovies(SearchString, Protocol);
            Trace.TraceInformation("WER >>> New item count {0}", movies.Count );
            if (movies != null)
            {
                //add movies to SQL database
                db.Movies.AddRange(movies);
                await db.SaveChangesAsync();

                //send msg to queue
                repo.SendMessages("Create", movies, AppConfiguration.QueueName);

                //return view with data
                return View("Index", await db.Movies.ToListAsync());
            }
            else
            {
                return View();
            }
        }

        // GET: Movies
        public async Task<ActionResult> Index()
        {
            //Accendig sort for item, using OrderbyDescending for DESC sort
            //var movies = db.Movies.OrderBy(mv => mv.imdbID);
            return View(await db.Movies.ToListAsync());
        }

        // GET: Movies/Details/5
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Movie movie = await db.Movies.FindAsync(id);
            if (movie == null)
            {
                return HttpNotFound();
            }
            return View(movie);
        }

        // GET: Movies/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Movies/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "MovieId,Title,Type,Year,imdbID,Poster,ImageURL,ThumbnailURL")] Movie movie)
        {
            if (ModelState.IsValid)
            {
                db.Movies.Add(movie);
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            return View(movie);
        }

        // GET: Movies/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Movie movie = await db.Movies.FindAsync(id);
            if (movie == null)
            {
                return HttpNotFound();
            }
            return View(movie);
        }

        // POST: Movies/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "MovieId,Title,Type,Year,imdbID,Poster,ImageURL,ThumbnailURL")] Movie movie)
        {
            if (ModelState.IsValid)
            {
                db.Entry(movie).State = EntityState.Modified;
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            return View(movie);
        }

        // GET: Movies/Delete/5
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Movie movie = await db.Movies.FindAsync(id);
            if (movie == null)
            {
                return HttpNotFound();
            }
            return View(movie);
        }

        // POST: Movies/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            Movie movie = await db.Movies.FindAsync(id);
            db.Movies.Remove(movie);
            repo.SendDeleteMessages(Action.Delete, AppConfiguration.QueueName, movie.ImageURL, movie.ThumbnailURL);
            await db.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
