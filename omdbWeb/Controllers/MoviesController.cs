using System.Data.Entity;
using System.Threading.Tasks;
using System.Net;
using System.Web.Mvc;
using omdbCommon;
using omdbWeb.Models;
using System.Diagnostics;

namespace omdbWeb.Controllers
{
    public class MoviesController : Controller
    {
        //private MoviesContext db = new MoviesContext();
        //private Repository repo = new Repository();

        private AzureConnector azureSub = new AzureConnector(Connection.GetConnStrs());
        private DataContext dc = new DataContext(Connection.GetConnStrs());

        // GET: Movies/Search
        public ActionResult Search(){
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Search(SubmitData message)
        {
            if (message.Title == null & message.Type == null & message.Year == null) {
                //return full list if there is no input
                
                return View(await dc.ToListAsync());
                
            } else {
                var ret = dc.Search(message);
                if (ret.Count == 0){
                  ModelState.AddModelError("NotFound", "Movie not found");
                }
                return View(ret);
            }
        }

        // GET: Movies/DeleteAll
        public async Task<ActionResult> DeleteAll() {
            //create SQL entry
            dc.ExecuteSqlCommand("TRUNCATE TABLE Movies");
            azureSub.SendDeleteMessages(Action.DeleteAll);
            return View("Index", await dc.ToListAsync());
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
            var httpHelper = new HttpServices();
            var movies = httpHelper.GetMovies(SearchString, Protocol);
            Trace.TraceInformation("WER >>> New item count {0}", movies.Count );
            if (movies != null)
            {
                //add movies to SQL database
                await dc.AddRangeAsync(movies);
                //send msg to queue
                azureSub.SendMessages("Create", movies);

                //return view with data
                return View("Index", await dc.ToListAsync());
            } else {
                return View();
            }
        }

        // GET: Movies
        public async Task<ActionResult> Index()
        {
            //Accendig sort for item, using OrderbyDescending for DESC sort
            //var movies = db.Movies.OrderBy(mv => mv.imdbID);
            return View(await dc.ToListAsync());
        }

        // GET: Movies/Details/5
        public async Task<ActionResult> Details(int? id){
            if (id == null){
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Movie movie = await dc.FindAsync(id);
            if (movie == null){
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
            if (ModelState.IsValid) {
                await dc.AddAsync(movie);
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
            Movie movie = await dc.FindAsync(id);
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
               await dc.SetEntityState(movie, EntityState.Modified);
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
            Movie movie = await dc.FindAsync(id);
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
            Movie movie = await dc.FindAsync(id);
            dc.Remove(movie);
            azureSub.SendDeleteMessages(Action.Delete, movie.ImageURL, movie.ThumbnailURL);
            await dc.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                dc.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
