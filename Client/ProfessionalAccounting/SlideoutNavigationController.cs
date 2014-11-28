using System;
using System.Drawing;
using MonoTouch.Foundation;
using MonoTouch.UIKit;

namespace ProfessionalAccounting
{
    /// <summary>
    ///     Slideout view controller.
    /// </summary>
    public sealed class SlideoutNavigationController : UIViewController
    {
        #region private attributes

        private readonly ProxyNavigationController m_InternalMenuViewLeft;
        private readonly ProxyNavigationController m_InternalMenuViewRight;
        private readonly UIViewController m_InternalTopView;
        private readonly UIPanGestureRecognizer m_PanGesture;
        private readonly UITapGestureRecognizer m_TapGesture;
        private bool m_DisplayNavigationBarCenteral;
        private bool m_DisplayNavigationBarOnSideBarLeft;
        private bool m_DisplayNavigationBarOnSideBarRight;
        private UIViewController m_ExternalContentView;
        private UIViewController m_ExternalMenuViewLeft;
        private UIViewController m_ExternalMenuViewRight;
        private bool m_IgnorePan;
        private UINavigationController m_InternalTopNavigation;
        private bool m_LeftMenuEnabled = true;
        private bool m_LeftMenuShowing = true;
        private string m_MenuTextLeft = " < Menu Left";
        private string m_MenuTextRight = "Right Menu > ";
        private float m_PanOriginX;
        private bool m_RightMenuEnabled;
        private bool m_RightMenuShowing = true;
        private bool m_ShadowShown;

        #endregion private attributes

        #region public attributes

        /// <summary>
        ///     Gets or sets the color of the background.
        /// </summary>
        /// <value>The color of the background.</value>
        public UIColor BackgroundColor
        {
            get { return m_InternalTopView.View.BackgroundColor; }
            set { m_InternalTopView.View.BackgroundColor = value; }
        }

        public float SlideHeight { get; set; }

        /// <summary>
        ///     Gets or sets the current view.
        /// </summary>
        /// <value>
        ///     The current view.
        /// </value>
        public UIViewController TopView
        {
            get { return m_ExternalContentView; }
            set
            {
                if (m_ExternalContentView == value)
                    return;
                SelectView(value);
            }
        }

        public UINavigationController TopNavigation { get { return m_InternalTopNavigation; } }

