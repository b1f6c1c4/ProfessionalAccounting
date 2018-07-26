using System;
using System.IO;
using System.Reflection;
using AccountingServer.BLL.Parsing;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

namespace AccountingServer.QueryGeneration
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var method = "voucherQuery";
            var inputStream = new StreamReader(Console.OpenStandardInput());
            while (true)
            {
                Console.Write($@"({method})> ");
                var line = inputStream.ReadLine();
                if (line.StartsWith("use "))
                {
                    method = line.Substring(4).Trim();
                    continue;
                }

                if (line.StartsWith("exit") ||
                    line.StartsWith("quit"))
                    return;

                var input = new AntlrInputStream(line);
                var lexer = new QueryLexer(input);
                var tokens = new CommonTokenStream(lexer);
                var parser = new QueryParser(tokens);
                var tree = CallParser(parser, method);
                Console.WriteLine(tree.ToStringTree(parser));
            }
        }

        private static IParseTree CallParser(QueryParser parser, string name)
            => (IParseTree)typeof(QueryParser)
                .GetMethod(name, BindingFlags.Public | BindingFlags.Instance)
                .Invoke(parser, new object[0]);
    }
}
