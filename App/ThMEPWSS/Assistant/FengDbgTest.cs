//this file is for debugging only by Feng

#if DEBUG
namespace ThMEPWSS.DebugNs
{
  using System;
  using System.Linq;
  using System.Text;
  using System.Reflection;
  using System.Collections.Generic;
  using System.Windows.Forms;
  using ThMEPWSS.JsonExtensionsNs;
  using Dbg = ThMEPWSS.DebugNs.ThDebugTool;
  using Autodesk.AutoCAD.EditorInput;
  using AcHelper;
  using Autodesk.AutoCAD.Geometry;
  using Linq2Acad;
  using ThMEPWSS.Pipe.Model;
  using ThMEPWSS.Pipe.Engine;
  using Autodesk.AutoCAD.DatabaseServices;
  using System.Diagnostics;
  using Autodesk.AutoCAD.ApplicationServices;

  public class FengDbgTest
  {
    public static Dictionary<string, object> processContext;
    public static Dictionary<string, object> ctx => (Dictionary<string, object>)processContext["context"];
    //using (var adb = AcadDatabase.Active())
    //using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
    //{
    //}
    public static void Test(Dictionary<string, object> ctx)
    {
      processContext = (Dictionary<string, object>)ctx["processContext"];
      ctx.TryGetValue("entryMethod", out object o);
      if (o is Action entryMethod)
      {
        Action initMethod = null;
        initMethod = new Action(() =>
        {
          ((Action<Assembly, string>)ctx["pushAcadActions"])((Assembly)ctx["currentAsm"], typeof(ThDebugClass).FullName);
          ((Action<object>)ctx["clearBtns"])(ctx["currentPanel"]);
          ((Action<object, string, Action>)ctx["addBtn"])(ctx["currentPanel"], "initMethod", initMethod);
          ((Action<object, string, Action>)ctx["addBtn"])(ctx["currentPanel"], "reloadMe", () =>
          {
            var asm = ((Func<string, Assembly>)ctx["loadAsm"])((string)ctx["asmDllFullPath"]);
            asm.GetType(typeof(FengDbgTest).FullName).GetField(nameof(processContext)).SetValue(null, processContext);
            initMethod();
          });
          var fs = (List<Action>)ctx["actions"];
          var names = (List<string>)ctx["names"];
          for (int i = 0; i < fs.Count; i++)
          {
            var f = fs[i];
            var name = names[i];
            ((Action<object, string, Action>)ctx["addBtn"])(ctx["currentPanel"], name, f);
          }
        });
        ctx["initMethod"] = initMethod;
        entryMethod();
      }
      else
      {
        MessageBox.Show("entryMethod not set!");
      }
    }
  }
  public class ThDebugTool
  {
    public static Editor Editor => Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor;
    public static Document MdiActiveDocument => Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
    static Dictionary<string, object> ctx => ThDebugClass.ctx;
    public static void ShowString(string str)
    {
      ((Action<string>)ctx["showString"])(str);
    }


    public static void FocusMainWindow()
    {
      Autodesk.AutoCAD.ApplicationServices.Application.MainWindow.Focus();
    }
    public static void Print(string str, params object[] objs)
    {
      var dt = DateTime.Now.ToString("HH:mm:ss.fff");
      if (objs.Length == 0) Editor.WriteMessage($"\n[{dt}] " + str + "\n");
      else Editor.WriteMessage($"\n[{dt}] " + str + "\n", objs);
    }
  }
  
  public class ThDebugClass
  {
    public static Dictionary<string, object> ctx => FengDbgTest.ctx;
    public static Dictionary<string, object> processContext => (Dictionary<string, object>)ctx["processContext"];

    public static void Test1()
    {
      MessageBox.Show("test1");
    }
    public static void Test2()
    {
      MessageBox.Show("test2");
    }
    //todo:wrap into data class
    public static void demo1()
    {
      FengDbgTest.processContext["xx"] = 123;
    }
    public static void demo2()
    {
      FengDbgTest.processContext["xx"] = ((int)FengDbgTest.processContext["xx"]) + 1;
    }
    public static void demo3()
    {
      MessageBox.Show(FengDbgTest.processContext["xx"].ToJson());
    }
    public static void ShowString()
    {
      Dbg.ShowString("hello Feng");
    }
    public static void CollectSelectionTest()
    {
      var json = ThMEPWSS.Command.ThRainSystemDiagramCmd.CollectSelectionTest();
      Dbg.ShowString(json);
    }
    public static void RunThRainSystemDiagramCmd()
    {
      using (var cmd = new Command.ThRainSystemDiagramCmd())
      {
        cmd.Execute();
      }
    }
    public static void PrintTest()
    {
      Dbg.Print("hello?");
    }
    public static void RunThRainSystemDiagramCmd_NoRectSelection()
    {


      using (var adb = AcadDatabase.Active())
      using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
      {

        var pt1 = "{x:-51259.1494784583,y:914935.416475339,z:0}".JsonToPoint3d();
        var pt2 = "{x:214660.913246393,y:791487.904818842,z:0}".JsonToPoint3d();
        var points = new Point3dCollection();
        points.Add(pt1);
        points.Add(new Point3d(pt1.X, pt2.Y, 0));
        points.Add(pt2);
        points.Add(new Point3d(pt2.X, pt1.Y, 0));
        Dbg.FocusMainWindow();
        var basePtOptions = new PromptPointOptions("\n选择图纸基点");
        var rst = Active.Editor.GetPoint(basePtOptions);
        if (rst.Status != PromptStatus.OK) return;
        var bastPt = rst.Value;

        var diagram = new ThWRainSystemDiagram();
        var storeysRecEngine = new ThWStoreysRecognitionEngine();
        storeysRecEngine.Recognize(adb.Database, points);
        var sw = new Stopwatch();
        //sw.Start();
        diagram.InitCacheData(adb);
        //Dbg.Print(sw.Elapsed.TotalSeconds.ToString());
        diagram.InitStoreys(storeysRecEngine.Elements);
        //Dbg.Print(sw.Elapsed.TotalSeconds.ToString());
        diagram.InitVerticalPipeSystems(adb.Database, points);//9s,slow
        //Dbg.Print(sw.Elapsed.TotalSeconds.ToString());
        diagram.Draw(bastPt);
        //sw.Stop();
      }

    }

  }
}
#endif