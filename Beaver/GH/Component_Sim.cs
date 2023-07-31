using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Seastar.Core;
using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.Display;
using Rhino.Geometry;
using Grasshopper.Kernel.Attributes;
using Grasshopper.GUI.Canvas;
using System.Threading;

namespace SeastarGrasshopper
{
    public class PathPreview : GH_Component
    {
        public PathPreview()
          : base("Path Preview", "PathPreview",
              "Preview path geometry, feedrate, action",
              "Seastar","06 | Sim")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Path", "P", "Path to preview", GH_ParamAccess.list);
            pManager[0].DataMapping = GH_DataMapping.Flatten;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("out", "out", "out", GH_ParamAccess.list);
            pManager.AddIntervalParameter("Feed Rate Range", "fr", "Feed Rate Range", GH_ParamAccess.item);
            pManager.AddGenericParameter("SPM", "R", "test spm out", GH_ParamAccess.list);
        }

        //add menu item
        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalComponentMenuItems(menu);
            
            Menu_AppendItem(menu, "Display Path", Menu_displayPath, true, displayPath);
            Menu_AppendItem(menu, "Display Feed Rate", Menu_displayFeed, true, displayFeed);
            Menu_AppendItem(menu, "Display Print Width", Menu_displayWidth, true, displayWidth);
            Menu_AppendSeparator(menu);
            Menu_AppendItem(menu, "Display text/numbers", Menu_displayText, true, displayText);
        }

        
        public bool displayPath = true;
        public bool displayFeed = false;
        public bool displayWidth = false;
        public bool displayText = true;

        
        private void Menu_displayText(object Sender, EventArgs e)
        {
            displayText = !displayText;
        }
        private void Menu_displayPath(object Sender, EventArgs e)
        {
            displayPath = true;
            displayFeed = false;
            displayWidth = false;
        }
        private void Menu_displayFeed(object Sender, EventArgs e)
        {
            displayPath = false;
            displayFeed = true;
            displayWidth = false;
        }
        private void Menu_displayWidth(object Sender, EventArgs e)
        {
            displayPath = false;
            displayFeed = false;
            displayWidth = true;
        }
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Path> pp = new List<Path>();
            List<SPM> ss = new List<SPM>();
            DA.GetDataList<Path>(0, pp);

            if (pp.Count == 0) return;
            paths = pp;
            

            Path pthJ = new Path(Path.Join(pp, 99999, false)[0]);
            Config cfg = pthJ.config;
            parkPosition = cfg.Machine.parkPos;
            foreach(Block blk in pthJ.blocks)
            {
                if (blk.coordinate.HasValue)
                {
                    switch (cfg.Machine.rAxes.type)
                    {
                        case RAxesType.None:

                            break;
                        case RAxesType.SPM:
                            Plane pln;
                            if (blk.orientation.HasValue)
                            {
                                pln = new Plane(blk.orientation.Value);
                            }
                            else
                            {
                                pln = Plane.WorldXY;
                                pln.Flip();
                            }
                            SPM s = new SPM( cfg.Machine.rAxes.spm);
                            s.UpdateConfig(pln, true);
                            ss.Add(s);
                            break;
                    }
                }

            }

            spms = ss;
            fRange = Path.FeedRange(pp);
            // eRange = Path.ESRange(pp);
            DA.SetData(1, fRange);
        }

        //public Path PathOut;
        public List<Path> paths;
        public List<SPM> spms = new List<SPM>();
        public Curve pCrv;
        public Point3d parkPosition = new Point3d(0, 0, 0);
        public Interval fRange;
        public Interval eRange;
        public string psc = "F2";

        public override void DrawViewportWires(IGH_PreviewArgs args)

        {
            GH_Document document = this.OnPingDocument();
            Color defSelectedCol = document.PreviewColourSelected;
            Color defCol =  document.PreviewColour;
            int textSize = 20;
            int lnThick = 3;

            if (displayPath && paths != null)
            {
                Point3d lastPt = parkPosition;
                foreach (Path path in paths)
                {
                    for (int i = 0; i < path.blocks.Count; i++)
                    {
                        if (path.blocks[i].coordinate.HasValue)
                        {
                            Point3d nowPt = new Point3d(path.blocks[i].coordinate.Value);
                            double nowF = (double)path.blocks[i].F;
                            args.Display.DrawLine(lastPt, path.blocks[i].coordinate.Value, defCol, lnThick);
                            if (displayText)
                            {
                                args.Display.Draw2dText(nowF.ToString("F0"), defCol, (lastPt + nowPt) * 0.5, false, textSize);
                            }
                            lastPt = path.blocks[i].coordinate.Value;
                        }
                        else
                        {
                            string text = path.blocks[i].ToGCode(2);
                            args.Display.DrawDot(lastPt, text, Color.Black, path.DefaultColor);
                        }
                    }
                }
            }
            if (displayFeed)
            {
                
                Point3d lastPt = parkPosition;
                double hueLow = 0.6; //colour in hue degree for lowest range
                double hueHigh = 0;
                foreach (Path path in paths)
                {
                    for (int i = 0; i < path.blocks.Count; i++)
                    {
                        if (path.blocks[i].coordinate.HasValue)
                        {
                        Point3d nowPt = new Point3d(path.blocks[i].coordinate.Value);
                            double nowF = (double)path.blocks[i].F;
                            double normalF = (nowF - fRange.Min) / (fRange.Max - fRange.Min);
                            double hue = (normalF * (hueHigh - hueLow)) + hueLow;
                            Color c = new Rhino.Display.ColorHSL(hue, 1, 0.5).ToArgbColor();
                            args.Display.DrawLine(lastPt, path.blocks[i].coordinate.Value, c, lnThick);
                            if (displayText)
                            {
                                args.Display.Draw2dText(nowF.ToString("F0"), defCol, (lastPt + nowPt) * 0.5, false, textSize);
                            }
                            lastPt = path.blocks[i].coordinate.Value;
                        }
                        else
                        {
                            string text = path.blocks[i].ToGCode(2);
                            args.Display.DrawDot(lastPt, text, Color.Black, path.DefaultColor);
                        }
                    }
                }
                if (spms.Count > 0)
                {
                    foreach (SPM spm in spms)
                    {
                        DataTree<Arc> arcTree = spm.LinkArc();
                        arcTree.Flatten(null);
                        foreach (Arc ac in arcTree.Branch(0))
                        {
                            args.Display.DrawArc(ac, defCol, 3);
                        }
                    }
                }

            }
            if (displayWidth)
            {

            }

        }
        protected override System.Drawing.Bitmap Icon => Seastar.Properties.Resources.simPreview;

        public override Guid ComponentGuid
        {
            get { return new Guid("1b9d0730-00b0-45da-b260-f085122a5d9e"); }
        }
    }

    public class PathSim : GH_Component
    {
        public PathSim()
          : base("Simulate Paths", "PathSim",
              "Simulate path on a time line",
              "Seastar", "06 | Sim")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Seastar Path", "P", "Seastar path to simulate", GH_ParamAccess.list);
            pManager[0].DataMapping = GH_DataMapping.Flatten;
            pManager.AddNumberParameter("Time", "t", "Time in second.\nIf checked normalised time, time from 0 to 1", GH_ParamAccess.item);
            pManager.AddNumberParameter("Playback Speed", "S", "Playback speed multiplier", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Play", "P", "Set to true to start playing simulation", GH_ParamAccess.item);
            pManager.AddGenericParameter("Config", "C", "Configuration\nThis will override configuration in path if there are any", GH_ParamAccess.item);
            pManager[4].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("test", "test", "test", GH_ParamAccess.item);
        }

        public bool useTrail = true;
        public bool normaliseTime = false;
        public int trailCount = 10;

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalComponentMenuItems(menu);
            Menu_AppendItem(menu, "Normalise time", Menu_norTime, true, normaliseTime);
            Menu_AppendItem(menu, "Display trail", Menu_trail, true, useTrail);
            Menu_AppendTextItem(menu, trailCount.ToString(), null, TextChange, true);
        }

        private void TextChange(GH_MenuTextBox sender, string text)
        {
            string temp = text;
            try
            {
                trailCount = Convert.ToInt32(temp);
            }
            catch
            {
                sender.TextBoxItem.ForeColor = Color.Red;
                trailCount = Convert.ToInt32(sender.OriginalText);
            }
        }
        private void Menu_trail(object Sender, EventArgs e)
        {
            useTrail = !useTrail;
        }
        private void Menu_norTime(object Sender, EventArgs e)
        {
            normaliseTime = !normaliseTime;
        }

        public double currentTime = 0;
        public double dt = 100;  //simulate with (dt) millisecond interval

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Path> paths = new List<Path>();
            double t = 0;           //current time
            double pbSpeed = 1.0;   //playback speed
            bool start = false;     //start playing
            DA.GetDataList<Path>(0, paths);
            DA.GetData(1, ref t);
            DA.GetData(2, ref pbSpeed);
            DA.GetData(3, ref start);

            Path pathJ = new Path(Path.Join(paths, double.MaxValue, false)[0]);

            Config cfg = new Config();
            if (this.Params.Input[4].SourceCount > 0) DA.GetData(4, ref cfg);
            else cfg = paths[0].config;


            Point3d parkPos = cfg.Machine.parkPos;

            //timer..................................................
            DateTime now = DateTime.Now;
            nowTime = now;
            //A = now.ToLongTimeString();

            //if (_timer == null)
            //{
            //    // Set up timer to fire at dt interval.
            //    _timer = new System.Timers.Timer(dt);
            //    _timer.AutoReset = true;
            //    _timer.Elapsed += TimerElapsed;
            //    _timer.Enabled = true;
            //}

            for (int i = 0; i < pathJ.blocks.Count; i++)
            {
                
                if(pathJ.blocks[i].coordinate.HasValue && nextCoor == null)
                {
                    lastCoor = parkPos;
                    nextCoor = pathJ.blocks[i].coordinate.Value;
                }
                

                if (pathJ.blocks[i].M.Count > 0)
                {
                    for(int j = 0; j< pathJ.blocks[i].M.Count; j++)
                    {
                        Block blk = new Block(pathJ.blocks[i].M[j], pathJ.blocks[i].P[j], pathJ.blocks[i].S[j]);
                        blkDisplay.Enqueue(blk);
                    }
                }


                switch (cfg.Machine.rAxes.type)
                {
                    case RAxesType.None:

                        break;
                    case RAxesType.SPM:

                        break;
                }

                //if(blkDisplay.Count > trailCount)
                //{
                //    do
                //    {
                //        blkDisplay.Dequeue();
                //    } while (blkDisplay.Count >= trailCount);
                //}
            }

            foreach(Path pth in paths)
            {
                pth.ToPolyline();
            }

            if (start)
            {
                Form e = new Seastar.JoggerForm();
#if DEBUG
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "form opening");
#endif
            }

            DA.SetData(0, trailCount);
        }

        public Queue<Block> blkDisplay = new Queue<Block>();
        private System.Timers.Timer _timer;
        DateTime nowTime;
        public Point3d lastPos;
        public Point3d lastCoor;
        public Point3d nextCoor;
        Seastar.JoggerForm frm;
        public static Thread thr;
        public static bool run = true;


        //double click to open form start...................................................................................
        private void UpdateSetData(GH_Document gh)
        {
            ExpireSolution(false);
        }
        public void DisplayForm()
        {
            frm = new Seastar.JoggerForm();
            Grasshopper.GUI.GH_WindowsFormUtil.CenterFormOnCursor(frm, true);
            frm.Show(Grasshopper.Instances.DocumentEditor);
        }

        public override void CreateAttributes()
        {
            m_attributes = new ControlAttributes(this);
        }
        public class ControlAttributes : GH_ComponentAttributes
        {
            public ControlAttributes(IGH_Component PathSim) : base(PathSim) { }

            public override GH_ObjectResponse RespondToMouseDoubleClick(GH_Canvas sender, GH_CanvasMouseEvent e)
            {
                (Owner as PathSim)?.DisplayForm();
                return GH_ObjectResponse.Handled;
            }
        }
        //double click to open form end...................................................................................


        private void TimerElapsed(object sender, EventArgs e)
        {
            // First check to see if the current minute is the same as the previous minute.
            //DateTime now = DateTime.Now;
            //if (nowTime == now) return;

            // If not, invoke the ExpireSolution method on the UI thread.
            // To do so, we must get access to a UI control. On Rhino6 you can use Rhino.RhinoApp.InvokeOnUiThread().
            //System.Windows.Forms.Control control = Grasshopper.Instances.ActiveCanvas;
            //if (control == null)
            //    return;

            //control code here

            //

            //Action<bool> action = new Action<bool>(this.ExpireSolution);
            //control.Invoke(action, true);
        }
        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            GH_Document document = this.OnPingDocument();
            Color defCol = document.PreviewColour;
            int textSize = 20;
            int lnThick = 3;
        }
        protected override System.Drawing.Bitmap Icon => Seastar.Properties.Resources.simSim;

        public override Guid ComponentGuid
        {
            get { return new Guid("d21573ef-bcf9-4ef8-ab3a-0be75ae37ae7"); }
        }
    }

    
}
