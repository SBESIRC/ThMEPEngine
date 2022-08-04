#pragma once

#ifdef THBIMMODULE_DLL
#  define THBIMMODULE_EXPORT __declspec(dllexport)
#else
#  define THBIMMODULE_EXPORT __declspec(dllimport)
#endif