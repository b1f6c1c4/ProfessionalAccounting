using System.IO;
using System.Reflection;
using System.Resources;
using System.Text;
using AccountingServer.BLL;

namespace AccountingServer.Console
{
    public partial class AccountingConsole
    {
        /// <summary>
        ///     显示控制台帮助
        /// </summary>
        /// <returns>帮助内容</returns>
        private static IQueryResult ListHelp()
        {
            using (
                var stream =
                    Assembly.GetExecutingAssembly()
                            .GetManifestResourceStream("AccountingServer.Console.Resources.Console.txt"))
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
    }
}
