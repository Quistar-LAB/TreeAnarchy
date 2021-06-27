#include "AccelLayer.h"

#pragma warning(disable:4483)
void __clrcall __identifier(".cctor")() { }

#define DebugPrint(msg) UnityEngine::Debug::Log("TreeAnarchy: " + msg)
#define GetMethod(type, name) AccessTools::Method(type::typeid, name, nullptr, nullptr)
#define GetMethodArgs(type, name, args) AccessTools::Method(type::typeid, name, args, nullptr)
#define GetField(type, name) AccessTools::Field(type::typeid, name)
#define GetPropertyGetter(type, name) AccessTools::PropertyGetter(type::typeid, name)
#define GetPropertySetter(type, name) AccessTools::PropertySetter(type::typeid, name)

#define refreshLOD(data, mesh) if (data) { if (!mesh) mesh = gcnew Mesh(); data->PopulateMesh(mesh); data = nullptr; }
namespace TreeAnarchy {
    void AccelLayer::BeginRedenderingLoopOpt(TreeManager^ instance) {
        // unroll loop... wish we had .net5 so these can be done automatically by the compiler
        unsigned int maxCount = PrefabCollection<TreeInfo^>::PrefabCount();
        for (unsigned int i = 0; i < maxCount; i++) {
            TreeInfo^ prefab = PrefabCollection<TreeInfo^>::GetPrefab(i);
            if (prefab) {
                refreshLOD(prefab->m_lodMeshData1, prefab->m_lodMesh1);
                refreshLOD(prefab->m_lodMeshData4, prefab->m_lodMesh4);
                refreshLOD(prefab->m_lodMeshData8, prefab->m_lodMesh8);
                refreshLOD(prefab->m_lodMeshData16, prefab->m_lodMesh16);
                if (!prefab->m_lodMaterial) {
                    Shader^ shader = Singleton<RenderManager^>::instance->m_properties->m_groupLayerShaders[instance->m_treeLayer];
                    prefab->m_lodMaterial = gcnew Material(shader);
                    prefab->m_lodMaterial->EnableKeyword("MULTI_INSTANCE");
                }
                prefab->m_lodMaterial->mainTexture = instance->m_renderDiffuseTexture;
                prefab->m_lodMaterial->SetTexture(instance->ID_XYCAMap, instance->m_renderXycaTexture);
                prefab->m_lodMaterial->SetTexture(instance->ID_ShadowAMap, instance->m_renderShadowTexture);
            }
        }
    }

    IEnumerable<CodeInstruction^>^ AccelLayer::BeginRenderingImplTranspiler(IEnumerable<CodeInstruction^>^ instructions, ILGenerator^ il) {
        int firstIndex = 0, lastIndex = 0;
        Label lb;

        auto codes = gcnew List<CodeInstruction^>(instructions);
        for (int i = 0; i < codes->Count; i++) {
            if (codes[i]->opcode == OpCodes::Endfinally) {
                firstIndex = i + 1;
                lb = codes[i + 1]->labels[0];
            }
            if (codes[i]->opcode == OpCodes::Ret) {
                lastIndex = i;
            }
        }
        codes->RemoveRange(firstIndex, lastIndex - firstIndex);

        auto snippet = gcnew CodeInstruction(OpCodes::Ldarg_0, nullptr);
        snippet->labels->Add(lb);
        codes->Insert(firstIndex, gcnew CodeInstruction(OpCodes::Call, GetMethod(AccelLayer, "BeginRedenderingLoopOpt")));
        codes->Insert(firstIndex, snippet);

        return codes;
    }

    bool AccelLayer::EndRenderingImplPrefixProfiled(TreeManager^ __instance, RenderManager::CameraInfo^ cameraInfo) {
        EndRenderingTimer->Reset();
        EndRenderingTimer->Start();
        FastList<RenderGroup^>^ renderedGroups = Singleton<RenderManager^>::instance->m_renderedGroups;
        int layerMask = 1 << __instance->m_treeLayer;
        array<unsigned int>^ treeGrid = __instance->m_treeGrid;
        array<::TreeInstance>^ buf = __instance->m_trees->m_buffer;
        int maxSize = renderedGroups->m_size;
        for (int i = 0; i < maxSize; i++) {
            RenderGroup^ renderGroup = renderedGroups->m_buffer[i];
            if ((renderGroup->m_instanceMask & layerMask)) {
                int minX = renderGroup->m_x * 12; /* 540 / 45; Avoid division */
                int minZ = renderGroup->m_z * 12; /* 540 / 45; Avoid division */
                int maxX = (renderGroup->m_x + 1) * 12 - 1; // 540 / 45 - 1; avoid division
                int maxZ = (renderGroup->m_z + 1) * 12 - 1; // 540 / 45 - 1; avoid division
                for (int j = minZ; j <= maxZ; j++) {
                    for (int k = minX; k <= maxX; k++) {
                        unsigned int treeID = treeGrid[j * 540 + k];
                        while (treeID) {
                            buf[treeID].RenderInstance(cameraInfo, treeID, renderGroup->m_instanceMask);
                            treeID = buf[treeID].m_nextGridTree;
                            // removed bound check here
                        }
                    }
                }
            }
        }
        unsigned int maxLen = PrefabCollection<TreeInfo^>::PrefabCount();
        for (unsigned int i = 0; i < maxLen; i++) {
            TreeInfo^ prefab = PrefabCollection<TreeInfo^>::GetPrefab(i);
            if (prefab) {
                if (prefab->m_lodCount != 0) ::TreeInstance::RenderLod(cameraInfo, prefab);
            }
        }
        if (Singleton<InfoManager^>::instance->CurrentMode == InfoManager::InfoMode::None) {
            int size = __instance->m_burningTrees->m_size;
            for (int m = 0; m < size; m++) {
                TreeManager::BurningTree burningTree = __instance->m_burningTrees->m_buffer[m];
                if (burningTree.m_treeIndex) {
                    float fireIntensity = (float)burningTree.m_fireIntensity * 0.003921569f;
                    float fireDamage = (float)burningTree.m_fireDamage * 0.003921569f;
                    __instance->RenderFireEffect(cameraInfo, burningTree.m_treeIndex, __instance->m_trees->m_buffer[burningTree.m_treeIndex], fireIntensity, fireDamage);
                }
            }
        }
        return false; // replace the original method
    }

