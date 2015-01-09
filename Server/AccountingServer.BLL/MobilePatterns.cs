using System;
using System.Collections.Generic;
using AccountingServer.Entities;

namespace AccountingServer.BLL
{
    /// <summary>
    ///     模板特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class PatternAttribute : Attribute
    {
        /// <summary>
        ///     静态图形界面
        /// </summary>
        private string m_UI;

        /// <summary>
        ///     显示在添加页的名称
        /// </summary>
        /// <returns>名称</returns>
        public string Name { get; set; }

        /// <summary>
        ///     显示在记录页的名称
        /// </summary>
        /// <returns>格式化字符串</returns>
        public string TextPattern { get; set; }

        /// <summary>
        ///     获取图形界面
        ///     <para>
        ///         图形界面序列化为一个逗号分隔的列表，每一项对应一个单元格，类型有：
        ///         <list type="bullet">
        ///             <item>
        ///                 <description>E[$1;$2;$3;$4]：文本输入，标题$1，提示$2，默认$3，类型$4（空或者NP）</description>
        ///             </item>
        ///             <item>
        ///                 <descripion>GUID[$1;$2]：Guid输入，标题$1，生成/复制（G）或者粘贴/复制（P）$2</descripion>
        ///             </item>
        ///             <item>
        ///                 <description>O[$1;$2]：单独页面单选，标题$1，内容$2（分号分隔的列表）</description>
        ///             </item>
        ///             <item>
        ///                 <description>o[$1]：单选，内容$1（分号分隔的列表）</description>
        ///             </item>
        ///             <item>
        ///                 <description>Date[$1]：日期选择，标题$1</description>
        ///             </item>
        ///         </list>
        ///     </para>
        /// </summary>
        /// <returns>序列化的图形界面</returns>
        public string UI { get { return m_UI ?? UIFunc(); } set { m_UI = value; } }

        /// <summary>
        ///     动态图形界面
        /// </summary>
        private Func<string> UIFunc { get; set; }
    }

    /// <summary>
    ///     模板
    /// </summary>
    public static class MobilePatterns
    {
        /// <summary>
        ///     食堂
        /// </summary>
        /// <param name="result">序列化的数据</param>
        /// <param name="helper">会计业务处理类</param>
        [Pattern(Name = "食堂",
            TextPattern = "{0} {1}元学生卡 {2}食堂",
            UI = "Date[日期],E[金额;人民币元;;NP],O[食堂;观畴园;紫荆园;桃李园;清青比萨;清青快餐;清青休闲;清青时代;玉树园;闻馨园;听涛园;丁香园;芝兰园]")]
        public static void 清华食堂(string result, Accountant helper)
        {
            var sp = result.Split(',');
            var fund = sp[1].AsCurrency();
            var remarks = new[]
                              { "观畴园", "紫荆园", "桃李园", "清青比萨", "清青快餐", "清青休闲", "清青时代", "玉树园", "闻馨园", "听涛园", "丁香园", "芝兰园" };
            helper.Upsert(
                          new Voucher
                              {
                                  Date = sp[0].AsDate(),
                                  Details =
                                      new[]
                                          {
                                              new VoucherDetail
                                                  {
                                                      Title = 1012,
                                                      SubTitle = 05,
                                                      Content = null,
                                                      Fund = -fund
                                                  },
                                              new VoucherDetail
                                                  {
                                                      Title = 6602,
                                                      SubTitle = 03,
                                                      Content = remarks[Convert.ToInt32(sp[2])],
                                                      Fund = fund
                                                  }
                                          }
                              });
        }