        /// <summary>
        ///     Gets or sets a value indicating whether the left menu us enabled.
        ///     If this is true then you can reach the menu. If false then all hooks to get to the menu view will be disabled.
        ///     This is only necessary when you don't want the user to get to the menu.
        /// </summary>
        /// <value><c>true</c> if left menu enabled; otherwise, <c>false</c>.</value>
        public bool LeftMenuEnabled
        {
            get { return m_LeftMenuEnabled; }
            set
            {
                if (value == m_LeftMenuEnabled)
                    return;

                if (!value)
                    Hide();

                if (m_InternalTopNavigation != null &&
                    m_InternalTopNavigation.ViewControllers.Length > 0)
                {
                    var view = m_InternalTopNavigation.ViewControllers[0];
                    view.NavigationItem.LeftBarButtonItem = value ? CreateLeftMenuButton() : null;
                }

                m_LeftMenuEnabled = value;
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether the right menu is enabled.
        ///     If this is true then you can reach the menu. If false then all hooks to get to the menu view will be disabled.
        ///     This is only necessary when you don't want the user to get to the menu.
        /// </summary>
        /// <value><c>true</c> if right menu enabled; otherwise, <c>false</c>.</value>
        public bool RightMenuEnabled
        {
            get { return m_RightMenuEnabled; }
            set
            {
                if (value == m_RightMenuEnabled)
                    return;

                if (!value)
                    Hide();

                if (m_InternalTopNavigation != null &&
                    m_InternalTopNavigation.ViewControllers.Length > 0)
                {
                    var view = m_InternalTopNavigation.ViewControllers[0];
                    view.NavigationItem.RightBarButtonItem = value ? CreateRightMenuButton() : null;
                }

                m_RightMenuEnabled = value;
            }
        }

        /// <summary>
        ///     Gets or sets the menu on the left side, also enables it, set LeftMenuEnabled to disable.
        /// </summary>
        /// <value>
        ///     The list view.
        /// </value>
        public UIViewController MenuViewLeft
        {
            get { return m_ExternalMenuViewLeft; }
            set
            {
                if (m_ExternalMenuViewLeft == value)
                    return;
                m_InternalMenuViewLeft.SetController(value);
                m_ExternalMenuViewLeft = value;
                LeftMenuEnabled = true;
            }
        }

        /// <summary>
        ///     Gets or sets the menu on the right side, also enables it, set RightMenuEnabled to disable.
        /// </summary>
        /// <value>The menu view right.</value>
        public UIViewController MenuViewRight
        {
            get { return m_ExternalMenuViewRight; }
            set
            {
                if (m_ExternalMenuViewRight == value)
                    return;
                m_InternalMenuViewRight.SetController(value);
                m_ExternalMenuViewRight = value;
                RightMenuEnabled = true;
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether there should be shadowing effects on the top view
        /// </summary>
        /// <value>
        ///     <c>true</c> if layer shadowing; otherwise, <c>false</c>.
        /// </value>
        public bool LayerShadowing { get; set; }

        /// <summary>
        ///     Gets or sets the shadow opacity.
        /// </summary>
        /// <value>The shadow opacity.</value>
        public float ShadowOpacity { get; set; }

        /// <summary>
        ///     Gets or sets the slide speed.
        /// </summary>
        /// <value>
        ///     The slide speed.
        /// </value>
        public float SlideSpeed { get; set; }

        /// <summary>
        ///     Gets a value indicating whether this <see cref="SlideoutNavigationController" /> is visible.
        /// </summary>
        /// <value>
        ///     <c>true</c> if visible; otherwise, <c>false</c>.
        /// </value>
        public bool Visible { get; private set; }

        /// <summary>
        ///     Gets or sets the width of the slide.
        /// </summary>
        /// <value>
        ///     The width of the slide.
        /// </value>
        public float SlideWidth { get; set; }

        /// <summary>
        ///     Gets or sets the left menu button text.
        /// </summary>
        /// <value>The left menu button text.</value>
        public string LeftMenuButtonText
        {
            get { return m_MenuTextLeft; }
            set
            {
                m_MenuTextLeft = value;
                if (LeftMenuEnabled)
                {
                    if (m_InternalTopNavigation.ViewControllers == null ||
                        m_InternalTopNavigation.ViewControllers.Length < 1)
                        return;

                    var view = m_InternalTopNavigation.ViewControllers[0];
                    view.NavigationItem.LeftBarButtonItem = LeftMenuEnabled ? CreateLeftMenuButton() : null;
                }
            }
        }

        /// <summary>
        ///     Gets or sets the right menu button text.
        /// </summary>
        /// <value>The right menu button text.</value>
        public string RightMenuButtonText
        {
            get { return m_MenuTextRight; }
            set
            {
                m_MenuTextRight = value;
                if (RightMenuEnabled)
                {
                    if (m_InternalTopNavigation.ViewControllers == null ||
                        m_InternalTopNavigation.ViewControllers.Length < 1)
                        return;

                    var view = m_InternalTopNavigation.ViewControllers[0];
                    view.NavigationItem.RightBarButtonItem = RightMenuEnabled ? CreateRightMenuButton() : null;
                }
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether the main navigation bar is shown.
        /// </summary>
        /// <value><c>true</c> if display main navigation bar; otherwise, <c>false</c>.</value>
        public bool DisplayNavigationBarCenteral
        {
            get { return m_DisplayNavigationBarCenteral; }
            set
            {
                m_DisplayNavigationBarCenteral = value;
                m_InternalTopNavigation.SetNavigationBarHidden(!value, false);
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether the navigation bar is shown on the left menu.
        /// </summary>
        /// <value><c>true</c> if display navigation bar on left menu; otherwise, <c>false</c>.</value>
        public bool DisplayNavigationBarOnLeftMenu
        {
            get { return m_DisplayNavigationBarOnSideBarLeft; }
            set
            {
                m_DisplayNavigationBarOnSideBarLeft = value;
                m_InternalMenuViewLeft.SetNavigationBarHidden(!value, false);
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether the navigation bar is shown on the right menu.
        /// </summary>
        /// <value><c>true</c> if display navigation bar on right menu; otherwise, <c>false</c>.</value>
        public bool DisplayNavigationBarOnRightMenu
        {
            get { return m_DisplayNavigationBarOnSideBarRight; }
            set
            {
                m_DisplayNavigationBarOnSideBarRight = value;
                m_InternalMenuViewRight.SetNavigationBarHidden(!value, false);
            }
        }

        #endregion public attributes

        /// <summary>
        ///     Initializes a new instance of the <see cref="SlideoutNavigationController" /> class.
        /// </summary>
        public SlideoutNavigationController()
        {
            SlideSpeed = 0.2f;
            SlideWidth = 245f;
            SlideHeight = 44f + 20f;
            LayerShadowing = true;
            ShadowOpacity = 0.5f;

            m_InternalMenuViewLeft = new ProxyNavigationController
                                         {
                                             ParentController = this,
                                             View = {AutoresizingMask = UIViewAutoresizing.FlexibleHeight}
                                         };
            m_InternalMenuViewRight = new ProxyNavigationController
                                          {
                                              ParentController = this,
                                              View = {AutoresizingMask = UIViewAutoresizing.FlexibleHeight}
                                          };


            m_InternalMenuViewLeft.SetNavigationBarHidden(DisplayNavigationBarOnLeftMenu, false);
            m_InternalMenuViewRight.SetNavigationBarHidden(DisplayNavigationBarOnRightMenu, false);

            m_InternalTopView = new UIViewController {View = {UserInteractionEnabled = true}};
            m_InternalTopView.View.Layer.MasksToBounds = false;

            m_TapGesture = new UITapGestureRecognizer();
            m_TapGesture.AddTarget(() => Hide());
            m_TapGesture.NumberOfTapsRequired = 1;

            m_PanGesture = new CustomGestureRecognizer
                               {
                                   Delegate = new SlideoutPanDelegate(this),
                                   MaximumNumberOfTouches = 1,
                                   MinimumNumberOfTouches = 1
                               };
            //_panGesture.AddTarget (() => Pan (_internalTopView.View));
            //_internalTopView.View.AddGestureRecognizer (_panGesture);
            m_PanGesture.AddTarget(() => Pan(m_InternalTopView.View, View));
            View.AddGestureRecognizer(m_PanGesture);
        }

        /// <summary>
        ///     Pan the specified view.
        /// </summary>
        /// <param name='view'>
        ///     View.
        /// </param>
        /// <param name="touchView">
        ///     View which contains _panGesture.
        /// </param>
        private void Pan(UIView view, UIView touchView)
        {
            try
            {
                if (m_PanGesture.State == UIGestureRecognizerState.Began)
                {
                    m_PanOriginX = view.Frame.X;
                    m_IgnorePan = false;

                    if (!Visible)
                    {
                        if (m_PanGesture.NumberOfTouches == 0)
                            return;
                        var touch = m_PanGesture.LocationOfTouch(0, view);
                        if (touch.Y > SlideHeight)
                            //if (touch.Y > SlideHeight || _internalTopNavigation.NavigationBarHidden)
                            m_IgnorePan = true;
                    }
                }
                else if (!m_IgnorePan &&
                         (m_PanGesture.State == UIGestureRecognizerState.Changed))
                {
                    var to = m_PanGesture.TranslationInView(touchView).X;

                    var t = to + m_PanGesture.VelocityInView(touchView).X * 0.25f;

                    if (Visible)
                    {
                        if (m_RightMenuShowing)
                        {
                            if (t < 0)
                                t = 0;
                            else if (t > SlideWidth)
                                t = SlideWidth;
                        }
                        else if (m_LeftMenuShowing)
                            if (t > 0)
                                t = 0;
                            else if (t < -SlideWidth)
                                t = -SlideWidth;
                    }
                    else if (t < -SlideWidth)
                        t = -SlideWidth;
                    else if (t > SlideWidth)
                        t = SlideWidth;

                    if (RightMenuEnabled && m_PanOriginX + t < 0)
                    {
                        HideLeft();
                        ShowRight();
                        ShowShadowRight();
                    }
                    else if (LeftMenuEnabled && m_PanOriginX + t > 0)
                    {
                        HideRight();
                        ShowLeft();
                        ShowShadowLeft();
                    }

                    if ((LeftMenuEnabled && (m_PanOriginX + to) >= 0) ||
                        (RightMenuEnabled && (m_PanOriginX + to) <= 0))
                        view.Frame = new RectangleF(
                            m_PanOriginX + to,
                            view.Frame.Y,
                            view.Frame.Width,
                            view.Frame.Height);
                }
                else if (!m_IgnorePan &&
                         (m_PanGesture.State == UIGestureRecognizerState.Ended ||
                          m_PanGesture.State == UIGestureRecognizerState.Cancelled))
                {
                    var velocity = m_PanGesture.VelocityInView(view).X;

                    if (Visible)
                    {
                        if ((view.Frame.X < (SlideWidth / 2) && m_LeftMenuShowing) ||
                            (view.Frame.X > -(SlideWidth / 2) && m_RightMenuShowing))
                            Hide();
                        else if (m_LeftMenuShowing)
                            UIView.Animate(
                                           SlideSpeed,
                                           0,
                                           UIViewAnimationOptions.CurveEaseInOut,
                                           () =>
                                           {
                                               view.Frame = new RectangleF(
                                                   SlideWidth,
                                                   view.Frame.Y,
                                                   view.Frame.Width,
                                                   view.Frame.Height);
                                           },
                                           () => { });
                        else if (m_RightMenuShowing)
                            UIView.Animate(
                                           SlideSpeed,
                                           0,
                                           UIViewAnimationOptions.CurveEaseInOut,
                                           () =>
                                           {
                                               view.Frame = new RectangleF(
                                                   -SlideWidth,
                                                   view.Frame.Y,
                                                   view.Frame.Width,
                                                   view.Frame.Height);
                                           },
                                           () => { });
                    }
                    else if (velocity > 800.0f ||
                             (view.Frame.X > (SlideWidth / 2)))
                    {
                        if (LeftMenuEnabled)
                            ShowMenuLeft();
                    }
                    else if (velocity < -800.0f ||
                             (view.Frame.X < -(SlideWidth / 2)))
                    {
                        if (RightMenuEnabled)
                            ShowMenuRight();
                    }
                    else
                        UIView.Animate(
                                       SlideSpeed,
                                       0,
                                       UIViewAnimationOptions.CurveEaseInOut,
                                       () => { view.Frame = new RectangleF(0, 0, view.Frame.Width, view.Frame.Height); },
                                       () => { });
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("SlideoutNavigation Pan Exception: " + e);
            }
        }

        /// <Docs>
        ///     Called after the controller’s view is loaded into memory.
        /// </Docs>
        /// <summary>
        ///     Views the did load.
        /// </summary>
        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            m_InternalTopView.View.Frame = new RectangleF(0, 0, View.Frame.Width, View.Frame.Height);
            m_InternalMenuViewLeft.View.Frame = new RectangleF(0, 0, SlideWidth, View.Frame.Height);
            m_InternalMenuViewRight.View.Frame = new RectangleF(
                View.Frame.Width - SlideWidth,
                0,
                SlideWidth,
                View.Frame.Height);

            //Add the list View
            AddChildViewController(m_InternalMenuViewLeft);
            AddChildViewController(m_InternalMenuViewRight);
            View.AddSubview(m_InternalMenuViewLeft.View);
            View.AddSubview(m_InternalMenuViewRight.View);

            //Add the parent view
            AddChildViewController(m_InternalTopView);
            View.AddSubview(m_InternalTopView.View);
        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);
            if (NavigationController != null)
                NavigationController.SetNavigationBarHidden(true, true);
        }

        public override void ViewWillDisappear(bool animated)
        {
            base.ViewWillDisappear(animated);
            if (NavigationController != null)
                NavigationController.SetNavigationBarHidden(false, true);
        }

        /// <summary>
        ///     Shows the shadow of the left side of the top view.
        /// </summary>
        private void ShowShadowLeft()
        {
            ShowShadow(-5);
        }

        /// <summary>
        ///     Shows the shadow of the right side of the top view.
        /// </summary>
        private void ShowShadowRight()
        {
            ShowShadow(5);
        }

        private void ShowShadow(float position)
        {
            //Dont need to call this twice if its already shown
            if (!LayerShadowing || m_ShadowShown)
                return;

            m_InternalTopView.View.Layer.ShadowOffset = new SizeF(position, 0);
            m_InternalTopView.View.Layer.ShadowPath = UIBezierPath.FromRect(m_InternalTopView.View.Bounds).CGPath;
            m_InternalTopView.View.Layer.ShadowRadius = 4.0f;
            m_InternalTopView.View.Layer.ShadowOpacity = ShadowOpacity;
            m_InternalTopView.View.Layer.ShadowColor = UIColor.Black.CGColor;

            m_ShadowShown = true;
        }

        /// <summary>
        ///     Hides the shadow of the top view
        /// </summary>
        private void HideShadow()
        {
            //Dont need to call this twice if its already hidden
            if (!LayerShadowing ||
                !m_ShadowShown)
                return;

            m_InternalTopView.View.Layer.ShadowOffset = new SizeF(0, 0);
            m_InternalTopView.View.Layer.ShadowRadius = 0.0f;
            m_InternalTopView.View.Layer.ShadowOpacity = 0.0f;
            m_InternalTopView.View.Layer.ShadowColor = UIColor.Clear.CGColor;
            m_ShadowShown = false;
        }

        /// <summary>
        ///     Open the left menu programmaticly.
        /// </summary>
        public void ShowMenuLeft()
        {
            //Don't show if already shown
            if (Visible)
                return;
            Visible = true;

            ShowLeft();
            HideRight();
            //Show some shadow!
            ShowShadowLeft();

            m_InternalMenuViewLeft.View.Frame = new RectangleF(0, 0, SlideWidth, View.Bounds.Height);
            if (MenuViewLeft != null)
                MenuViewLeft.ViewWillAppear(true);

            var view = m_InternalTopView.View;
            UIView.Animate(
                           SlideSpeed,
                           0,
                           UIViewAnimationOptions.CurveEaseInOut,
                           () => view.Frame = new RectangleF(SlideWidth, 0, view.Frame.Width, view.Frame.Height),
                           () =>
                           {
                               if (view.Subviews.Length > 0)
                                   view.Subviews[0].UserInteractionEnabled = false;
                               view.AddGestureRecognizer(m_TapGesture);

                               if (MenuViewLeft != null)
                                   MenuViewLeft.ViewDidAppear(true);
                           });
        }

        /// <summary>
        ///     Shows the left menu view, this is done to prevent the two menu's from being displayed at the same time.
        /// </summary>
        private void ShowLeft()
        {
            if (m_LeftMenuShowing)
                return;
            m_InternalMenuViewLeft.View.Hidden = false;
            m_LeftMenuShowing = true;
        }

        /// <summary>
        ///     Hides the left menu view, this is done to prevent the two menu's from being displayed at the same time.
        /// </summary>
        private void HideLeft()
        {
            if (!m_LeftMenuShowing)
                return;
            m_InternalMenuViewLeft.View.Hidden = true;
            m_LeftMenuShowing = false;
        }

        /// <summary>
        ///     Open the right menu programmaticly
        /// </summary>
        public void ShowMenuRight()
        {
            if (Visible)
                return;
            Visible = true;

            ShowRight();
            HideLeft();

            ShowShadowRight();

            m_InternalMenuViewRight.View.Frame = new RectangleF(
                View.Frame.Width - SlideWidth,
                0,
                SlideWidth,
                View.Bounds.Height);
            if (MenuViewRight != null)
                MenuViewRight.ViewWillAppear(true);

            var view = m_InternalTopView.View;
            UIView.Animate(
                           SlideSpeed,
                           0,
                           UIViewAnimationOptions.CurveEaseInOut,
                           () => view.Frame = new RectangleF(-SlideWidth, 0, view.Frame.Width, view.Frame.Height),
                           () =>
                           {
                               if (view.Subviews.Length > 0)
                                   view.Subviews[0].UserInteractionEnabled = false;
                               view.AddGestureRecognizer(m_TapGesture);
                               if (MenuViewRight != null)
                                   MenuViewRight.ViewDidAppear(true);
                           });
        }

        /// <summary>
        ///     Shows the right menu view, this is done to prevent the two menu's from being displayed at the same time.
        /// </summary>
        private void ShowRight()
        {
            if (m_RightMenuShowing)
                return;
            m_InternalMenuViewRight.View.Hidden = false;
            m_RightMenuShowing = true;
        }

        /// <summary>
        ///     Hides the right menu view, this is done to prevent the two menu's from being displayed at the same time.
        /// </summary>
        private void HideRight()
        {
            if (!m_RightMenuShowing)
                return;
            m_InternalMenuViewRight.View.Hidden = true;
            m_RightMenuShowing = false;
        }

        /// <summary>
        ///     Creates the menu button for the left side.
        /// </summary>
        private UIBarButtonItem CreateLeftMenuButton()
        {
            return new UIBarButtonItem(LeftMenuButtonText, UIBarButtonItemStyle.Plain, (s, e) => ShowMenuLeft());
        }

        /// <summary>
        ///     Creates the menu button for the right side.
        /// </summary>
        private UIBarButtonItem CreateRightMenuButton()
        {
            return new UIBarButtonItem(RightMenuButtonText, UIBarButtonItemStyle.Plain, (s, e) => ShowMenuRight());
        }

        /// <summary>
        ///     Selects the view.
        /// </summary>
        /// <param name='view'>
        ///     View.
        /// </param>
        public void SelectView(UIViewController view)
        {
            if (m_InternalTopNavigation != null)
            {
                m_InternalTopNavigation.RemoveFromParentViewController();
                m_InternalTopNavigation.View.RemoveFromSuperview();
                m_InternalTopNavigation.Dispose();
            }

            m_InternalTopNavigation = new UINavigationController(view)
                                          {
                                              View =
                                                  {
                                                      Frame = new RectangleF(
                                                          0,
                                                          0,
                                                          m_InternalTopView.View.Frame.Width,
                                                          m_InternalTopView.View.Frame.Height)
                                                  }
                                          };
            m_InternalTopNavigation.SetNavigationBarHidden(DisplayNavigationBarCenteral, false);

            m_InternalTopView.AddChildViewController(m_InternalTopNavigation);
            m_InternalTopView.View.AddSubview(m_InternalTopNavigation.View);

            if (LeftMenuEnabled)
                view.NavigationItem.LeftBarButtonItem = CreateLeftMenuButton();
            if (RightMenuEnabled)
                view.NavigationItem.RightBarButtonItem = CreateRightMenuButton();

            m_ExternalContentView = view;

            Hide();
        }

        /// <summary>
        ///     Hide the menu's and returns the topview to the center.
        /// </summary>
        public void Hide(bool animate = true)
        {
            //Don't hide if its not visible.
            if (!Visible)
                return;
            Visible = false;

            var view = m_InternalTopView.View;

            NSAction animation = () => { view.Frame = new RectangleF(0, 0, view.Frame.Width, view.Frame.Height); };
            NSAction finished = () =>
                                {
                                    if (view.Subviews.Length > 0)
                                        view.Subviews[0].UserInteractionEnabled = true;
                                    view.RemoveGestureRecognizer(m_TapGesture);
                                    //Hide the shadow when not needed to increase performance of the top layer!
                                    HideShadow();
                                };

            if (animate)
                UIView.Animate(SlideSpeed, 0, UIViewAnimationOptions.CurveEaseInOut, animation, finished);
            else
            {
                animation();
                finished();
            }
        }

        /// <summary>
        ///     Shoulds the autorotate to interface orientation.
        /// </summary>
        /// <returns>
        ///     The autorotate to interface orientation.
        /// </returns>
        /// <param name='toInterfaceOrientation'>
        ///     If set to <c>true</c> to interface orientation.
        /// </param>
        [Obsolete(
            "Deprecated in iOS6. Replace it with both GetSupportedInterfaceOrientations and PreferredInterfaceOrientationForPresentation",
            false)]
        public override bool ShouldAutorotateToInterfaceOrientation(UIInterfaceOrientation toInterfaceOrientation)
        {
            return true;
        }

        /// <summary>
        ///     Sets the menu navigation background image.
        /// </summary>
        /// <param name='image'>Image to be displayed as the background</param>
        /// <param name='metrics'>Metrics.</param>
        public void SetMenuNavigationBackgroundImage(UIImage image, UIBarMetrics metrics)
        {
            m_InternalMenuViewLeft.NavigationBar.SetBackgroundImage(image, metrics);
            m_InternalMenuViewRight.NavigationBar.SetBackgroundImage(image, metrics);
        }

        /// <summary>
        ///     Sets the top view navigation background image.
        /// </summary>
        /// <param name='image'>Image to be displayed as the background</param>
        /// <param name='metrics'>Metrics.</param>
        public void SetTopNavigationBackgroundImage(UIImage image, UIBarMetrics metrics)
        {
            m_InternalTopNavigation.NavigationBar.SetBackgroundImage(image, metrics);
        }

        #region Nested type: ProxyNavigationController

        /// <summary>
        ///     A proxy class for the navigation controller.
        ///     This allows the menu view to make requests to the navigation controller
        ///     and have them forwarded to the topview.
        /// </summary>
        private class ProxyNavigationController : UINavigationController
        {
            /// <summary>
            ///     Gets or sets the parent controller.
            /// </summary>
            /// <value>
            ///     The parent controller.
            /// </value>
            public SlideoutNavigationController ParentController { get; set; }

            /// <summary>
            ///     Sets the controller.
            /// </summary>
            /// <param name='viewController'>
            ///     View controller.
            /// </param>
            public void SetController(UIViewController viewController)
            {
                base.PopToRootViewController(false);
                base.PushViewController(viewController, false);
            }

            /// <Docs>
            ///     To be added.
            /// </Docs>
            /// <summary>
            ///     To be added.
            /// </summary>
            /// <param name='viewController'>
            ///     View controller.
            /// </param>
            /// <param name='animated'>
            ///     Animated.
            /// </param>
            public override void PushViewController(UIViewController viewController, bool animated)
            {
                ParentController.SelectView(viewController);
            }
        }

        #endregion

        #region Nested type: SlideoutPanDelegate

        private class CustomGestureRecognizer : UIPanGestureRecognizer
        {
            //			bool _drag;
            //			float _moveX;
            //			public override void TouchesMoved(NSSet touches, UIEvent evt)
            //			{
            //				base.TouchesMoved(touches, evt);
            //				if (this.State == UIGestureRecognizerState.Failed) return;
            //				var nowPoint = ((UITouch)touches.AnyObject).LocationInView(this.View);
            //				var prevPoint = ((UITouch)touches.AnyObject).PreviousLocationInView(this.View);
            //				_moveX += prevPoint.X - nowPoint.X;
            //
            //				if (!_drag)
            //				{
            //					if (Math.Abs(_moveX) > 20)
            //					{
            //						_drag = true;
            //					}
            //				}
            //			}
            //
            //			public override void Reset()
            //			{
            //				base.Reset();
            //				_drag = false;
            //				_moveX = 0;
            //			}
        }


        /// <summary>
        ///     A custom UIGestureRecognizerDelegate activated only when the controller
        ///     is visible or touch is within the 44.0f boundary.
        ///     Special thanks to Gerry High for this snippet!
        /// </summary>
        private class SlideoutPanDelegate : UIGestureRecognizerDelegate
        {
            private readonly SlideoutNavigationController m_Controller;

            public SlideoutPanDelegate(SlideoutNavigationController controller) { m_Controller = controller; }

            public override bool ShouldBegin(UIGestureRecognizer recognizer)
            {
                if (m_Controller.Visible)
                    return true;

                var rec = (UIPanGestureRecognizer)recognizer;
                var velocity = rec.VelocityInView(m_Controller.m_InternalTopView.View);
                return Math.Abs(velocity.X) > Math.Abs(velocity.Y);
            }

            public override bool ShouldReceiveTouch(UIGestureRecognizer recognizer, UITouch touch)
            {
                return (m_Controller.Visible ||
                        (touch.LocationInView(m_Controller.m_InternalTopView.View).Y <= m_Controller.SlideHeight)) &&
                       (m_Controller.LeftMenuEnabled || m_Controller.RightMenuEnabled);
            }
        }

        #endregion
    }
}
