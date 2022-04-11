using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.DrainageADPrivate.Model
{
    internal class ThDrainageTreeNode
    {
        public Point3d Node { get; set; }
        public ThDrainageTreeNode Parent { get; set; }
        public List<ThDrainageTreeNode> Child { get; set; }
        public ThSaniterayTerminal Terminal { get; set; } //如果是末端node，末端
        public ThDrainageTreeNode TerminalPair { get; set; } //如果是末端node，对应热/冷水口
        public bool IsCool { get; set; }

        public int Dim { get; set; }

        public ThDrainageTreeNode(Point3d pt)
        {
            Node = pt;
            Child = new List<ThDrainageTreeNode>();
        }

        public List<ThDrainageTreeNode> GetSibling()
        {
            var sibling = new List<ThDrainageTreeNode>();
            if (Parent != null)
            {
                sibling = Parent.Child.Where(x => x != this).ToList();
            }

            return sibling;
        }

        public List<ThDrainageTreeNode> GetDescendant()
        {
            var descendant = new List<ThDrainageTreeNode>();

            var nextChild = Child;

            if (nextChild.Count != 0)
            {
                descendant.AddRange(nextChild);

                foreach (var c in nextChild)
                {
                    descendant.AddRange(c.GetDescendant());
                }
            }

            return descendant;
        }

        public int GetLeafCount()
        {
            int i = 0;

            foreach (var c in Child)
            {
                i = i + c.GetLeafCount();
            }

            if (Child.Count() == 0)
            {
                i = i + 1;
            }

            return i;
        }

        public List<ThDrainageTreeNode> GetLeaf()
        {
            var leaf = new List<ThDrainageTreeNode>();

            foreach (var c in Child)
            {
                leaf.AddRange(c.GetLeaf());
            }

            if (Child.Count() == 0)
            {
                leaf.Add(this);
            }

            return leaf;
        }

        public bool IsDescendant(ThDrainageTreeNode node)
        {
            bool bReturn = false;

            foreach (var c in node.Child)
            {

                bReturn = IsDescendant(c);

                if (bReturn == true)
                {
                    break;
                }

            }

            if (node == this)
            {
                bReturn = true;
            }

            return bReturn;
        }

        public int GetDepth()
        {
            int i = 0;
            var bParent = Parent;

            while (bParent != null)
            {
                i = i + 1;
                bParent = bParent.Parent;
            }

            return i;
        }

        public ThDrainageTreeNode GetRoot()
        {
            var p = Parent;
            while (p != null)
            {
                p = p.Parent;
            }
            return p;
        }
    }
}
