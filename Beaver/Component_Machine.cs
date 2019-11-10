using System;
using System.Collections.Generic;
using System.Drawing;
using Seastar;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Rhino.Geometry;

namespace SeastarGrasshopper
{
    
    public class MachineCreate : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public MachineCreate()
          : base("Create Machine", "MachineCreate",
              "Create the machine and its settings for this operation\nA machine consist of its dimensions and a list of tools",
              "Seastar", "02 | Machine")
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.primary;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Machine Name", "n", "Name of the machine", GH_ParamAccess.item, "");
            pManager.AddIntervalParameter("Size X", "x", "X dimension of machine\n Or radius if you have a cylindical/delta machine", GH_ParamAccess.item);
            pManager.AddIntervalParameter("Size Y", "y", "Y dimension of machine\nIgnore this input if you have a cylindical/delta machine\nSize X will be used as radius of machine", GH_ParamAccess.item);
            pManager[2].Optional = true;
            pManager.AddIntervalParameter("Size Z", "z", "Z dimension of machine or height", GH_ParamAccess.item);
            //pManager.AddIntegerParameter("Number of axes", "A", "Number of axes", GH_ParamAccess.item, 3);
            pManager.AddGenericParameter("Tool", "t", "List of tools the machine can use", GH_ParamAccess.list);
            pManager[4].Optional = true;
            pManager.AddPointParameter("Park Position", "P", "Park position", GH_ParamAccess.item, new Point3d(0, 0, 0));
            pManager.AddGenericParameter("Rotational Axes", "R", "Rotational axes mechanism\nThos will affect how ABC axes are exported in Gcode", GH_ParamAccess.item);
            pManager[6].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Machine configuration", "M", "Machine configuration", GH_ParamAccess.item);
            pManager.AddBrepParameter("Volume", "V", "Machine working volume", GH_ParamAccess.item);
            pManager.AddTextParameter("Message", "msg", "Message and description of machine", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string msg = "";
            string name = "";
            Interval x = new Interval();
            Interval y = new Interval();
            Interval z = new Interval();
            List<Tool> tools = new List<Tool>();
            RotationAxes rx = new RotationAxes();

            DA.GetData<string>(0, ref name);
            DA.GetData<Interval>(1, ref x);
            DA.GetData<Interval>(3, ref z);


            DA.GetDataList<Tool>(4, tools);
            if(this.Params.Input[6].SourceCount > 0)
            {
                DA.GetData(6, ref rx);
                //msg += rx.ToString();
            }


            Machine mOut;
           

            if(DA.GetData<Interval>(2, ref y)) //is carteasian
            {
                DA.GetData<Interval>(2, ref y);
                mOut = new Machine(x, y, z, tools);
            }
            else //is delta
            {
                //mOut = new Machine(x, z, tools);
                mOut = new Machine(x, z, 30.22, 200.0, 27.1, 290.8, tools, rx); //only default value WIP
            }

            msg += "A Seastar Machine was successfully created\n";
            //msg += mOut.rAxes.ToString();

            DA.SetData(0, new Config(mOut));
            DA.SetData(1, mOut.Volume);
            msg += mOut.ToString();
            DA.SetData(2, msg);

        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Seastar.Properties.Resources.createMach;

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("40ddff76-b51d-497c-8ae7-14ab431bb629"); }
        }
    }

    public class ToolCreateExtruder : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public ToolCreateExtruder()
          : base("Create Extruder Tool", "ToolCreateExtruder",
              "Create tools for 3D printing extrusion",
              "Seastar", "02 | Machine")
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Tool Name", "n", "Name of the tool", GH_ParamAccess.item, "");

            pManager.AddNumberParameter("Diameter", "d", "Nozzle diameter", GH_ParamAccess.item);
            pManager.AddNumberParameter("X Offset", "ox", "X offset distance", GH_ParamAccess.item);
            pManager.AddNumberParameter("Y Offset", "oy", "Y offset distance", GH_ParamAccess.item);
            pManager.AddNumberParameter("Z Offset", "oz", "Z offset distance", GH_ParamAccess.item, 0);
            pManager.AddTextParameter("Tool Change Code", "tc", "Tool change Gcode as list", GH_ParamAccess.list, new List<string>());
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Tool", "T", "Seastar Tool", GH_ParamAccess.item);
            pManager.AddTextParameter("Message", "msg", "Message and description of machine", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string name = "";
            double d = 0;
            double ox = 0;
            double oy = 0;
            double oz = 0;
            List<string> tc = new List<string>();
            DA.GetData<string>(0, ref name);
            DA.GetData<double>(1, ref d);
            DA.GetData<double>(2, ref ox);
            DA.GetData<double>(3, ref oy);
            DA.GetData<double>(4, ref oz);
            //DA.GetData<double>()
            DA.GetDataList<string>(5, tc);

            string msg = "";

            Tool t = new Tool(name, ToolType.extruder, d, ox, oy, oz);
            t.ToolChange = tc;
            msg += "A Seastar Tool is successfully created\n";
            msg += t.ToString();

            DA.SetData(0, t);
            DA.SetData(1, msg);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Seastar.Properties.Resources.createExtruder;

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("367ec29e-7ecf-4c52-935e-2f260ecf840c"); }
        }
    }

    public class ToolCreateMill : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public ToolCreateMill()
          : base("Create Milling Tool", "ToolCreateMill",
              "Create tools for milling",
              "Seastar", "02 | Machine")
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Tool Name", "n", "Name of the tool", GH_ParamAccess.item, "");
            pManager.AddNumberParameter("Bit Diameter", "d", "Milling bit diameter", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Tool Shape", "s", "Milling bit profile index\n0 for flat end mill\n1for ball end mill", GH_ParamAccess.item, 0);
            pManager.AddNumberParameter("X Offset", "ox", "X offset distance", GH_ParamAccess.item);
            pManager.AddNumberParameter("Y Offset", "oy", "Y offset distance", GH_ParamAccess.item);
            pManager.AddNumberParameter("Z Offset", "oz", "Z offset distance", GH_ParamAccess.item, 0);
            pManager.AddNumberParameter("Feed", "F", "Feed rate mm/min\nDefault speed of movement of tool", GH_ParamAccess.item);
            pManager.AddNumberParameter("Spindle Speed", "S", "Splindle speed rpm\nDefault rotational speed of milling bit", GH_ParamAccess.item);
            pManager.AddNumberParameter("PlungeSpeed", "pr", "Plunge rate mm/min\nDefault speed to engage material after a travel move", GH_ParamAccess.item);
            pManager.AddNumberParameter("Retract Rate", "rr", "Retract rate mm/min\nDefault speed to retract from material \nat the begining of a travel move", GH_ParamAccess.item);
            pManager.AddTextParameter("Tool Change Code", "tc", "Tool change Gcode as list", GH_ParamAccess.list, new List<string>());
            //pManager.AddIntegerParameter("Direction", "dir", "Tool right )
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Tool", "T", "Seastar Tool", GH_ParamAccess.item);
            pManager.AddTextParameter("Message", "msg", "Message and description of machine", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<string> shps = new List<string>();
            shps.Add("flat");
            shps.Add("round");
            Extension.DropDown(2, "tool profile : ", shps, this);

            string name = "";
            double d = 0;
            int type = 0;
            double ox = 0;
            double oy = 0;
            double oz = 0;
            double F = 0;
            double S = 0;
            double pr = 0;
            double rr = 0;
            List<string> tc = new List<string>();

            DA.GetData<string>(0, ref name);
            DA.GetData<double>(1, ref d);
            DA.GetData<int>(2, ref type);
            DA.GetData<double>(3, ref ox);
            DA.GetData<double>(4, ref oy);
            DA.GetData<double>(5, ref oz);
            DA.GetData<double>(6, ref F);
            DA.GetData<double>(7, ref S);
            DA.GetData<double>(8, ref pr);
            DA.GetData<double>(9, ref rr);
            DA.GetDataList<string>(10, tc);

            string msg = "";
            Tool t;

            if (type == 0) //is flat
            {
                t = new Tool(name, ToolType.mill, d, ox, oy, oz, ToolShape.flat, F, S, pr, rr);
                msg += "A Seastar Tool is successfully created\n";
                msg += t.ToString();
                t.ToolChange = tc;
                DA.SetData(0, t);
                DA.SetData(1, msg);
            }
            if (type == 1) //is ball end
            {
                t = new Tool(name, ToolType.mill, d, ox, oy, oz, ToolShape.ball, F, S, pr, rr);
                msg += "A Seastar Tool is successfully created\n";
                msg += t.ToString();
                t.ToolChange = tc;
                DA.SetData(0, t);
                DA.SetData(1, msg);
            }
            

        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Seastar.Properties.Resources.createMill;

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("efc332ae-5d0b-4948-b4c1-9d1fdad49194"); }
        }
    }

    public class AxesCreateSPM : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public AxesCreateSPM()
          : base("Create SPM Axes", "AxesCreateSPM",
              "Create SPM end effector for +3 axes operation",
              "Seastar", "02 | Machine")
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            //pManager.AddTextParameter("Tool Name", "n", "Name of the tool", GH_ParamAccess.item, "");
            pManager.AddPlaneParameter("Home Plane", "hp", "Home orientation of effector", GH_ParamAccess.item, new Plane(Point3d.Origin, Vector3d.ZAxis*-1));
            pManager.AddPlaneParameter("Target Plane", "tp", "Target orientation of effector", GH_ParamAccess.item);
            pManager.AddNumberParameter("Alpha1", "a1", "Alpha1 angle of SPM", GH_ParamAccess.item, 45);
            pManager.AddNumberParameter("Alpha2", "a2", "Alpha2 angle of SPM", GH_ParamAccess.item, 90);
            pManager.AddNumberParameter("Gamma", "g", "Gamma angle of SPM", GH_ParamAccess.item, 0);
            pManager.AddNumberParameter("Beta", "b", "Beta angle of SPM", GH_ParamAccess.item, 80);
            pManager.AddNumberParameter("Radius", "r", "Radius of SPM", GH_ParamAccess.item, 20);
            pManager.AddVectorParameter("Plane Offset", "po", "End effector plane offset from centre of rotation", GH_ParamAccess.item, new Vector3d(0, 0, 0));
            pManager.AddVectorParameter("Mounting Offset", "mo", "Mounting plane offset from centre of rotation", GH_ParamAccess.item, new Vector3d(0, 0, 0));
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("SPM Axes", "RA", "Seastar SPM Axes.\nConnect to Configuration", GH_ParamAccess.item);
            pManager.AddNumberParameter("Rotational Input", "RI", "Absolute rotational input from motor", GH_ParamAccess.list);
            pManager.AddArcParameter("Linkage Arc", "AC", "Arc curve representing linkages", GH_ParamAccess.tree);
            pManager.AddVectorParameter("Axes", "AX", "Axes of the SPM", GH_ParamAccess.tree);
            pManager.AddPlaneParameter("Offset Plane", "OP", "Offset plane of end effector", GH_ParamAccess.item);
            pManager.AddPlaneParameter("Mounting Plane", "MP", "Mounting plane of the spherical axes", GH_ParamAccess.item);
            pManager.AddPointParameter("Work Space", "WS", "Work space of this SPM config.\nUpdate only when you click menu: Calculate Work Space", GH_ParamAccess.list);
            pManager.AddTextParameter("Message", "msg", "Message and description of machine", GH_ParamAccess.item);
        }

        private bool useDegree = true;  //vs use radian
        private bool calWorkSpace = false;

        protected override void AppendAdditionalComponentMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            base.AppendAdditionalComponentMenuItems(menu);
            Menu_AppendItem(menu, "Use Degree", click, true, useDegree);
            Menu_AppendSeparator(menu);
            Menu_AppendItem(menu, "Calculate Work Space", EventCalWorkSpace, true, calWorkSpace);
        }

        private void click(object Sender, EventArgs e)
        {
            useDegree = !useDegree;
            this.ExpireSolution(true);
        }

        private void EventCalWorkSpace(object Sender, EventArgs e)
        {
            WorkSpace = s1.WorkSpace(70, Math.PI*0.5);
            this.ExpireSolution(true);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string msg = "";
            Plane hp = new Plane();
            Plane op = new Plane();
            double a1 = 0;
            double a2 = 0;
            double g = 0;
            double b = 0;
            double r = 0;
            Vector3d po = new Vector3d();
            Vector3d mo = new Vector3d();
            DA.GetData<Plane>("Home Plane", ref hp);
            DA.GetData<Plane>("Target Plane", ref op);
            DA.GetData<double>("Alpha1", ref a1);
            DA.GetData<double>("Alpha2", ref a2);
            DA.GetData<double>("Gamma", ref g);
            DA.GetData<double>("Beta", ref b);
            DA.GetData<double>("Radius", ref r);
            DA.GetData<Vector3d>("Plane Offset", ref po);
            DA.GetData<Vector3d>("Mounting Offset", ref mo);
            //offset = po;
            //Transform offsetTrans = Transform.Translation(po);

            if (useDegree) //convert from degree input to radian
            {
                a1 *= Math.PI / 180;
                a2 *= Math.PI / 180;
                g *= Math.PI / 180;
                b *= Math.PI / 180;
            }
            
            if (a1 + a2 + g + b < Math.PI)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Sum of a1, a2, b and g should be larger than 180 degree/Pi");
                return;
            }

            s1 = new SPM(hp, a1, a2, g, b, r, po, mo);
            s1.ComputeInitialConfig(true);
            if(!s1.UpdateConfig(op, true))
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "This configuration is out of workspace of this SPM");
                return;
            }

            List<double> ial = s1.GetAnglesList(useDegree);

            DataTree<Vector3d> axes = new DataTree<Vector3d>();
            for (int j = 0; j < 3; j++)
            {
                GH_Path pth = new GH_Path(j);
                axes.Add(s1.axesU[j], pth);
                axes.Add(s1.axesW[j], pth);
                axes.Add(s1.axesV[j], pth);
            }

            //msg += s1.angleAHome.ToString();
            //msg += "\n";
            //msg += s1.angleBHome.ToString();
            //msg += "\n";
            //msg += s1.angleCHome.ToString();
            //msg += "\n";

            DA.SetDataTree(2, s1.LinkArc());
            DA.SetDataTree(3, axes);
            DA.SetDataList("Rotational Input", ial);
            DA.SetData("SPM Axes", new RotationAxes(s1));
            DA.SetData("Offset Plane", s1.targetPlane);
            DA.SetData("Mounting Plane", s1.mountPlane);
            DA.SetDataList("Work Space", WorkSpace);
            DA.SetData("Message", msg);
        }

        public SPM s1;
        public List<Point3d> WorkSpace = new List<Point3d>();
        

        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            GH_Document document = this.OnPingDocument();
            Color defCol = document.PreviewColour;
            int lineThickness = 3;
            
            if (s1 == null)
            {
                return;
            }

            Vector3d os = s1.offsetVector();
            Transform tl = Transform.Translation(os);
            
            //document.PreviewCustomMeshParameters
            Rhino.Display.DisplayMaterial m = new Rhino.Display.DisplayMaterial(defCol, 0.8);
            if (s1 == null) return;
            double axalLength = s1.radius * 0.1;
            List<Point3d> chasis = new List<Point3d>();

            List<Line> axal = new List<Line>();
            DataTree<Arc> arcTree = s1.LinkArc();
            for (int i = 0; i < arcTree.BranchCount; i++)
            {
                foreach (Arc ac in arcTree.Branch(i))
                {
                    args.Display.DrawArc(ac, defCol, lineThickness);
                }
            }
            for (int j = 0; j < 3; j++)
            {
                Point3d pA1 = new Point3d(s1.homePlane.Origin + (s1.axesU[j] * (s1.radius - axalLength)));
                Point3d pA2 = new Point3d(s1.homePlane.Origin + (s1.axesU[j] * (s1.radius + axalLength)));

                args.Display.DrawLine(pA1, pA2, defCol, lineThickness);

                Point3d pB1 = new Point3d(s1.homePlane.Origin + (s1.axesW[j] * (s1.radius - axalLength)));
                Point3d pB2 = new Point3d(s1.homePlane.Origin + (s1.axesW[j] * (s1.radius + axalLength)));
                
                args.Display.DrawLine(pB1, pB2, defCol, lineThickness);

                Point3d pC1 = new Point3d(s1.homePlane.Origin + (s1.axesV[j] * (s1.radius - axalLength)));
                Point3d pC2 = new Point3d(s1.homePlane.Origin + (s1.axesV[j] * (s1.radius + axalLength)));

                args.Display.DrawLine(pC1, pC2, defCol, lineThickness);

                chasis.Add(new Point3d(s1.homePlane.Origin + (s1.axesV[j] * s1.radius)));
            }

            Brep b = Brep.CreateFromCornerPoints(chasis[0] , chasis[1], chasis[2], 0.01);
            args.Display.DrawBrepShaded(b, m);
            args.Display.DrawPoint(s1.mountPlane.Origin, defCol);


            //args.Display.DrawLine(lastPt, PathOut.blocks[i].coordinate.Value, c, lnThick);
        }
        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Seastar.Properties.Resources.createSPM2;

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("38d42740-8f5a-4924-bc01-d02f4328a38a"); }
        }
    }


    public class AxesCreateSpherical : GH_Component
    {
        public AxesCreateSpherical()
          : base("Create Spherical Axes", "AxesCreateSpherical",
              "Create Spherical end effector for +3 axes operation",
              "Seastar", "02 | Machine")
        {
        }
        public override GH_Exposure Exposure => GH_Exposure.secondary;

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPlaneParameter("Home Plane", "hp", "Home orientation of effector", GH_ParamAccess.item, new Plane(Point3d.Origin, Vector3d.ZAxis * -1));
            pManager.AddPlaneParameter("Target Plane", "tp", "Target orientation of effector", GH_ParamAccess.item);
            pManager.AddIntervalParameter("A Range", "a", "Rotational range of first axis", GH_ParamAccess.item, new Interval(Math.PI*-1, Math.PI));
            pManager.AddIntervalParameter("B Range", "b", "Rotational range of second axis", GH_ParamAccess.item, new Interval(Math.PI * -1, Math.PI));
            pManager.AddIntervalParameter("C Range", "c", "Rotational range of third axis", GH_ParamAccess.item, new Interval(Math.PI * -1, Math.PI));
            pManager.AddIntegerParameter("Rotational System", "s", 
                "The order of local axes to rotate the plane around.\n" +
                "xyz = 72, zyx = 6, zxz = 8, xzy = 33, yxz = 24, yzx = 18, zxy = 9", 
                GH_ParamAccess.item, 8);
            pManager.AddVectorParameter("Plane Offset", "po", "End effector plane offset from centre of rotation", GH_ParamAccess.item, new Vector3d(0, 0, 0));
            pManager.AddVectorParameter("Mounting Offset", "mo", "Mounting plane offset from centre of rotation", GH_ParamAccess.item, new Vector3d(0, 0, 0));
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Spherical Axes", "RA", "Seastar Spherical Axes.\nConnect to Configuration", GH_ParamAccess.item);
            pManager.AddNumberParameter("Rotational Input", "RI", "Absolute rotational input of each axis", GH_ParamAccess.list);
            pManager.AddVectorParameter("Axes", "AX", "Axes of the spherical axes", GH_ParamAccess.list);
            
            pManager.AddPlaneParameter("Offset Plane", "OP", "Offset plane of end effector", GH_ParamAccess.item);
            pManager.AddPlaneParameter("Mounting Plane", "MP", "Mounting plane of the spherical axes", GH_ParamAccess.item);
            pManager.AddTextParameter("Message", "msg", "Message and description of machine", GH_ParamAccess.item);

        }

        private bool useDegree = true;  //vs use radian

        protected override void AppendAdditionalComponentMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            base.AppendAdditionalComponentMenuItems(menu);
            Menu_AppendItem(menu, "Use Degree", click, true, useDegree);
        }

        private void click(object Sender, EventArgs e)
        {
            useDegree = !useDegree;
            this.ExpireSolution(true);
        }
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string msg = "";
            Plane home = new Plane();
            Plane target = new Plane();
            Interval aRange = new Interval();
            Interval bRange = new Interval(); ;
            Interval cRange = new Interval(); ;
            int system = 0;
            //double r = 0;
            Vector3d po = new Vector3d();
            Vector3d mo = new Vector3d();
            DA.GetData<Plane>("Home Plane", ref home);
            DA.GetData<Plane>("Target Plane", ref target);
            DA.GetData<Interval>("A Range", ref aRange);
            DA.GetData<Interval>("B Range", ref bRange);
            DA.GetData<Interval>("C Range", ref cRange);
            DA.GetData<int>("Rotational System", ref system);
            DA.GetData<Vector3d>("Plane Offset", ref po);
            DA.GetData<Vector3d>("Mounting Offset", ref mo);

            if (useDegree)
            {
                aRange = new Interval(aRange.Min * 180 / Math.PI, aRange.Max * 180 / Math.PI);
                bRange = new Interval(bRange.Min * 180 / Math.PI, bRange.Max * 180 / Math.PI);
                cRange = new Interval(cRange.Min * 180 / Math.PI, cRange.Max * 180 / Math.PI);
            }

            s1 = new SphericalAxes(home, system, aRange, bRange, cRange, po, mo);
            s1.UpdateConfig(target);

            List<double> angles = s1.GetAnglesList(useDegree);

            DA.SetData("Spherical Axes", s1);
            DA.SetDataList("Rotational Input", angles);
            DA.SetDataList("Axes", s1.axes);
            DA.SetData("Offset Plane", s1.targetPlane);
            DA.SetData("Mounting Plane", s1.mountPlane);
            DA.SetData("Message", msg);
        }

        SphericalAxes s1;

        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            GH_Document document = this.OnPingDocument();
            Color defCol = document.PreviewColour;
            int lineThickness = 3;

            if (s1 == null) return;
            
            Vector3d os = s1.offsetVector();
            Transform tl = Transform.Translation(os);

            Rhino.Display.DisplayMaterial m = new Rhino.Display.DisplayMaterial(defCol, 0.8);

            Line a1 = new Line(s1.homePlane.Origin, s1.targetPlane.ZAxis * s1.offsetZ);
            args.Display.DrawLine(a1, defCol, lineThickness);

            Line a2 = new Line(s1.homePlane.Origin, s1.mountPlane.ZAxis * s1.offsetZmount);
            args.Display.DrawLine(a2, defCol, lineThickness);  

            args.Display.DrawPoint(s1.homePlane.Origin, defCol);
            args.Display.DrawPoint(s1.mountPlane.Origin, defCol);
        }
        protected override System.Drawing.Bitmap Icon => Seastar.Properties.Resources.createSPM;

        public override Guid ComponentGuid
        {
            get { return new Guid("6f274672-7e7f-4934-b1c5-d4f48dd4595b"); }
        }
    }


    public class ZXZPlane : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public ZXZPlane()
          : base("Plane from ZXZ", "ZXZPlane",
              "Construct a plane from local ZXZ rotation. Also known as Euler angle",
              "Seastar", "02 | Machine")
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPlaneParameter("Plane", "p", "Plane to rotate", GH_ParamAccess.item, new Plane(Point3d.Origin, Vector3d.ZAxis * -1));
            pManager.AddNumberParameter("Alpha", "a", "Alpha angle", GH_ParamAccess.item);
            pManager.AddNumberParameter("Beta", "b", "Beta angle", GH_ParamAccess.item);
            pManager.AddNumberParameter("Gamma", "g", "Gamma angle", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPlaneParameter("Plane", "P", "Output plane", GH_ParamAccess.item);
        }

        private Boolean useDegree = true;  //vs use radian
        protected override void AppendAdditionalComponentMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            base.AppendAdditionalComponentMenuItems(menu);
            Menu_AppendItem(menu, "Use Degree", click, true, useDegree);
        }

        private void click(object Sender, EventArgs e)
        {
            useDegree = !useDegree;
            this.ExpireSolution(true);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Plane pln = new Plane();
            double a = 0;
            double b = 0;
            double g = 0;
            DA.GetData<Plane>("Plane", ref pln);
            DA.GetData<double>("Alpha", ref a);
            DA.GetData<double>("Beta", ref b);
            DA.GetData<double>("Gamma", ref g);

            if (useDegree)
            {
                a *= Math.PI / 180;
                b *= Math.PI / 180;
                g *= Math.PI / 180;
            }

            DA.SetData(0, RotationAxes.RotateZXZ(pln, a,b, g));
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Seastar.Properties.Resources.planeZXZ2;

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("f86b8658-7e5b-4bcc-9d22-9d72442314a2"); }
        }
    }

    public class XYZPlane : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public XYZPlane()
          : base("Plane from Local Rotation", "RotatePlane",
              "Construct a plane from Local XYZ Rotation",
              "Seastar", "02 | Machine")
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPlaneParameter("Plane", "p", "Plane to rotate", GH_ParamAccess.item, new Plane(Point3d.Origin, Vector3d.ZAxis * -1));
            pManager.AddNumberParameter("X Rotation", "x", "X angle", GH_ParamAccess.item);
            pManager.AddNumberParameter("Y Rotation", "y", "Y angle", GH_ParamAccess.item);
            pManager.AddNumberParameter("Z Rotation", "z", "Z angle", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPlaneParameter("Plane", "P", "Output plane", GH_ParamAccess.item);
        }


        private Boolean useDegree = true;  //vs use radian
        protected override void AppendAdditionalComponentMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            base.AppendAdditionalComponentMenuItems(menu);
            Menu_AppendItem(menu, "Use Degree", click, true, useDegree);
        }

        private void click(object Sender, EventArgs e)
        {
            useDegree = !useDegree;
            this.ExpireSolution(true);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Plane pln = new Plane();
            double a = 0;
            double b = 0;
            double c = 0;
            DA.GetData<Plane>("Plane", ref pln);
            DA.GetData<double>("X Rotation", ref a);
            DA.GetData<double>("Y Rotation", ref b);
            DA.GetData<double>("Z Rotation", ref c);

            if (useDegree)
            {
                a *= Math.PI / 180;
                b *= Math.PI / 180;
                c *= Math.PI / 180;
            }

            DA.SetData(0, RotationAxes.RotateXYZ(pln, a, b, c));
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Seastar.Properties.Resources.planeLocal;

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("471906f0-6e30-4665-a886-8855df882bff"); }
        }
    }
}