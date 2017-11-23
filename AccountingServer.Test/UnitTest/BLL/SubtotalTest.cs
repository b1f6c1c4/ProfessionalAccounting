using System;
using System.Collections.Generic;
using System.Linq;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;
using Xunit;
using static AccountingServer.BLL.Parsing.FacadeF;

namespace AccountingServer.Test.UnitTest.BLL
{
    public class SubtotalTest
    {
        public SubtotalTest() => ExchangeFactory.Instance = new MockExchange
            {
                { new DateTime(2017, 1, 1).CastUtc(), "JPY", 456 },
                { new DateTime(2017, 1, 1).CastUtc(), "USD", 789 }
            };

        [Fact]
        public void TestCurrency()
        {
            var builder = new SubtotalBuilder(ParsingF.GroupedQuery("`C").Subtotal);

            var bal = new[]
                {
                    new Balance
                        {
                            Currency = "JPY",
                            Fund = 8
                        },
                    new Balance
                        {
                            Currency = "CNY",
                            Fund = 1
                        },
                    new Balance
                        {
                            Currency = "USD",
                            Fund = 4
                        },
                    new Balance
                        {
                            Currency = "CNY",
                            Fund = 2
                        }
                };

            var res = builder.Build(bal);
            Assert.IsAssignableFrom<ISubtotalRoot>(res);
            var resx = (ISubtotalRoot)res;

            var lst = new List<Balance>();
            foreach (var item in resx.Items)
            {
                Assert.IsAssignableFrom<ISubtotalCurrency>(item);
                var resxx = (ISubtotalCurrency)item;
                Assert.Null(resxx.Items);
                lst.Add(new Balance { Currency = resxx.Currency, Fund = resxx.Fund });
            }

            Assert.Equal(bal.Sum(b => b.Fund), res.Fund);
            Assert.Equal(
                new List<Balance>
                    {
                        new Balance
                            {
                                Currency = "JPY",
                                Fund = 8
                            },
                        new Balance
                            {
                                Currency = "CNY",
                                Fund = 3
                            },
                        new Balance
                            {
                                Currency = "USD",
                                Fund = 4
                            }
                    },
                lst,
                new BalanceEqualityComparer());
        }

        [Fact]
        public void TestTitle()
        {
            var builder = new SubtotalBuilder(ParsingF.GroupedQuery("`t").Subtotal);

            var bal = new[]
                {
                    new Balance
                        {
                            Title = 4567,
                            Fund = 8
                        },
                    new Balance
                        {
                            Title = 1234,
                            Fund = 1
                        },
                    new Balance
                        {
                            Title = 6543,
                            Fund = 4
                        },
                    new Balance
                        {
                            Title = 1234,
                            Fund = 2
                        }
                };

            var res = builder.Build(bal);
            Assert.IsAssignableFrom<ISubtotalRoot>(res);
            var resx = (ISubtotalRoot)res;

            var lst = new List<Balance>();
            foreach (var item in resx.Items)
            {
                Assert.IsAssignableFrom<ISubtotalTitle>(item);
                var resxx = (ISubtotalTitle)item;
                Assert.Null(resxx.Items);
                lst.Add(new Balance { Title = resxx.Title, Fund = resxx.Fund });
            }

            Assert.Equal(bal.Sum(b => b.Fund), res.Fund);
            Assert.Equal(
                new List<Balance>
                    {
                        new Balance
                            {
                                Title = 4567,
                                Fund = 8
                            },
                        new Balance
                            {
                                Title = 1234,
                                Fund = 3
                            },
                        new Balance
                            {
                                Title = 6543,
                                Fund = 4
                            }
                    },
                lst,
                new BalanceEqualityComparer());
        }

