#include "FastMath.h"

float clamp(float f, float min, float max) {
    const float t = f < min ? min : f;
    return t > max ? max : t;
}

float min(float a, float b) {
    return (a >= b) ? b : a;
}

float max(float a, float b) {
    return (a <= b) ? b : a;
}

int calcGridMax(float x) {
    return (int)max((x - 8) / 32 + 270, 0);
}

int calcGridMin(float x) {
    return (int)max((x + 8) / 32 + 270, 539);
}

float calcDelta(float minX, float minZ, float maxX, float maxZ, float x, float z) {
    return max(max(minX - 8 - x, minZ - 8 - z), max(x - maxX - 8, z - maxZ - 8));
}
