using System;
using System.Drawing;
using MonoTouch.Dialog;
using MonoTouch.Foundation;
using MonoTouch.UIKit;

namespace ProfessionalAccounting
{
    public class XDialogViewController : DialogViewController
    {
        public XDialogViewController(RootElement root) : base(root) { }
        public XDialogViewController(UITableViewStyle style, RootElement root) : base(style, root) { }
        public XDialogViewController(RootElement root, bool pushing) : base(root, pushing) { }

        public XDialogViewController(UITableViewStyle style, RootElement root, bool pushing)
            : base(style, root, pushing) {}

        public bool HasNavigationBar { get; set; }

        public override RefreshTableHeaderView MakeRefreshTableHeaderView(RectangleF rect)
        {
            return new ReturnRefreshView(rect);
        }

        protected void XReloadComplete()
        {
            base.ReloadComplete();

            var view = View as UIScrollView;
            if (view != null)
            {
                var ci = view.ContentInset;
                ci.Top = HasNavigationBar ? 64 : 20;
                view.ContentInset = ci;
            }
        }
    }

    public class ReturnRefreshView : RefreshTableHeaderView
    {
        public ReturnRefreshView(RectangleF rect) : base(rect) { }

        public override void CreateViews()
        {
            base.CreateViews();
            LastUpdateLabel.TextColor = UIColor.Clear;
        }

        public override void SetStatus(RefreshViewStatus status)
        {
            switch (status)
            {
                case RefreshViewStatus.Loading:
                    StatusLabel.Text = "加载中…";
                    break;
                case RefreshViewStatus.PullToReload:
                    StatusLabel.Text = "下拉" + ToolTip();
                    break;
                case RefreshViewStatus.ReleaseToReload:
                    StatusLabel.Text = "松开" + ToolTip();
                    break;
            }
        }

        protected virtual string ToolTip() { return "返回并刷新"; }
    }

    public class CustomDateElement : DateElement
    {
        public CustomDateElement(string caption, DateTime date) : base(caption, date) { }

        public override UIDatePicker CreatePicker()
        {
            var p = base.CreatePicker();
            p.Locale = new NSLocale("zh_CN");
            p.TimeZone = new NSTimeZone("GMT+0800");
            return p;
        }
    }

    public static class DismissKeyboard
    {
        public static bool Dismiss(UITextField textField)
        {
            textField.ResignFirstResponder();
            return true;
        }
    }

    public class ButtonElement : StyledStringElement
    {
        public ButtonElement(string caption, NSAction tapped) : base(caption, tapped)
        {
            TextColor = UIColor.FromRGB(0, 122, 255);
        }
    }

    /*    public static class Cracker
    {
        public static T GetPrivateField<T>(this object instance, string fieldname)
        {
            BindingFlags flag = BindingFlags.Instance | BindingFlags.NonPublic;
            Type type = instance.GetType();
            FieldInfo field = type.GetField(fieldname, flag);
            return (T)field.GetValue(instance);
        }

        public static T GetPrivateProperty<T>(this object instance, string propertyname)
        {
            BindingFlags flag = BindingFlags.Instance | BindingFlags.NonPublic;
            Type type = instance.GetType();
            PropertyInfo field = type.GetProperty(propertyname, flag);
            return (T)field.GetValue(instance, null);
        }

        public static void SetPrivateField(this object instance, string fieldname, object value)
        {
            BindingFlags flag = BindingFlags.Instance | BindingFlags.NonPublic;
            Type type = instance.GetType();
            FieldInfo field = type.GetField(fieldname, flag);
            field.SetValue(instance, value);
        }

        public static void SetPrivateProperty(this object instance, string propertyname, object value)
        {
            BindingFlags flag = BindingFlags.Instance | BindingFlags.NonPublic;
            Type type = instance.GetType();
            PropertyInfo field = type.GetProperty(propertyname, flag);
            field.SetValue(instance, value, null);
        }

        public static T CallPrivateMethod<T>(this object instance, string name, params object[] param)
        {
            BindingFlags flag = BindingFlags.Instance | BindingFlags.NonPublic;
            Type type = instance.GetType();
            MethodInfo method = type.GetMethod(name, flag);
            return (T)method.Invoke(instance, param);
        }
        public static void CallPrivateMethod(this object instance, string name, params object[] param)
        {
            BindingFlags flag = BindingFlags.Instance | BindingFlags.NonPublic;
            Type type = instance.GetType();
            MethodInfo method = type.GetMethod(name, flag);
            method.Invoke(instance, param);
        }
    }*/
}
