using Antlr4.Runtime;

namespace AccountingServer.BLL.Parsing
{
    internal partial class QueryParser
    {
        public static QueryParser From(string str)
            => new QueryParser(new CommonTokenStream(new QueryLexer(new AntlrInputStream(str))))
                   {
                       ErrorHandler = new BailErrorStrategy()
                   };
    }
}
