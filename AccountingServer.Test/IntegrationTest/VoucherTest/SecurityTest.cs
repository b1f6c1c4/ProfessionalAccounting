using AccountingServer.Entities.Util;
using Xunit;
using static AccountingServer.BLL.Parsing.FacadeF;

namespace AccountingServer.Test.IntegrationTest.VoucherTest
{
    [Collection("SecurityTestCollection")]
    public class SecurityTest
    {
        [Theory]
        [InlineData(true, "")]
        [InlineData(false, "^hhh^")]
        [InlineData(false, ".")]
        [InlineData(true, ".~")]
        [InlineData(true, "~.")]
        [InlineData(true, "[~~.]")]
        [InlineData(true, "2018")]
        [InlineData(true, "0")]
        [InlineData(false, ",,,")]
        [InlineData(false, ",,,~,,")]
        [InlineData(true, ",,,~,")]
        [InlineData(true, "[]")]
        [InlineData(true, "[null]")]
        [InlineData(true, "[.~~.]")]
        [InlineData(false, "[.~.]")]
        [InlineData(false, "^hhh^ []")]
        [InlineData(true, "{}")]
        [InlineData(true, "+{}")]
        [InlineData(false, "+{.}")]
        [InlineData(true, "-{}")]
        [InlineData(true, "-{.}")]
        [InlineData(true, "{}+{}")]
        [InlineData(true, "{}-{}")]
        [InlineData(true, "{}*{}")]
        [InlineData(true, "{=114}+{}")]
        [InlineData(false, "{=114}-{}")]
        [InlineData(false, "{=114}*{}")]
        [InlineData(true, "{}+{=514}")]
        [InlineData(true, "{}-{=514}")]
        [InlineData(false, "{}*{=514}")]
        [InlineData(false, "{=114}+{=514}")]
        [InlineData(false, "{=114}-{=514}")]
        [InlineData(false, "{=114}*{=514}")]
        public void VoucherQueryTest(bool dangerous, string expr)
        {
            Assert.Equal(dangerous, ParsingF.VoucherQuery(expr).IsDangerous());
        }

        [Theory]
        [InlineData(true, "")]
        [InlineData(true, ">")]
        [InlineData(false, "=0.0")]
        [InlineData(true, "''")]
        [InlineData(false, "'xx'")]
        [InlineData(true, "\"\"")]
        [InlineData(false, "\"xx\"")]
        [InlineData(true, "@USD")]
        [InlineData(true, "Asset")]
        [InlineData(true, "T1234")]
        [InlineData(true, "@CNY Asset T123456 '' \"\" >")]
        [InlineData(false, "@CNY Asset T123456 '' \"\" =666 >")]
        [InlineData(true, "()")]
        [InlineData(true, "+()")]
        [InlineData(false, "+('a')")]
        [InlineData(true, "-()")]
        [InlineData(true, "-('a')")]
        [InlineData(true, "()+'a'")]
        [InlineData(true, "()-")]
        [InlineData(true, "()-'a'")]
        [InlineData(false, "'b'-'a'")]
        [InlineData(false, "'b'-()")]
        [InlineData(true, "*")]
        [InlineData(true, "'a'+")]
        [InlineData(false, "'a'-")]
        [InlineData(false, "'a'*")]
        [InlineData(false, "+'a'")]
        [InlineData(true, "-'a'")]
        [InlineData(false, "*'a'")]
        [InlineData(false, "'a'+'a'")]
        [InlineData(false, "'a'-'a'")]
        [InlineData(false, "'a'*'a'")]
        [InlineData(true, "{}:")]
        [InlineData(false, "{.}:")]
        [InlineData(false, "{}:=114514")]
        [InlineData(false, "{.}:=114514")]
        public void DetailQueryTest(bool dangerous, string expr)
        {
            Assert.Equal(dangerous, ParsingF.DetailQuery(expr).IsDangerous());
        }

        [Theory]
        [InlineData(true, "")] // TODO
        public void DistributedQueryTest(bool dangerous, string expr)
        {
            Assert.Equal(dangerous, ParsingF.DistributedQuery(expr).IsDangerous());
        }

        [Fact]
        public void ConstantTest()
        {
            Assert.True(DetailQueryUnconstrained.Instance.IsDangerous());
            Assert.True(VoucherQueryUnconstrained.Instance.IsDangerous());
            Assert.True(DistributedQueryUnconstrained.Instance.IsDangerous());
        }
    }
}
