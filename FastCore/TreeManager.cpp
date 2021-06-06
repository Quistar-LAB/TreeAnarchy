#include "FastMath.h"
#include "TreeManager.h"

#pragma pack(push, 1)
typedef struct {
	intptr_t hTreeGridBuffer;
	intptr_t DrawMeshFunc;
	float x, y, z;

} treedata_t;
#pragma pack(pop)

void UpdateTrees(intptr_t hTreeManager, float minX, float minZ, float maxX, float maxZ) {
	int xMin = calcGridMax(minX);
	int zMin = calcGridMax(minZ);
	int xMax = calcGridMin(maxX);
	int zMax = calcGridMin(maxZ);
#if 0
	for (int i = zMin; i <= zMax; i++) {
		for (int j = xMin; j <= xMax; j++) {
			unsigned int treeID = tm->m_treeGrid[i * 540 + j];
			int treeCount = 0;
			while (treeID != 0) {
				Vector3 position = tm->m_trees->m_buffer[treeID].Position;
				float delta = calcDelta(minX, minZ, maxX, maxZ, position.x, position.y);
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
#endif
}
