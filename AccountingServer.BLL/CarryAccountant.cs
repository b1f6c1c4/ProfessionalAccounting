using System;
using System.Collections.Generic;
using System.Linq;
using AccountingServer.DAL;
using AccountingServer.Entities;

namespace AccountingServer.BLL
{
    /// <summary>
    ///     结转会计业务处理类
    /// </summary>
    internal class CarryAccountant
    {
        /// <summary>
        ///     数据库访问
        /// </summary>
        private readonly IDbAdapter m_Db;

        public CarryAccountant(IDbAdapter db) { m_Db = db; }

        /// <summary>
        ///     月末结转
        /// </summary>
        /// <param name="dt">月，若为<c>null</c>则表示对无日期进行结转</param>
        public void Carry(DateTime? dt)
        {
            DateTime? ed;
            DateFilter rng;
            if (dt.HasValue)
            {
                var sd = new DateTime(dt.Value.Year, dt.Value.Month, 1);
                ed = AccountantHelper.LastDayOfMonth(dt.Value.Year, dt.Value.Month);
                rng = new DateFilter(sd, ed);
            }
            else
            {
                ed = null;
                rng = DateFilter.TheNullOnly;
            }

            var voucher00 = new Voucher
                                {
                                    Date = ed,
                                    Type = VoucherType.Carry,
                                    Currency = Voucher.BaseCurrency,
                                    Details = new List<VoucherDetail>()
                                };
            var voucher01 = new Voucher
                                {
                                    Date = ed,
                                    Type = VoucherType.Carry,
                                    Currency = Voucher.BaseCurrency,
                                    Details = new List<VoucherDetail>()
                                };
            var voucher02 = new Voucher
                                {
                                    Date = ed,
                                    Type = VoucherType.Carry,
                                    Currency = Voucher.BaseCurrency,
                                    Details = new List<VoucherDetail>()
                                };

            {
                var res = m_Db
                    .SelectVoucherDetailsGrouped(
                                                 new GroupedQueryBase(
                                                     filter: new VoucherDetail { Title = 6301, SubTitle = 03 },
                                                     rng: rng,
                                                     subtotal: new SubtotalBase
                                                                   {
                                                                       Levels = new[] { SubtotalLevel.Content }
                                                                   }));
                foreach (var balance in res.Where(b => !b.Fund.IsZero()))
                    switch (balance.Content)
                    {
                        case "706DA4F0-3674-4232-8C0B-92720BD30A57":
                        case "BADA4D1D-6425-4C32-A069-B975BA6A377F":
                        case "刘焕青":
                        case "刘若溪":
                        case "包懿":
                        case "包殿文":
                        case "包美玲":
                        case "华北电力大学同学":
                        case "吴迪":
                        case "张国平":
                        case "文勇":
                        case "文瑞韬":
                        case "文红":
                        case "文维寿":
                        case "李茹宏":
                        case "涂景一":
                        case "涂真榕":
                        case "涂秋平":
                        case "涂秋艳":
                        case "涂秋霞":
                        case "王英":
                        case "石凤君":
                        case "胡亭立":
                            voucher00.Details.Add(
                                                  new VoucherDetail
                                                      {
                                                          Title = 6301,
                                                          SubTitle = 03,
                                                          Content = balance.Content,
                                                          Fund = -balance.Fund
                                                      });
                            break;
                        default:
                            voucher01.Details.Add(
                                                  new VoucherDetail
                                                      {
                                                          Title = 6301,
                                                          SubTitle = 03,
                                                          Content = balance.Content,
                                                          Fund = -balance.Fund
                                                      });
                            break;
                    }
            }

            {
                var res = m_Db
                    .SelectVoucherDetailsGrouped(
                                                 new GroupedQueryBase(
                                                     filter: new VoucherDetail { Title = 6301, SubTitle = 04 },
                                                     rng: rng,
                                                     subtotal: new SubtotalBase
                                                                   {
                                                                       Levels = new[] { SubtotalLevel.Content }
                                                                   }));

                foreach (var balance in res.Where(b => !b.Fund.IsZero()))
                    switch (balance.Content)
                    {
                        case null:
                        case "专用":
                            voucher00.Details.Add(
                                                  new VoucherDetail
                                                      {
                                                          Title = 6301,
                                                          SubTitle = 04,
                                                          Content = balance.Content,
                                                          Fund = -balance.Fund
                                                      });
                            break;
                        default:
                            voucher01.Details.Add(
                                                  new VoucherDetail
                                                      {
                                                          Title = 6301,
                                                          SubTitle = 04,
                                                          Content = balance.Content,
                                                          Fund = -balance.Fund
                                                      });
                            break;
                    }
            }

            {
                var res = m_Db
                    .SelectVoucherDetailsGrouped(
                                                 new GroupedQueryBase(
                                                     filters: new[]
                                                                  {
                                                                      new VoucherDetail { Title = 6001 },
                                                                      new VoucherDetail { Title = 6051 },
                                                                      new VoucherDetail { Title = 6402, SubTitle = 00 },
                                                                      new VoucherDetail { Title = 6101 },
                                                                      new VoucherDetail { Title = 6111 },
                                                                      new VoucherDetail { Title = 6301, SubTitle = 01 },
                                                                      new VoucherDetail { Title = 6301, SubTitle = 02 },
                                                                      new VoucherDetail { Title = 6301, SubTitle = 99 },
                                                                      new VoucherDetail { Title = 6603, SubTitle = 00 },
                                                                      new VoucherDetail { Title = 6603, SubTitle = 01 },
                                                                      new VoucherDetail { Title = 6603, SubTitle = 02 },
                                                                      new VoucherDetail { Title = 6603, SubTitle = 03 }
                                                                  },
                                                     rng: rng,
                                                     subtotal: new SubtotalBase
                                                                   {
                                                                       Levels =
                                                                           new[]
                                                                               {
                                                                                   SubtotalLevel.Title,
                                                                                   SubtotalLevel.SubTitle,
                                                                                   SubtotalLevel.Content
                                                                               }
                                                                   }));

                foreach (var balance in res.Where(b => !b.Fund.IsZero()))
                    voucher01.Details.Add(
                                          new VoucherDetail
                                              {
                                                  Title = balance.Title,
                                                  SubTitle = balance.SubTitle,
                                                  Content = balance.Content,
                                                  Fund = -balance.Fund
                                              });
            }

            {
                var res = m_Db
                    .SelectVoucherDetailsGrouped(
                                                 new GroupedQueryBase(
                                                     filters: new[]
                                                                  {
                                                                      new VoucherDetail { Title = 6401 },
                                                                      new VoucherDetail { Title = 6402, SubTitle = 99 },
                                                                      new VoucherDetail { Title = 6602 },
                                                                      new VoucherDetail { Title = 6603, SubTitle = 04 },
                                                                      new VoucherDetail { Title = 6603, SubTitle = 99 },
                                                                      new VoucherDetail { Title = 6701 },
                                                                      new VoucherDetail { Title = 6711 }
                                                                  },
                                                     rng: rng,
                                                     subtotal: new SubtotalBase
                                                                   {
                                                                       Levels =
                                                                           new[]
                                                                               {
                                                                                   SubtotalLevel.Title,
                                                                                   SubtotalLevel.SubTitle,
                                                                                   SubtotalLevel.Content
                                                                               }
                                                                   }));

                foreach (var balance in res.Where(b => !b.Fund.IsZero()))
                    voucher02.Details.Add(
                                          new VoucherDetail
                                              {
                                                  Title = balance.Title,
                                                  SubTitle = balance.SubTitle,
                                                  Content = balance.Content,
                                                  Fund = -balance.Fund
                                              });
            }

            // ReSharper disable PossibleInvalidOperationException
            var b00 = voucher00.Details.Sum(d => d.Fund.Value);
            var b01 = voucher01.Details.Sum(d => d.Fund.Value);
            var b02 = voucher02.Details.Sum(d => d.Fund.Value);
            // ReSharper restore PossibleInvalidOperationException

            if (!b00.IsZero())
            {
                voucher00.Details.Add(new VoucherDetail { Title = 4103, Fund = -b00 });
                m_Db.Upsert(voucher00);
            }
            if (!b01.IsZero())
            {
                voucher01.Details.Add(new VoucherDetail { Title = 4103, SubTitle = 01, Fund = -b01 });
                m_Db.Upsert(voucher01);
            }
            if (!b02.IsZero())
            {
                voucher02.Details.Add(new VoucherDetail { Title = 4103, Fund = -b02 });
                m_Db.Upsert(voucher02);
            }
        }

