using Antlr4.Runtime;

namespace AccountingServer.Shell.Parsing
{
    public partial class ShellParser
    {
        public static ShellParser From(string str)
            => new ShellParser(new CommonTokenStream(new ShellLexer(new AntlrInputStream(str))))
                   {
                       ErrorHandler = new BailErrorStrategy()
                   };
    }
}
