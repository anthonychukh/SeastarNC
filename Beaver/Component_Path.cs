using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using Beaver;
using Grasshopper.Kernel;
using Rhino;
using Rhino.Display;
using Rhino.Geometry;

namespace BeaverGrasshopper
{

    public class PathPoint : GH_Component
    {
        public PathPoint()
          : base("Path Point", "PathPioint",
              "Create Path block from point",
              "Beaver", "Path")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Extrusion Width", "EW", "Extrusion Width", GH_ParamAccess.item);
            pManager.AddNumberParameter("Layer Height", "LH", "Layer Height", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("Extrusion Rate", "ER", "Extrusion Rate in mm/min", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            double EW = 0;
            double LH = 0;
            DA.GetData<double>(0, ref EW);
            DA.GetData<double>(1, ref LH);

            DA.SetData(0, EW * LH);
        }

        protected override System.Drawing.Bitmap Icon => Resources.joker;

        public override Guid ComponentGuid
        {
            get { return new Guid("3e925ed6-e163-47e6-8f85-c6aa41994cf9"); }
        }
    }
    public class PathPolylinePrint : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public PathPolylinePrint()
          : base("Polyline Print Path", "PathPolylinePrint",
              "Create printing tool path from polyline",
              "Beaver", "Path")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Path Curve", "C", "Curve representing tool path", GH_ParamAccess.item);
            //pManager.AddPointParameter("Way Point", "P", "Way points of tool path", GH_ParamAccess.list);
            pManager.AddVectorParameter("Tool Orientation", "V", "Orientation of tool as vector\nInput one value or values matching polyline point count" +
                "This is only for 4+ axis machine, leave empty for 3 axis machine", GH_ParamAccess.list, new Vector3d(Vector3d.ZAxis * -1));
            pManager.AddNumberParameter("Feed Rate", "F", "Feed rate to get to this way point\nInput one value or values matching polyline point count", GH_ParamAccess.list);
            pManager.AddNumberParameter("Extrusion", "E",
                "Extrusion rate/width to get to this way point\nInput one value or values matching polyline point count" +
                "\nWith Auto Extrusion Rate Disable, input extrusion rate of path(mm^2)\nThis is useful for spitial/freeform path" +
                "With Auto Extrusion Rate ENABLE, input desire extrusion width of the path(mm)\nThis is only for planar path that was sliced using configuration layer setting" +
                "Actual E number will be calculated from this number with account of filament diameter and path length at gcode export"
                , GH_ParamAccess.list);
            //pManager.AddNumberParameter("Extrusion Width", "EW", "Extrusion width\nOnly works if path is planar and contoured from configuration layer setting", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Tool", "T", "Index of tools to use for this way point\nInput one value or values matching polyline point count", GH_ParamAccess.list);
            pManager.AddTextParameter("Configuration", "config", "Configuration for checking setting", GH_ParamAccess.list, "");

        }

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalComponentMenuItems(menu);
            Menu_AppendItem(menu, "Auto Extrusion Rate", Menu_AutoER, true, autoER);
            Menu_AppendItem(menu, "Display Path", Menu_displayPath, true, displayPath);
            Menu_AppendItem(menu, "Display Feed Rate", Menu_displayFeed, true, displayFeed);
            Menu_AppendItem(menu, "Display Print Width", Menu_displayWidth, true, displayWidth);
        }

        public bool autoER = false;
        public bool displayPath = true;
        public bool displayFeed = false;
        public bool displayWidth = false;

        private void Menu_AutoER(object Sender, EventArgs e)
        {
            autoER = !autoER;
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
        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Beaver Path", "bp", "Beaver Path object", GH_ParamAccess.item);
            pManager.AddIntervalParameter("Feed Range", "f", "Range of feed rate", GH_ParamAccess.item);
            pManager.AddIntervalParameter("Extrusion Rate Range", "er", "Range of extrusion rate", GH_ParamAccess.item);
            pManager.AddIntegerParameter("test", "test", "test", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Polyline pl = new Polyline();
            List<Vector3d> dir = new List<Vector3d>();
            List<double> F = new List<double>();
            List<double> ER = new List<double>();
            List<double> EW = new List<double>();
            List<int> T = new List<int>();
            List<string> configText = new List<string>();
            List<Point3d> testPts = new List<Point3d>();
            Config cfg = new Config();

            Curve crv = new PolyCurve();
            DA.GetData<Curve>("Path Curve", ref crv);
            bool goOn = crv.TryGetPolyline(out pl);
            //int step = 0;
            

            if (goOn)
            {
                bool planar = crv.TryGetPlane(out Plane pTemp);
                bool flatXY = (pTemp.ZAxis.IsParallelTo(Vector3d.ZAxis) != 0);

                pCrv = crv;
                DA.GetDataList<Vector3d>(1, dir);
                DA.GetDataList<double>(2, F);
                DA.GetDataList<double>(3, ER);
                DA.GetDataList<int>(4, T);
                if((F.Count != pl.Count && F.Count != 1) || (ER.Count != pl.Count && ER.Count != 1) || (T.Count != pl.Count && T.Count != 1))
                {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "F or E or T does not match vertices count. These number represent eg the feedrate to reach the point on the path including starting point\n" +
                            "ie. Count should equal to vertices count, not segment count or any other");
                        return;
                }

                List<Block> wayPoints = new List<Block>();
                

                for (int i = 0; i < pl.Count; i++)
                {
                    Vector3d eachDir;
                    double eachF;
                    double? eachER;
                    int eachT;

                    if (dir.Count == 1) { eachDir = dir[0]; }
                    else { eachDir = dir[i]; }
                    if (F.Count == 1) { eachF = F[0]; }
                    else { eachF = F[i]; }
                    if (T.Count == 1) { eachT = T[0]; }
                    else { eachT = T[i]; }

                    //auto er enable--------------------------------------------------------------------------
                    if (autoER)
                    {
                        //fetch configuration..........................
                        if (DA.GetDataList<string>("Configuration", configText))
                        {
                            DA.GetDataList<string>("Configuration", configText);
                            cfg = new Config(configText);
                        }
                        else
                        {
                            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Configuration is required to calculate extrusion rate. Abort. Disable Auto ER otherwise");
                            return;
                        }
                        if (!cfg.Settings.Contains("layer_height"))
                        {
                            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Configuration do not contains layer height information. Abort. Disable Auto ER otherwise");
                            return;
                        }
                        if (!cfg.Settings.Contains("first_layer_height"))
                        {
                            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Configuration do not contains first layer height information. Abort. Disable Auto ER otherwise");
                            return;
                        }

                        //double l
                        
                        if (!planar)
                        {
                            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Auto Extrusion Rate only works for planar path. Abort. Disable Auto ER otherwise");
                            return;
                        }
                        if(!flatXY)//if plane is not parallel to world xy
                        {
                            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Path is not running on World XY plane. Advise to disable Auto ER mode and calculate extrusion rate by comparing consecutive path z distance");
                        }
 
                        double LH;
                        double FLH = (double)cfg.Settings["first layer_height"];
                        double multiplier = 1;
                            

                        if (pl[i].Z > (double)cfg.Settings["first_layer_height"]) //layers above first layer
                        {
                            LH = (double)cfg.Settings["layer_height"];
                            if (cfg.Settings.Contains("extrusion_multiplier"))
                            {
                                multiplier = (double)cfg.Settings["extrusion_multiplier"];
                            }
                            if (flatXY && Math.Abs(pl[i].Z - FLH % LH) > DocumentTolerance()) //not lies on exact layer height
                            {
                                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Path does not lies on layer height as described on configuration. Slice layers with Beaver Slicing Plane component");
                            }
                        }
                        else  //is first layer
                        {
                            LH = (double)cfg.Settings["first layer_height"];
                            if (cfg.Settings.Contains("first_layer_extrusion_multiplier"))
                                {
                                    multiplier = (double)cfg.Settings["first_layer_extrusion_multiplier"];
                                }
                            if(LH - pl[i].Z> DocumentTolerance()) //lower than first layer
                            {
                                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "First layer is lower than configuration first_layer_height");
                            }
                            //LH = pl[i].Z; //bring first layer back to first layer height
                         }
                        DA.GetDataList<double>("Extrusion", EW);

                            if(EW.Count == 1)
                            {
                                if(EW[0] != 0)
                                {
                                    eachER = EW[0] * LH;
                                    eachER *= multiplier;
                                }
                                else
                                {
                                    eachER = null;
                                }
                            }
                            else
                            {
                                eachER = EW[i] * LH;
                                eachER *= multiplier;
                            }
                        
                    }

                    //auto er disable---------------------------------------------------------------------------------------
                    else
                    {
                        if (ER.Count == 1)
                        {
                            if (ER[0] != 0)
                            {
                                eachER = ER[0];
                            }
                            else
                            {
                                eachER = null;
                            }
                        }
                        else { eachER = ER[i]; }
                    }
                    

                   // Block wp = new Block(pl[i], eachDir, eachF, eachER, eachT);
                    wayPoints.Add(new Block(pl[i], eachDir, eachF, eachER, eachT));
                    //testPts.Add(pl[i]);
                   //path
                }

                Path path = new Path(wayPoints);
                
                fRange = path.FeedRange();
                eRange = path.ESRange();
                PathOut = path;

                DA.SetData(0, path);
                DA.SetData(1, fRange);
                DA.SetData(2, eRange);
                DA.SetData(3, wayPoints.Count);
            }
            else
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Provide Polyline for input C");
            }
        }

        public Path PathOut;
        public Curve pCrv;
        public Point3d parkPosition = new Point3d(0, 0, 0);
        public Interval fRange;
        public Interval eRange;
        
        public override void DrawViewportWires(IGH_PreviewArgs args)
        
        {
            //args.Pipeline.DrawPoint(Point3d.Origin, Color.Blue);
            //for(int j = 0; j < 10; j++)
            //{
            //    args.Display.DrawPoint(Point3d.Origin + Vector3d.XAxis*j, Color.Blue);
            //}

            if (displayPath)
            {
                //args.Display.DrawCurve(pCrv, PathOut.DefaultColor);
            }
            if (displayFeed)
            {
                //double parkStack = 0;
                Point3d lastPt = parkPosition;
                double hueLow = 120/360; //colour in hue degree for lowest range
                double hueHigh = 0;

                for (int i = 0; i < PathOut.blocks.Count; i++)
                {
                    if (PathOut.blocks[i].coordinate.HasValue)
                    {
                        double normalF = (double)PathOut.blocks[i].F - fRange.Min / (fRange.Max - fRange.Min);
                        double hue = (normalF / (hueHigh - hueLow)) + hueLow;
                        Color c = new Rhino.Display.ColorHSL(hue, 1, 1).ToArgbColor();
                        args.Display.DrawLine(lastPt, PathOut.blocks[i].coordinate.Value, c);
                        lastPt = PathOut.blocks[i].coordinate.Value;
                    }
                    else
                    {
                        string text = PathOut.blocks[i].GetGCode(false);
                        args.Display.DrawDot(lastPt, text, Color.Black, PathOut.DefaultColor);
                    }
                }

            }

        }
        
        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override Bitmap Icon => Resources.joker;

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("4268bf63-0e5e-42a0-b384-4f4cfd4719e7"); }
        }
    }

    public class PathPolyline : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public PathPolyline()
          : base("Polyline Path", "PathPolyline",
              "Create tool path from polyline??",
              "Beaver", "Path")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            //pManager.AddCurveParameter("Path Curve", "C", "Curve representing tool path", GH_ParamAccess.item);
            pManager.AddGenericParameter("Path Geometry", "P", "Geometry to convert to Path\nInput curves or points", GH_ParamAccess.item);

            pManager.AddVectorParameter("Tool Orientation", "V", "Orientation of tool as vector\nInput one value or values matching polyline point count" +
                "This is only for 4+ axis machine, leave empty for 3 axis machine", GH_ParamAccess.list, new Vector3d(Vector3d.ZAxis * -1));
            pManager.AddNumberParameter("Feed Rate", "F", "Feed rate to get to this way point" +
                "\nInput one value or values matching polyline point count", GH_ParamAccess.list);
            pManager.AddNumberParameter("Extrusion", "E",
                "Extrusion rate/width to get to this way point" +
                "\nInput one value or values matching polyline point count" +
                "\ninput extrusion rate of path(mm^2) i.e. Cross sectional area of extrusion" +
                "\nConnect to PathER to calculate ER" +
                "\nFor 3D printing only, leave empty for milling. Leave empty for travel moves" , GH_ParamAccess.list, 0);
            pManager.AddNumberParameter("Speed", "S", 
                "Spindle Speed to get to this way point" +
                "\nFor milling only. Leave empty for 3d printing, Leave empty for travel moves", GH_ParamAccess.list, 0);
            pManager.AddIntegerParameter("Tool", "T", "Index of tools to use for this way point\nInput one value or values matching polyline point count", GH_ParamAccess.list,0);
            pManager.AddTextParameter("Configuration", "config", "Configuration for checking setting", GH_ParamAccess.list, "");

        }

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalComponentMenuItems(menu);
            Menu_AppendItem(menu, "Display Path", Menu_displayPath, true, displayPath);
            Menu_AppendItem(menu, "Display Feed Rate", Menu_displayFeed, true, displayFeed);
            Menu_AppendItem(menu, "Display Extrusion Rate/Spindle Speed", Menu_displayWidth, true, displayWidth);
        }

        public bool displayPath = true;
        public bool displayFeed = false;
        public bool displayWidth = false;

 
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
        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Beaver Path", "bp", "Beaver Path object", GH_ParamAccess.item);
            pManager.AddIntervalParameter("Feed Range", "f", "Range of feed rate", GH_ParamAccess.item);
            pManager.AddIntervalParameter("Extrusion Rate Range", "er", "Range of extrusion rate", GH_ParamAccess.item);
            pManager.AddIntegerParameter("test", "test", "test", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Polyline pl = new Polyline();
            Arc arc = new Arc();
            Circle cir = new Circle();
            List<Vector3d> dir = new List<Vector3d>();
            List<double> F = new List<double>();
            List<double> ES = new List<double>();
            ES.Add(0);
            List<int> T = new List<int>();
            List<string> configText = new List<string>();
            List<Point3d> testPts = new List<Point3d>();
            Config cfg = new Config();
            bool IsPrint;

           Debug.WriteLine(this.Params.Input[0].Sources[0].TypeName);

            Curve crv = new PolyCurve();
                        
            if (this.Params.Input[0].Sources[0].TypeName.Equals("Curve"))
            {
                DA.GetData<Curve>(0, ref crv);
                bool goOnPl = crv.TryGetPolyline(out pl);
                bool goOnArc = crv.TryGetArc(out arc);
                bool goOnCir = crv.TryGetCircle(out cir);
                //int step = 0;

                if (goOnPl)
                {

                    bool planar = crv.TryGetPlane(out Plane pTemp);
                    bool flatXY = (pTemp.ZAxis.IsParallelTo(Vector3d.ZAxis) != 0);

                    pCrv = crv;
                    DA.GetDataList<Vector3d>(1, dir);
                    DA.GetDataList<double>(2, F);

                    if (this.Params.Input[3].SourceCount > 0) //extrusion rate
                    {
                        ES.Clear();
                        DA.GetDataList<double>(3, ES);
                        IsPrint = true;

                    }
                    if (this.Params.Input[4].SourceCount > 0) //spindle speed
                    {
                        ES.Clear();
                        DA.GetDataList<double>(4, ES);
                        IsPrint = false;
                    }

                    DA.GetDataList<int>(5, T);

                    //.....check input.........................
                    if (this.Params.Input[3].SourceCount > 0 && this.Params.Input[4].SourceCount > 0)
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Only input either E or S but not both");
                        return;
                    }
                    if ((F.Count != pl.Count && F.Count != 1) || (T.Count != pl.Count && T.Count != 1))
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "F or T does not match vertices count. These number represent info to reach the point on the path including starting point\n" +
                            "ie. Count should equal to vertices count\n" +
                            "Or input one value for all vertices");
                        return;
                    }
                    if (ES.Count != pl.Count && ES.Count != 1)
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "E or S does not match vertices count. These number represent info to reach the point on the path including starting point\n" +
                            "ie. Count should equal to vertices count\n" +
                            "Or input one value for all vertices");
                        Debug.WriteLine("ESCount " + ES.Count.ToString());
                        return;
                    }

                    List<Block> wayPoints = new List<Block>();

                    //set waypoint value...............................
                    for (int i = 0; i < pl.Count; i++)
                    {
                        Vector3d eachDir;
                        double eachF;
                        double? eachES;
                        //double? eachS;
                        int eachT;

                        if (dir.Count == 1) { eachDir = dir[0]; }
                        else { eachDir = dir[i]; }
                        if (F.Count == 1) { eachF = F[0]; }
                        else { eachF = F[i]; }
                        if (T.Count == 1) { eachT = T[0]; }
                        else { eachT = T[i]; }


                        if (ES.Count == 1)
                        {
                            if (ES[0] != 0)
                            {
                                eachES = ES[0];
                            }
                            else
                            {
                                eachES = null;
                            }
                        }
                        else { eachES = ES[i]; }




                        wayPoints.Add(new Block(pl[i], eachDir, eachF, eachES, eachT));

                    }

                    Path path = new Path(wayPoints);

                    fRange = path.FeedRange();
                    if (ES.Count == 1 && ES[0] == 0)
                    {

                    }
                    else
                    {
                        eRange = path.ESRange();
                    }
                    PathOut = path;


                    //cross check Config..................
                    if (this.Params.Input[6].SourceCount > 0)
                    {

                    }

                    DA.SetData(0, path);
                    DA.SetData(1, fRange);
                    DA.SetData(2, eRange);
                    DA.SetData(3, wayPoints.Count);
                }


                if (goOnArc)
                {
                    //List<double> fList = new List<double>();

                    DA.GetDataList<Vector3d>(1, dir);
                    DA.GetDataList<double>(2, F);

                    if (this.Params.Input[3].SourceCount > 0) //extrusion rate
                    {
                        DA.GetDataList<double>(3, ES);
                        IsPrint = true;

                    }
                    if (this.Params.Input[4].SourceCount > 0) //spindle speed
                    {
                        DA.GetDataList<double>(4, ES);
                        IsPrint = false;
                    }
                    else
                    {
                        ES.Add(0); //just travel move
                    }
                    DA.GetDataList<int>(5, T);

                    //.....check input.........................
                    if (this.Params.Input[3].SourceCount > 0 && this.Params.Input[4].SourceCount > 0)
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Only input either E or S but not both");
                        return;
                    }
                    if ((F.Count != 1) || (ES.Count != 1) || (T.Count != 1))
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "F or E or T does not match arc count.");
                        return;
                    }

                    Block arcMove = new Block(arc.Center, arc, F[0], ES[0], T[0]);
                    DA.SetData(0, arcMove);
                    //Path path = new Path()
                }
                //if (goOnCir)
                //{

                //}

                //else
                //{
                //    this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Provide polyline, arc or circle for input C");
                //}
            }
            

            Point3d pt = new Point3d();
            
            if (this.Params.Input[0].Sources[0].TypeName.Equals("Point"))
            {
                DA.GetData<Point3d>(0, ref pt);
                DA.GetDataList<Vector3d>(1, dir);
                DA.GetDataList<double>(2, F);

                if (this.Params.Input[3].SourceCount > 0) //extrusion rate
                {
                    ES.Clear();
                    DA.GetDataList<double>(3, ES);
                    IsPrint = true;
                }
                if (this.Params.Input[4].SourceCount > 0) //spindle speed
                {
                    ES.Clear();
                    DA.GetDataList<double>(4, ES);
                    IsPrint = false;
                }

                DA.GetDataList<int>(5, T);

                Block blk = new Block(pt, dir[0], F[0], ES[0], T[0]);
                Path pth = new Path(blk);
                PathOut = pth;
                DA.SetData(0, pth);
                Debug.WriteLine("created path from point");
            }
        }

        public Path PathOut;
        public Curve pCrv;
        //public Point3d pPt;
        public Point3d parkPosition = new Point3d(0, 0, 0);
        public Interval fRange;
        public Interval eRange;

        public override void DrawViewportWires(IGH_PreviewArgs args)

        {
            
            if(PathOut == null)
            {
                return;
            }
            if (displayPath)
            {
                Point3d lastPt = PathOut.StartCoor;
                Color c = Color.Red;
                if (PathOut.blocks.Count > 1)
                {
                    for (int i = 0; i < PathOut.blocks.Count; i++)
                    {
                        if (PathOut.blocks[i].coordinate.HasValue)
                        {
                            args.Display.DrawLine(lastPt, PathOut.blocks[i].coordinate.Value, c);
                            lastPt = PathOut.blocks[i].coordinate.Value;
                        }
                        else
                        {
                            string text = PathOut.blocks[i].GetGCode(false);
                            args.Display.DrawDot(lastPt, text, c, PathOut.DefaultColor);
                        }
                    }
                }
                else
                {
                    if (PathOut.blocks[0].coordinate.HasValue)
                    {
                        args.Display.DrawPoint(PathOut.blocks[0].coordinate.Value, c);
                    }
                    else
                    {
                        string text = PathOut.blocks[0].GetGCode(false);
                        args.Display.DrawDot(lastPt, text, c, PathOut.DefaultColor);
                    }
                }
            }
            if (displayFeed)
            {
                
                Point3d lastPt = PathOut.StartCoor;
                double hueLow = 0.333; //colour in hue degree for lowest range
                double hueHigh = 0.0;

                for (int i = 0; i < PathOut.blocks.Count; i++)
                {
                    if (PathOut.blocks[i].coordinate.HasValue)
                    {
                        double normalF = ((double)PathOut.blocks[i].F - fRange.Min) / (fRange.Max - fRange.Min);
                        double hue = normalF * (hueHigh - hueLow);
                        hue += hueLow;
                        ColorHSV hsv = new Rhino.Display.ColorHSV(hue, 1, 1);
                        Color c = hsv.ToArgbColor();

                        args.Display.DrawLine(lastPt, PathOut.blocks[i].coordinate.Value, c);
                        lastPt = PathOut.blocks[i].coordinate.Value;
                    }
                    else
                    {
                        string text = PathOut.blocks[i].GetGCode(false);
                        args.Display.DrawDot(lastPt, text, Color.Red, PathOut.DefaultColor);
                    }
                }

            }

            if (displayWidth)
            {
                Point3d lastPt = PathOut.StartCoor;
                double hueLow = 0.666; //colour in hue degree for lowest range
                double hueHigh = 1.0;

                for (int i = 0; i < PathOut.blocks.Count; i++)
                {
                    if (PathOut.blocks[i].coordinate.HasValue)
                    {
                        Color c;
                        if (PathOut.blocks[i].ES.HasValue)
                        {
                            double normalF = ((double)PathOut.blocks[i].ES - eRange.Min) / (eRange.Max - eRange.Min);
                            double hue = normalF * (hueHigh - hueLow);
                            hue += hueLow;
                            ColorHSV hsv = new Rhino.Display.ColorHSV(hue, 1, 1);
                            c = hsv.ToArgbColor();
                        }
                        else
                        {
                            ColorHSV hsv = new Rhino.Display.ColorHSV(hueLow, 1, 0.2);
                            c = hsv.ToArgbColor();
                        }
                        
                        

                        args.Display.DrawLine(lastPt, PathOut.blocks[i].coordinate.Value, c);
                        lastPt = PathOut.blocks[i].coordinate.Value;
                    }
                    else
                    {
                        string text = PathOut.blocks[i].GetGCode(false);
                        args.Display.DrawDot(lastPt, text, Color.Red, PathOut.DefaultColor);
                    }
                }
            }

        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override Bitmap Icon => Resources.joker;

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("9bae3614-b57a-4867-9a63-8f3a338b76fb"); }
        }
    }

    public class PathER : GH_Component
    {
        public PathER()
          : base("Path Extrusion Rate", "PathER",
              "Calculate path extrusion rate\n" +
                "Extrusion rate equals to extrusion width x extrusion height",
              "Beaver", "Path")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Extrusion Width", "EW", "Extrusion Width", GH_ParamAccess.item);
            pManager.AddNumberParameter("Layer Height", "LH", "Layer Height", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("Extrusion Rate", "ER", "Extrusion Rate in mm/min", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            double EW = 0;
            double LH = 0;
            DA.GetData<double>(0, ref EW);
            DA.GetData<double>(1, ref LH);

            DA.SetData(0, EW * LH);
        }

        protected override System.Drawing.Bitmap Icon => Resources.joker;

        public override Guid ComponentGuid
        {
            get { return new Guid("8df0c520-3695-41c6-b3ac-21d998ee10a9"); }
        }
    }
    
    public class PathJoin : GH_Component
    {
        public PathJoin()
          : base("Join Paths", "PathJoin",
              "Join multpile paths",
              "Beaver", "Path")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Beaver Path", "bp", "Beaver Paths to join", GH_ParamAccess.list);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Beaver Path", "bp", "Joined Beaver Paths", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Path> paths = new List<Path>();
            DA.GetDataList<Path>(0, paths);

            double tol = GH_Component.DocumentTolerance();

            List<Path> pathOut = Path.Join(paths, tol, false);
            DA.SetDataList(0, pathOut);
        }

        protected override System.Drawing.Bitmap Icon => Resources.joker;

        public override Guid ComponentGuid
        {
            get { return new Guid("6777b45c-c9a0-416c-b577-cddc91588ec9"); }
        }
    }

    public class PathInsert : GH_Component
    {
        public PathInsert()
          : base("Insert Paths", "PathInsert",
              "Insert Path and command at specific index",
              "Beaver", "Path")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Beaver Path", "bp", "Beaver Paths to join", GH_ParamAccess.list);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Beaver Path", "bp", "Joined Beaver Paths", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Path> paths = new List<Path>();
            DA.GetDataList<Path>(0, paths);

            double tol = GH_Component.DocumentTolerance();

            //List<Path> pathOut = Path.Join(paths, tol, false);
            List<Path> pathOut = Path.Join(paths, tol, false);

            DA.SetDataList(0, pathOut);
        }

        protected override System.Drawing.Bitmap Icon => Resources.joker;

        public override Guid ComponentGuid
        {
            get { return new Guid("b51535a9-ef20-4670-9e98-22b7c8d9daaf"); }
        }
    }

    public class InsetAction : GH_Component  //incompleted
    {
        public InsetAction()
          : base("Insert Action", "InsertAction",
              "Insert non-move command into Path",
              "Beaver", "Path")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Beaver Path", "bp", "Beaver Paths to join", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Action", "a", "Action to insert", GH_ParamAccess.list);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Beaver Path", "bp", "Joined Beaver Paths", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Path> paths = new List<Path>();
            DA.GetDataList<Path>(0, paths);

            double tol = GH_Component.DocumentTolerance();

            List<Path> pathOut = Path.Join(paths, tol, false);
            DA.SetDataList(0, pathOut);
        }

        protected override System.Drawing.Bitmap Icon => Resources.joker;

        public override Guid ComponentGuid
        {
            get { return new Guid("11cff715-1fb0-4b79-9b78-97f1958a1bc7"); }
        }
    }

    public class PathDecompose: GH_Component  //debug
    {
        public PathDecompose()
          : base("Decompose Path", "PathDecompose",
              "Decompose path into its coordinate and values",
              "Beaver", "Path")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Beaver Path", "bp", "Beaver Paths to decompose", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            //pManager.AddGenericParameter("Beaver Path", "bp", "Joined Beaver Paths", GH_ParamAccess.list);
            pManager.AddCurveParameter("Polyline", "C", "Underlying Polyline", GH_ParamAccess.item);
            pManager.AddNumberParameter("Feed Rate", "F", "Feed Rate in mm/min", GH_ParamAccess.list);
            pManager.AddIntegerParameter("G", "G", "G command Line", GH_ParamAccess.list);
            pManager.AddNumberParameter("ES", "ES", "Extrusion Rate or Spindle Speed", GH_ParamAccess.list);
            pManager.AddArcParameter("Arc", "A", "Underlying Arc", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Starting Coordinate", "si", "Starting Coordinate", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Ending Coordinate", "ei", "Ending Coordinate", GH_ParamAccess.item);

        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Path p = new Path();
            DA.GetData<Path>(0, ref p);
            List<Point3d> ptList = new List<Point3d>();
            List<Arc> arcList = new List<Arc>();
            List<int> GList = new List<int>();
            List<double> ESList = new List<double>();
            List<double> FList = new List<double>();
            

            for (int i = 0; i < p.blocks.Count; i++)
            {
                if (p.blocks[i].coordinate.HasValue)
                {
                    ptList.Add(p.blocks[i].coordinate.Value);
                }
                if (p.blocks[i].arc.HasValue)
                {
                    arcList.Add(p.blocks[i].arc.Value);
                }

                if (p.blocks[i].G.HasValue)
                {
                    GList.Add(p.blocks[i].G.Value);
                }
                if (p.blocks[i].F.HasValue)
                {
                    FList.Add(p.blocks[i].F.Value);
                }
                if (p.blocks[i].ES.HasValue)
                {
                    ESList.Add(p.blocks[i].ES.Value);
                }
            }

            Polyline pl = new Polyline(ptList);

            DA.SetData(0, pl);
            DA.SetDataList("Feed Rate", FList);
            DA.SetDataList("G", GList);
            DA.SetDataList("Arc", arcList);
            DA.SetDataList("ES", ESList);
            DA.SetData("Starting Coordinate", p.startCoori);
            DA.SetData("Ending Coordinate", p.endCoori);
        }

        protected override System.Drawing.Bitmap Icon => Resources.joker;

        public override Guid ComponentGuid
        {
            get { return new Guid("5c6df3b1-da4b-4b49-8ad5-fa7b1ba0ef1e"); }
        }
    }
}