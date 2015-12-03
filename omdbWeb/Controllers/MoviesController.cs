using System.Data.Entity;
using System.Threading.Tasks;
using System.Net;
using System.Web.Mvc;
using omdbCommon;
using System.Diagnostics;

namespace omdbWeb.Controllers
{
    public class MoviesController : Controller
    {
        //Azure service provider
        private AzureServiceProvider cloudServiceProvider = new AzureServiceProvider(ConnectionStrings.GetConnStrs());

        //Data provider
        private DataProvider dataProvider = new DataProvider(ConnectionStrings.GetConnStrs());

        // GET: Movies/Search
        public ActionResult Search(){
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Search(SubmitData message) {
            if (message.Title == null & message.Type == null & message.Year == null) {
                
                //return full list if there is no input
                return View(await dataProvider.ToListAsync());
                
            } else {
                var ret = dataProvider.Search(message);
                
                //Setup model state status
                if (ret.Count == 0){
                  ModelState.AddModelError("NotFound", "Movie not found");
                }
                return View(ret);
            }
        }

        // GET: Movies/DeleteAll
        public async Task<ActionResult> DeleteAll() {
            //create & execute SQL command
            dataProvider.ExecuteSqlCommand("TRUNCATE TABLE Movies");

            //send delete all request
            cloudServiceProvider.SendDeleteMessages(Action.DeleteAll);
            return View("Index", await dataProvider.ToListAsync());
        }

        
        // GET: Movies/Populate
        public ActionResult Populate(){
            return View();
        }

        // Post: Movies/Populate
        [HttpPost]
        public async Task<ActionResult> Populate(string SearchString, string Protocol) {
            //get data from omdbapi.com
            var httpHelper = new HttpServices();
            var movies = httpHelper.GetMoviesInfo(SearchString, Protocol);
            Trace.TraceInformation("WER >>> Retrieved {0} movies", movies.Count );
            if (movies != null){
                //add movies to SQL database
                await dataProvider.AddRangeAsync(movies);

                //send msg to queue
                cloudServiceProvider.SendMessages("Create", movies);

                //return view with data
                return View("Index", await dataProvider.ToListAsync());
            } else {
                return View();
            }
        }

        // GET: Movies
        public async Task<ActionResult> Index()
        {
            //Accendig sort for item, using OrderbyDescending for DESC sort
            //var movies = db.Movies.OrderBy(mv => mv.imdbID);
            return View(await dataProvider.ToListAsync());
        }

        // GET: Movies/Details/5
        public async Task<ActionResult> Details(int? id){
            if (id == null){
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Movie movie = await dataProvider.FindAsync(id);
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
                await dataProvider.AddAsync(movie);
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
            Movie movie = await dataProvider.FindAsync(id);
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
               await dataProvider.SetEntityState(movie, EntityState.Modified);
               return RedirectToAction("Index");
            }
            return View(movie);
        }

        // GET: Movies/Delete/5
        public async Task<ActionResult> Delete(int? id){
            if (id == null){
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Movie movie = await dataProvider.FindAsync(id);
            if (movie == null){
                return HttpNotFound();
            }

            //show confirm window
            return View(movie);
        }

        // POST: Movies/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            //remove movie in db
            Movie movie = await dataProvider.FindAsync(id);
            dataProvider.Remove(movie);

            //send delete images request to worker
            cloudServiceProvider.SendDeleteMessages(Action.Delete, movie.ImageURL, movie.ThumbnailURL);

            await dataProvider.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing){
                dataProvider.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
