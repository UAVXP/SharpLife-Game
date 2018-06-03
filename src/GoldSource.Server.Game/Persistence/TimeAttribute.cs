using System;

namespace Server.Persistence
{
    /// <summary>
    /// Marks a persisted member as being a time field
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class TimeAttribute : Attribute
    {
    }
}
