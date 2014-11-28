using MonoTouch.UIKit;
using ProfessionalAccounting.BLL;

namespace ProfessionalAccounting
{
    public class Application
    {
        public static MainBLL BusinessHelper;
        public static SlideoutNavigationController Container;

        // This is the main entry point of the application.
        private static void Main(string[] args)
        {
            // if you want to use a different Application Delegate class from "AppDelegate"
            // you can specify it here.
            UIApplication.Main(args, null, "AppDelegate");
        }
    }
}