    bool AccelLayer::EndRenderingImplPrefix(TreeManager^ __instance, RenderManager::CameraInfo^ cameraInfo) {
        FastList<RenderGroup^>^ renderedGroups = Singleton<RenderManager^>::instance->m_renderedGroups;
        int layerMask = 1 << __instance->m_treeLayer;
        array<unsigned int>^ treeGrid = __instance->m_treeGrid;
        array<::TreeInstance>^ buf = __instance->m_trees->m_buffer;
        int maxSize = renderedGroups->m_size;
        for (int i = 0; i < maxSize; i++) {
            RenderGroup^ renderGroup = renderedGroups->m_buffer[i];
            if ((renderGroup->m_instanceMask & layerMask)) {
                int minX = renderGroup->m_x * 12; /* 540 / 45; Avoid division */
                int minZ = renderGroup->m_z * 12; /* 540 / 45; Avoid division */
                int maxX = (renderGroup->m_x + 1) * 12 - 1; // 540 / 45 - 1; avoid division
                int maxZ = (renderGroup->m_z + 1) * 12 - 1; // 540 / 45 - 1; avoid division
                for (int j = minZ; j <= maxZ; j++) {
                    for (int k = minX; k <= maxX; k++) {
                        unsigned int treeID = treeGrid[j * 540 + k];
                        while (treeID) {
                            buf[treeID].RenderInstance(cameraInfo, treeID, renderGroup->m_instanceMask);
                            treeID = buf[treeID].m_nextGridTree;
                            // removed bound check here
                        }
                    }
                }
            }
        }
        unsigned int maxLen = PrefabCollection<TreeInfo^>::PrefabCount();
        for (unsigned int i = 0; i < maxLen; i++) {
            TreeInfo^ prefab = PrefabCollection<TreeInfo^>::GetPrefab(i);
            if (prefab) {
                if (prefab->m_lodCount != 0) ::TreeInstance::RenderLod(cameraInfo, prefab);
            }
        }
        if (Singleton<InfoManager^>::instance->CurrentMode == InfoManager::InfoMode::None) {
            int size = __instance->m_burningTrees->m_size;
            for (int m = 0; m < size; m++) {
                TreeManager::BurningTree burningTree = __instance->m_burningTrees->m_buffer[m];
                if (burningTree.m_treeIndex) {
                    float fireIntensity = (float)burningTree.m_fireIntensity * 0.003921569f;
                    float fireDamage = (float)burningTree.m_fireDamage * 0.003921569f;
                    __instance->RenderFireEffect(cameraInfo, burningTree.m_treeIndex, __instance->m_trees->m_buffer[burningTree.m_treeIndex], fireIntensity, fireDamage);
                }
            }
        }
        return false; // replace the original method
    }

    bool AccelLayer::BeginRenderingImplPrefix() {
        BeginRenderingTimer->Reset();
        BeginRenderingTimer->Start();
        return true;
    }
    void AccelLayer::BeginRenderingImplPostfix() {
        m_storedBeginRenderProfile.Profile(BeginRenderingTimer->Elapsed.TotalMilliseconds * 1000000);
    }

    bool AccelLayer::EndRenderingImplPrefixProfiledWithoutAccel() {
        EndRenderingTimer->Reset();
        EndRenderingTimer->Start();
        return true;
    }

    void AccelLayer::EndRenderingImplPostfix() {
        m_storedEndRenderProfile.Profile(EndRenderingTimer->Elapsed.TotalMilliseconds * 1000000);
        String^ output = String::Format(
            "BeginRenderingImpl min process time: {0}ns\n"
            "BeginRenderingImpl max process time: {1}ns\n"
            "BeginRenderingImpl average process time: {2}ns\n"
            "EndRenderingImpl min process time: {3}ns\n"
            "EndRenderingImpl max process time: {4}ns\n"
            "EndRenderingImpl average process time: {5}ns",
            m_storedBeginRenderProfile.minProfile, m_storedBeginRenderProfile.maxProfile, m_storedBeginRenderProfile.averageProfile,
            m_storedEndRenderProfile.minProfile, m_storedEndRenderProfile.maxProfile, m_storedEndRenderProfile.averageProfile);
        m_Output->BaseStream->SetLength(0);
        m_Output->WriteLine(output);
        m_Output->Flush();
    }
}
