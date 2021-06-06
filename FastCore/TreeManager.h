#pragma once
#ifndef __TREEMANAGER_H__
#define __TREEMANAGER_H__
#include <stdint.h>
#ifdef __cplusplus
extern "C" {
#endif
#define APIEXPORT __declspec(dllexport)
    void APIEXPORT __cdecl UpdateTrees(intptr_t);

#ifdef __cplusplus
}
#endif
#endif