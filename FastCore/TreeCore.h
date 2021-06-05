#pragma once

using namespace ColossalFramework;
using namespace UnityEngine;

namespace FastCore {
    ref class TreeCore
    {
    public:
        void UpdateTrees(TreeManager^ tm, float minX, float minZ, float maxX, float maxZ);
    };
}

