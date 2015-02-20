using System;
using System.IO;
using AccountingServer.QueryGeneration;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

namespace Calculator
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var inputStream = new StreamReader(Console.OpenStandardInput());
            while (true)
            {
                var input = new AntlrInputStream(inputStream.ReadLine());
                var lexer = new ConsoleLexer(input);
                var tokens = new CommonTokenStream(lexer);
                var parser = new ConsoleParser(tokens);
                IParseTree tree = parser.command();
                Console.WriteLine(tree.ToStringTree(parser));
            }
        }
    }
}