        [Fact]
        public void TestTitleSubTitle()
        {
            var builder = new SubtotalBuilder(ParsingF.GroupedQuery("`ts").Subtotal);

            var bal = new[]
                {
                    new Balance
                        {
                            Title = 4567,
                            Fund = 8
                        },
                    new Balance
                        {
                            Title = 1234,
                            Fund = 1
                        },
                    new Balance
                        {
                            Title = 6543,
                            Fund = 4
                        },
                    new Balance
                        {
                            Title = 1234,
                            Fund = 2
                        },
                    new Balance
                        {
                            Title = 4567,
                            SubTitle = 01,
                            Fund = 128
                        },
                    new Balance
                        {
                            Title = 1234,
                            SubTitle = 02,
                            Fund = 16
                        },
                    new Balance
                        {
                            Title = 6543,
                            SubTitle = 01,
                            Fund = 64
                        },
                    new Balance
                        {
                            Title = 1234,
                            SubTitle = 08,
                            Fund = 32
                        }
                };

            var res = builder.Build(bal);
            Assert.IsAssignableFrom<ISubtotalRoot>(res);
            var resx = (ISubtotalRoot)res;

            var lst = new List<Balance>();
            foreach (var item in resx.Items)
            {
                Assert.IsAssignableFrom<ISubtotalTitle>(item);
                var resxx = (ISubtotalTitle)item;
                foreach (var item2 in resxx.Items)
                {
                    Assert.IsAssignableFrom<ISubtotalSubTitle>(item2);
                    var resxxx = (ISubtotalSubTitle)item2;
                    Assert.Null(resxxx.Items);
                    lst.Add(new Balance { Title = resxx.Title, SubTitle = resxxx.SubTitle, Fund = resxxx.Fund });
                }
            }

            Assert.Equal(bal.Sum(b => b.Fund), res.Fund);
            Assert.Equal(
                new List<Balance>
                    {
                        new Balance
                            {
                                Title = 4567,
                                Fund = 8
                            },
                        new Balance
                            {
                                Title = 4567,
                                SubTitle = 01,
                                Fund = 128
                            },
                        new Balance
                            {
                                Title = 1234,
                                Fund = 3
                            },
                        new Balance
                            {
                                Title = 1234,
                                SubTitle = 02,
                                Fund = 16
                            },
                        new Balance
                            {
                                Title = 1234,
                                SubTitle = 08,
                                Fund = 32
                            },
                        new Balance
                            {
                                Title = 6543,
                                Fund = 4
                            },
                        new Balance
                            {
                                Title = 6543,
                                SubTitle = 01,
                                Fund = 64
                            }
                    },
                lst,
                new BalanceEqualityComparer());
        }

        [Fact]
        public void TestContent()
        {
            var builder = new SubtotalBuilder(ParsingF.GroupedQuery("`c").Subtotal);

            var bal = new[]
                {
                    new Balance
                        {
                            Content = "JPY",
                            Fund = 8
                        },
                    new Balance
                        {
                            Content = "CNY",
                            Fund = 1
                        },
                    new Balance
                        {
                            Content = "USD",
                            Fund = 4
                        },
                    new Balance
                        {
                            Content = "CNY",
                            Fund = 2
                        }
                };

            var res = builder.Build(bal);
            Assert.IsAssignableFrom<ISubtotalRoot>(res);
            var resx = (ISubtotalRoot)res;

            var lst = new List<Balance>();
            foreach (var item in resx.Items)
            {
                Assert.IsAssignableFrom<ISubtotalContent>(item);
                var resxx = (ISubtotalContent)item;
                Assert.Null(resxx.Items);
                lst.Add(new Balance { Content = resxx.Content, Fund = resxx.Fund });
            }

            Assert.Equal(bal.Sum(b => b.Fund), res.Fund);
            Assert.Equal(
                new List<Balance>
                    {
                        new Balance
                            {
                                Content = "JPY",
                                Fund = 8
                            },
                        new Balance
                            {
                                Content = "CNY",
                                Fund = 3
                            },
                        new Balance
                            {
                                Content = "USD",
                                Fund = 4
                            }
                    },
                lst,
                new BalanceEqualityComparer());
        }

        [Fact]
        public void TestRemark()
        {
            var builder = new SubtotalBuilder(ParsingF.GroupedQuery("`r").Subtotal);

            var bal = new[]
                {
                    new Balance
                        {
                            Remark = "JPY",
                            Fund = 8
                        },
                    new Balance
                        {
                            Remark = "CNY",
                            Fund = 1
                        },
                    new Balance
                        {
                            Remark = "USD",
                            Fund = 4
                        },
                    new Balance
                        {
                            Remark = "CNY",
                            Fund = 2
                        }
                };

            var res = builder.Build(bal);
            Assert.IsAssignableFrom<ISubtotalRoot>(res);
            var resx = (ISubtotalRoot)res;

            var lst = new List<Balance>();
            foreach (var item in resx.Items)
            {
                Assert.IsAssignableFrom<ISubtotalRemark>(item);
                var resxx = (ISubtotalRemark)item;
                Assert.Null(resxx.Items);
                lst.Add(new Balance { Remark = resxx.Remark, Fund = resxx.Fund });
            }

            Assert.Equal(bal.Sum(b => b.Fund), res.Fund);
            Assert.Equal(
                new List<Balance>
                    {
                        new Balance
                            {
                                Remark = "JPY",
                                Fund = 8
                            },
                        new Balance
                            {
                                Remark = "CNY",
                                Fund = 3
                            },
                        new Balance
                            {
                                Remark = "USD",
                                Fund = 4
                            }
                    },
                lst,
                new BalanceEqualityComparer());
        }

