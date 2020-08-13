﻿using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPEngineCore.Service
{
    public abstract class ThStructureDbExtension
    {
        public Database HostDb { get; set; }
        public List<string> LayerFilter { get; set; }
        protected ThStructureDbExtension(Database db)
        {
            HostDb = db;
            LayerFilter = new List<string>();
        }
        public abstract void BuildElementTexts();
        public abstract void BuildElementCurves();
        protected bool CheckCurveLayerValid(Curve curve)
        {
            return LayerFilter.Where(o => string.Compare(curve.Layer, o, true) == 0).Any();
        }
        protected bool IsBuildElementBlock(BlockTableRecord blockTableRecord)
        {
            // 暂时不支持动态块，外部参照，覆盖
            if (blockTableRecord.IsDynamicBlock)
            {
                return false;
            }

            // 忽略图纸空间和匿名块
            if (blockTableRecord.IsLayout || blockTableRecord.IsAnonymous)
            {
                return false;
            }

            // 忽略不可“炸开”的块
            if (!blockTableRecord.Explodable)
            {
                return false;
            }

            return true;
        }
    }
}
