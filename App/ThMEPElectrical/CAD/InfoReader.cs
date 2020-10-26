using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.Assistant;
using ThMEPElectrical.Business;
using ThMEPElectrical.Model;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Extension;

namespace ThMEPElectrical.CAD
{
    /// <summary>
    /// Io 数据读取提取器
    /// </summary>
    public class InfoReader
    {
        private Point3dCollection m_preWindow; // 预处理窗口
        public List<Polyline> RecognizeMainBeamColumnWalls
        {
            get;
            set;
        } = new List<Polyline>(); // 主梁,柱子，剪力墙

        public List<SecondBeamProfileInfo> RecognizeSecondBeams
        {
            get;
            set;
        } = new List<SecondBeamProfileInfo>(); // 次梁

        /// <summary>
        /// 无梁读取柱子
        /// </summary>
        public List<Polyline> Columns
        {
            get;
            set;
        } = new List<Polyline>(); // 柱子

        public InfoReader(Point3dCollection preWindow)
        {
            m_preWindow = preWindow;
        }

        public void Do()
        {
            ComponentPicker(m_preWindow);
        }

        public void PickColumns()
        {
            ColumnPicker(m_preWindow);
        }

        /// <summary>
        /// 提取柱子信息
        /// </summary>
        /// <param name="ptCollection"></param>
        private void ColumnPicker(Point3dCollection ptCollection)
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                // 启动柱识别引擎
                var columnEngine = new ThColumnRecognitionEngine();
                columnEngine.Recognize(acadDatabase.Database, ptCollection);

                // 柱子
                columnEngine.Elements.ForEach(columnElement =>
                {
                    if (columnElement.Outline is Polyline columnPoly)
                        Columns.Add(columnPoly);
                });
            }
        }

        /// <summary>
        /// Io modol 前置数据提取
        /// </summary>
        /// <param name="ptCollection"></param>
        private void ComponentPicker(Point3dCollection ptCollection)
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                // 接口，提取主梁，次梁，柱子，剪力墙等数据
                var beamConnectEngine = ThBeamConnectRecogitionEngine.ExecuteRecognize(acadDatabase.Database, ptCollection);

                // 主梁 = 主梁 + 悬挑主梁
                beamConnectEngine.PrimaryBeamLinks.ForEach(mainBeamInfo =>
                {
                    mainBeamInfo.Beams.ForEach(beamInfo =>
                    {
                        beamInfo.ExtendBoth(ThMEPCommon.ExtendBeamLength, ThMEPCommon.ExtendBeamLength);
                        if (beamInfo.Outline is Polyline mainBeamPoly)
                            RecognizeMainBeamColumnWalls.Add(mainBeamPoly);
                    });
                });

                // 悬挑主梁
                beamConnectEngine.OverhangingPrimaryBeamLinks.ForEach(overHangingBeamInfo =>
                {
                    overHangingBeamInfo.Beams.ForEach(beamInfo =>
                    {
                        beamInfo.ExtendBoth(ThMEPCommon.ExtendBeamLength, ThMEPCommon.ExtendBeamLength);
                        if (beamInfo.Outline is Polyline overBeamPoly)
                            RecognizeMainBeamColumnWalls.Add(overBeamPoly);
                    });
                });
                // 柱子
                beamConnectEngine.ColumnEngine.Elements.ForEach(columnElement =>
                {
                    if (columnElement.Outline is Polyline columnPoly)
                    {
                        RecognizeMainBeamColumnWalls.Add(columnPoly);
                        Columns.Add(columnPoly);
                    }
                });
                // 剪力墙
                beamConnectEngine.ShearWallEngine.Elements.ForEach(shearWallElement =>
                {
                    if (shearWallElement.Outline is Polyline shearWallPoly)
                        RecognizeMainBeamColumnWalls.Add(shearWallPoly);
                });

                // 次梁 = 次梁 + 半主梁
                // 次梁
                beamConnectEngine.SecondaryBeamLinks.ForEach(secondBeamInfo =>
                {
                    secondBeamInfo.Beams.ForEach(singleBeamInfo =>
                    {
                        singleBeamInfo.ExtendBoth(ThMEPCommon.ExtendBeamLength, ThMEPCommon.ExtendBeamLength);
                        if (singleBeamInfo.Outline is Polyline secondBeamPoly)
                            RecognizeSecondBeams.Add(new SecondBeamProfileInfo(secondBeamPoly, singleBeamInfo.Height - ThMEPCommon.StoreyHeight));
                    });
                });

                // 半主梁
                beamConnectEngine.HalfPrimaryBeamLinks.ForEach(halfBeamInfo =>
                {
                    halfBeamInfo.Beams.ForEach(singleHalfBeam =>
                    {
                        singleHalfBeam.ExtendBoth(ThMEPCommon.ExtendBeamLength, ThMEPCommon.ExtendBeamLength);
                        if (singleHalfBeam.Outline is Polyline halfBeamPoly)
                            RecognizeSecondBeams.Add(new SecondBeamProfileInfo(halfBeamPoly, singleHalfBeam.Height - ThMEPCommon.StoreyHeight));
                    });
                });
            }
        }
    }
}
