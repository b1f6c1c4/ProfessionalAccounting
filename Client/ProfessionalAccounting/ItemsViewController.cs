using System;
using System.Collections.Generic;
using System.Globalization;
using MonoTouch.Dialog;
using MonoTouch.UIKit;
using ProfessionalAccounting.BLL;
using ProfessionalAccounting.Entities;

namespace ProfessionalAccounting
{
    public class ItemViewController : XDialogViewController
    {
        public ItemViewController()
            : base(UITableViewStyle.Plain, new RootElement("添加"))
        {
            HasNavigationBar = true;
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
            foreach (var pattern in Application.BusinessHelper.GetPatterns())
            {
                var copiedPattern = pattern;
                var data = copiedPattern.GetDefaultData();
                IDictionary<int, RadioGroup> radioGroups;
                var rootElement = UIHelper.FromPatternData(data, out radioGroups, false);
                var buttonElement = new ButtonElement(
                    "添加",
                    () =>
                    {
                        Application.BusinessHelper.AddData(UIHelper.GatherData(rootElement, data.Pattern, radioGroups));
                        UIHelper.ForcePatternData(rootElement, data, radioGroups, false);
                        NavigationController.PopToRootViewController(true);
                    });
                rootElement.Add(new Section {buttonElement});
                sec.Add(rootElement);
            }

            NavigationItem.SetLeftBarButtonItem(
                                                new UIBarButtonItem(
                                                    "传输",
                                                    UIBarButtonItemStyle.Bordered,
                                                    (sender, e) =>
                                                    Application.Container.ShowMenuLeft()),
                                                true);
            NavigationItem.SetRightBarButtonItem(
                                                 new UIBarButtonItem(
                                                     "记录",
                                                     UIBarButtonItemStyle.Bordered,
                                                     (sender, e) =>
                                                     NavigationController.PushViewController(
                                                                                             new ItemsViewController(),
                                                                                             false)),
                                                 true);

            Root.Add(sec);
        }
    }

    public class ItemsViewController : XDialogViewController
    {
        public ItemsViewController()
            : base(UITableViewStyle.Plain, new RootElement("记录"))
        {
            HasNavigationBar = true;
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
            foreach (var data in Application.BusinessHelper.GetPatternDatas())
            {
                IDictionary<int, RadioGroup> radioGroups;
                var root = UIHelper.FromPatternData(data, out radioGroups);
                var rootElement = root;
                var copiedData = data;
                rootElement.Add(
                                new Section
                                    {
                                        new ButtonElement(
                                            "删除",
                                            () =>
                                            {
                                                Application.BusinessHelper.RemoveData(copiedData);
                                                sec.Remove(rootElement);
                                                NavigationController.PopViewControllerAnimated(true);
                                            })
                                    });
                sec.Add(rootElement);
            }

            NavigationItem.SetLeftBarButtonItem(
                                                new UIBarButtonItem(
                                                    "传输",
                                                    UIBarButtonItemStyle.Bordered,
                                                    (sender, e) =>
                                                    Application.Container.ShowMenuLeft()),
                                                true);
            NavigationItem.SetRightBarButtonItem(
                                                 new UIBarButtonItem(
                                                     "添加",
                                                     UIBarButtonItemStyle.Bordered,
                                                     (sender, e) =>
                                                     NavigationController.PopToRootViewController(false)),
                                                 true);

            Root.Add(sec);
        }
    }

