#pragma once
#ifndef __FASTMATH_H__
#define __FASTMATH_H__

#ifdef __cplusplus
extern "C" {
#endif
#define APIEXPORT __declspec(dllexport)
float APIEXPORT clamp(float, float, float);
float APIEXPORT min(float, float);
float APIEXPORT max(float, float);
int APIEXPORT calcGridMax(float);
int APIEXPORT calcGridMin(float);
float APIEXPORT calcDelta(float, float, float, float, float, float);
#ifdef __cplusplus
}
#endif
#endif