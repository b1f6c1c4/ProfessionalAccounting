$ErrorActionPreference = "Stop"

cp AccountingServer.QueryGeneration/obj/Gen/net7.0/Query.tokens AccountingServer.BLL/Parsing/
cp AccountingServer.QueryGeneration/obj/Gen/net7.0/QueryLexer.cs AccountingServer.BLL/Parsing/
cp AccountingServer.QueryGeneration/obj/Gen/net7.0/QueryLexer.tokens AccountingServer.BLL/Parsing/
cp AccountingServer.QueryGeneration/obj/Gen/net7.0/QueryParser.cs AccountingServer.BLL/Parsing/
cp AccountingServer.QueryGeneration/obj/Gen/net7.0/Subtotal.tokens AccountingServer.BLL/Parsing/
cp AccountingServer.QueryGeneration/obj/Gen/net7.0/SubtotalLexer.cs AccountingServer.BLL/Parsing/
cp AccountingServer.QueryGeneration/obj/Gen/net7.0/SubtotalLexer.tokens AccountingServer.BLL/Parsing/
cp AccountingServer.QueryGeneration/obj/Gen/net7.0/SubtotalParser.cs AccountingServer.BLL/Parsing/

sed '-i.bak' 's/^public partial class Query/internal partial class Query/' `
    AccountingServer.BLL/Parsing/QueryLexer.cs `
    AccountingServer.BLL/Parsing/QueryParser.cs
sed '-i.bak' 's/^public partial class Subtotal/internal partial class Subtotal/' `
    AccountingServer.BLL/Parsing/SubtotalLexer.cs `
    AccountingServer.BLL/Parsing/SubtotalParser.cs

sed '-i.bak' 's/AccountingServer\.QueryGeneration/AccountingServer.BLL.Parsing/' `
    AccountingServer.BLL/Parsing/QueryLexer.cs `
    AccountingServer.BLL/Parsing/QueryParser.cs
sed '-i.bak' 's/AccountingServer\.QueryGeneration/AccountingServer.BLL.Parsing/' `
    AccountingServer.BLL/Parsing/SubtotalLexer.cs `
    AccountingServer.BLL/Parsing/SubtotalParser.cs

rm -Force sed*