    public static class UIHelper
    {
        public static RootElement FromPatternData(PatternData data, out IDictionary<int, RadioGroup> radioGroups,
                                                  bool isModify = true)
        {
            var pattern = data.Pattern;
            var sp = pattern.UI.Split(',');
            var root = new RootElement(isModify ? data.Name : pattern.Name);
            var sec = new Section();
            radioGroups = new Dictionary<int, RadioGroup>();
            for (var index = 0; index < sp.Length; index++)
            {
                var s = sp[index];
                var copiedIndex = index;
                if (s.StartsWith("E"))
                {
                    var spx = s.Substring(2, s.Length - 3).Split(';');
                    var kbt = UIKeyboardType.Default;
                    switch (spx[3])
                    {
                        case "":
                            kbt = UIKeyboardType.Default;
                            break;
                        case "NP":
                            kbt = UIKeyboardType.NumbersAndPunctuation;
                            break;
                    }
                    var entryElement = new EntryElement(spx[0], spx[1], data[index])
                                           {
                                               KeyboardType = kbt,
                                               Value = data[index]
                                           };
                    if (isModify)
                        entryElement.Changed += (sender, e) => data[copiedIndex] = entryElement.Value;
                    sec.Add(entryElement);
                }
                else if (s.StartsWith("GUID"))
                {
                    var spx = s.Substring(5, s.Length - 4).Split(';');
                    var entryElement = new EntryElement(spx[0], null, null);
                    if (isModify)
                        entryElement.Changed += (sender, e) => data[copiedIndex] = entryElement.Value;
                    sec.Add(entryElement);
                    if (spx[1] == "G")
                        sec.Add(
                                new ButtonElement(
                                    "生成Guid",
                                    () => entryElement.Value = Guid.NewGuid().ToString().ToUpperInvariant()));
                    else if (spx[1] == "P")
                        sec.Add(
                                new ButtonElement(
                                    "粘贴",
                                    () => entryElement.Value = UIPasteboard.General.String));
                    sec.Add(new ButtonElement("复制", () => UIPasteboard.General.String = entryElement.Value));
                }
                else if (s.StartsWith("O", StringComparison.InvariantCultureIgnoreCase))
                {
                    var spx = s.Substring(2, s.Length - 3).Split(';');
                    var radioGroup = new RadioGroup(s, Convert.ToInt32(data[index]));
                    var rx = new RootElement(spx[0], radioGroup);
                    var secI = new Section();
                    for (var i = 1; i < spx.Length; i++)
                    {
                        var radioElement = new RadioElement(spx[i], s);
                        if (isModify)
                            radioElement.Tapped +=
                                () => data[copiedIndex] = radioGroup.Selected.ToString(CultureInfo.InvariantCulture);
                        secI.Add(radioElement);
                    }
                    rx.Add(secI);
                    sec.Add(rx);
                    radioGroups.Add(index, radioGroup);
                }
                else if (s.StartsWith("DT"))
                {
                    var dateElement = new CustomDateElement(
                        s.Substring(3, s.Length - 4),
                        data[index].AsDT() ?? DateTime.Now);
                    if (isModify)
                        dateElement.DateSelected += dt => data[copiedIndex] = dateElement.DateValue.ToLocalTime().AsDT();
                    sec.Add(dateElement);
                }
            }
            root.Add(sec);
            return root;
        }

        public static PatternData GatherData(RootElement rootElement, PatternUI pattern,
                                             IDictionary<int, RadioGroup> radioGroups)
        {
            var data = pattern.GetDefaultData();
            var sp = pattern.UI.Split(',');
            var sec = rootElement[0];
            for (int index = 0, indexUI = 0; index < sp.Length; index++, indexUI++)
            {
                var s = sp[index];
                if (s.StartsWith("E"))
                {
                    var entryElement = sec[indexUI] as EntryElement;
                    if (entryElement != null)
                        data[index] = entryElement.Value;
                }
                else if (s.StartsWith("GUID"))
                {
                    var entryElement = sec[indexUI] as EntryElement;
                    if (entryElement != null)
                        data[index] = entryElement.Value;
                    indexUI += 2;
                }
                else if (s.StartsWith("O", StringComparison.InvariantCultureIgnoreCase))
                    data[index] = radioGroups[index].Selected.ToString(CultureInfo.InvariantCulture);
                else if (s.StartsWith("DT"))
                {
                    var dateElement = sec[indexUI] as DateElement;
                    if (dateElement != null)
                        data[index] = dateElement.DateValue.AsDT();
                }
            }
            return data;
        }

        public static void ForcePatternData(RootElement rootElement, PatternData data,
                                            IDictionary<int, RadioGroup> radioGroups, bool useDataName = true)
        {
            var pattern = data.Pattern;
            var sp = pattern.UI.Split(',');
            rootElement.Caption = useDataName ? data.Name : pattern.Name;
            var sec = rootElement[0];
            for (int index = 0, indexUI = 0; index < sp.Length; index++, indexUI++)
            {
                var s = sp[index];
                if (s.StartsWith("E"))
                {
                    var entryElement = sec[indexUI] as EntryElement;
                    if (entryElement != null)
                        entryElement.Value = data[index];
                }
                else if (s.StartsWith("GUID"))
                {
                    var entryElement = sec[indexUI] as EntryElement;
                    if (entryElement != null)
                        entryElement.Value = data[index];
                    indexUI += 2;
                }
                else if (s.StartsWith("O", StringComparison.InvariantCultureIgnoreCase))
                    radioGroups[index].Selected = Convert.ToInt32(data[index]);
                else if (s.StartsWith("DT"))
                {
                    var dateElement = sec[indexUI] as DateElement;
                    if (dateElement != null)
                        dateElement.DateValue = data[index].AsDT() ?? DateTime.Now;
                }
            }
        }
    }
}
