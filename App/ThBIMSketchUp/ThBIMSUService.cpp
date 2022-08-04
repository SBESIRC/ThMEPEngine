#include "pch.h"
#include "ThBIMSUService.h"

// SketchUp
#include <SketchUpAPI/slapi.h>
#include <SketchUpAPI/geometry.h>
#include <SketchUpAPI/initialize.h>
#include <SketchUpAPI/unicodestring.h>
#include <SketchUpAPI/model/model.h>
#include <SketchUpAPI/model/entities.h>
#include <SketchUpAPI/model/face.h>
#include <SketchUpAPI/model/edge.h>
#include <SketchUpAPI/model/vertex.h>

using namespace ThBIM;

ThBIMSUService::ThBIMSUService()
{
    SUInitialize();
}

ThBIMSUService::~ThBIMSUService()
{
    SUTerminate();
}
