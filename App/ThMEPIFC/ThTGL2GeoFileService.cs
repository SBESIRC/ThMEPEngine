using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ThMEPIFC.Ifc2x3;
using ThMEPTCH.Model;

namespace ThMEPIFC
{
    class ThTGL2GeoFileService
    {
		protected int gIndex = 0;
		protected int pIndex = 1;
		List<IfcMeshModel> allModelMeshs = new List<IfcMeshModel>();
		List<PointNormal> allModelPointNormals = new List<PointNormal>();
		public void GenerateIfcModelAndSave(ThTCHProject project, string file)
        {
			allModelMeshs.Clear();
			allModelPointNormals.Clear();
			gIndex = 0; 
			pIndex = 1;

			#region 单线程(多线程 失败，CAD一些API多线程下会报错)
			foreach (var thtchstorey in project.Site.Building.Storeys) 
            {
                var floorOrigin = thtchstorey.Origin;
                foreach (var thtchwall in thtchstorey.Walls)
                {
                    var wallSolid = thtchwall.CreateSolid3d(floorOrigin);
					GetSolidMeshModels(wallSolid, "wall");
					foreach (var thtchdoor in thtchwall.Doors) 
					{
						var doorSolid = thtchdoor.CreateSolid3d(floorOrigin);
						GetSolidMeshModels(doorSolid, "door");
					}
					foreach (var thtchwindow in thtchwall.Windows)
					{
						var windowSolid = thtchwindow.CreateSolid3d(floorOrigin);
						GetSolidMeshModels(windowSolid, "window");
					}
				}
                foreach (var thtchslab in thtchstorey.Slabs)
                {
					var slabSolid = thtchslab.CreateSlabSolid(floorOrigin);
					GetSolidMeshModels(slabSolid, "slab");
				}
                foreach (var thtchrailing in thtchstorey.Railings)
                {
					var railingSolid = thtchrailing.CreateSolid3d(floorOrigin);
					GetSolidMeshModels(railingSolid, "railing");
				}
            }
			WriteMidFile(allModelMeshs, allModelPointNormals, file);
			#endregion
		}
        private void GetSolidMeshModels(Solid3d solid,string typeStr) 
		{
			if (null == solid || solid.Area<10)
				return;
			var solidMesh = solid.ToMesh2d();
			var meshModel = new IfcMeshModel(gIndex, gIndex);
			var material = GetMeshModelMaterial(typeStr);
			foreach (var item in solidMesh.Element2ds)
			{
				var faceTriangle = new FaceTriangle();
				faceTriangle.TriangleMaterial = material;
				//这里有些面方向获取会失败，具体原因还没有跟踪，后续再处理
				Vector3d ptNormal = Vector3d.ZAxis;
				try
				{
					ptNormal = item.Normal;
				}
				catch { }
				foreach (var point in item.Nodes)
				{
					var pointNormal = CADPointToEnginePoint(pIndex,point.Point, ptNormal);
					allModelPointNormals.Add(pointNormal);
					faceTriangle.PtIndexs.Add(pIndex);
					pIndex += 1;
				}
				meshModel.FaceTriangles.Add(faceTriangle);
			}
			allModelMeshs.Add(meshModel);
			gIndex += 1;
		}
		private IfcMaterial GetMeshModelMaterial(string typeStr)
		{
			var defalutMaterial = new IfcMaterial
			{
				Kd_R = 169 / 255f,
				Kd_G = 179 / 255f,
				Kd_B = 218 / 255f,
				Ks_R = 0,
				Ks_B = 0,
				Ks_G = 0,
				K = 0.5f,
				NS = 12,
			};
			if (typeStr.Contains("wall"))
			{
				defalutMaterial = new IfcMaterial
				{
					Kd_R = 226 / 255f,
					Kd_G = 212 / 255f,
					Kd_B = 190 / 255f,
					Ks_R = 0,
					Ks_B = 0,
					Ks_G = 0,
					K = 1f,
					NS = 12,
				};
			}
			else if (typeStr.Contains("beam"))
			{
				defalutMaterial = new IfcMaterial
				{
					Kd_R = 194 / 255f,
					Kd_G = 178 / 255f,
					Kd_B = 152 / 255f,
					Ks_R = 0,
					Ks_B = 0,
					Ks_G = 0,
					K = 1f,
					NS = 12,
				};
			}
			else if (typeStr.Contains("door"))
			{
				defalutMaterial = new IfcMaterial
				{
					Kd_R = 167 / 255f,
					Kd_G = 182 / 255f,
					Kd_B = 199 / 255f,
					Ks_R = 0,
					Ks_B = 0,
					Ks_G = 0,
					K = 1f,
					NS = 12,
				};
			}
			else if (typeStr.Contains("slab"))
			{
				defalutMaterial = new IfcMaterial
				{
					Kd_R = 167 / 255f,
					Kd_G = 182 / 255f,
					Kd_B = 199 / 255f,
					Ks_R = 0,
					Ks_B = 0,
					Ks_G = 0,
					K = 1f,
					NS = 12,
				};
			}
			else if (typeStr.Contains("window"))
			{
				defalutMaterial = new IfcMaterial
				{
					Kd_R = 116 / 255f,
					Kd_G = 195 / 255f,
					Kd_B = 219 / 255f,
					Ks_R = 0,
					Ks_B = 0,
					Ks_G = 0,
					K = 0.5f,
					NS = 12,
				};
			}
			else if (typeStr.Contains("column"))
			{
				defalutMaterial = new IfcMaterial
				{
					Kd_R = 171 / 255f,
					Kd_G = 157 / 255f,
					Kd_B = 135 / 255f,
					Ks_R = 0,
					Ks_B = 0,
					Ks_G = 0,
					K = 1f,
					NS = 12,
				};
			}
			else if (typeStr.Contains("railing"))
			{
				defalutMaterial = new IfcMaterial { Kd_R = 136 / 255f, Kd_G = 211 / 255f, Kd_B = 198 / 255f, Ks_R = 0, Ks_B = 0, Ks_G = 0, K = 0.5f, NS = 12, };
			}
			return defalutMaterial;
		}
		private void WriteMidFile(List<IfcMeshModel> meshModels, List<PointNormal> meshPoints, string midFilePath)
		{
			if (null == meshModels || meshModels.Count < 1 || null == meshPoints || meshPoints.Count < 1)
				return;
			if (!string.IsNullOrEmpty(midFilePath) && File.Exists(midFilePath))
				File.Delete(midFilePath);
			var create = new FileStream(midFilePath, FileMode.Create);
			BinaryWriter writer = new BinaryWriter(create);
			ulong ptCount = (ulong)meshPoints.Count();
			//vertices
			writer.Write(ptCount * 3);
			for (int i = 0; i < meshPoints.Count; i++)
			{
				var point = meshPoints[i];
				writer.Write(point.Point.X);
				writer.Write(point.Point.Y);
				writer.Write(point.Point.Z);
			}
			//normals
			writer.Write(ptCount * 3);
			for (int i = 0; i < meshPoints.Count; i++)
			{
				var point = meshPoints[i];
				writer.Write(point.Normal.X);
				writer.Write(point.Normal.Y);
				writer.Write(point.Normal.Z);
			}
			//global_indices, All triangle faces info
			var sumCount = (ulong)meshModels.Sum(c => c.FaceTriangles.Sum(x => x.PtIndexs.Count()));
			writer.Write(sumCount);
			for (int i = 0; i < meshModels.Count; i++)
			{
				var meshModel = meshModels[i];
				foreach (var item in meshModel.FaceTriangles)
				{
					foreach (int ptIndex in item.PtIndexs)
						writer.Write(ptIndex);
				}
			}
			//components' indices, all components indices
			ulong cIdCount = (ulong)meshModels.Count;
			writer.Write(cIdCount);
			foreach (var item in meshModels)
			{
				ulong vCount = (ulong)item.FaceTriangles.Sum(c => c.PtIndexs.Count);
				writer.Write(vCount);
				foreach (var value in item.FaceTriangles)
				{
					foreach (int ptIndex in value.PtIndexs)
						writer.Write(ptIndex);
				}
			}
			//material datas
			ulong mCount = (ulong)meshModels.Sum(c => c.FaceTriangles.Count());
			writer.Write(mCount);
			foreach (var mesh in meshModels)
			{
				foreach (var item in mesh.FaceTriangles)
				{
					writer.Write(item.TriangleMaterial.Kd_R);
					writer.Write(item.TriangleMaterial.Kd_G);
					writer.Write(item.TriangleMaterial.Kd_B);
					writer.Write(item.TriangleMaterial.Ks_R);
					writer.Write(item.TriangleMaterial.Ks_G);
					writer.Write(item.TriangleMaterial.Ks_B);
					writer.Write(item.TriangleMaterial.K);
					writer.Write(item.TriangleMaterial.NS);
				}
			}
			writer.Close();
		}
		private PointNormal CADPointToEnginePoint(int pIndex, Point3d point,Vector3d normal) 
		{
			//渲染引擎中万向轴为Y轴，将数据旋转，刚好正面显示
			return new PointNormal
			{
				PointIndex = pIndex,
				Point = new PointVector() { X = -(float)point.X, Y = (float)point.Z, Z = (float)point.Y },
				Normal = new PointVector() { X = -(float)normal.X, Y = (float)normal.Z, Z = (float)normal.Y },
			};
		}
	}
	class PointNormal
	{
		public int PointIndex { get; set; }
		public PointVector Point { get; set; }
		public PointVector Normal { get; set; }

	}
	class FaceTriangle
	{
		public List<int> PtIndexs { get; }
		public IfcMaterial TriangleMaterial { get; set; }
		public FaceTriangle()
		{
			PtIndexs = new List<int>();
		}
	}
	class IfcMeshModel
	{
		public int CIndex { get; set; }
		public int IfcIndex { get; }
		public List<FaceTriangle> FaceTriangles { get; }
		public IfcMeshModel(int index, int ifcIndex)
		{
			CIndex = index;
			IfcIndex = ifcIndex;
			FaceTriangles = new List<FaceTriangle>();
		}
	}
	class PointVector
	{
		public float X { get; set; }
		public float Y { get; set; }
		public float Z { get; set; }
	}
	class IfcMaterial
	{
		public float Kd_R { get; set; }
		public float Kd_G { get; set; }
		public float Kd_B { get; set; }
		public float Ks_R { get; set; }
		public float Ks_G { get; set; }
		public float Ks_B { get; set; }
		public float K { get; set; }
		public int NS { get; set; }
	}
}
