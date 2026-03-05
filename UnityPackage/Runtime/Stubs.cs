using System;

namespace osu.Framework.Statistics
{
    public static class GlobalStatistics 
    { 
        public static GlobalStatistic<T> Get<T>(string a, string b) => new GlobalStatistic<T>(); 
    }
    
    public class GlobalStatistic<T> 
    { 
        public T Value { get; set; } 
    }
}

namespace osu.Framework.Testing
{
    public static class HotReloadCallbackReceiver
    {
        public static event Action<object> CompilationFinished;
        
        // This is a stub. Unity handles its Domain Reload logic via [RuntimeInitializeOnLoadMethod].
        // We will explicitly call DependencyActivator.ClearCache() from a Unity bootstrapper later.
    }
}
