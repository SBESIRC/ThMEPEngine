﻿using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPElectrical.SecurityPlaneSystem.ConnectPipe.Model
{
    public class BlockModel
    {
        public BlockModel(BlockReference block)
        {
            blockModel = block;
            position = new Point3d(block.Position.X, block.Position.Y, 0);
        }

        /// <summary>
        /// 原始块
        /// </summary>
        public BlockReference blockModel { get; set; }

        /// <summary>
        /// 块基点
        /// </summary>
        public Point3d position { get; set; }
    }
}
