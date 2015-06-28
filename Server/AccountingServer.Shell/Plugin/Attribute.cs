using System;

namespace AccountingServer.Shell.Plugin
{
    public class PluginAttribute : Attribute
    {
        public string Alias { get; set; }
    }
}
