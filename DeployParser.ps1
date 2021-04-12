$ErrorActionPreference = "Stop"

cp AccountingServer.QueryGeneration/obj/Gen/net5.0/Query.tokens `
    AccountingServer.QueryGeneration/obj/Gen/net5.0/QueryLexer.cs `
    AccountingServer.QueryGeneration/obj/Gen/net5.0/QueryLexer.tokens `
    AccountingServer.QueryGeneration/obj/Gen/net5.0/QueryParser.cs `
    AccountingServer.BLL/Parsing/
cp AccountingServer.SubtotalGeneration/obj/Gen/net5.0/Subtotal.tokens `
    AccountingServer.SubtotalGeneration/obj/Gen/net5.0/SubtotalLexer.cs `
    AccountingServer.SubtotalGeneration/obj/Gen/net5.0/SubtotalLexer.tokens `
    AccountingServer.SubtotalGeneration/obj/Gen/net5.0/SubtotalParser.cs `
    AccountingServer.BLL/Parsing/

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
