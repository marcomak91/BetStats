using System.Web;
using System.Web.Optimization;

namespace BetStats
{
    public class BundleConfig
    {
        // For more information on bundling, visit https://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {
            // Enable bundling and minification
            BundleTable.EnableOptimizations = true;
        }
    }
}
