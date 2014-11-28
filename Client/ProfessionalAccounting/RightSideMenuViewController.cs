using MonoTouch.Dialog;
using MonoTouch.UIKit;

namespace ProfessionalAccounting
{
    internal class RightSideMenuViewController : XDialogViewController
    {
        public RightSideMenuViewController()
            : base(UITableViewStyle.Plain, new RootElement(""))
        {
            HasNavigationBar = false;
            Root.UnevenRows = true;
            RefreshRequested += (sender, e) =>
                                {
                                    Root.Clear();
                                    GetData();
                                    XReloadComplete();
                                };
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            GetData();
        }

        private void GetData()
        {
            var sec = new Section();
            foreach (var item in Application.BusinessHelper.GetBalanceItems())
            {
                var copiedItem = item;
                var stringElement = new StyledStringElement(
                    copiedItem.Head,
                    copiedItem.Balance,
                    UITableViewCellStyle.Subtitle);
                stringElement.Tapped += () => UIPasteboard.General.String = copiedItem.Data;
                sec.Add(
                        stringElement);
            }
            Root.Add(sec);
        }
    }
}
