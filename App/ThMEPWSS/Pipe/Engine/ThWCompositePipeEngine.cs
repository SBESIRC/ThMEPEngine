﻿using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPWSS.Pipe.Model;

namespace ThMEPWSS.Pipe.Engine
{
    public class ThWCompositePipeEngine : IDisposable
    {
        public ThWToiletPipeEngine ToiletPipeEngine { get; set; }
        public ThWKitchenPipeEngine KitchenPipeEngine { get; set; }

        public List<ThWKitchenPipe> KitchenPipes
        {
            get
            {
                return KitchenPipeEngine.Pipes;
            }
        }

        public List<ThWToiletPipe>  ToiletPipes
        {
            get
            {
                return ToiletPipeEngine.Pipes;
            }
        }

        public ThWCompositePipeEngine(ThWKitchenPipeEngine kitchenPipeEngine, ThWToiletPipeEngine toiletPipeEngine)
        {
            ToiletPipeEngine = toiletPipeEngine;
            KitchenPipeEngine = kitchenPipeEngine;
        }

        public void Dispose()
        {
        }

        public void Run(Polyline boundary, Polyline outline, BlockReference basinline, Polyline pype,Polyline boundary1, Polyline outline1, Polyline urinal)
        {
            if (boundary != null && outline != null && basinline != null )
            {
                KitchenPipeEngine.Run(boundary, outline, basinline, pype);
            }
            if (boundary1 != null && outline1 != null && urinal != null)
            {
                ToiletPipeEngine.Run(boundary1, outline1, urinal);
            }
        }
    }
}