        /// <summary>
        ///     各种福利费
        /// </summary>
        /// <param name="result">序列化的数据</param>
        /// <param name="helper">会计业务处理类</param>
        [Pattern(Name = "福利", TextPattern = "{0} {1}元{2} {3}福利{4}",
            UI = "Date[日期],E[付款金额;人民币元;2.4;NP],O[支付;6439;7064;现金],E[实际金额;人民币元;3;NP],E[类型;食品/生活用品/理发/...;食品;]")]
        public static void 福利(string result, Accountant helper)
        {
            var sp = result.Split(',');
            var fund = -sp[1].AsCurrency();
            var fund2 = sp[3].AsCurrency();
            VoucherDetail credit;
            switch (sp[2])
            {
                case "0":
                    credit = new VoucherDetail { Title = 2241, SubTitle = 01, Content = "6439", Fund = fund };
                    break;
                case "1":
                    credit = new VoucherDetail { Title = 1012, SubTitle = 01, Content = "7064", Fund = fund };
                    break;
                case "2":
                    credit = new VoucherDetail { Title = 1001, Content = "", Fund = fund };
                    break;
                default:
                    throw new InvalidOperationException();
            }
            var debit = new VoucherDetail { Title = 6602, SubTitle = 06, Content = sp[4], Fund = fund2 };
            VoucherDetail[] dbDetails;
            if (fund + fund2 != 0)
            {
                var f = new VoucherDetail { Title = 6603, Content = null, Fund = -fund - fund2 };
                dbDetails = new[] { credit, debit, f };
            }
            else
                dbDetails = new[] { credit, debit };
            helper.Upsert(new Voucher { Date = sp[0].AsDate(), Details = dbDetails });
        }

        /// <summary>
        ///     金融工具
        /// </summary>
        /// <param name="result">序列化的数据</param>
        /// <param name="helper">会计业务处理类</param>
        [Pattern(Name = "金融工具", TextPattern = "{0} 金融工具",
            UI =
                "Date[日期],E[广发天天红;人民币元;;NP],E[余额宝;人民币元;;NP],E[中银活期宝;人民币元;;NP],E[中银增利;人民币元;;NP],E[中银优选;人民币元;;NP],E[中银纯债C;人民币元;;NP]"
            )]
        public static void 金融工具(string result, Accountant helper)
        {
            var sp = result.Split(',');
            var dt = sp[0].AsDate();
            var remarks = new[] { "广发基金天天红", "余额宝", "中银活期宝", "中银增利", "中银优选", "中银纯债C" };
            for (var id = 0; id < remarks.Length; id++)
            {
                var content = remarks[id];

                if (String.IsNullOrEmpty(sp[id + 1]))
                    continue;

                double? fund;
                if (sp[id + 1].StartsWith(":"))
                {
                    var orig = helper.GetBalance(new Balance { Title = 1101, Content = content, Date = dt });
                    fund = sp[id + 1].Substring(1).AsCurrency() - orig;
                    if (fund == 0)
                        continue;
                }
                else
                    fund = sp[id + 1].AsCurrency();

                int title, subTitle;
                switch (id)
                {
                    case 0:
                    case 1:
                    case 2:
                        title = 6101;
                        subTitle = 01;
                        break;
                    default:
                        title = 6111;
                        subTitle = 02;
                        break;
                }

                helper.Upsert(
                              new Voucher
                                  {
                                      Date = dt,
                                      Details =
                                          new[]
                                              {
                                                  new VoucherDetail
                                                      {
                                                          Title = 1101,
                                                          SubTitle = 02,
                                                          Content = content,
                                                          Fund = fund
                                                      },
                                                  new VoucherDetail
                                                      {
                                                          Title = title,
                                                          SubTitle = subTitle,
                                                          Content = content,
                                                          Fund = -fund
                                                      }
                                              }
                                  });
            }
        }

