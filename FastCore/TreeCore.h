#pragma once
#ifndef __TREECORE_H__
#define __TREECORE_H__
#include <stdint.h>

using namespace System::Runtime::InteropServices;
using namespace ColossalFramework;
using namespace UnityEngine;

#ifdef __cplusplus
extern "C" {
#endif

#define APIEXPORT __declspec(dllexport)

namespace FastCore {
    class TreeCore
    {
    public:
        void UpdateTrees(TreeManager^ tm, float minX, float minZ, float maxX, float maxZ);
    };
}
#ifdef __cplusplus
}
#endif
#endif