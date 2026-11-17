using System;
using System.Reflection;

namespace Heck;

public class Capability
{
    private static Action<string>? _deregister;
    private static bool _initialized;
    private static Action<string>? _register;

    private readonly string _capability;

    public Capability(string capability)
    {
        _capability = capability;

        if (_initialized)
        {
            return;
        }

        Type? collections = Type.GetType("SongCore.Collections, SongCore");
        if (collections == null)
        {
            _initialized = true;
            return;
        }

        const BindingFlags flags = BindingFlags.Public | BindingFlags.Static;
        Type[] signature = { typeof(string) };
        MethodInfo register = collections.GetMethod("RegisterCapability", flags, null, signature, null)
            ?? throw new MissingMethodException(collections.FullName, "RegisterCapability(string)");
        MethodInfo deregister = collections.GetMethod("DeregisterCapability", flags, null, signature, null)
            ?? collections.GetMethod("DeregisterizeCapability", flags, null, signature, null)
            ?? throw new MissingMethodException(collections.FullName, "DeregisterCapability(string)");
        var registerAction = (Action<string>)Delegate.CreateDelegate(typeof(Action<string>), register);
        var deregisterAction = (Action<string>)Delegate.CreateDelegate(typeof(Action<string>), deregister);
        _register = registerAction;
        _deregister = deregisterAction;
        _initialized = true;
    }

    public void Deregister()
    {
        _deregister?.Invoke(_capability);
    }

    public void Register()
    {
        _register?.Invoke(_capability);
    }
}
