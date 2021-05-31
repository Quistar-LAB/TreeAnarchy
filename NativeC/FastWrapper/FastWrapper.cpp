#pragma unmanaged
#include "pch.h"
#include "FastWrapper.h"

using namespace System;
using namespace System::Diagnostics;
using namespace UnityEngine;
using namespace ColossalFramework;

namespace TreeAnarchy
{
	static void TestMe() {

	}

    public class FastWrapper {
	public:
		static void FastWrapper::TerrainUpdated(TreeManager tm, TerrainArea heightArea, TerrainArea surfaceArea, TerrainArea zoneArea)
		{
			float x = surfaceArea.m_min.x;
			float z = surfaceArea.m_min.z;
			float x2 = surfaceArea.m_max.x;
			float z2 = surfaceArea.m_max.z;
			int maxX = Mathf::Max((int)((x - 8) / 32 + 270), 0);
			int maxZ = Mathf::Max((int)((z - 8) / 32 + 270), 0);
			int minX = Mathf::Min((int)((x2 + 8) / 32 + 270), 539);
			int minZ = Mathf::Min((int)((z2 + 8) / 32 + 270), 539);

			for (int i = maxZ; i <= minZ; i++) {
				for (int j = maxX; j <= minX; j++) {
					unsigned int treeID = tm.m_treeGrid[i * 540 + j];
					int num6 = 0;
					while (treeID != 0) {
						Vector3 *position = &tm.m_trees->m_buffer[treeID].Position;
						float num7 = Mathf::Max(Mathf::Max(x - 8 - position->x, z - 8 - position->z), Mathf::Max(position->x - x2 - 8, position->z - z2 - 8));
						if (num7 < 0) tm.m_trees->m_buffer[treeID].TerrainUpdated(treeID, x, z, x2, z2);
						treeID = tm.m_trees->m_buffer[treeID].m_nextGridTree;
						if (++num6 >= 262144) {
							CODebugBase<LogChannel>::Error(LogChannel::Core, "Invalid list detected!\n" + Environment::StackTrace);
							break;
						}
					}
				}
			}

		}

    };
}
