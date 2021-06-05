#include "TreeManager.h"

#pragma unmanaged
int Addition(int x, int y) {
    return x + y;
}

float Clamp(float f, float min, float max) {
    const float t = f < min ? min : f;
    return t > max ? max : t;
}

float min(float a, float b) {
	return (a >= b) ? b : a;
}

float max(float a, float b) {
	return (a <= b) ? b : a;
}

#define MAXTREE 2000000
#pragma managed
// TreeManager
int UpdateTrees(float minX, float minZ, float maxX, float maxZ)
{
	bool updated = false;
	int num = (int)max((minX - 8) / 32 + 270, 0);
	int num2 = (int)max((minZ - 8) / 32 + 270, 0);
	int num3 = (int)min((maxX + 8) / 32 + 270, 539);
	int num4 = (int)min((maxZ + 8) / 32 + 270, 539);
	for (int i = num2; i <= num4; i++)
	{
		for (int j = num; j <= num3; j++)
		{
			unsigned int num5 = 10;
			unsigned int num6 = 0;
			while (num5 != 0)
			{
				float num7 = max(max(minX - 8, minZ - 8), max(maxX - 8, maxZ - 8));
				if (num7 < 0)
				{
					updated = true;
				}

				if (++num6 >= MAXTREE)
				{
					return (int)(num7 + num + num2 + num3 + num4);
				}
			}
		}
	}
	return 0;
}

int useTest(float x, float y, float x1, float y1) {
	float xz = x + y;
	float ab = x1 + y1;
	return (int)(xz + ab);
}

int Test() {
	float x = 100;
	float y = 200;
	float x1 = 233;
	float y1 = 400;
	int result = 0;
	for (int i = 0; i < 500; i++) {
		x += UpdateTrees(x, y, x1, y1);
		x += 1;
		y += 1;
		x1 += 1;
		y1 += 1;
		result += useTest(x, y, x1, y1);
	}
	return result;
}