        /// <summary>
        ///     洗澡、洗衣、水费
        /// </summary>
        /// <param name="result">序列化的数据</param>
        /// <param name="helper">会计业务处理类</param>
        [Pattern(Name = "洗澡/洗衣/水费", TextPattern = "{0} {1}元{2}", UI = "Date[日期],E[金额;人民币元;2.5;NP],O[项目;洗澡;洗衣;水费]")]
        public static void 洗澡洗衣用水(string result, Accountant helper)
        {
            var sp = result.Split(',');
            var dt = sp[0].AsDate();

            string contentD, contentC;
            int? titleD, subTitleD;
            switch (sp[2])
            {
                case "0":
                    contentC = "洗澡卡";
                    contentD = "洗澡";
                    titleD = 6602;
                    subTitleD = 06;
                    break;
                case "1":
                    contentC = "洗衣卡";
                    contentD = "洗衣";
                    titleD = 6602;
                    subTitleD = 06;
                    break;
                case "2":
                    contentC = "学生卡小钱包";
                    contentD = "水费";
                    titleD = 6602;
                    subTitleD = 01;
                    break;
                default:
                    return;
            }

            double? fund;
            if (sp[1].StartsWith(":"))
            {
                var orig = helper.GetBalance(new Balance { Title = 1123, Content = contentC, Date = dt });
                fund = orig - sp[1].Substring(1).AsCurrency();
            }
            else
                fund = sp[1].AsCurrency();

            helper.Upsert(
                          new Voucher
                              {
                                  Date = dt,
                                  Details =
                                      new[]
                                          {
                                              new VoucherDetail
                                                  {
                                                      Title = 1123,
                                                      Content = contentC,
                                                      Fund = -fund
                                                  },
                                              new VoucherDetail
                                                  {
                                                      Title = titleD,
                                                      SubTitle = subTitleD,
                                                      Content = contentD,
                                                      Fund = fund
                                                  }
                                          }
                              });
        }

        /// <summary>
        ///     交通费
        /// </summary>
        /// <param name="result">序列化的数据</param>
        /// <param name="helper">会计业务处理类</param>
        [Pattern(Name = "出行", TextPattern = "{0} {1}元{2} 出行{3}",
            UI = "Date[日期],E[金额;人民币元;;NP],O[支付;7064;1476;6439;现金],E[工具;.+路/地铁/出租车;地铁;]")]
        public static void 出行(string result, Accountant helper)
        {
            var sp = result.Split(',');
            var fund = sp[1].AsCurrency();
            VoucherDetail credit;
            switch (sp[2])
            {
                case "0":
                    credit = new VoucherDetail { Title = 1012, SubTitle = 01, Content = "7064", Fund = -fund };
                    break;
                case "1":
                    credit = new VoucherDetail { Title = 1012, SubTitle = 01, Content = "1476", Fund = -fund };
                    break;
                case "2":
                    credit = new VoucherDetail { Title = 2241, SubTitle = 01, Content = "6439", Fund = -fund };
                    break;
                case "3":
                    credit = new VoucherDetail { Title = 1001, Content = "", Fund = -fund };
                    break;
                default:
                    throw new InvalidOperationException();
            }
            helper.Upsert(
                          new Voucher
                              {
                                  Date = sp[0].AsDate(),
                                  Details =
                                      new[]
                                          {
                                              credit,
                                              new VoucherDetail
                                                  {
                                                      Title = 6602,
                                                      SubTitle = 08,
                                                      Content = sp[3],
                                                      Fund = fund
                                                  }
                                          }
                              });
        }

        /// <summary>
        ///     餐费
        /// </summary>
        /// <param name="result">序列化的数据</param>
        /// <param name="helper">会计业务处理类</param>
        [Pattern(Name = "餐饮", TextPattern = "{0} {1}元{2} 餐饮{3}", UI = "Date[日期],E[金额;人民币元;;NP],O[支付;6439;现金],E[名称;;;]")]
        public static void 餐饮(string result, Accountant helper)
        {
            var sp = result.Split(',');
            var fund = sp[1].AsCurrency();
            VoucherDetail credit;
            switch (sp[2])
            {
                case "0":
                    credit = new VoucherDetail { Title = 2241, SubTitle = 01, Content = "6439", Fund = -fund };
                    break;
                case "1":
                    credit = new VoucherDetail { Title = 1001, Content = "", Fund = -fund };
                    break;
                default:
                    throw new InvalidOperationException();
            }
            helper.Upsert(
                          new Voucher
                              {
                                  Date = sp[0].AsDate(),
                                  Details =
                                      new[]
                                          {
                                              credit,
                                              new VoucherDetail
                                                  {
                                                      Title = 6602,
                                                      SubTitle = 03,
                                                      Content = sp[3],
                                                      Fund = fund
                                                  }
                                          }
                              });
        }

