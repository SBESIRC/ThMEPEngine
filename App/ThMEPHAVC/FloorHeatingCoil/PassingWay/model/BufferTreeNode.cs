using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPHVAC.FloorHeatingCoil
{
    public class BufferTreeNode : IDisposable
    {
        public Polyline shell { get; set; } = null;
        public List<BufferTreeNode> childs { get; set; } = null;
        public BufferTreeNode parent { get; set; } = null;
        public int depth { get; set; } = -1;
        public BufferTreeNode() { }
        public BufferTreeNode(Polyline poly) { shell = poly; }

        public BufferTreeNode(Polyline poly, int depth)
        {
            shell = poly;
            this.depth = depth;
        }
        public void SetShell(Polyline poly)
        {
            shell.Dispose();
            shell = poly;
        }
        public void Dispose()
        {
            shell.Dispose();
            if (childs == null) return;
            foreach (var child in childs)
                child.Dispose();
            childs.Clear();
        }
    }
}
