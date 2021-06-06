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
		public static TAWrapper instance = default;
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

		[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		public delegate float DCore_Clamp(float d, float min, float max);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		public delegate float DCore_Min(float x, float x1);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		public delegate float DCore_Max(float x, float x1);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		public delegate int DCore_calcGridMax(float x);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		public delegate int DCore_calcGridMin(float x);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		public delegate float DCore_calcDelta(float xMin, float zMin, float xMax, float zMax, float x, float y);

		public readonly DCore_Clamp Clamp;
		public readonly DCore_Min Min;
		public readonly DCore_Max Max;
		public readonly DCore_calcGridMax CalcGridMax;
		public readonly DCore_calcGridMin CalcGridMin;
		public readonly DCore_calcDelta CalcDelta;

        public TAWrapper(string name) : base(ExtractResource(name)) {
			instance = this;
			Clamp = GetDelegateFromFuncName<DCore_Clamp>("clamp");
			Min = GetDelegateFromFuncName<DCore_Min>("min");
			Max = GetDelegateFromFuncName<DCore_Max>("max");
			CalcGridMin = GetDelegateFromFuncName<DCore_calcGridMin>("calcGridMin");
			CalcGridMax = GetDelegateFromFuncName<DCore_calcGridMax>("calcGridMax");
			CalcDelta = GetDelegateFromFuncName<DCore_calcDelta>("calcDelta");
        }

		// TreeManager
		public static bool TAEndRenderingImplPrefix(TreeManager __instance, RenderManager.CameraInfo cameraInfo) {
			FastList<RenderGroup> renderedGroups = Singleton<RenderManager>.instance.m_renderedGroups;
			for(int i = 0; i < renderedGroups.m_size; i++) {
				RenderGroup renderGroup = renderedGroups.m_buffer[i];
				if((renderGroup.m_instanceMask & 1 << __instance.m_treeLayer) != 0) {
					int num = renderGroup.m_x * 540 / 45;
					int num2 = renderGroup.m_z * 540 / 45;
					int num3 = (renderGroup.m_x + 1) * 540 / 45 - 1;
					int num4 = (renderGroup.m_z + 1) * 540 / 45 - 1;
					for(int j = num2; j <= num4; j++) {
						for(int k = num; k <= num3; k++) {
							int num5 = j * 540 + k;
							uint num6 = __instance.m_treeGrid[num5];
							int num7 = 0;
							while(num6 != 0u) {
								__instance.m_trees.m_buffer[(int)((UIntPtr)num6)].RenderInstance(cameraInfo, num6, renderGroup.m_instanceMask);
								num6 = __instance.m_trees.m_buffer[(int)((UIntPtr)num6)].m_nextGridTree;
								if(++num7 >= 262144) {
									CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
									break;
								}
							}
						}
					}
				}
			}
			int num8 = PrefabCollection<TreeInfo>.PrefabCount();
			for(int l = 0; l < num8; l++) {
				TreeInfo prefab = PrefabCollection<TreeInfo>.GetPrefab((uint)l);
				if(prefab != null) {
					if(prefab.m_lodCount != 0) {
						TreeInstance.RenderLod(cameraInfo, prefab);
					}
				}
			}
			if(Singleton<InfoManager>.instance.CurrentMode == InfoManager.InfoMode.None) {
				int size = __instance.m_burningTrees.m_size;
				for(int m = 0; m < size; m++) {
					TreeManager.BurningTree burningTree = __instance.m_burningTrees.m_buffer[m];
					if(burningTree.m_treeIndex != 0u) {
						float fireIntensity = (float)burningTree.m_fireIntensity * 0.003921569f;
						float fireDamage = (float)burningTree.m_fireDamage * 0.003921569f;
						__instance.RenderFireEffect(cameraInfo, burningTree.m_treeIndex, ref __instance.m_trees.m_buffer[(int)((UIntPtr)burningTree.m_treeIndex)], fireIntensity, fireDamage);
					}
				}
			}
			return false; // skip original code
		}
	}
}
