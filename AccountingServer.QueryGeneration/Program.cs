/* Copyright (C) 2020-2023 b1f6c1c4
 *
 * This file is part of ProfessionalAccounting.
 *
 * ProfessionalAccounting is free software: you can redistribute it and/or
 * modify it under the terms of the GNU Affero General Public License as
 * published by the Free Software Foundation, version 3.
 *
 * ProfessionalAccounting is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Affero General Public License
 * for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with ProfessionalAccounting.  If not, see
 * <https://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Reflection;
using AccountingServer.QueryGeneration;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

Type GetLexer(string kind)
    => kind switch
        {
            "Query" => typeof(QueryLexer),
            "Subtotal" => typeof(SubtotalLexer),
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };

Type GetParser(string kind)
    => kind switch
        {
            "Query" => typeof(QueryParser),
            "Subtotal" => typeof(SubtotalParser),
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };

IParseTree CallParser(Parser parser, string name)
    => (IParseTree)parser
        .GetType()
        .GetMethod(name, BindingFlags.Public | BindingFlags.Instance)
        .Invoke(parser, new object[0]);

var kind = "Query";
var method = "lexer";
var inputStream = new StreamReader(Console.OpenStandardInput());
while (true)
{
    Console.Write($"({kind}.{method})> ");
    var line = inputStream.ReadLine();
    if (line.StartsWith("use "))
    {
        var desc = line[4..].Trim().Split('.');
        kind = desc[0];
        method = desc[1];
        continue;
    }

    if (line.StartsWith("exit") ||
        line.StartsWith("quit"))
        break;

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
