using System.IO;
using System.Reflection;
using System.Resources;
using System.Text;
using AccountingServer.BLL;

namespace AccountingServer.Console
{
    public partial class AccountingConsole
    {
        ///// <summary>
        /////     呈现二维码
        ///// </summary>
        ///// <param name="qrCode">二维码图像，若为null表示隐藏二维码</param>
        //public delegate void PresentQRCodeEventHandler(Bitmap qrCode);

        ///// <summary>
        /////     呈现二维码
        ///// </summary>
        //public event PresentQRCodeEventHandler PresentQRCode;

        ///// <summary>
        /////     移动数据传输
        ///// </summary>
        //private MobileComm m_Mobile;

        /// <summary>
        ///     显示控制台帮助
        /// </summary>
        /// <returns>帮助内容</returns>
        private static IQueryResult ListHelp()
        {
            using (
                var stream =
                    Assembly.GetExecutingAssembly().GetManifestResourceStream("AccountingServer.Console.Resources.Console.txt"))
            {
                if (stream == null)
                    throw new MissingManifestResourceException();
                using (var reader = new StreamReader(stream))
                    return new UnEditableText(reader.ReadToEnd());
            }
        }

        /// <summary>
        ///     显示所有会计科目及其编号
        /// </summary>
        /// <returns>会计科目及其编号</returns>
        private static IQueryResult ListTitles()
        {
            var sb = new StringBuilder();
            foreach (var title in TitleManager.GetTitles())
            {
                sb.AppendFormat(
                                "{0}{1}\t\t{2}",
                                title.Item1.AsTitle(),
                                title.Item2.AsSubTitle(),
                                title.Item3);
                sb.AppendLine();
            }
            return new UnEditableText(sb.ToString());
        }

        /// <summary>
        ///     从info.tsinghua.edu.cn抓取信息
        /// </summary>
        /// <returns></returns>
        private IQueryResult FetchInfo()
        {
            AutoConnect();

            var thuInfo = new THUInfo(m_Accountant);
            thuInfo.FetchData(@"2014010914", @"");
            return new EditableText(thuInfo.Compare());
        }

        ///// <summary>
        /////     启动/关闭移动通信模块，同时显示隐藏二维码
        ///// </summary>
        //private void ToggleMobile()
        //{
        //    if (m_Mobile == null)
        //    {
        //        m_Mobile = new MobileComm();

        //        m_Mobile.Connect(m_Accountant);

        //        PresentQRCode(m_Mobile.GetQRCode(256, 256));
        //    }
        //    else
        //    {
        //        m_Mobile.Dispose();
        //        m_Mobile = null;

        //        PresentQRCode(null);
        //    }
        //}
    }
}
