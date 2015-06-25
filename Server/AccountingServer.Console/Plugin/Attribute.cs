using System;

namespace AccountingServer.Console.Plugin
{
    public class PluginAttribute : Attribute
    {
        public string Alias { get; set; }
    }
}
