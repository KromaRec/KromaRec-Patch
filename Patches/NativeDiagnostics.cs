using System;
using System.Reflection;
using Il2CppInterop.Runtime;
using UnityEngine;

namespace RecNetPlugin.Patches;

internal static class NativeDiagnostics
{
    internal static void Install() =>
        Subscribe(typeof(Application), "logMessageReceived", (Action<string, string, LogType>)SceneError);

    // Some generated proxies omit the CLR Event row. Never write a delegate field
    // or patch a native scene method as a substitute for the public subscription.
    internal static void Subscribe(Type owner, string name, Delegate callback)
    {
        try
        {
            var evt = owner.GetEvent(name, BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
            var add = evt?.GetAddMethod();
            if (add == null || !add.IsStatic)
            {
                Plugin.Log.LogInfo($"Optional Unity callback unavailable: {name}.");
                return;
            }
            var wrapper = add.GetParameters()[0].ParameterType;
            object handler;
            if (typeof(Delegate).IsAssignableFrom(wrapper))
                handler = Delegate.CreateDelegate(wrapper, callback.Target, callback.Method);
            else
            {
                var convert = typeof(DelegateSupport).GetMethod(nameof(DelegateSupport.ConvertDelegate),
                    new[] { typeof(Delegate) });
                handler = convert.MakeGenericMethod(wrapper).Invoke(null, new object[] { callback });
            }
            add.Invoke(null, new[] { handler });
            Plugin.Log.LogInfo($"Optional Unity callback subscribed: {name}.");
        }
        catch (Exception error)
        {
            Plugin.Log.LogWarning($"Optional Unity callback {name} unavailable ({error.GetType().Name}).");
        }
    }

    private static void SceneError(string message, string stack, LogType type)
    {
        if (Plugin.Debug.Value && (type is LogType.Error or LogType.Exception) &&
            StartupTrace.LooksLikeSceneFailure(message))
            Plugin.Log.LogWarning("Unity reported a scene-load failure.");
    }
}
