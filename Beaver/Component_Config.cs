using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Seastar;
using Grasshopper.Kernel.Data;
using Grasshopper;
using System.Linq;
using System.Resources;
using System.Drawing;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace SeastarGrasshopper
{
    public class ConfigINI : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public ConfigINI()
          : base(".ini Configuration File", "ConfigINI",
              "Open slic3r configuration bundle from .ini file and select configuration",
              "Seastar", "01 | Config")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter(".ini File Path", "ini", "File path to .ini configuration file", 0);
            pManager.AddIntegerParameter("Print Setting", "ps", "List of print setting", 0, 0);
            pManager.AddIntegerParameter("Filament Setting", "fs", "List of filament setting", 0, 0);
            pManager.AddIntegerParameter("Printer Setting", "prs", "List of printer setting", 0, 0);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Print Setting", "ps", "Print setting", GH_ParamAccess.list);
            pManager.AddTextParameter("Filament Setting", "fs", "Filament setting", GH_ParamAccess.list);
            pManager.AddTextParameter("Printer Setting", "prs", "Printer setting", GH_ParamAccess.list);
            pManager.AddTextParameter("Configuration", "config", "Configuration", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string filePath = "";
            
            

            
            if (this.Params.Input[0].SourceCount == 0)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Input .ini file path");
                return;
            }
            else
            {
                DA.GetData<string>(0, ref filePath);
            }

            if (!System.IO.File.Exists(filePath))
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "File path does not exist");
                return;
            }
            string[] lines = System.IO.File.ReadAllLines(filePath);


            List<string> test = new List<string>();
            List<string> FSList = new List<string>();
            List<string> PSList = new List<string>();
            List<string> PrSList = new List<string>();

            DataTree<string> FSTree = new DataTree<string>();  //filament setting
            DataTree<string> PSTree = new DataTree<string>();  //print setting
            DataTree<string> PrSTree = new DataTree<string>(); //printer setting

            int FSpCount = -1;
            int PSpCount = -1;
            int PrSpCount = -1;

            bool recordFS = false;
            bool recordPS = false;
            bool recordPrS = false;

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("[filament:"))
                {
                    recordFS = true;
                    FSpCount += 1;
                    FSList.Add(lines[i]);
                }

                if (lines[i].Contains("[print:"))
                {
                    recordPS = true;
                    PSpCount += 1;
                    PSList.Add(lines[i]);
                }
                if (lines[i].Contains("[printer:"))
                {
                    recordPrS = true;
                    PrSpCount += 1;
                    PrSList.Add(lines[i]);
                }

                if (lines[i].Length == 0)
                {
                    recordFS = false;
                    recordPS = false;
                    recordPrS = false;
                }

                if (recordFS)
                {
                    GH_Path FSpth = new GH_Path(FSpCount);
                    FSTree.Add(lines[i], FSpth);
                }

                if (recordPS)
                {
                    GH_Path PSpth = new GH_Path(PSpCount);
                    PSTree.Add(lines[i], PSpth);
                }

                if (recordPrS)
                {
                    GH_Path PrSpth = new GH_Path(PrSpCount);
                    PrSTree.Add(lines[i], PrSpth);
                }

                
            } //sort different setting into catergory

            int fs = 0;
            int ps = 0;
            int prs = 0;

            DA.GetData<int>(1, ref fs);
            DA.GetData<int>(2, ref ps);
            DA.GetData<int>(3, ref prs);
            
            List<string> cc = new List<string>();
            cc.AddRange(FSTree.Branch(fs));
            cc.AddRange(PSTree.Branch(ps));
            cc.AddRange(PrSTree.Branch(prs));

            DA.SetDataList(0, FSTree.Branch(fs));
            DA.SetDataList(1, PSTree.Branch(fs));
            DA.SetDataList(2, PrSTree.Branch(fs));
            DA.SetDataList(3, cc.ToList());
            
            Extension.DropDown(1, "print setting:", PSList, this);
            Extension.DropDown(2, "filament setting:", FSList, this);
            Extension.DropDown(3, "printer setting:", PrSList, this);

            this.AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "finished component");
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        /*protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                object @object = ResourceManager.GetObject("joker");
                return (Bitmap)@object;
                //return null;
            }
        }*/

        protected override Bitmap Icon => Seastar.Properties.Resources.cfgOpen;

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("11aa9d22-044f-4047-aeb4-8be075d510c5"); }
        }
    }

    public class ConfigPrint : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public ConfigPrint()
          : base("Print Configuration", "ConfigPrint",
              "Set Print Configuration from .ini and/or sliders",
              "Seastar", "01 | Config")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            for (int i = 0; i < Enum.GetNames(typeof(Config.PrintSetting)).Length; i++)
            {
                string name = Enum.GetName(typeof(Config.PrintSetting), i);
                //string nn = Enum.GetName()
                pManager.AddNumberParameter(name, name, name, GH_ParamAccess.item, -1);
            }
            pManager.AddTextParameter("Configuration", "config", "Configuration inherited from previous", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Printer", "printer", "Seastar Printer Object", 0);
            pManager.AddTextParameter("Configuration", "config", "Modified configuration string", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //update config
            List<string> cfgS = new List<string>();
            DA.GetDataList<string>("Configuration", cfgS);
            Config cfg = new Config(cfgS);

            Config.CreateSlider(cfg, this, false); //put config input data onto sliders

            List<string> cfgOut = new List<string>();
            for (int i = 0; i < this.Params.Input.Count - 1; i++)
            {
                double val = 0;
                if (DA.GetData<double>(i, ref val))
                {
                    DA.GetData<double>(i, ref val);
                    string s = this.Params.Input[i].Name + " = " + val.ToString();
                    cfgOut.Add(s);
                }
                else
                {
                    string s = this.Params.Input[i].Name + " = ";
                    cfgOut.Add(s);
                }
            } 

            DA.SetDataList("Configuration", cfgOut);
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Seastar.Properties.Resources.configExtruder;

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("528f1154-da9a-4574-bcf0-8ed9e4178d30"); }
        }
    }

    public class ConfigFilament : GH_Component
    {
        public ConfigFilament()
          : base("Filament Configuration", "ConfigFilament",
              "Set filament configuration from .ini and/or sliders",
              "Seastar", "01 | Config")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            for (int i = 0; i < Enum.GetNames(typeof(Config.FilamentSetting)).Length; i++)
            {
                string name = Enum.GetName(typeof(Config.FilamentSetting), i);
                
                pManager.AddNumberParameter(name, name, name, GH_ParamAccess.item, 0);
            }
            pManager.AddTextParameter("Configuration", "config", "Configuration inherited from previous", GH_ParamAccess.list);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Configuration", "config", "Modified configuration string", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //update config
            List<string> cfgS = new List<string>();
            DA.GetDataList<string>("Configuration", cfgS);
            Config cfg = new Config(cfgS);

            Config.CreateSlider(cfg, this, false);

            List<string> cfgOut = new List<string>();
            for (int i = 0; i < this.Params.Input.Count - 1; i++)
            {
                double val = 0;
                DA.GetData<double>(i, ref val);
                string s = this.Params.Input[i].Name + " = " + val.ToString();
                cfgOut.Add(s);
            }

            DA.SetDataList("Configuration", cfgOut);
        }

        protected override System.Drawing.Bitmap Icon => Seastar.Properties.Resources.cfgFila;

        public override Guid ComponentGuid
        {
            get { return new Guid("a17a60af-4e42-4338-b3ef-94d22efa150d"); }
        }
    }

    public class ConfigPrinter : GH_Component
    {
        public ConfigPrinter()
          : base("Printer Configuration", "ConfigPrinter",
              "Set Printer configuration from .ini and/or sliders",
              "Seastar", "01 | Config")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            for (int i = 0; i < Enum.GetNames(typeof(Config.PrinterSetting)).Length; i++)
            {
                string name = Enum.GetName(typeof(Config.PrinterSetting), i);
                pManager.AddNumberParameter(name, name, name, GH_ParamAccess.item, 0);
            }
            pManager.AddTextParameter("Configuration", "config", "Configuration inherited from previous", GH_ParamAccess.list);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Configuration", "config", "Modified configuration string", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //update config
            List<string> cfgS = new List<string>();
            DA.GetDataList<string>("Configuration", cfgS);
            Config cfg = new Config(cfgS);

            Config.CreateSlider(cfg, this, false);

            List<string> cfgOut = new List<string>();
            for (int i = 0; i < this.Params.Input.Count - 1; i++)
            {
                double val = 0;
                DA.GetData<double>(i, ref val);
                string s = this.Params.Input[i].Name + " = " + val.ToString();
                cfgOut.Add(s);
            }

            DA.SetDataList("Configuration", cfgOut);
        }

        protected override System.Drawing.Bitmap Icon => Seastar.Properties.Resources.cfgMac;

        public override Guid ComponentGuid
        {
            get { return new Guid("200a8e21-fe06-4e73-bbe3-00340d395253"); }
        }
    }

    public class ConfigRouter : GH_Component
    {
        public ConfigRouter()
          : base("Router Configuration", "ConfigRouter",
              "Set Router configuration from .ini and/or sliders",
              "Seastar", "01 | Config")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            for (int i = 0; i < Enum.GetNames(typeof(Config.RouterSetting)).Length; i++)
            {
                string name = Enum.GetName(typeof(Config.RouterSetting), i);
                pManager.AddNumberParameter(name, name, name, GH_ParamAccess.item, 0);
            }
            pManager.AddTextParameter("Configuration", "config", "Configuration inherited from previous", GH_ParamAccess.list);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Configuration", "config", "Modified configuration string", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //update config
            List<string> cfgS = new List<string>();
            DA.GetDataList<string>("Configuration", cfgS);
            Config cfg = new Config(cfgS);

            Config.CreateSlider(cfg, this, false);

            List<string> cfgOut = new List<string>();
            for (int i = 0; i < this.Params.Input.Count - 1; i++)
            {
                double val = 0;
                DA.GetData<double>(i, ref val);
                string s = this.Params.Input[i].Name + " = " + val.ToString();
                cfgOut.Add(s);
            }

            DA.SetDataList("Configuration", cfgOut);
        }

        protected override System.Drawing.Bitmap Icon => Seastar.Properties.Resources.cfgMill;

        public override Guid ComponentGuid
        {
            get { return new Guid("30c95c77-8a4d-42eb-8226-bce882c902d8"); }
        }
    }

    public class ConfigGetSetting : GH_Component
    {
        public ConfigGetSetting()
          : base("Get Setting", "ConfigGetSetting",
              "Retreive a specific setting from config",
              "Seastar", "01 | Config")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Configuration", "config", "config to retreive setting from", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Setting Index", "i", "Index of setting to retreive", GH_ParamAccess.item, 0);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Name", "n", "Name of setting", GH_ParamAccess.item);
            pManager.AddTextParameter("Setting", "s", "Value of setting", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<string> config = new List<string>();
            DA.GetDataList<string>("Configuration", config);
            //Config cfg = new Config(cfgS);

            List<string> configNames = new List<string>();
            List<string> configVal = new List<string>();

            for (int i = config.Count - 1; i >= 0; i--)
            {
                if (config[i].Contains("[") && config[i].Contains("]"))
                {
                    config.RemoveAt(i);
                }
                if (config[i].Contains("="))
                {
                    configNames.Add(Config.GetSettingName(config[i]));
                    configVal.Add(Config.GetSettingValue(config[i]));
                }
            }

            configVal.Reverse();
            configNames.Reverse();
            Extension.DropDown(1, "setting", configNames, this);

            int k = 0;
            DA.GetData<int>(1, ref k);
            DA.SetData(0, configNames[k]);
            DA.SetData(1, configVal[k]);
        }

        protected override System.Drawing.Bitmap Icon => Resources.joker;

        public override Guid ComponentGuid
        {
            get { return new Guid("ab8a9b02-b526-4b01-95ae-97d74d5877b6"); }
        }
    }

    public class ConfigSaveINI : GH_Component
    {
        public ConfigSaveINI()
          : base("Save Configuration", "ConfigSaveINI",
              "Save configuration to .ini file",
              "Seastar", "01 | Config")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Configuration", "config", "Configuration to save", GH_ParamAccess.list);
            pManager.AddTextParameter("File Path", "path", "File path to save to", GH_ParamAccess.item);
            pManager.AddTextParameter("Name", "name", "File name", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Overwrite?", "overwrite", "True to overwrite file\nFalse to save incrementally", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Write", "write", "True to write file\nConnect to button", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Message", "msg", "Message", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string loc = "";
            string name = "";
            string ext = "ini";
            List<string> lines = new List<string>();
            bool overwrite = false;
            bool write = false;

            DA.GetData<string>(1, ref loc);
            DA.GetData<string>(2, ref name);
            DA.GetDataList<string>(0, lines);
            DA.GetData<bool>(3, ref overwrite);
            DA.GetData<bool>(4, ref write);

            string msg = Gcode.SaveFile(loc, name, ext, lines, overwrite, write);
            DA.SetData(0, msg);
        }

        protected override System.Drawing.Bitmap Icon => Seastar.Properties.Resources.cfgSave;

        public override Guid ComponentGuid
        {
            get { return new Guid("9dfa61ad-eccc-46ef-9f28-e45507def6bf"); }
        }
    }

}
