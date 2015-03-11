using Antlr4.Runtime;

namespace AccountingServer.Console
{
    public partial class ConsoleParser
    {
        public static ConsoleParser From(string str)
        {
            return new ConsoleParser(new CommonTokenStream(new ConsoleLexer(new AntlrInputStream(str))))
                       {
                           ErrorHandler = new BailErrorStrategy()
                       };
        }
    }
}
