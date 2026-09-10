using System;

namespace VitaCernita.Core.Configuration;

public class LuaConfigException : Exception
{
    public LuaConfigException(string message) : base(message) { }
    public LuaConfigException(string message, Exception innerException) : base(message, innerException) { }
}
