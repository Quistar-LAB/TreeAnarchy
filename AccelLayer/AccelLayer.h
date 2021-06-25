#pragma once
#ifndef __ACCELLAYER_H__
#define __ACCELLAYER_H__

using namespace System;
using namespace System::IO;
using namespace System::Diagnostics;
using namespace System::Reflection::Emit;
using namespace System::Collections::Generic;
using namespace HarmonyLib;
using namespace UnityEngine;
using namespace ColossalFramework;

#define PROFILE_LENGTH  50
namespace TreeAnarchy {
	public ref class AccelLayer
	{
    private:
        literal String^ fileName = "TAProfile.txt";
        static Stopwatch^ BeginRenderingTimer = gcnew Stopwatch();
        static Stopwatch^ EndRenderingTimer = gcnew Stopwatch();
        //static FileStream^ m_fileStream = gcnew FileStream(fileName, FileMode::Open);
        static StreamWriter^ m_Output = gcnew StreamWriter(fileName);
        ref struct Profiler {
            array<long long>^ m_profile = gcnew array<long long>(PROFILE_LENGTH);
            int index = 0;
            bool profileFull = false;
            int averageProfile = 0;
            int minProfile = 10000; /* arbitrary default max number */
            int maxProfile = 0;
            void Profile(int elapsedTime) {
                int average = 0;
                minProfile = Mathf::Min(minProfile, elapsedTime);
                maxProfile = Mathf::Max(maxProfile, elapsedTime);
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

        static void BeginRedenderingLoopOpt(TreeManager^);
    public:
        static IEnumerable<CodeInstruction^>^ BeginRenderingImplTranspiler(IEnumerable<CodeInstruction^>^, ILGenerator^);
        static bool EndRenderingImplPrefixProfiled(TreeManager^ __instance, RenderManager::CameraInfo^ cameraInfo);
        static bool EndRenderingImplPrefix(TreeManager^ __instance, RenderManager::CameraInfo^ cameraInfo);
        __declspec(noinline) static bool EndRenderingImplPrefixProfiledWithoutAccel();
        __declspec(noinline) static void EndRenderingImplPostfix();
        __declspec(noinline) static bool BeginRenderingImplPrefix();
        __declspec(noinline) static void BeginRenderingImplPostfix();

        static void StartProfile() {
        }
    };
}

#endif /* __ACCELLAYER_H__ */