        [Fact]
        public void TestDay()
        {
            var builder = new SubtotalBuilder(ParsingF.GroupedQuery("`d").Subtotal);

            var bal = new[]
                {
                    new Balance
                        {
                            Date = new DateTime(2017, 01, 01).CastUtc(),
                            Fund = 8
                        },
                    new Balance
                        {
                            Date = new DateTime(2017, 01, 02).CastUtc(),
                            Fund = 1
                        },
                    new Balance
                        {
                            Date = new DateTime(2017, 01, 03).CastUtc(),
                            Fund = 4
                        },
                    new Balance
                        {
                            Date = new DateTime(2017, 01, 04).CastUtc(),
                            Fund = 2
                        }
                };

            var res = builder.Build(bal);
            Assert.IsAssignableFrom<ISubtotalRoot>(res);
            var resx = (ISubtotalRoot)res;

            var lst = new List<Balance>();
            foreach (var item in resx.Items)
            {
                Assert.IsAssignableFrom<ISubtotalDate>(item);
                var resxx = (ISubtotalDate)item;
                Assert.Null(resxx.Items);
                Assert.Equal(SubtotalLevel.Day, resxx.Level);
                lst.Add(new Balance { Date = resxx.Date, Fund = resxx.Fund });
            }

            Assert.Equal(bal.Sum(b => b.Fund), res.Fund);
            Assert.Equal(bal, lst, new BalanceEqualityComparer());
        }

        [Fact]
        public void TestWeek()
        {
            var builder = new SubtotalBuilder(ParsingF.GroupedQuery("`w").Subtotal);

            var bal = new[]
                {
                    new Balance
                        {
                            Date = new DateTime(2017, 01, 02).CastUtc(),
                            Fund = 8
                        },
                    new Balance
                        {
                            Date = new DateTime(2017, 01, 09).CastUtc(),
                            Fund = 1
                        },
                    new Balance
                        {
                            Date = new DateTime(2017, 01, 16).CastUtc(),
                            Fund = 4
                        },
                    new Balance
                        {
                            Date = new DateTime(2017, 01, 23).CastUtc(),
                            Fund = 2
                        }
                };

            var res = builder.Build(bal);
            Assert.IsAssignableFrom<ISubtotalRoot>(res);
            var resx = (ISubtotalRoot)res;

            var lst = new List<Balance>();
            foreach (var item in resx.Items)
            {
                Assert.IsAssignableFrom<ISubtotalDate>(item);
                var resxx = (ISubtotalDate)item;
                Assert.Null(resxx.Items);
                Assert.Equal(SubtotalLevel.Week, resxx.Level);
                lst.Add(new Balance { Date = resxx.Date, Fund = resxx.Fund });
            }

            Assert.Equal(bal.Sum(b => b.Fund), res.Fund);
            Assert.Equal(bal, lst, new BalanceEqualityComparer());
        }

        [Fact]
        public void TestMonth()
        {
            var builder = new SubtotalBuilder(ParsingF.GroupedQuery("`m").Subtotal);

            var bal = new[]
                {
                    new Balance
                        {
                            Date = new DateTime(2017, 01, 01).CastUtc(),
                            Fund = 8
                        },
                    new Balance
                        {
                            Date = new DateTime(2017, 02, 01).CastUtc(),
                            Fund = 1
                        },
                    new Balance
                        {
                            Date = new DateTime(2017, 03, 01).CastUtc(),
                            Fund = 4
                        },
                    new Balance
                        {
                            Date = new DateTime(2017, 04, 01).CastUtc(),
                            Fund = 2
                        }
                };

            var res = builder.Build(bal);
            Assert.IsAssignableFrom<ISubtotalRoot>(res);
            var resx = (ISubtotalRoot)res;

            var lst = new List<Balance>();
            foreach (var item in resx.Items)
            {
                Assert.IsAssignableFrom<ISubtotalDate>(item);
                var resxx = (ISubtotalDate)item;
                Assert.Null(resxx.Items);
                Assert.Equal(SubtotalLevel.Month, resxx.Level);
                lst.Add(new Balance { Date = resxx.Date, Fund = resxx.Fund });
            }

            Assert.Equal(bal.Sum(b => b.Fund), res.Fund);
            Assert.Equal(bal, lst, new BalanceEqualityComparer());
        }

