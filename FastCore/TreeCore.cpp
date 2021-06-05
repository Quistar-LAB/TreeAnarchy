#include "FastMath.h"
#include "TreeCore.h"

namespace FastCore {
    void TreeCore::UpdateTrees(TreeManager^ tm, float minX, float minZ, float maxX, float maxZ) {
		int xMin = (int)max((minX + 8) / 32 + 270, 0);
		int zMin = (int)max((minZ + 8) / 32 + 270, 0);
		int xMax = (int)max((maxX + 8) / 32 + 270, 0);
		int zMax = (int)max((maxZ + 8) / 32 + 270, 0);
		for (int i = zMin; i <= zMax; i++)
		{
			for (int j = xMin; j <= xMax; j++)
			{
				unsigned int treeID = tm->m_treeGrid[i * 540 + j];
				int treeCount = 0;
				while (treeID != 0) {
					Vector3 position = tm->m_trees->m_buffer[treeID].Position;
					float delta = max(max(minX - 8 - position.x, minZ - 8 - position.z), max(position.x - maxX - 8, position.z - maxZ - 8));
					if (delta < 0) {
						tm->m_updatedTrees[treeID >> 6] |= (long)(1 << treeID);
						tm->m_treesUpdated = true;
					}
					treeID = tm->m_trees->m_buffer[treeID].m_nextGridTree;
					if (++treeCount >= 262144) {
						break; // Error occured, invalid list!!
					}
				}
			}
		}
    }
}