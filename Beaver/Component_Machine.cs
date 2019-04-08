using System;
using System.Collections.Generic;
using Beaver;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace BeaverGrasshopper
{
    public class MachineCreate : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public MachineCreate()
          : base("Create Machine", "MachineCreate",
              "Create the machine and its settings for this operation\nA machine consist of its dimensions and a list of tools",
              "Beaver", "Machine")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Machine Name", "n", "Name of the machine", GH_ParamAccess.item, "");
            pManager.AddIntervalParameter("Size X", "x", "X dimension of machine\n Or radius if you have a cylindical/delta machine", GH_ParamAccess.item);
            pManager.AddIntervalParameter("Size Y", "y", "Y dimension of machine\nIgnore this input if you have a cylindical/delta machine\nSize X will be used as radius of machine", GH_ParamAccess.item);
            pManager.AddIntervalParameter("Size Z", "z", "Z dimension of machine or height", GH_ParamAccess.item);
            pManager.AddGenericParameter("Tool", "t", "List of tools the machine can use", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Machine", "M", "Beaver Machine", GH_ParamAccess.item);
            pManager.AddBrepParameter("Volume", "V", "Machine working volume", GH_ParamAccess.item);
            pManager.AddTextParameter("Message", "msg", "Message and description of machine", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string name = "";
            Interval x = new Interval();
            Interval y = new Interval();
            Interval z = new Interval();
            List<Tool> tools = new List<Tool>();
            DA.GetData<string>(0, ref name);
            DA.GetData<Interval>(1, ref x);
            DA.GetData<Interval>(3, ref z);

            Machine mOut;
            string msg = "";

            if(DA.GetData<Interval>(1, ref y))
            {
                DA.GetData<Interval>(1, ref y);
                mOut = new Machine(x, y, z, tools);
                
            }
            else
            {
                mOut = new Machine(x, z, tools); 
            }

            msg += "A Beaver Machine was successfully created\n";
            msg += mOut.ToString();
            DA.SetData(0, mOut);
            DA.SetData(1, mOut.Volume);
            DA.SetData(2, msg);

        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return null;
            }
        }

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
              "Beaver", "Machine")
        {
        }

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
            pManager.AddGenericParameter("Tool", "T", "Beaver Tool", GH_ParamAccess.item);
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
            msg += "A Beaver Tool is successfully created\n";
            msg += t.ToString();

            DA.SetData(0, t);
            DA.SetData(1, msg);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return null;
            }
        }

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
              "Beaver", "Machine")
        {
        }

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
            pManager.AddGenericParameter("Tool", "T", "Beaver Tool", GH_ParamAccess.item);
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
                msg += "A Beaver Tool is successfully created\n";
                msg += t.ToString();
                t.ToolChange = tc;
                DA.SetData(0, t);
                DA.SetData(1, msg);
            }
            if (type == 1) //is ball end
            {
                t = new Tool(name, ToolType.mill, d, ox, oy, oz, ToolShape.ball, F, S, pr, rr);
                msg += "A Beaver Tool is successfully created\n";
                msg += t.ToString();
                t.ToolChange = tc;
                DA.SetData(0, t);
                DA.SetData(1, msg);
            }
            

        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("efc332ae-5d0b-4948-b4c1-9d1fdad49194"); }
        }
    }
}