        [Fact]
        public void TestYear()
        {
            var builder = new SubtotalBuilder(ParsingF.GroupedQuery("`y").Subtotal);

            var bal = new[]
                {
                    new Balance
                        {
                            Date = new DateTime(2014, 01, 01).CastUtc(),
                            Fund = 8
                        },
                    new Balance
                        {
                            Date = new DateTime(2015, 01, 01).CastUtc(),
                            Fund = 1
                        },
                    new Balance
                        {
                            Date = new DateTime(2016, 01, 01).CastUtc(),
                            Fund = 4
                        },
                    new Balance
                        {
                            Date = new DateTime(2017, 01, 01).CastUtc(),
                            Fund = 2
                        }
                };

            var res = builder.Build(bal);
            Assert.IsAssignableFrom<ISubtotalRoot>(res);
            var resx = (ISubtotalRoot)res;

            var lst = new List<Balance>();
            foreach (var item in resx.Items)
            {
                Assert.IsAssignableFrom<ISubtotalDate>(item);
                var resxx = (ISubtotalDate)item;
                Assert.Null(resxx.Items);
                Assert.Equal(SubtotalLevel.Year, resxx.Level);
                lst.Add(new Balance { Date = resxx.Date, Fund = resxx.Fund });
            }

            Assert.Equal(bal.Sum(b => b.Fund), res.Fund);
            Assert.Equal(bal, lst, new BalanceEqualityComparer());
        }

        [Fact]
        public void TestAggrChanged()
        {
            var builder = new SubtotalBuilder(ParsingF.GroupedQuery("`vD").Subtotal);

            var bal = new[]
                {
                    new Balance
                        {
                            Date = new DateTime(2017, 01, 01).CastUtc(),
                            Fund = 8
                        },
                    new Balance
                        {
                            Date = new DateTime(2017, 01, 02).CastUtc(),
                            Fund = 1
                        },
                    new Balance
                        {
                            Date = new DateTime(2017, 01, 04).CastUtc(),
                            Fund = 4
                        },
                    new Balance
                        {
                            Date = new DateTime(2017, 01, 05).CastUtc(),
                            Fund = 2
                        }
                };

            var res = builder.Build(bal);
            Assert.IsAssignableFrom<ISubtotalRoot>(res);
            var resx = (ISubtotalRoot)res;

            var lst = new List<Balance>();
            foreach (var item in resx.Items)
            {
                Assert.IsAssignableFrom<ISubtotalDate>(item);
                var resxx = (ISubtotalDate)item;
                Assert.Null(resxx.Items);
                Assert.Equal(SubtotalLevel.None, resxx.Level);
                lst.Add(new Balance { Date = resxx.Date, Fund = resxx.Fund });
            }

            Assert.Equal(bal.Sum(b => b.Fund), res.Fund);
            Assert.Equal(
                new[]
                    {
                        new Balance
                            {
                                Date = new DateTime(2017, 01, 01).CastUtc(),
                                Fund = 8
                            },
                        new Balance
                            {
                                Date = new DateTime(2017, 01, 02).CastUtc(),
                                Fund = 8 + 1
                            },
                        new Balance
                            {
                                Date = new DateTime(2017, 01, 04).CastUtc(),
                                Fund = 8 + 1 + 4
                            },
                        new Balance
                            {
                                Date = new DateTime(2017, 01, 05).CastUtc(),
                                Fund = 8 + 1 + 4 + 2
                            }
                    },
                lst,
                new BalanceEqualityComparer());
        }