        /// <summary>
        ///     借记卡存取现金
        /// </summary>
        /// <param name="result">序列化的数据</param>
        /// <param name="helper">会计业务处理类</param>
        [Pattern(Name = "存取现金",
            TextPattern = "{0} {1}元{2} 现金",
            UI = "Date[日期],E[金额;人民币元；支取为正;;NP],O[银行卡;5184;3593;9767]")]
        public static void 存取现金(string result, Accountant helper)
        {
            var sp = result.Split(',');
            var fund = sp[1].AsCurrency();
            var remarks = new[] { "5184", "3593", "9767" };
            helper.Upsert(
                          new Voucher
                              {
                                  Date = sp[0].AsDate(),
                                  Details =
                                      new[]
                                          {
                                              new VoucherDetail { Title = 1001, Content = null, Fund = fund },
                                              new VoucherDetail
                                                  {
                                                      Title = 1002,
                                                      Content = remarks[Convert.ToInt32(sp[2])],
                                                      Fund = -fund
                                                  }
                                          }
                              });
        }

        /// <summary>
        ///     上级拨入资金
        /// </summary>
        /// <param name="result">序列化的数据</param>
        /// <param name="helper">会计业务处理类</param>
        [Pattern(Name = "上级拨入", TextPattern = "{0} {1}元上级拨入 {2}",
            UI = "Date[日期],E[金额;人民币元;;NP],O[方式;5184;3593;9767;现金;6439]")]
        public static void 上级拨入(string result, Accountant helper)
        {
            var sp = result.Split(',');
            var fund = sp[1].AsCurrency();
            VoucherDetail debit;
            switch (sp[2])
            {
                case "0":
                    debit = new VoucherDetail { Title = 1002, Content = "5184", Fund = fund };
                    break;
                case "1":
                    debit = new VoucherDetail { Title = 1002, Content = "3593", Fund = fund };
                    break;
                case "2":
                    debit = new VoucherDetail { Title = 1002, Content = "9767", Fund = fund };
                    break;
                case "3":
                    debit = new VoucherDetail { Title = 1001, Content = "", Fund = fund };
                    break;
                case "4":
                    debit = new VoucherDetail { Title = 2241, SubTitle = 01, Content = "6439", Fund = fund };
                    break;
                default:
                    throw new InvalidOperationException();
            }
            helper.Upsert(
                          new Voucher
                              {
                                  Date = sp[0].AsDate(),
                                  Details =
                                      new[]
                                          {
                                              debit,
                                              new VoucherDetail
                                                  {
                                                      Title = 6301,
                                                      SubTitle = 04,
                                                      Content = "",
                                                      Fund = -fund
                                                  }
                                          }
                              });
        }

        /// <summary>
        ///     自定义
        /// </summary>
        /// <param name="result">序列化的数据</param>
        /// <param name="helper">会计业务处理类</param>
        [Pattern(Name = "（自定义）", TextPattern = "{0} {1} {2} {3} {4} {5} {6} {7} {8} {9}",
            UI = "Date[日期]" +
                 ",E[科目;科目代码;;NP],E[备注;或三级科目;;],E[金额;人民币元;;NP]" +
                 ",E[科目;科目代码;;NP],E[备注;或三级科目;;],E[金额;人民币元;;NP]" +
                 ",E[科目;科目代码;;NP],E[备注;或三级科目;;],E[金额;人民币元;;NP]")]
        public static void 自定义(string result, Accountant helper)
        {
            var sp = result.Split(',');
            var dbDetails = new List<VoucherDetail>((sp.Length - 1) / 3);
            for (var i = 1; i < sp.Length; i += 3)
            {
                if (String.IsNullOrEmpty(sp[i]))
                    continue;
                dbDetails.Add(
                              new VoucherDetail
                                  {
                                      Title = sp[i].AsTitleOrSubTitle(),
                                      Content = sp[i + 1],
                                      Fund = sp[i + 2].AsCurrency()
                                  });
            }

            helper.Upsert(new Voucher { Date = sp[0].AsDate(), Details = dbDetails.ToArray() });
        }
    }
}
