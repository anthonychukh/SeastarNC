using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Melanchall.DryWetMidi.Smf;
using Melanchall.DryWetMidi.Smf.Interaction;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Beaver
{
    public class ReadMIDI : GH_Component
    {
        public ReadMIDI()
          : base("Read Midi", "Midi",
              "Convert midi file to text",
              "Beaver", "Midi")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("File Path", "F", "File path to midi text file", GH_ParamAccess.item);
           // pManager.AddIntegerParameter("Read Channel")
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            //pManager.AddTextParameter("Gcode", "G", "Gcode", GH_ParamAccess.list);
            //pManager.AddTextParameter("Channel", "C", "Channel", GH_ParamAccess.list);
            //pManager.AddTextParameter("Channel", "CC", "allChannel", GH_ParamAccess.tree);
            //pManager.AddTextParameter("Time", "T", "Unque Times", GH_ParamAccess.tree);

            pManager.AddTextParameter("Channel", "CC", "All channel as string.\nEach line stands for [channel] [note] [starting time] [duration]", GH_ParamAccess.tree);
            pManager.AddTextParameter("Channel", "Ch", "Channel number", GH_ParamAccess.tree);
            pManager.AddTextParameter("Note", "Nt", "Note", GH_ParamAccess.tree);
            pManager.AddTextParameter("Starting time", "St", "Starting time in millisecond", GH_ParamAccess.tree);
            pManager.AddTextParameter("Duration", "Dr", "Duration in millisecond", GH_ParamAccess.tree);
            pManager.AddTextParameter("All strings", "S", "All channel as one list", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string filePath = "";
            DA.GetData<string>(0, ref filePath);
            filePath = filePath.Replace("\"", ""); //remove all " in file name

            if (!System.IO.File.Exists(filePath))
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "File path does not exist");
                return;
            }
            
            ReadingSettings setting = null;
            MidiFile m = MidiFile.Read(filePath, setting);
            //IEnumerable<TrackChunk> trkC =  m.GetTrackChunks();
            
            
            List<string> noteStrings = m.GetNotes().Select(n => $"{n.Channel} {n.NoteNumber} {n.Time} {n.Length} ").ToList(); // single list with all string
            
            List<Note> noteList = m.GetNotes().ToList();
            GH_Structure<GH_String> allStrings = MidiExtension.NoteInfo(noteList); //each channel separated into different branch
            GH_Structure<GH_String> stringsCh = MidiExtension.NoteInfo(noteList, Info.channel);
            GH_Structure<GH_String> stringsNt = MidiExtension.NoteInfo(noteList, Info.note);
            GH_Structure<GH_String> stringsSt = MidiExtension.NoteInfo(noteList, Info.time);
            GH_Structure<GH_String> stringsDr = MidiExtension.NoteInfo(noteList, Info.duration);

            //DataTree<Note> noteTree = MidiExtension.ChannelTree(noteList); //each channel separated into different branch
            //List<long> uniqueTimes = MidiExtension.AllUniqueTime(noteList);

            //int stepSize = 20; //each step is 20ms 
            //int channelCount = 6; //output first 6 channels
            //GH_Structure<GH_Integer> channelTimeStep = MidiExtension.TimeStep(noteTree, stepSize, channelCount);


            DA.SetDataTree(0, allStrings);
            DA.SetDataTree(1, stringsCh);
            DA.SetDataTree(2, stringsNt);
            DA.SetDataTree(3, stringsSt);
            DA.SetDataTree(4, stringsDr);
            DA.SetDataList(5, noteStrings);

            //DA.SetDataList(0, noteStrings);
            //DA.SetDataList(1, channelStrings.Branches[0]);
            //DA.SetDataTree(2, channelTimeStep);
            //DA.SetDataTree(3, channelStrings);

        }

        protected override System.Drawing.Bitmap Icon => Resources.joker;

        public override Guid ComponentGuid
        {
            get { return new Guid("d61fdbe8-a196-4fef-9c8d-51560f9f8f94"); }
        }
    }

    public class DeltaJogCarriage : GH_Component
    {
        public DeltaJogCarriage()
          : base("Delta Jog Carriage", "JogCarriage",
              "Approximate joint jogging in delta",
              "Beaver", "Midi")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("X Carriage", "X", "Absolute position of X Carriage", GH_ParamAccess.list);
            pManager.AddNumberParameter("Y Carriage", "Y", "Absolute position of Y Carriage", GH_ParamAccess.list);
            pManager.AddNumberParameter("Z Carriage", "Z", "Absolute position of Z Carriage", GH_ParamAccess.list);
            //pManager.AddNumberParameter("Feed rate", "F", "Feed rate to get to this position", GH_ParamAccess.list);
            pManager.AddGenericParameter("Delta Machine", "M", "Delta Machine Object\nConnect to machine component", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Path", "P", "Path that represent carriage jogging", GH_ParamAccess.item);
            pManager.AddPointParameter("ptout", "pt", "pointout", GH_ParamAccess.list);
           
            pManager.AddTextParameter("Message", "msg", "Message", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string msg = "";
            Config delta = new Config();
            DA.GetData<Config>("Delta Machine", ref delta);

            //stop if isnt delta machine ............................................................
            if(delta.Machine.IsDelta && delta.Machine.dCol[0].IsValid) { } 
            else {
                msg += "Input machine is not a delta machine or setting is incomplete";
                DA.SetData("Message", msg);
                return;
            }

            List<double> xList = new List<double>();
            List<double> yList = new List<double>();
            List<double> zList = new List<double>();
            DA.GetDataList<double>(0, xList);
            DA.GetDataList<double>(1, yList);
            DA.GetDataList<double>(2, zList);

            List<int> counts = new int[3] { xList.Count, yList.Count, zList.Count }.ToList<int>();
            counts.Sort();
            counts.Reverse();
            int LCount = counts[0];

            xList = Extension.SetListLength<double>(xList, LCount, xList[xList.Count - 1]);
            yList = Extension.SetListLength<double>(yList, LCount, yList[yList.Count - 1]);
            zList = Extension.SetListLength<double>(zList, LCount, zList[zList.Count - 1]);
            Path tpth = new Path();
            Point3d lastPoint = delta.Machine.parkPos;
            double stepSize = 20; //stepsize in ms
            stepSize = stepSize / (60 * 1000); //step size in min

            List<Point3d> pts = new List<Point3d>();  //WIP have to switch to speed not abs pos
            for(int i = 0; i< LCount-1; i++)
            {
                double[] dz = new double[3] { xList[i], yList[i], zList[i] };
                Point3d cPos = delta.Machine.DeltaForwardKinematics(dz);
               // Point3d[] ptemp = delta.dCol;
                pts.Add(cPos);
                double dist = cPos.DistanceTo(lastPoint);
                //double f = dist / stepSize;
                double f = 1200;
                Block blk = new Block(cPos, Plane.WorldXY, f, 0);
                tpth.Add(blk);
                lastPoint = cPos;
            }

            DA.SetData(0, tpth);
            DA.SetDataList(1, pts);
        }

        protected override System.Drawing.Bitmap Icon => Resources.joker;

        public override Guid ComponentGuid
        {
            get { return new Guid("b9d78880-f380-41d3-bf00-0b7d1f4d3be8"); }
        }
    }
}
