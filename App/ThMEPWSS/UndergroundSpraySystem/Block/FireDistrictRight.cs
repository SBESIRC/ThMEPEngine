using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using Linq2Acad;
using System;
using System.Collections.Generic;
using ThMEPWSS.UndergroundFireHydrantSystem.Model;
using ThMEPWSS.Uitl.ExtensionsNs;
using AcHelper;
using GeometryExtensions;

namespace ThMEPWSS.UndergroundSpraySystem.Block
{
    public class FireDistrictRight
    {
        public Point3d StPt { get; set; }
        public string FloorNum { get; set; }
        public TermPoint2 TermPt { get; set; }
        private string PipeDN { get; set; }
        public bool HasFlow { get; set; }
        private Matrix3d U2WMat { get; set; }
        private string FlowType { get; set; }

        public FireDistrictRight(Point3d stPt, TermPoint2 termPoint, string DN, bool hasflow,string flowType)
        {
            StPt = stPt;
            FloorNum = termPoint.PipeNumber.Replace("接至", "").Split('喷')[0];
            TermPt = termPoint;
            PipeDN = DN;
            HasFlow = hasflow;
            U2WMat = Active.Editor.UCS2WCS();
            if(flowType.Contains("闸")||flowType.Contains("070"))
            {
                FlowType = "信号闸阀2";
            }
            else
            {
                FlowType = "信号蝶阀2";
            }
        }

        public void InsertBlock(AcadDatabase acadDatabase)
        {
            InsertLine(acadDatabase, StPt, StPt.OffsetX(300), "W-FRPT-SPRL-PIPE");
            if(!PipeDN.Equals(""))
            {
                InsertText(acadDatabase, StPt.OffsetXY(50, 150), PipeDN);
            }
            
            if (HasFlow)
            {
                var objID2 = acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-FRPT-HYDT-EQPM", "信号阀＋水流指示器",
                    StPt.OffsetXY(300-57592-350, 11322), new Scale3d(1, 1, 1), 0);
                objID2.SetDynBlockValue("可见性", FlowType);
                var blk2 = acadDatabase.Element<BlockReference>(objID2);
                blk2.TransformBy(U2WMat);
            }
            else
            {
                InsertLine(acadDatabase, StPt.OffsetX(300), StPt.OffsetX(890), "W-FRPT-SPRL-PIPE");
            }

            InsertLine(acadDatabase, StPt.OffsetX(890), StPt.OffsetX(1090), "W-FRPT-SPRL-PIPE");

            var objID = acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-FRPT-HYDT-EQPM", "减压孔板",
                    StPt.OffsetX(1190), new Scale3d(1, 1, 1), 0);
            objID.SetDynBlockValue("可见性","水平");
            var blk = acadDatabase.Element<BlockReference>(objID);
            blk.TransformBy(U2WMat);

            InsertLine(acadDatabase, StPt.OffsetX(1290), StPt.OffsetX(2140), "W-FRPT-SPRL-PIPE");
            InsertLine(acadDatabase, StPt.OffsetX(2140), StPt.OffsetX(2740), "W-FRPT-SPRL-EQPM", "DASH");