        /// <summary>
        ///     年末结转
        /// </summary>
        /// <param name="dt">年，若为<c>null</c>则表示对无日期进行结转</param>
        /// <param name="includeNull">是否计入无日期</param>
        public void CarryYear(DateTime? dt, bool includeNull = false)
        {
            DateTime? ed;
            DateFilter rng;
            if (dt.HasValue)
            {
                var sd = new DateTime(dt.Value.Year, 1, 1);
                ed = sd.AddYears(1).AddDays(-1);
                rng = new DateFilter(includeNull ? (DateTime?)null : sd, ed);
            }
            else
            {
                ed = null;
                rng = DateFilter.TheNullOnly;
            }

            var r00 = m_Db.SelectVoucherDetailsGrouped(
                                                       new GroupedQueryBase(
                                                           filter: new VoucherDetail { Title = 4103, SubTitle = 00 },
                                                           rng: rng)).Single();
            var b00 = r00.Fund;

            var r01 = m_Db.SelectVoucherDetailsGrouped(
                                                       new GroupedQueryBase(
                                                           filter: new VoucherDetail { Title = 4103, SubTitle = 01 },
                                                           rng: rng)).Single();
            var b01 = r01.Fund;

            if (!b00.IsZero())
                m_Db.Upsert(
                            new Voucher
                                {
                                    Date = ed,
                                    Type = VoucherType.AnnualCarry,
                                    Currency = Voucher.BaseCurrency,
                                    Details =
                                        new List<VoucherDetail>
                                            {
                                                new VoucherDetail { Title = 4101, Fund = b00 },
                                                new VoucherDetail { Title = 4103, Fund = -b00 }
                                            }
                                });

            if (!b01.IsZero())
                m_Db.Upsert(
                            new Voucher
                                {
                                    Date = ed,
                                    Type = VoucherType.AnnualCarry,
                                    Currency = Voucher.BaseCurrency,
                                    Details =
                                        new List<VoucherDetail>
                                            {
                                                new VoucherDetail { Title = 4101, SubTitle = 01, Fund = b01 },
                                                new VoucherDetail { Title = 4103, SubTitle = 01, Fund = -b01 }
                                            }
                                });
        }
    }
}
