using System;
using DotNetARX;
using Linq2Acad;
using ThMEPEngineCore.CAD;
using ThMEPLighting.Common;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPLighting.Garage.Service
{
    /// <summary>
    /// for print
    /// </summary>
    public static class ThPrintService
    {
        public static void Print(this ThLightGraphService lightGraph)
        {
            int index = 1;
            using (var acadDatabase = AcadDatabase.Active())
            {
                short colorIndex = 1;
                lightGraph.Links.ForEach(o =>
                {
                    var objIds = new ObjectIdList();
                    var circle = new Circle(o.Start, Vector3d.ZAxis, 100.0);
                    circle.ColorIndex = colorIndex;
                    objIds.Add(acadDatabase.ModelSpace.Add(circle));

                    o.Path.ForEach(p =>
                    {
                        var edge = new Line(p.Edge.StartPoint, p.Edge.EndPoint);
                        edge.ColorIndex = colorIndex;
                        objIds.Add(acadDatabase.ModelSpace.Add(edge));

                        var edgeIndex = new DBText();
                        edgeIndex.TextString = (index++).ToString();
                        edgeIndex.Position = ThGeometryTool.GetMidPt(p.Edge.StartPoint, p.Edge.EndPoint);
                        edgeIndex.Rotation = p.Edge.Angle % Math.PI;
                        edgeIndex.Height = 200;
                        edgeIndex.ColorIndex = colorIndex;
                        objIds.Add(acadDatabase.ModelSpace.Add(edgeIndex));
                    });
                    if (objIds.Count > 0)
                    {
                        var groupName = Guid.NewGuid().ToString();
                        GroupTools.CreateGroup(acadDatabase.Database, groupName, objIds);
                        colorIndex++;
                    }
                });
            }
        }
    }
}