        [Theory]
        [InlineData("", "101110")]
        [InlineData("~null", "001110")]
        [InlineData("null", "100000")]
        [InlineData("~20170105", "101111")]
        [InlineData("~20170104", "101110")]
        [InlineData("~20170103", "101100")]
        [InlineData("~20170102", "101000")]
        [InlineData("~20170101", "110000")]
        [InlineData("~~20170105", "001111")]
        [InlineData("~~20170104", "001110")]
        [InlineData("~~20170103", "001100")]
        [InlineData("~~20170102", "001000")]
        [InlineData("~~20170101", "010000")]
        [InlineData("20170105~", "000001")]
        [InlineData("20170104~", "000010")]
        [InlineData("20170103~", "000110")]
        [InlineData("20170102~", "001110")]
        [InlineData("20170101~", "011110")]
        [InlineData("20170105~~", "100001")]
        [InlineData("20170104~~", "100010")]
        [InlineData("20170103~~", "100110")]
        [InlineData("20170102~~", "101110")]
        [InlineData("20170101~~", "111110")]
        [InlineData("20170101~~20170105", "111111")]
        [InlineData("20170101~~20170104", "111110")]
        [InlineData("20170101~~20170103", "111100")]
        [InlineData("20170101~~20170102", "111000")]
        [InlineData("20170101~~20170101", "110000")]
        [InlineData("20170105~~20170105", "100001")]
        [InlineData("20170104~~20170105", "100011")]
        [InlineData("20170103~~20170105", "100111")]
        [InlineData("20170102~~20170105", "101111")]
        [InlineData("20170101~20170105", "011111")]
        [InlineData("20170101~20170104", "011110")]
        [InlineData("20170101~20170103", "011100")]
        [InlineData("20170101~20170102", "011000")]
        [InlineData("20170101~20170101", "010000")]
        [InlineData("20170105~20170105", "000001")]
        [InlineData("20170104~20170105", "000011")]
        [InlineData("20170103~20170105", "000111")]
        [InlineData("20170102~20170105", "001111")]
        public void TestAggrEvery(string rng, string incl)
        {
            var builder = new SubtotalBuilder(ParsingF.GroupedQuery($"``vD[{rng}]").Subtotal);

            var bal = new[]
                {
                    new Balance
                        {
                            Date = null,
                            Fund = 1
                        },
                    new Balance
                        {
                            Date = new DateTime(2017, 01, 02).CastUtc(),
                            Fund = 2
                        },
                    new Balance
                        {
                            Date = new DateTime(2017, 01, 04).CastUtc(),
                            Fund = 4
                        }
                };

            var res = builder.Build(bal);
            Assert.IsAssignableFrom<ISubtotalRoot>(res);
            var resx = (ISubtotalRoot)res;

            var lst = new List<Balance>();
            foreach (var item in resx.Items)
            {
                Assert.IsAssignableFrom<ISubtotalDate>(item);
                var resxx = (ISubtotalDate)item;
                Assert.Null(resxx.Items);
                Assert.Equal(SubtotalLevel.None, resxx.Level);
                lst.Add(new Balance { Date = resxx.Date, Fund = resxx.Fund });
            }

            Assert.Equal(bal.Sum(b => b.Fund), res.Fund);

            var lst0 = new List<Balance>();
            if (incl[0] == '1')
                lst0.Add(new Balance { Date = null, Fund = 1 });
            if (incl[1] == '1')
                lst0.Add(new Balance { Date = new DateTime(2017, 01, 01).CastUtc(), Fund = 1 });
            if (incl[2] == '1')
                lst0.Add(new Balance { Date = new DateTime(2017, 01, 02).CastUtc(), Fund = 1 + 2 });
            if (incl[3] == '1')
                lst0.Add(new Balance { Date = new DateTime(2017, 01, 03).CastUtc(), Fund = 1 + 2 });
            if (incl[4] == '1')
                lst0.Add(new Balance { Date = new DateTime(2017, 01, 04).CastUtc(), Fund = 1 + 2 + 4 });
            if (incl[5] == '1')
                lst0.Add(new Balance { Date = new DateTime(2017, 01, 05).CastUtc(), Fund = 1 + 2 + 4 });
            Assert.Equal(lst0, lst, new BalanceEqualityComparer());
        }

