using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;
using ColossalFramework;
using ColossalFramework.Plugins;
using TreeAnarchy.Utils;

namespace TreeAnarchy {
    class TAWrapper : MemoryModule {
        private static bool AssemblyPath(string name, out string path) {
            path = null;
            foreach(PluginManager.PluginInfo info in Singleton<PluginManager>.instance.GetPluginsInfo()) {
                if (info.name.Contains(name)) {
                    path = info.modPath;
                    return true;
                }
            }
            return false;
        }

        private static byte[] ExtractResource(string filename) {
            byte[] buf = default;
            Assembly a = Assembly.GetExecutingAssembly();
            using(Stream resFilestream = a.GetManifestResourceStream(filename)) {
                if (resFilestream == null) return null;
                buf = new byte[resFilestream.Length];
                resFilestream.Read(buf, 0, buf.Length);
            }
            return buf;
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public delegate int Delegate_Core_Addition(int x, int y);
        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public delegate int Delegate_Core_Test();

        readonly Delegate_Core_Addition Addition;
        readonly Delegate_Core_Test Test;

        public TAWrapper(string name) : base(ExtractResource(name)) {
            Addition = GetDelegateFromFuncName<Delegate_Core_Addition>("Addition");
            Test = GetDelegateFromFuncName<Delegate_Core_Test>("Test");
        }

        public void PrintDebug() {
            int x = 10;
            int y = 13;
            int z = Addition(x, y);
            Debug.Log($"TreeAnarchy: Result is {z}");
        }
    }
}
