using System;
using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Engine
{
    public abstract class ThDistributionElementRecognitionEngine : IDisposable
    {
        public List<ThIfcDistributionFlowElement> Elements { get; set; }
        protected ThDistributionElementRecognitionEngine()
        {
            Elements = new List<ThIfcDistributionFlowElement>();
        }

        public void Dispose()
        {
        }

        public abstract void Recognize(Database database, Point3dCollection polygon);
        public abstract void RecognizeMS(Database database, Point3dCollection polygon);
        /// <summary>
        /// 把ExtractionEngine提取的数据转成Ifc model
        /// </summary>
        /// <param name="datas"></param>
        /// <param name="polygon"></param>
        public virtual void Recognize(List<ThRawIfcDistributionElementData> datas, Point3dCollection polygon)
        {
            //
        }
    }
}
