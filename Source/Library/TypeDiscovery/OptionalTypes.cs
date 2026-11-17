#nullable enable
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;

namespace Legato.Reflection
{
    internal static class OptionalTypes
    {
        private static readonly ConcurrentDictionary<string, Assembly> Loaded = new(StringComparer.OrdinalIgnoreCase);

        static OptionalTypes()
        {
            AppDomain.CurrentDomain.AssemblyLoad += (_, args) => Remember(args.LoadedAssembly);
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()) Remember(assembly);
        }

        private static void Remember(Assembly assembly)
        {
            string? name = assembly.GetName().Name;
            if (name != null) Loaded[name] = assembly;
        }

        internal static Type? FindLoaded(string typeName, string assemblyName) =>
            Loaded.TryGetValue(assemblyName, out var assembly) ? assembly.GetType(typeName, false) : null;

        internal static Type? Resolve(string name, bool throwOnError = false, bool ignoreCase = false) =>
            Type.GetType(name, ResolveAssembly, null, throwOnError, ignoreCase);

        private static Assembly? ResolveAssembly(AssemblyName requested)
        {
            string? name = requested.Name;
            if (name == null) return null;
            if (Loaded.TryGetValue(name, out var loaded)) return loaded;

            string? managed = Path.GetDirectoryName(typeof(UnityEngine.Object).Assembly.Location);
            if (managed == null) return null;
            string? game = Directory.GetParent(managed)?.Parent?.FullName;
            string fileName = name + ".dll";
            bool present = File.Exists(Path.Combine(managed, fileName));
            if (!present && game != null)
            {
                foreach (string directory in new[] { "Mods", "Mods/Core", "Mods/Libraries", "IPA/Data/Managed", "IPA/CoreLibraries", "Plugins", "Libs" })
                    if (File.Exists(Path.Combine(game, directory, fileName))) { present = true; break; }
            }
            // Preserve normal loader preparation for an available assembly, without probing absent optional DLLs.
            return present ? Assembly.Load(requested) : null;
        }
    }
}
