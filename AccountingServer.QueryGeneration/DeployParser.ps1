cp ./obj/Release/Query* ../AccountingServer.BLL/Parsing/
pushd
cd ../AccountingServer.BLL/Parsing/
sed '-i.bak' 's/^public partial class Query/internal partial class Query/' QueryLexer.cs QueryParser.cs
rm sed*
popd