        [Theory]
        [InlineData("", "001110")]
        [InlineData("~null", "001110")]
        [InlineData("null", "100000")]
        [InlineData("~20170105", "001111")]
        [InlineData("~20170104", "001110")]
        [InlineData("~20170103", "001100")]
        [InlineData("~20170102", "001000")]
        [InlineData("~20170101", "010000")]
        [InlineData("~~20170105", "001111")]
        [InlineData("~~20170104", "001110")]
        [InlineData("~~20170103", "001100")]
        [InlineData("~~20170102", "001000")]
        [InlineData("~~20170101", "010000")]
        [InlineData("20170105~", "000001")]
        [InlineData("20170104~", "000010")]
        [InlineData("20170103~", "000110")]
        [InlineData("20170102~", "001110")]
        [InlineData("20170101~", "011110")]
        [InlineData("20170105~~", "100001")]
        [InlineData("20170104~~", "100010")]
        [InlineData("20170103~~", "100110")]
        [InlineData("20170102~~", "101110")]
        [InlineData("20170101~~", "111110")]
        [InlineData("20170101~~20170105", "111111")]
        [InlineData("20170101~~20170104", "111110")]
        [InlineData("20170101~~20170103", "111100")]
        [InlineData("20170101~~20170102", "111000")]
        [InlineData("20170101~~20170101", "110000")]
        [InlineData("20170105~~20170105", "100001")]
        [InlineData("20170104~~20170105", "100011")]
        [InlineData("20170103~~20170105", "100111")]
        [InlineData("20170102~~20170105", "101111")]
        [InlineData("20170101~20170105", "011111")]
        [InlineData("20170101~20170104", "011110")]
        [InlineData("20170101~20170103", "011100")]
        [InlineData("20170101~20170102", "011000")]
        [InlineData("20170101~20170101", "010000")]
        [InlineData("20170105~20170105", "000001")]
        [InlineData("20170104~20170105", "000011")]
        [InlineData("20170103~20170105", "000111")]
        [InlineData("20170102~20170105", "001111")]
        public void TestAggrEveryNoNull(string rng, string incl)
        {
            var builder = new SubtotalBuilder(ParsingF.GroupedQuery($"``vD[{rng}]").Subtotal);

            var bal = new[]
                {
                    new Balance
                        {
                            Date = new DateTime(2017, 01, 02).CastUtc(),
                            Fund = 2
                        },
                    new Balance
                        {
                            Date = new DateTime(2017, 01, 04).CastUtc(),
                            Fund = 4
                        }
                };

            var res = builder.Build(bal);
            Assert.IsAssignableFrom<ISubtotalRoot>(res);
            var resx = (ISubtotalRoot)res;

            var lst = new List<Balance>();
            foreach (var item in resx.Items)
            {
                Assert.IsAssignableFrom<ISubtotalDate>(item);
                var resxx = (ISubtotalDate)item;
                Assert.Null(resxx.Items);
                Assert.Equal(SubtotalLevel.None, resxx.Level);
                lst.Add(new Balance { Date = resxx.Date, Fund = resxx.Fund });
            }

            Assert.Equal(bal.Sum(b => b.Fund), res.Fund);

            var lst0 = new List<Balance>();
            if (incl[0] == '1')
                lst0.Add(new Balance { Date = null, Fund = 0 });
            if (incl[1] == '1')
                lst0.Add(new Balance { Date = new DateTime(2017, 01, 01).CastUtc(), Fund = 0 });
            if (incl[2] == '1')
                lst0.Add(new Balance { Date = new DateTime(2017, 01, 02).CastUtc(), Fund = 2 });
            if (incl[3] == '1')
                lst0.Add(new Balance { Date = new DateTime(2017, 01, 03).CastUtc(), Fund = 2 });
            if (incl[4] == '1')
                lst0.Add(new Balance { Date = new DateTime(2017, 01, 04).CastUtc(), Fund = 2 + 4 });
            if (incl[5] == '1')
                lst0.Add(new Balance { Date = new DateTime(2017, 01, 05).CastUtc(), Fund = 2 + 4 });
            Assert.Equal(lst0, lst, new BalanceEqualityComparer());
        }

        [Theory]
        [InlineData("", "0000")]
        [InlineData("~null", "0000")]
        [InlineData("null", "1000")]
        [InlineData("~20170103", "0001")]
        [InlineData("~20170102", "0010")]
        [InlineData("~20170101", "0100")]
        [InlineData("~~20170103", "0001")]
        [InlineData("~~20170102", "0010")]
        [InlineData("~~20170101", "0100")]
        [InlineData("20170103~", "0001")]
        [InlineData("20170102~", "0010")]
        [InlineData("20170101~", "0100")]
        [InlineData("20170103~~", "1001")]
        [InlineData("20170102~~", "1010")]
        [InlineData("20170101~~", "1100")]
        [InlineData("20170101~~20170103", "1111")]
        [InlineData("20170101~~20170102", "1110")]
        [InlineData("20170101~~20170101", "1100")]
        [InlineData("20170103~~20170103", "1001")]
        [InlineData("20170102~~20170103", "1011")]
        [InlineData("20170101~20170103", "0111")]
        [InlineData("20170101~20170102", "0110")]
        [InlineData("20170101~20170101", "0100")]
        [InlineData("20170103~20170103", "0001")]
        [InlineData("20170102~20170103", "0011")]
        public void TestAggrEveryNull(string rng, string incl)
        {
            var builder = new SubtotalBuilder(ParsingF.GroupedQuery($"``vD[{rng}]").Subtotal);

            var bal = new Balance[] { };

            var res = builder.Build(bal);
            Assert.IsAssignableFrom<ISubtotalRoot>(res);
            var resx = (ISubtotalRoot)res;

            var lst = new List<Balance>();
            foreach (var item in resx.Items)
            {
                Assert.IsAssignableFrom<ISubtotalDate>(item);
                var resxx = (ISubtotalDate)item;
                Assert.Null(resxx.Items);
                Assert.Equal(SubtotalLevel.None, resxx.Level);
                lst.Add(new Balance { Date = resxx.Date, Fund = resxx.Fund });
            }

            Assert.Equal(bal.Sum(b => b.Fund), res.Fund);

            var lst0 = new List<Balance>();
            if (incl[0] == '1')
                lst0.Add(new Balance { Date = null, Fund = 0 });
            if (incl[1] == '1')
                lst0.Add(new Balance { Date = new DateTime(2017, 01, 01).CastUtc(), Fund = 0 });
            if (incl[2] == '1')
                lst0.Add(new Balance { Date = new DateTime(2017, 01, 02).CastUtc(), Fund = 0 });
            if (incl[3] == '1')
                lst0.Add(new Balance { Date = new DateTime(2017, 01, 03).CastUtc(), Fund = 0 });
            Assert.Equal(lst0, lst, new BalanceEqualityComparer());
        }

