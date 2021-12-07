#!/bin/bash

set -euo pipefail

cp AccountingServer.QueryGeneration/obj/Gen/net6.0/Query{.tokens,Lexer.{cs,tokens},Parser.cs} \
    AccountingServer.BLL/Parsing/
cp AccountingServer.QueryGeneration/obj/Gen/net6.0/Subtotal{.tokens,Lexer.{cs,tokens},Parser.cs} \
    AccountingServer.BLL/Parsing/

sed -i 's/^public partial class Query/internal partial class Query/
        s/AccountingServer\.QueryGeneration/AccountingServer.BLL.Parsing/' \
    AccountingServer.BLL/Parsing/QueryLexer.cs \
    AccountingServer.BLL/Parsing/QueryParser.cs
sed -i 's/^public partial class Subtotal/internal partial class Subtotal/
        s/AccountingServer\.QueryGeneration/AccountingServer.BLL.Parsing/' \
    AccountingServer.BLL/Parsing/SubtotalLexer.cs \
    AccountingServer.BLL/Parsing/SubtotalParser.cs
