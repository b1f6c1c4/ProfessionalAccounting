using System.Drawing;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using ProfessionalAccounting.BLL;

namespace ProfessionalAccounting
{
    // The UIApplicationDelegate for the application. This class is responsible for launching the 
    // User Interface of the application, as well as listening (and optionally responding) to 
    // application events from iOS.
    [Register("AppDelegate")]
    public class AppDelegate : UIApplicationDelegate
    {
        private UIWindow window;

        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            var w = UIScreen.MainScreen.Bounds.Width;
            var h = UIScreen.MainScreen.Bounds.Height;

            window = new UIWindow(new RectangleF(0, 0, w, h));

            Application.BusinessHelper = new MainBLL();

            var leftSideMenuController = new SettingsViewController();
            var rightSideMenuController = new RightSideMenuViewController();

            Application.Container = new SlideoutNavigationController
                                        {
                                            SlideHeight = 9999f,
                                            TopView = new ItemViewController(),
                                            MenuViewLeft = leftSideMenuController,
                                            MenuViewRight = rightSideMenuController,
                                            DisplayNavigationBarCenteral = true,
                                            DisplayNavigationBarOnLeftMenu = false,
                                            DisplayNavigationBarOnRightMenu = false,
                                            LeftMenuButtonText = null,
                                            RightMenuButtonText = null,
                                            LeftMenuEnabled = false
                                        };
            window.RootViewController = Application.Container;

            // make the window visible
            window.MakeKeyAndVisible();

            return true;
        }

        public override void DidEnterBackground(UIApplication application)
        {
            Application.BusinessHelper.SaveData();
            base.DidEnterBackground(application);
        }

        public override void WillTerminate(UIApplication application)
        {
            Application.BusinessHelper.SaveData();
            base.WillTerminate(application);
        }
    }
}
