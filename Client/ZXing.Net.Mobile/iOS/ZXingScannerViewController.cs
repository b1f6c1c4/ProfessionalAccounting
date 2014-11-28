using System;
using System.Threading.Tasks;
#if __UNIFIED__
using UIKit;
using Foundation;
using AVFoundation;
using CoreGraphics;
#else
using MonoTouch.UIKit;
using MonoTouch.CoreGraphics;
using CGSize = System.Drawing.SizeF;
using CGRect = System.Drawing.RectangleF;
#endif

namespace ZXing.Mobile
{
    public class ZXingScannerViewController : UIViewController, IScannerViewController
    {
        private ZXingScannerView scannerView;

        public event Action<Result> OnScannedResult;

        public MobileBarcodeScanningOptions ScanningOptions { get; set; }
        public MobileBarcodeScanner Scanner { get; set; }

        private UIActivityIndicatorView loadingView;
        private UIView loadingBg;

        public UIView CustomLoadingView { get; set; }

        public ZXingScannerViewController(MobileBarcodeScanningOptions options, MobileBarcodeScanner scanner)
        {
            ScanningOptions = options;
            Scanner = scanner;

            var appFrame = UIScreen.MainScreen.ApplicationFrame;

            View.Frame = new CGRect(0, 0, appFrame.Width, appFrame.Height);
            View.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
        }

        public UIViewController AsViewController() { return this; }


        public void Cancel() { InvokeOnMainThread(scannerView.StopScanning); }

        private UIStatusBarStyle originalStatusBarStyle = UIStatusBarStyle.Default;

        public override void ViewDidLoad()
        {
            loadingBg = new UIView(View.Frame) {BackgroundColor = UIColor.Black};
            loadingView = new UIActivityIndicatorView(UIActivityIndicatorViewStyle.WhiteLarge);
            loadingView.Frame = new CGRect(
                (View.Frame.Width - loadingView.Frame.Width) / 2,
                (View.Frame.Height - loadingView.Frame.Height) / 2,
                loadingView.Frame.Width,
                loadingView.Frame.Height);

            loadingBg.AddSubview(loadingView);
            View.AddSubview(loadingBg);
            loadingView.StartAnimating();

            scannerView = new ZXingScannerView(new CGRect(0, 0, View.Frame.Width, View.Frame.Height));
            scannerView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
            scannerView.UseCustomOverlayView = Scanner.UseCustomOverlay;
            scannerView.CustomOverlayView = Scanner.CustomOverlay;
            scannerView.TopText = Scanner.TopText;
            scannerView.BottomText = Scanner.BottomText;
            scannerView.CancelButtonText = Scanner.CancelButtonText;
            scannerView.FlashButtonText = Scanner.FlashButtonText;

            //this.View.AddSubview(scannerView);
            View.InsertSubviewBelow(scannerView, loadingView);

            View.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
        }

        public void Torch(bool on)
        {
            if (scannerView != null)
                scannerView.SetTorch(on);
        }

        public void ToggleTorch()
        {
            if (scannerView != null)
                scannerView.ToggleTorch();
        }

        public bool IsTorchOn { get { return scannerView.IsTorchOn; } }

        public override void ViewDidAppear(bool animated)
        {
            scannerView.OnScannerSetupComplete += HandleOnScannerSetupComplete;

            originalStatusBarStyle = UIApplication.SharedApplication.StatusBarStyle;

            if (UIDevice.CurrentDevice.CheckSystemVersion(7, 0))
            {
                UIApplication.SharedApplication.StatusBarStyle = UIStatusBarStyle.Default;
                SetNeedsStatusBarAppearanceUpdate();
            }
            else
                UIApplication.SharedApplication.SetStatusBarStyle(UIStatusBarStyle.BlackTranslucent, false);

            Console.WriteLine("Starting to scan...");

            Task.Factory.StartNew(
                                  () =>
                                  {
                                      BeginInvokeOnMainThread(
                                                              () => scannerView.StartScanning(
                                                                                              ScanningOptions,
                                                                                              result =>
                                                                                              {
                                                                                                  Console.WriteLine(
                                                                                                                    "Stopping scan...");
                                                                                                  scannerView
                                                                                                      .StopScanning();
                                                                                                  var evt =
                                                                                                      OnScannedResult;
                                                                                                  if (evt != null)
                                                                                                      evt(result);
                                                                                              }));
                                  });
        }

        public override void ViewDidDisappear(bool animated)
        {
            if (scannerView != null)
                scannerView.StopScanning();

            scannerView.OnScannerSetupComplete -= HandleOnScannerSetupComplete;
        }

        public override void ViewWillDisappear(bool animated)
        {
            UIApplication.SharedApplication.SetStatusBarStyle(originalStatusBarStyle, false);
        }

        public override void DidRotate(UIInterfaceOrientation fromInterfaceOrientation)
        {
            if (scannerView != null)
                scannerView.DidRotate(InterfaceOrientation);

            //overlayView.LayoutSubviews();
        }

        public override bool ShouldAutorotate() { return true; }

        public override UIInterfaceOrientationMask GetSupportedInterfaceOrientations()
        {
            return UIInterfaceOrientationMask.All;
        }

        [Obsolete(
            "Deprecated in iOS6. Replace it with both GetSupportedInterfaceOrientations and PreferredInterfaceOrientationForPresentation"
            )]
        public override bool ShouldAutorotateToInterfaceOrientation(UIInterfaceOrientation toInterfaceOrientation)
        {
            return true;
        }

        private void HandleOnScannerSetupComplete()
        {
            BeginInvokeOnMainThread(
                                    () =>
                                    {
                                        if (loadingView != null &&
                                            loadingBg != null &&
                                            loadingView.IsAnimating)
                                        {
                                            loadingView.StopAnimating();

                                            UIView.BeginAnimations("zoomout");

                                            UIView.SetAnimationDuration(2.0f);
                                            UIView.SetAnimationCurve(UIViewAnimationCurve.EaseOut);

                                            loadingBg.Transform = CGAffineTransform.MakeScale(2.0f, 2.0f);
                                            loadingBg.Alpha = 0.0f;

                                            UIView.CommitAnimations();


                                            loadingBg.RemoveFromSuperview();
                                        }
                                    });
        }
    }
}
