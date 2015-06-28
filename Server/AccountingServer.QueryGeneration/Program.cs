using System.IO;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

namespace AccountingServer.QueryGeneration
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var inputStream = new StreamReader(System.Console.OpenStandardInput());
            while (true)
            {
                var input = new AntlrInputStream(inputStream.ReadLine());
                var lexer = new AccountingServer.Shell.Parsing.ShellLexer(input);
                var tokens = new CommonTokenStream(lexer);
                var parser = new AccountingServer.Shell.Parsing.ShellParser(tokens);
                IParseTree tree = parser.command();
                System.Console.WriteLine(tree.ToStringTree(parser));
            }
        }
    }
}
