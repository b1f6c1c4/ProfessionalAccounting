namespace AccountingServer.Console.Plugin
{
    public interface IPlugin
    {
        IQueryResult Execute(params string[] pars);
    }
}
