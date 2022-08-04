#pragma once
#include "ManagedObject.h"
#include "../ThBIMSketchUp/ThBIMCppCommon.h"

using namespace System;

namespace ThBIMCLI {
	public ref class ThBIMSUServiceMgd : public ManagedObject<ThBIM::ThBIMSUService>
	{
	public:
		ThBIMSUServiceMgd() :ManagedObject(new ThBIM::ThBIMSUService())
		{
			//
		}
	};
}
