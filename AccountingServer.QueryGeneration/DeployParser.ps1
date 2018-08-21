cp ./obj/Gen/Query* ../AccountingServer.BLL/Parsing/
cp ./obj/Gen/Subtotal* ../AccountingServer.BLL/Parsing/
pushd
cd ../AccountingServer.BLL/Parsing/
sed '-i.bak' 's/^public partial class Query/internal partial class Query/' QueryLexer.cs QueryParser.cs
sed '-i.bak' 's/^public partial class Subtotal/internal partial class Subtotal/' SubtotalLexer.cs SubtotalParser.cs
rm sed*
popd
