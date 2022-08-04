#pragma once
#include "ThBIMCppExport.h"

namespace ThBIM
{
    class THBIMMODULE_EXPORT ThBIMSUService
    {
    public:
        ThBIMSUService();
        ~ThBIMSUService();

    private:
        ThBIMSUService(ThBIMSUService&&) = delete;
        ThBIMSUService(const ThBIMSUService&) = delete;
        ThBIMSUService& operator=(ThBIMSUService&&) = delete;
        ThBIMSUService& operator=(const ThBIMSUService&) = delete;
    };
}