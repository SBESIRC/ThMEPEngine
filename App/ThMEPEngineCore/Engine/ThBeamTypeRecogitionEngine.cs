using System;
using System.Collections.Generic;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Engine
{
    public class ThBeamTypeRecogitionEngine:IDisposable
    {
        public List<ThBeamLink> PrimaryBeamLinks { get; set; } = new List<ThBeamLink>();
        public List<ThBeamLink> HalfPrimaryBeamLinks { get; set; } = new List<ThBeamLink>();
        public List<ThBeamLink> OverhangingPrimaryBeamLinks { get; set; } = new List<ThBeamLink>();
        public List<ThBeamLink> SecondaryBeamLinks { get; set; } = new List<ThBeamLink>();
        public ThBeamTypeRecogitionEngine()
        {
        }
        public void Dispose()
        {
            //TODO
        }
        public void Recognize(Database database)
        {
            //启动柱识别引擎
            ThColumnRecognitionEngine thColumnRecognitionEngine = new ThColumnRecognitionEngine();
            thColumnRecognitionEngine.Recognize(database);

            //启动梁识别引擎
            ThBeamRecognitionEngine thBeamRecognitionEngine = new ThBeamRecognitionEngine();
            thBeamRecognitionEngine.Recognize(database);

            //启动墙识别引擎
            //ThShearWallRecognitionEngine thWallRecognitionEngine = new ThShearWallRecognitionEngine();
            //thWallRecognitionEngine.Recognize();

            //创建空间索引
            ThSpatialIndexManager.Instance.CreateColumnSpaticalIndex(thColumnRecognitionEngine.Collect());

            //Pass One 通过单根梁过滤
            foreach(ThIfcElement beamElement in thBeamRecognitionEngine.Elements)
            {
                ThBeamLinkExtension thBeamLinkExtension = new ThBeamLinkExtension(beamElement as ThIfcBeam);
                thBeamLinkExtension.CreateSinglePrimaryBeamLink();
                if(thBeamLinkExtension.BeamLink.Beams.Count>0)
                {
                    PrimaryBeamLinks.Add(thBeamLinkExtension.BeamLink);
                }
            }
        }
    }
}