            objID = acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-FRPT-HYDT-EQPM", "喷头系统",
                    StPt.OffsetX(1790), new Scale3d(1, 1, 1), 0);
            objID.SetDynBlockValue("可见性", "上喷闭式");
            blk = acadDatabase.Element<BlockReference>(objID);
            blk.TransformBy(U2WMat);

            objID = acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-FRPT-HYDT-EQPM", "喷头系统",
                    StPt.OffsetX(3090), new Scale3d(1, 1, 1), 0);
            objID.SetDynBlockValue("可见性", "上喷闭式");
            blk = acadDatabase.Element<BlockReference>(objID);
            blk.TransformBy(U2WMat);

            objID = acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-FRPT-SPRL-EQPM", "水管中断",
                    StPt.OffsetX(2140), new Scale3d(-1.2, 1.2, 1.2), Math.PI);
            blk = acadDatabase.Element<BlockReference>(objID);
            blk.TransformBy(U2WMat);
            objID = acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-FRPT-SPRL-EQPM", "水管中断",
                    StPt.OffsetX(2740), new Scale3d(-1.2, 1.2, 1.2), Math.PI);
            blk = acadDatabase.Element<BlockReference>(objID);
            blk.TransformBy(U2WMat);

            InsertLine(acadDatabase, StPt.OffsetX(2740), StPt.OffsetX(3690), "W-FRPT-SPRL-PIPE");
            InsertLine(acadDatabase, StPt.OffsetX(3690), StPt.OffsetXY(3690, -1350), "W-FRPT-SPRL-PIPE");
            InsertLine(acadDatabase, StPt.OffsetXY(3690, -1200), StPt.OffsetXY(3940, -1200), "W-FRPT-SPRL-PIPE");
            objID = acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-FRPT-SPRL-EQPM", "压力表",
                    StPt.OffsetXY(3940, -1200), new Scale3d(1.5, 1.5, 1.5), 0);
            blk = acadDatabase.Element<BlockReference>(objID);
            blk.TransformBy(U2WMat);

            objID = acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-FRPT-SPRL-EQPM", "截止阀",
                    StPt.OffsetXY(3690, -1500), new Scale3d(1, 1, 1), Math.PI/2);
            blk = acadDatabase.Element<BlockReference>(objID);
            blk.TransformBy(U2WMat);

            InsertLine(acadDatabase, StPt.OffsetXY(3690, -1650), StPt.OffsetXY(3690, -1800), "W-FRPT-SPRL-PIPE");
            InsertLine(acadDatabase, StPt.OffsetXY(3540, -1800), StPt.OffsetXY(3840, -1800), "W-FRPT-SPRL-EQPM");
            InsertLine(acadDatabase, StPt.OffsetXY(3540, -1800), StPt.OffsetXY(3690, -2050), "W-FRPT-SPRL-EQPM");
            InsertLine(acadDatabase, StPt.OffsetXY(3840, -1800), StPt.OffsetXY(3690, -2050), "W-FRPT-SPRL-EQPM");
            InsertLine(acadDatabase, StPt.OffsetXY(3490, -2200), StPt.OffsetXY(3890, -2200), "W-DRAI-EQPM");

            var arc = new Arc(StPt.OffsetXY(3690, -2200), new Vector3d(0, 0, 1), 200, Math.PI, Math.PI * 2);
            arc.LayerId = DbHelper.GetLayerId("W-DRAI-EQPM");
            arc.TransformBy(U2WMat);
            acadDatabase.CurrentSpace.Add(arc);

            InsertLine(acadDatabase, StPt.OffsetXY(3690, -2400), StPt.OffsetXY(3690, -4500), "W-FRPT-DRAI-PIPE");
            InsertLine(acadDatabase, StPt.OffsetXY(690, -4500), StPt.OffsetXY(3690, -4500), "W-FRPT-SPRL-DIMS");
            InsertLine(acadDatabase, StPt.OffsetXY(690, -410), StPt.OffsetXY(690, -4500), "W-FRPT-DRAI-PIPE");
            InsertLine(acadDatabase, StPt.OffsetXY(690, -410), StPt.OffsetXY(1140, -410), "W-FRPT-DRAI-PIPE");

            objID = acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-FRPT-SPRL-EQPM", "截止阀",
                    StPt.OffsetXY(1290, -410), new Scale3d(1, 1, 1), Math.PI);
            blk = acadDatabase.Element<BlockReference>(objID);
            blk.TransformBy(U2WMat);

            InsertLine(acadDatabase, StPt.OffsetXY(1440, -410), StPt.OffsetXY(1640, -410), "W-FRPT-DRAI-PIPE");

            InsertLine(acadDatabase, StPt.OffsetX(1640), StPt.OffsetXY(1640, -410), "W-FRPT-DRAI-PIPE");

            objID = acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-FRPT-SPRL-DIMS", "标高", StPt.OffsetXY(3690, -1500),
                new Scale3d(1, 1, 1), 0, new Dictionary<string, string> { { "标高", "h+1.50" } });
            SetDynBlockValue(objID, "翻转状态2", 1);
            blk = acadDatabase.Element<BlockReference>(objID);
            blk.TransformBy(U2WMat);

            InsertLine(acadDatabase, StPt.OffsetXY(1346,-114), StPt.OffsetXY(1753, -690), "W-FRPT-SPRL-DIMS");
            InsertLine(acadDatabase, StPt.OffsetXY(1753, -690), StPt.OffsetXY(3640, -690), "W-FRPT-SPRL-DIMS");


            InsertLine(acadDatabase, StPt.OffsetXY(2977, -1965), StPt.OffsetXY(3340, -1670), "W-FRPT-NOTE");
            InsertLine(acadDatabase, StPt.OffsetXY(3100, -2630), StPt.OffsetXY(3496, -2088), "W-FRPT-NOTE");

            InsertText(acadDatabase, StPt.OffsetXY(1780, -640), "减压孔板XXmm");
            InsertText(acadDatabase, StPt.OffsetXY(1350, -3910), FloorNum);
            InsertText(acadDatabase, StPt.OffsetXY(1000, -4400), "排至地下一层集水坑");

            InsertText(acadDatabase, StPt.OffsetXY(2350, -2380), "截止阀", "W-FRPT-NOTE");
            InsertText(acadDatabase, StPt.OffsetXY(2340, -3000), "K=80", "W-FRPT-NOTE");
            InsertText(acadDatabase, StPt.OffsetXY(2310, -3450), "试水接头", "W-FRPT-NOTE");
            InsertText(acadDatabase, StPt.OffsetXY(1040, -3950), "DN50", "W-FRPT-SPRL-DIMS", Math.PI / 2);
            InsertText(acadDatabase, StPt.OffsetXY(4060, -3950), "DN80", "W-FRPT-SPRL-DIMS", Math.PI / 2);
            InsertSolid(acadDatabase, StPt.OffsetXY(3497, -1546), StPt.OffsetXY(3320, -1646), StPt.OffsetXY(3362, -1698));
            InsertSolid(acadDatabase, StPt.OffsetXY(3615, -1927), StPt.OffsetXY(3523, -2107), StPt.OffsetXY(3469, -2068));
        }

        private static void SetDynBlockValue(ObjectId blockId, string propName, object value)
        {
            var props = blockId.GetDynProperties();//获得动态块的所有动态属性
            //遍历动态属性
            foreach (DynamicBlockReferenceProperty prop in props)
            {
                //如果动态属性的名称与输入的名称相同且为可读
                if (prop.ReadOnly == false && prop.PropertyName == propName)
                {
                    //判断动态属性的类型并通过类型转化设置正确的动态属性值
                    switch (prop.PropertyTypeCode)
                    {
                        case 3:
                        case (short)DynBlockPropTypeCode.Short://短整型
                            prop.Value = Convert.ToInt16(value);
                            break;
                        case (short)DynBlockPropTypeCode.Long://长整型
                            prop.Value = Convert.ToInt64(value);
                            break;
                        case (short)DynBlockPropTypeCode.Real://实型
                            prop.Value = Convert.ToDouble(value);
                            break;
                        default://其它
                            prop.Value = value;
                            break;
                    }
                    break;
                }
            }
        }

        private void InsertLine(AcadDatabase acadDatabase, Point3d pt1, Point3d pt2, string layer)
        {
            var line = new Line(pt1, pt2)
            {
                LayerId = DbHelper.GetLayerId(layer),
                ColorIndex = (int)ColorIndex.BYLAYER
            };
            line.TransformBy(U2WMat);
            acadDatabase.CurrentSpace.Add(line);
        }

        private void InsertLine(AcadDatabase acadDatabase, Point3d pt1, Point3d pt2, string layer, string linetype)
        {
            var line = new Line(pt1, pt2)
            {
                LayerId = DbHelper.GetLayerId(layer),
                ColorIndex = (int)ColorIndex.BYLAYER,
                Linetype = linetype
            };
            line.TransformBy(U2WMat);
            acadDatabase.CurrentSpace.Add(line);
        }

        private void InsertSolid(AcadDatabase acadDatabase, Point3d pt1, Point3d pt2, Point3d pt3, string layer = "W-FRPT-NOTE")
        {
            var solid = new Solid(pt1, pt2, pt3)
            {
                LayerId = DbHelper.GetLayerId(layer),
                ColorIndex = (int)ColorIndex.BYLAYER
            };
            solid.TransformBy(U2WMat);
            acadDatabase.CurrentSpace.Add(solid);
        }
       
        private void InsertText(AcadDatabase acadDatabase, Point3d insertPt, string text, string layer = "W-FRPT-SPRL-DIMS", double rotation = 0)
        {
            var dbText = new DBText
            {
                TextString = text,
                Position = insertPt,
                LayerId = DbHelper.GetLayerId(layer),
                Rotation = rotation,
                TextStyleId = DbHelper.GetTextStyleId("TH-STYLE3"),
                Height = 350,
                WidthFactor = 0.7,
                ColorIndex = (int)ColorIndex.BYLAYER
            };
            dbText.TransformBy(U2WMat);
            acadDatabase.CurrentSpace.Add(dbText);
        }
    }
}