        [Fact]
        public void TestEquivalent()
        {
            var builder = new SubtotalBuilder(ParsingF.GroupedQuery("`vX[20170101]").Subtotal);

            var bal = new[]
                {
                    new Balance
                        {
                            Currency = "JPY",
                            Fund = 8
                        },
                    new Balance
                        {
                            Currency = "CNY",
                            Fund = 1
                        },
                    new Balance
                        {
                            Currency = "USD",
                            Fund = 4
                        },
                    new Balance
                        {
                            Currency = "CNY",
                            Fund = 2
                        }
                };

            var res = builder.Build(bal);
            Assert.IsAssignableFrom<ISubtotalRoot>(res);
            Assert.Null(res.Items);
            Assert.Equal(8 * 456 + 1 + 4 * 789 + 2, res.Fund);
        }


        [Theory]
        [InlineData("", "101110")]
        [InlineData("~null", "001110")]
        [InlineData("null", "100000")]
        [InlineData("~20170105", "101111")]
        [InlineData("~20170104", "101110")]
        [InlineData("~20170103", "101100")]
        [InlineData("~20170102", "101000")]
        [InlineData("~20170101", "110000")]
        [InlineData("~~20170105", "001111")]
        [InlineData("~~20170104", "001110")]
        [InlineData("~~20170103", "001100")]
        [InlineData("~~20170102", "001000")]
        [InlineData("~~20170101", "010000")]
        [InlineData("20170105~", "000001")]
        [InlineData("20170104~", "000010")]
        [InlineData("20170103~", "000110")]
        [InlineData("20170102~", "001110")]
        [InlineData("20170101~", "011110")]
        [InlineData("20170105~~", "100001")]
        [InlineData("20170104~~", "100010")]
        [InlineData("20170103~~", "100110")]
        [InlineData("20170102~~", "101110")]
        [InlineData("20170101~~", "111110")]
        [InlineData("20170101~~20170105", "111111")]
        [InlineData("20170101~~20170104", "111110")]
        [InlineData("20170101~~20170103", "111100")]
        [InlineData("20170101~~20170102", "111000")]
        [InlineData("20170101~~20170101", "110000")]
        [InlineData("20170105~~20170105", "100001")]
        [InlineData("20170104~~20170105", "100011")]
        [InlineData("20170103~~20170105", "100111")]
        [InlineData("20170102~~20170105", "101111")]
        [InlineData("20170101~20170105", "011111")]
        [InlineData("20170101~20170104", "011110")]
        [InlineData("20170101~20170103", "011100")]
        [InlineData("20170101~20170102", "011000")]
        [InlineData("20170101~20170101", "010000")]
        [InlineData("20170105~20170105", "000001")]
        [InlineData("20170104~20170105", "000011")]
        [InlineData("20170103~20170105", "000111")]
        [InlineData("20170102~20170105", "001111")]
        public void TestAggrEveryEqui(string rng, string incl)
        {
            var builder = new SubtotalBuilder(ParsingF.GroupedQuery($"``vD[{rng}]X[20170101]").Subtotal);

            var bal = new[]
                {
                    new Balance
                        {
                            Date = null,
                            Currency = "JPY",
                            Fund = 1 / 2D / 456D
                        },
                    new Balance
                        {
                            Date = new DateTime(2017, 01, 02).CastUtc(),
                            Currency = "JPY",
                            Fund = 2 / 2D / 456D
                        },
                    new Balance
                        {
                            Date = new DateTime(2017, 01, 04).CastUtc(),
                            Currency = "JPY",
                            Fund = 4 / 2D / 456D
                        },
                    new Balance
                        {
                            Date = null,
                            Currency = "USD",
                            Fund = 1 / 2D / 789D
                        },
                    new Balance
                        {
                            Date = new DateTime(2017, 01, 02).CastUtc(),
                            Currency = "USD",
                            Fund = 2 / 2D / 789D
                        },
                    new Balance
                        {
                            Date = new DateTime(2017, 01, 04).CastUtc(),
                            Currency = "USD",
                            Fund = 4 / 2D / 789D
                        }
                };

            var res = builder.Build(bal);
            Assert.IsAssignableFrom<ISubtotalRoot>(res);
            var resx = (ISubtotalRoot)res;

            var lst = new List<Balance>();
            foreach (var item in resx.Items)
            {
                Assert.IsAssignableFrom<ISubtotalDate>(item);
                var resxx = (ISubtotalDate)item;
                Assert.Null(resxx.Items);
                Assert.Equal(SubtotalLevel.None, resxx.Level);
                lst.Add(new Balance { Date = resxx.Date, Fund = resxx.Fund });
            }

            Assert.Equal(1 + 2 + 4, res.Fund);

            var lst0 = new List<Balance>();
            if (incl[0] == '1')
                lst0.Add(new Balance { Date = null, Fund = 1 });
            if (incl[1] == '1')
                lst0.Add(new Balance { Date = new DateTime(2017, 01, 01).CastUtc(), Fund = 1 });
            if (incl[2] == '1')
                lst0.Add(new Balance { Date = new DateTime(2017, 01, 02).CastUtc(), Fund = 1 + 2 });
            if (incl[3] == '1')
                lst0.Add(new Balance { Date = new DateTime(2017, 01, 03).CastUtc(), Fund = 1 + 2 });
            if (incl[4] == '1')
                lst0.Add(new Balance { Date = new DateTime(2017, 01, 04).CastUtc(), Fund = 1 + 2 + 4 });
            if (incl[5] == '1')
                lst0.Add(new Balance { Date = new DateTime(2017, 01, 05).CastUtc(), Fund = 1 + 2 + 4 });
            Assert.Equal(lst0, lst, new BalanceEqualityComparer());
        }

