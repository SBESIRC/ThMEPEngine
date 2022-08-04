#pragma once
using namespace System;
using namespace System::Runtime::InteropServices;

namespace ThBIMCLI
{
    template <class T>
    public ref class ManagedObject
    {
    public:
        ManagedObject(T* instance)
            :
            mInstance(instance)
        {
        }

        virtual ~ManagedObject()
        {
            if (mInstance != nullptr)
            {
                delete mInstance;
            }
        }

        !ManagedObject()
        {
            if (mInstance != nullptr)
            {
                delete mInstance;
                mInstance = nullptr;
            }
        }

        T* GetInstance()
        {
            return mInstance;
        }

    protected:
        T* mInstance;
    };
}
