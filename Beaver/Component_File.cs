using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using System.Resources;
using Seastar;
using System.IO;

namespace BeaverGrasshopper
{
    public class FileSaver : GH_Component
    {
        public FileSaver()
          : base("File Saver", "FileSave",
              "Save text file to a location and extension",
              "Seastar" , "04 | File")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            string path = "";
            try
            {
                path = this.OnPingDocument().FilePath;
            }
            catch
            {
                path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            }
            pManager.AddTextParameter("File Path", "path", "File path to save to", GH_ParamAccess.item, path);
            pManager.AddTextParameter("File Name", "name", "File name", GH_ParamAccess.item, "test");
            pManager.AddTextParameter("File Extension", "ext", "File extension", GH_ParamAccess.item, ".gcode");
            pManager.AddTextParameter("Lines of text", "lines", "Lines of text to write", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Overwrite file?", "overwrite", "True to overwrite file\nFalse to save incrementally", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Write file", "write", "True to write file\nConnect to button", GH_ParamAccess.item, false);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Message", "msg", "Message", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string loc = "";
            string name = "";
            string ext = "";
            List<string> lines = new List<string>();
            bool overwrite = false;
            bool write = false;
            DA.GetData<string>(0, ref loc);
            DA.GetData<string>(1, ref name);
            DA.GetData<string>(2, ref ext);
            DA.GetDataList<string>(3, lines);
            DA.GetData<bool>(4, ref overwrite);
            DA.GetData<bool>(5, ref write);

            string msg = Gcode.SaveFile(loc, name, ext, lines, overwrite, write);
            DA.SetData(0, msg);
        }

        protected override System.Drawing.Bitmap Icon => Seastar.Properties.Resources.fileSave;

        public override Guid ComponentGuid
        {
            get { return new Guid("61971140-2d9c-4b7d-ae73-7bbe95d87d30"); }
        }
    }

    public class FileDate : GH_Component
    {
        public FileDate()
          : base("File Name by Date", "FileDate",
              "File name in format of YYMMDD",
              "Seastar", "04 | File")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {

        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddIntegerParameter("Day", "day", "Today's day", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Month", "month", "Today's month", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Year", "year", "Today's year", GH_ParamAccess.item);
            pManager.AddTextParameter("Date Format", "date", "File name in YYMMDD format", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            int day = System.DateTime.Now.Day;
            int month = System.DateTime.Now.Month;
            int year = System.DateTime.Now.Year;
            DA.SetData(0, day);
            DA.SetData(1, month);
            DA.SetData(2, year);
            

            int z = day.ToString().Length;
            int u = month.ToString().Length;

            string dd = day.ToString();
            string mm = month.ToString();
            string yy = year.ToString().Remove(0, 2);  //from 2 0 1 7 to 17

            if (z == 1)
            {
                dd = "0" + day.ToString();
            }
            if (u == 1)
            {
                mm = "0" + month.ToString();
            }

            DA.SetData(3, yy + mm + dd);
        }

        protected override System.Drawing.Bitmap Icon => Seastar.Properties.Resources.fileDate;

        public override Guid ComponentGuid
        {
            get { return new Guid("bbb3d4cb-3857-48e4-914d-75f1a4ed0165"); }
        }
    }

    /*public class FileSaver : GH_Component
    {
        public FileSaver()
          : base("File Saver", "FileSave",
              "Save text file to a location and extension",
              "Seastar", "04 | File")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            
        }

        protected override System.Drawing.Bitmap Icon => Resources.joker;

        public override Guid ComponentGuid
        {
            get { return new Guid("bbb3d4cb-3857-48e4-914d-75f1a4ed0165"); }
        }
    }*/
}