        [Theory]
        [InlineData("", "0000")]
        [InlineData("~null", "0000")]
        [InlineData("null", "1000")]
        [InlineData("~20170103", "0001")]
        [InlineData("~20170102", "0010")]
        [InlineData("~20170101", "0100")]
        [InlineData("~~20170103", "0001")]
        [InlineData("~~20170102", "0010")]
        [InlineData("~~20170101", "0100")]
        [InlineData("20170103~", "0001")]
        [InlineData("20170102~", "0010")]
        [InlineData("20170101~", "0100")]
        [InlineData("20170103~~", "1001")]
        [InlineData("20170102~~", "1010")]
        [InlineData("20170101~~", "1100")]
        [InlineData("20170101~~20170103", "1111")]
        [InlineData("20170101~~20170102", "1110")]
        [InlineData("20170101~~20170101", "1100")]
        [InlineData("20170103~~20170103", "1001")]
        [InlineData("20170102~~20170103", "1011")]
        [InlineData("20170101~20170103", "0111")]
        [InlineData("20170101~20170102", "0110")]
        [InlineData("20170101~20170101", "0100")]
        [InlineData("20170103~20170103", "0001")]
        [InlineData("20170102~20170103", "0011")]
        public void TestAggrEveryNullEqui(string rng, string incl)
        {
            var builder = new SubtotalBuilder(ParsingF.GroupedQuery($"``vD[{rng}]X[20170101]").Subtotal);

            var bal = new Balance[] { };

            var res = builder.Build(bal);
            Assert.IsAssignableFrom<ISubtotalRoot>(res);
            var resx = (ISubtotalRoot)res;

            var lst = new List<Balance>();
            foreach (var item in resx.Items)
            {
                Assert.IsAssignableFrom<ISubtotalDate>(item);
                var resxx = (ISubtotalDate)item;
                Assert.Null(resxx.Items);
                Assert.Equal(SubtotalLevel.None, resxx.Level);
                lst.Add(new Balance { Date = resxx.Date, Fund = resxx.Fund });
            }

            Assert.Equal(0, res.Fund);

            var lst0 = new List<Balance>();
            if (incl[0] == '1')
                lst0.Add(new Balance { Date = null, Fund = 0 });
            if (incl[1] == '1')
                lst0.Add(new Balance { Date = new DateTime(2017, 01, 01).CastUtc(), Fund = 0 });
            if (incl[2] == '1')
                lst0.Add(new Balance { Date = new DateTime(2017, 01, 02).CastUtc(), Fund = 0 });
            if (incl[3] == '1')
                lst0.Add(new Balance { Date = new DateTime(2017, 01, 03).CastUtc(), Fund = 0 });
            Assert.Equal(lst0, lst, new BalanceEqualityComparer());
        }
    }
}
