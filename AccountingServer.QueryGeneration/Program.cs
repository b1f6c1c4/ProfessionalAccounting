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
            var kind = "Query";
            var method = "lexer";
            var inputStream = new StreamReader(Console.OpenStandardInput());
            while (true)
            {
                Console.Write($"({kind}.{method})> ");
                var line = inputStream.ReadLine();
                if (line.StartsWith("use "))
                {
                    var desc = line.Substring(4).Trim().Split('.');
                    kind = desc[0];
                    method = desc[1];
                    continue;
                }

                if (line.StartsWith("exit") ||
                    line.StartsWith("quit"))
                    return;

                var input = new AntlrInputStream(line);
                var lexer = (Lexer)Activator.CreateInstance(GetLexer(kind), input);
                var tokens = new CommonTokenStream(lexer);
                if (method == "lexer")
                {
                    var i = 0;
                    while (true)
                    {
                        var t = tokens.Lt(++i);
                        if (t.Type == -1)
                            break;
                        var vocabulary = lexer.Vocabulary;
                        Console.WriteLine(
                            $"#{t.Type}({vocabulary.GetDisplayName(t.Type)})({vocabulary.GetLiteralName(t.Type)})({vocabulary.GetSymbolicName(t.Type)}) {t.Text}");
                    }

                    continue;
                }

                var parser = (Parser)Activator.CreateInstance(GetParser(kind), tokens);
                var tree = CallParser(parser, method);
                Console.WriteLine(tree.ToStringTree(parser));
            }
        }

        private static Type GetLexer(string kind)
        {
            switch (kind)
            {
                case "Query":
                    return typeof(QueryLexer);
                case "Subtotal":
                    return typeof(SubtotalLexer);
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }

        private static Type GetParser(string kind)
        {
            switch (kind)
            {
                case "Query":
                    return typeof(QueryParser);
                case "Subtotal":
                    return typeof(SubtotalParser);
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }

        private static IParseTree CallParser(Parser parser, string name)
            => (IParseTree)parser
                .GetType()
                .GetMethod(name, BindingFlags.Public | BindingFlags.Instance)
                .Invoke(parser, new object[0]);
    }
}
