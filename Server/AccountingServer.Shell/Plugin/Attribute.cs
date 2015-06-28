using System;

// ReSharper disable once CheckNamespace
namespace AccountingServer.Plugin
{
    public class PluginAttribute : Attribute
    {
        public string Alias { get; set; }
    }
}
