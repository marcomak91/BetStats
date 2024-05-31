using System.Web.Mvc;

public class HomeController : Controller
{
     public HomeController()
    {

    }

    public ActionResult Index()
    {
        return View();
    }
}
