using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using MonoTouch.Dialog;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using ZXing;
using ZXing.Mobile;

namespace ProfessionalAccounting
{
    internal class SettingsViewController : DialogViewController
    {
        public SettingsViewController() : base(UITableViewStyle.Grouped, new RootElement("")) { }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            var ipElement = new EntryElement("IP ", "", NSUserDefaults.StandardUserDefaults.StringForKey("IP"));
            var portElement = new EntryElement("�˿�  ", "", NSUserDefaults.StandardUserDefaults.StringForKey("Port"));
            var timeElement = new StringElement("��δ����");

            Action<IPAddress, int> exData =
                (ip, port) => Application.BusinessHelper.ExchangeDataAsync(new IPEndPoint(ip, port));
            Root.Add(
                     new Section
                         {
                             ipElement,
                             portElement,
                             timeElement,
                             new ButtonElement(
                                 "��������",
                                 () =>
                                 {
                                     NSUserDefaults.StandardUserDefaults.SetString(ipElement.Value, "IP");
                                     NSUserDefaults.StandardUserDefaults.SetString(portElement.Value, "Port");
                                     try
                                     {
                                         InvokeOnMainThread(() => timeElement.Caption = "���ڴ���...");
                                         exData(IPAddress.Parse(ipElement.Value), Convert.ToInt32(portElement.Value));
                                         InvokeOnMainThread(() => timeElement.Caption = DateTime.Now.ToString("t"));
                                     }
                                     catch (Exception e)
                                     {
                                         Debug.Print(e.ToString());
                                     }
                                 }),
                             new ButtonElement(
                                 "ɨ�貢����",
                                 () =>
                                 {
                                     Action<Task<Result>> callback = t =>
                                                                     {
                                                                         if (t.Result == null)
                                                                             return;
                                                                         var sp = t.Result.Text.Split(':');
                                                                         NSUserDefaults.StandardUserDefaults.SetString(
                                                                                                                       sp
                                                                                                                           [
                                                                                                                            0
                                                                                                                           ],
                                                                                                                       "IP");
                                                                         NSUserDefaults.StandardUserDefaults.SetString(
                                                                                                                       sp
                                                                                                                           [
                                                                                                                            1
                                                                                                                           ],
                                                                                                                       "Port");
                                                                         exData(
                                                                                IPAddress.Parse(sp[0]),
                                                                                Convert.ToInt32(sp[1]));
                                                                         InvokeOnMainThread(
                                                                                            () =>
                                                                                            {
                                                                                                ipElement.Value = sp[0];
                                                                                                portElement.Value =
                                                                                                    sp[1];
                                                                                            });
                                                                     };
                                     var scanner = new MobileBarcodeScanner();
                                     var opt = new MobileBarcodeScanningOptions
                                                   {
                                                       PossibleFormats = new List<BarcodeFormat> {BarcodeFormat.QR_CODE}
                                                   };
                                     scanner.Scan(opt, true).ContinueWith(callback);
                                 })
                         });
            Root.Add(
                     new Section
                         {
                             new ButtonElement(
                                 "��������",
                                 () =>
                                 {
                                     try
                                     {
                                         Application.BusinessHelper.SaveData();
                                     }
                                     catch (Exception e)
                                     {
                                         Debug.Print(e.ToString());
                                     }
                                 })
                         });
        }
    }
}
