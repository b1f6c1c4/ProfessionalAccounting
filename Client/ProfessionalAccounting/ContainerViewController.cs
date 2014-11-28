using System.Drawing;
using MonoTouch.UIKit;

namespace ProfessionalAccounting
{
    public class ContainerViewController : UIViewController
    {
        public enum SideMenuState
        {
            Closed,
            LeftMenuOpen,
            RightMenuOpen
        }

        private const float LeftMenuWidth = 200f;
        private const float RightMenuWidth = 200f;

        private UIViewController m_Center, m_Left, m_Right;
        private UIView m_MainView;
        private readonly UIView m_CenterView;
        private readonly UIView m_LeftView;
        private readonly UIView m_RightView;
        private SideMenuState m_MenuState;

        public SideMenuState MenuState
        {
            get { return m_MenuState; }
            set
            {
                m_MenuState = value;
                switch (m_MenuState)
                {
                    case SideMenuState.Closed:
                        ShiftCenterView(0);
                        HideLeftView();
                        HideRightView();
                        break;
                    case SideMenuState.LeftMenuOpen:
                        ShiftCenterView(+1);
                        ShowLeftView();
                        HideRightView();
                        break;
                    case SideMenuState.RightMenuOpen:
                        ShiftCenterView(-1);
                        HideLeftView();
                        ShowRightView();
                        break;
                }
            }
        }

        public ContainerViewController(UIViewController center, UIViewController left, UIViewController right)
        {
            m_Center = center;
            m_Left = left;
            m_Right = right;

            m_CenterView = m_Center.View;
            m_LeftView = m_Left.View;
            m_RightView = m_Right.View;
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            m_MainView = new UIView(View.Bounds)
                             {
                                 BackgroundColor = UIColor.Gray,
                                 AutoresizingMask =
                                     UIViewAutoresizing.FlexibleHeight |
                                     UIViewAutoresizing.FlexibleWidth
                             };
            View.BackgroundColor = UIColor.Blue;
            View.InsertSubview(m_MainView, 0);
            m_MainView.AddSubviews(m_CenterView, m_LeftView, m_RightView);

            var reg = new UIPanGestureRecognizer(HandlePan);
            m_MainView.AddGestureRecognizer(reg);

            m_LeftView.Frame = new RectangleF(0, m_LeftView.Frame.Y, LeftMenuWidth, m_LeftView.Frame.Height);
            m_RightView.Frame = new RectangleF(
                m_MainView.Frame.Width - RightMenuWidth,
                m_RightView.Frame.Y,
                RightMenuWidth,
                m_RightView.Frame.Height);

            MenuState = SideMenuState.Closed;
        }

        private void ShowLeftView()
        {
            m_LeftView.Hidden = false;
            m_MainView.BringSubviewToFront(m_LeftView);
        }

        private void HideLeftView() { m_LeftView.Hidden = true; }

        private void ShowRightView()
        {
            m_RightView.Hidden = false;
            m_MainView.BringSubviewToFront(m_RightView);
        }

        private void HideRightView() { m_RightView.Hidden = true; }

        private void ShiftCenterView(int dir)
        {
            switch (dir)
            {
                case 0:
                    m_CenterView.Frame = new RectangleF(
                        0,
                        m_CenterView.Frame.Y,
                        m_CenterView.Frame.Width,
                        m_CenterView.Frame.Height);
                    break;
                case 1:
                    m_CenterView.Frame = new RectangleF(
                        LeftMenuWidth,
                        m_CenterView.Frame.Y,
                        m_CenterView.Frame.Width,
                        m_CenterView.Frame.Height);
                    break;
                case -1:
                    m_CenterView.Frame = new RectangleF(
                        -RightMenuWidth,
                        m_CenterView.Frame.Y,
                        m_CenterView.Frame.Width,
                        m_CenterView.Frame.Height);
                    break;
            }
        }

        private void HandlePan(object recognizer)
        {
            var reg = recognizer as UIPanGestureRecognizer;
            if (reg.State == UIGestureRecognizerState.Ended)
                if (reg.TranslationInView(m_MainView).X + 0.35 * reg.VelocityInView(m_MainView).X > LeftMenuWidth / 2)
                    switch (MenuState)
                    {
                        case SideMenuState.Closed:
                            MenuState = SideMenuState.LeftMenuOpen;
                            break;
                        case SideMenuState.RightMenuOpen:
                            MenuState = SideMenuState.Closed;
                            break;
                    }
                else if (reg.TranslationInView(m_MainView).X + 0.35 * reg.VelocityInView(m_MainView).X <
                         - RightMenuWidth / 2)
                    switch (MenuState)
                    {
                        case SideMenuState.Closed:
                            MenuState = SideMenuState.RightMenuOpen;
                            break;
                        case SideMenuState.LeftMenuOpen:
                            MenuState = SideMenuState.Closed;
                            break;
                    }
        }
    }
}
