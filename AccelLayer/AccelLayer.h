#pragma once
#pragma warning( disable : 4691 )
#ifndef __ACCELLAYER_H__
#define __ACCELLAYER_H__

using namespace System;
using namespace System::IO;
using namespace System::Diagnostics;
using namespace System::Reflection;
using namespace System::Reflection::Emit;
using namespace System::Collections::Generic;
using namespace System::Runtime::InteropServices;
using namespace HarmonyLib;
using namespace ColossalFramework;
using namespace UnityEngine;

#define PROFILE_LENGTH  512
namespace TreeAnarchy {
    [UnmanagedFunctionPointer(CallingConvention::StdCall, CharSet = CharSet::Ansi)]
    public delegate int AdditionDelegate(int, int);
    [UnmanagedFunctionPointer(CallingConvention::StdCall, CharSet = CharSet::Ansi)]
    public delegate float ClampDelegate(float, float, float);
    [UnmanagedFunctionPointer(CallingConvention::StdCall, CharSet = CharSet::Ansi)]
    public delegate void TestDelegate();

    public ref class AccelLayer
	{
    public:
        typedef ref struct corePointers {
            IntPtr coreAddition;
            IntPtr coreClamp;
            IntPtr coreTest;
        } corePointers;
    private:
        literal String^ fileName = "TAProfile.txt";
        static Stopwatch^ BeginRenderingTimer = gcnew Stopwatch();
        static Stopwatch^ EndRenderingTimer = gcnew Stopwatch();
        static StreamWriter^ m_Output = gcnew StreamWriter(fileName);
        ref struct Profiler {
            array<double>^ m_profile = gcnew array<double>(PROFILE_LENGTH);
            int index = 0;
            bool profileFull = false;
            double averageProfile = 0;
            double minProfile = 10000; /* arbitrary default max number */
            double maxProfile = 0;
            double min(double a, double b) { if (a < b) return a; return b; }
            double max(double a, double b) { if (a > b) return a; return b; }
            void Profile(double elapsedTime) {
                double average = 0;
                minProfile = min(minProfile, elapsedTime);
                maxProfile = max(maxProfile, elapsedTime);
                if (index == PROFILE_LENGTH) {
                    index = 0;
                    profileFull = true;
                }
                m_profile[index++] = elapsedTime;
                if (profileFull) {
                    for (int i = 0; i < PROFILE_LENGTH; i++) {
                        average += m_profile[i];
                    }
                    averageProfile = average / PROFILE_LENGTH;
                } else {
                    for (int i = 0; i < index; i++) {
                        average += m_profile[i];
                    }
                    averageProfile = average / index;
                }
            }
        };
        static Profiler m_storedBeginRenderProfile;
        static Profiler m_storedEndRenderProfile;

        static TreeManager^ treeManagerInstance;
        static InfoManager^ infoManagerInstance;
        static TerrainManager^ terrainManagerInstance;
        static RenderManager^ renderManagerInstance;

        static AdditionDelegate^ coreAddition;
        static ClampDelegate^ coreClamp;
        static TestDelegate^ coreTest;

        static FastList<PrefabCollection<TreeInfo^>::PrefabData>^ m_simulationPrefabs;

        static void BeginRedenderingLoopOpt(TreeManager^);
    public:

        static IEnumerable<CodeInstruction^>^ BeginRenderingImplTranspiler(IEnumerable<CodeInstruction^>^, ILGenerator^);
        static bool EndRenderingImplPrefixProfiled(TreeManager^ __instance, RenderManager::CameraInfo^ cameraInfo);
        static bool EndRenderingImplPrefix(TreeManager^ __instance, RenderManager::CameraInfo^ cameraInfo);
        __declspec(noinline) static bool EndRenderingImplPrefixProfiledWithoutAccel();
        __declspec(noinline) static void EndRenderingImplPostfix();
        __declspec(noinline) static bool BeginRenderingImplPrefix();
        __declspec(noinline) static void BeginRenderingImplPostfix();

        static void StartProfile() {}
        static void AccelLayer::SetupRenderingFramework(TreeManager^, InfoManager^, TerrainManager^, RenderManager^);
        static void SetupCore(corePointers^);
    };
}

#endif /* __ACCELLAYER_H__ */