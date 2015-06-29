using System;

// ReSharper disable once CheckNamespace
namespace AccountingServer.Plugins
{
    public class PluginAttribute : Attribute
    {
        public string Alias { get; set; }
    }
}
