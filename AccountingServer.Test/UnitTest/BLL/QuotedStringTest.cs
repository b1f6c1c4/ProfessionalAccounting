using AccountingServer.BLL.Util;
using Xunit;

namespace AccountingServer.Test.UnitTest.BLL
{
    public class QuotedStringTest
    {
        [Theory]
        [InlineData("simple", '\'')]
        [InlineData("'s'i'm'ple'''", '\'')]
        [InlineData("\"\"'s\\'i'm\"'\"ple'\"''\"", '\'')]
        [InlineData("simple", '"')]
        [InlineData("'s'i'm'ple'''", '"')]
        [InlineData("\"\"'s\\'i'm\"'\"ple'\"''\"", '"')]
        public void QuotationTest(string text, char ch)
        {
            Assert.Equal(text, text.Quotation(ch).Dequotation());
        }
    }
}
