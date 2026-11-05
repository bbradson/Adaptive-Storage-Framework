using System;
using System.Reflection;

namespace Multiplayer.API;

public struct SyncType
{
    public readonly Type type;
    public bool expose;
    public bool contextMap;

    public SyncType(Type type)
    {
        this.type = type;
        this.expose = false;
        contextMap = false;
    }

    public static implicit operator SyncType(ParameterInfo param)
    {
        return new SyncType(param.ParameterType) { /*expose = param.HasAttribute<SyncExpose>(), contextMap = param.HasAttribute<SyncContextMap>()*/ };
    }

    public static implicit operator SyncType(Type type)
    {
        return new SyncType(type);
    }
}