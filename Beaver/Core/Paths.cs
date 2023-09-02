
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Resources;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Melanchall.DryWetMidi.Smf.Interaction;
using Rhino.Geometry;
using static System.Math;


/*
 * READ ME
 * 
 * Seastar, formerly known as Beaver, is a library written for Rhino Common and Grasshopper for 3d printer control
 * It support real-time connection between Grasshopper and open-source 3D printing firmware
 * This project was made possible by the generous support of SPACE10 through a research resident program in summer 2019
 * 
 * 
 * -------------------Disclaimer--------------------------
 * This library and Grasshopper plugin was created out of the good intension of education and promotion of technology.  
 * User should take their own caution and risk when using this library.  
 * The creator(s) and contributors of this library do not provide any garantee or waranty to the use of this library and/or 
 * digital tool derives from this library.  
 * 
 * --------------------CAUTION---------------------------
 * Working with electronic devices could be dangerous.  
 * Always take precaution accordingly and make sure you are well trained and informed to work on the specific system.  
 * Failure to operate your machine correctly could result in hardware damage and/or serious injuries.  
 * 
*/

namespace Seastar.Core
{
    //public class Empty : GH_Component
    //{
    //    public Empty()
    //      : base("Insert Paths", "PathInsert",
    //          "Insert Path and command at specific index",
    //          "Seastar", "Path")
    //    {
    //    }

    //    protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
    //    {

    //    }

    //    protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
    //    {

    //    }

    //    protected override void SolveInstance(IGH_DataAccess DA)
    //    {

    //    }

    //    protected override System.Drawing.Bitmap Icon => Resources.joker;

    //    public override Guid ComponentGuid
    //    {
    //        get { return new Guid("b51535a9-ef20-4670-9e98-22b7c8d9daaf"); }
    //    }
    //}

    public interface IBlock
    {
        /// <summary> Move to location. </summary>
        Plane Plane { get; set; }
        /// <summary> Feed rate to move to location. </summary>
        double FeedRate { get; set; }
    }

    public class LinearMoveBlock : IBlock
    {
        public Plane Plane { get; set; }
        public double FeedRate { get; set; }

    }

    public class ExtrudeBlock : IBlock
    {
        public Plane Plane { get; set; }
        public double FeedRate { get; set; }
        public Extruder Tool { get; set; }
        public double extrusionRate { get; set; }
    }

    public class MBlock : IBlock
    {
        public Plane Plane { get; set; }
        public double FeedRate { get; set; }
        public int M { get; set; }
        public int? P { get; set; }
        public int? S { get; set; }

    }

    /// <summary>
    /// Contains location and speed, etc of gcode point ie each line of code.
    /// </summary>
    public class Block
    {
        public Point3d? coordinate;
        //public Vector3d? direction;  //six axis milling
        public Plane? orientation; //six axis
        public Arc? arc;
        public double? F;            //Feed Rate mm/min
        public double? ER;        //Extrusion Rate mm3/min, can only calculate E from ERate between 2 points OR Speed if spindle
        public int? SS; //Spindle speed
        public int? G;
        public int? T;  //tool to use TODO use ITool interface
        public List<int?> M = new List<int?>();  //List of M-command. P, S, Pin and PWM shoud have same length otherwise Null value
        public List<int?> P = new List<int?>();
        public List<int?> S = new List<int?>();

        public string comment = "";

        public Block()
        {

        }

        /// <summary>
        /// Rapid movement G0 command. No action
        /// </summary>
        public Block(Point3d _location, Plane _orientation, double _feedRate, int _toolIndex) //...........waypoint rapid move 
        {
            G = 0;
            coordinate = _location;
            Plane pln = new Plane(_orientation);
            pln.Origin = _location;
            orientation = pln;

            F = _feedRate;
            T = _toolIndex;
        }

        /// <summary>
        /// 3D printing linear move
        /// </summary>
        public Block(Point3d _location, double _feedRate, double? _extrusionRate, int _toolIndex) //...........waypoint 
        {
            G = 1;
            coordinate = _location;
            F = _feedRate;
            ER = _extrusionRate;
            T = _toolIndex;
        }

        //public Block(Point3d _location, Vector3d _direction, double _feedRate, double? _extrusionRate, int _toolIndex) //........6D waypoint
        //{
        //    G = 1;
        //    coordinate = _location;
        //    direction = _direction;
        //    F = _feedRate;
        //    ES = _extrusionRate;
        //    T = _toolIndex;
        //}


        /// <summary>
        /// generic wayPoint for printing and milling
        /// </summary>
        public Block(Point3d? _location, Plane? _orientation, double? _feedRate, double? _ES, int _toolIndex) //generic wayPoint for printing and milling
        {
            if (_feedRate == null)
            {
                Console.WriteLine("must provide feed rate for way point");
                return;
            }
            if (_ES == null)
            {
                G = (int)Command.GList.RapidPosition;
            }
            else
            {
                G = (int)Command.GList.LinearPosition;
                ER = _ES;
            }

            coordinate = _location;

            if (_orientation.HasValue)
            {
                Plane pln = new Plane(_orientation.Value);
                pln.Origin = _location.Value;
                orientation = pln;
            }

            F = _feedRate;
            T = _toolIndex;
        }



        public Block(Point3d? _centre, Arc _arc, double _feedRate, double _ES, int _toolIndex) //..........arc move
        {
            coordinate = _centre;
            arc = _arc;
            F = _feedRate;
            ER = _ES;
            T = _toolIndex;
        }

        /// <summary>
        /// Create new M command block 
        /// </summary>
        /// <param name="_M">M command number</param>
        /// <param name="_P">P values</param>
        /// <param name="_S">S values</param>
        public Block(int? _M, int? _P, int? _S)
        {
            List<int?> mm = new List<int?>();
            List<int?> pp = new List<int?>();
            List<int?> ss = new List<int?>();

            mm.Add(_M);
            pp.Add(_P);
            ss.Add(_S);
            M = mm;
            S = ss;
            P = pp;
        }

        /// <summary>
        /// Create new M command block 
        /// </summary>
        /// <param name="_M">M command number as list</param>
        /// <param name="_S">S values as list. Rmb not all M command need S</param>
        /// <param name="_P">P values as list. Rmb not all M command need P</param>
        public Block(List<int?> _M, List<int?> _P, List<int?> _S)  //..................command block
        {
            if (_M.Count != _S.Count || _M.Count != _P.Count)
            {
                Console.WriteLine("Block(List<int?> _M, List<int?> _S, List<int?> _P), all list should be same length");
                return;
            }
            M = _M;
            S = _S;
            P = _P;
        }

        /// <summary>
        /// Append new M command to this block
        /// </summary>
        /// <param name="_M">M command number</param>
        /// <param name="_S">S value. Rmb not all M command need S</param>
        /// <param name="_P">P value. Rmb not all M command need P</param>
        public void AddM(int _M, int? _S, int? _P)
        {
            this.M.Add(_M);
            this.S.Add(_S);
            this.P.Add(_P);
        }

        public void Overlap(Block _block, bool _overwrite)
        {
            if (!this.G.HasValue || _overwrite && _block.G.HasValue)
            {
                this.G = _block.G;
            }
            if (!this.coordinate.HasValue || _overwrite && _block.coordinate.HasValue)
            {
                this.coordinate = _block.coordinate;
            }
            if (!this.orientation.HasValue || _overwrite && _block.orientation.HasValue)
            {
                this.orientation = _block.orientation;
            }
            if (!this.arc.HasValue || _overwrite && _block.arc.HasValue)
            {
                this.arc = _block.arc;
            }
            if (!this.F.HasValue || _overwrite && _block.F.HasValue)
            {
                this.F = _block.F;
            }
            if (!this.ER.HasValue || _overwrite && _block.ER.HasValue)
            {
                this.ER = _block.ER;
            }
            if (!this.T.HasValue || _overwrite && _block.T.HasValue)
            {
                this.T = _block.T;
            }
            M.AddRange(_block.M);
            P.AddRange(_block.P);
            S.AddRange(_block.S);
        }
        /// <summary>
        /// Convert block into Gcode
        /// </summary>
        /// <param name="psc">precision of coordinate</param>
        /// <returns></returns>
        public string ToGCode(int _psc)
        {
            string text = "";
            string psc = "F" + _psc.ToString();

            if (this.G.HasValue)
            {
                text += "G";
                text += this.G.Value.ToString();
                text += " ";
            }
            if (this.coordinate.HasValue)
            {
                text += "X";
                text += this.coordinate.Value.X.ToString(psc);
                text += " Y";
                text += this.coordinate.Value.Y.ToString(psc);
                text += " Z";
                text += this.coordinate.Value.Z.ToString(psc);
                text += " ";
            }
            if (this.orientation.HasValue)
            {
                //text += "A";
                //text += this.orientation.Value.
                //text += " B";
                //text += this.coordinate.Value.Y.ToString();
                //text += " C";
                //text += this.coordinate.Value.Z.ToString();
                //text += " ";
            }
            if (this.F.HasValue)
            {
                text += "F";
                text += this.F.Value.ToString("F0");
                text += " ";
            }
            if (this.ER.HasValue)
            {
                text += "ExtrusionRate";
                text += this.ER.Value.ToString();
                text += " ";
            }
            if (this.SS.HasValue)
            {
                text += "SpindleSpeed";
                text += this.SS.Value.ToString();
                text += " ";
            }

            if (M.Count > 0)
            {
                for (int i = 0; i < M.Count; i++)
                {
                    if (this.M[i].HasValue)
                    {
                        if (M[i].Value >= 0)
                        {
                            text += "M";
                            text += this.M[i].Value.ToString();
                        }
                        else
                        {
                            text += "G";
                            text += (this.M[i].Value * -1).ToString();
                        }

                        text += " ";
                    }
                    if (this.P[i].HasValue)
                    {
                        text += "P";
                        text += this.P[i].Value.ToString();
                        text += " ";
                    }
                    if (this.S[i].HasValue)
                    {
                        text += "S";
                        text += this.S[i].Value.ToString();
                        text += " ";
                    }
                    text += "\n";
                }
            }

            //if (this.T.HasValue)
            //{
            //    text += "T";
            //    text += this.T.Value.ToString();
            //    text += " ";
            //}
            return text;
        }
    }


    public class Command
    {
        public enum GList   //supported G format. 0 denote available input, p/s for optional PS, P/S for required
        {
            RapidPosition = 0,
            LinearPosition = 1,
            CircularInterpolationCW = 2,
            CircularInterpolationCCW = 3,
            Dwell = 4,
            HPCC = 5,
            ExactStop = 9,
            XYPlane = 17,
            ZXPlane = 18,
            YZPlane = 19,
            Empirical = 20,
            Metric = 21,
            Home = 28,
            SecondaryHome = 30,
            PositionRegister = 92

        }

        public enum MList   //supported M format. 0 denote available input, p/s for optional PS, P/S for required
        {
            Dwell0P = -4,
            Unconditional_Stop = 0,
            Sleep = 1,
            Program_End = 2,
            Spindle_On_CW = 3,
            Spindle_On_CCW = 4,
            Spindle_Off = 5,
            Auto_Tool_Change = 6,
            Coolant_On_Mist = 7,
            Coolant_On_Flood = 8,
            Coolant_Off = 9,
            Vacuum_On = 10,
            Vacuum_Off = 11,
            Enable_all_stepper_motor = 17,
            Diable_all_stepper_motor = 18,
            List_SD_card0ps = 20,
            Initialize_SD_card0p = 21,
            Switch_IO_pin0PS = 22
        }

        public string StartingGcode =
            "G21 ; Use Millimeter\n"+
            "G28 ; Home\n";

        public string EndingGcode =
            "M104 S0 ; turn off all temperature\n" +
            "M140 S0 ; turn off bed\n" +
            "G28 ; Home all\n" +
            "M84 ; Disable all motor";
       

        /// <summary>
        /// Check if this M command supports or requires P input.
        /// 0 for not support, 1 for optional, 2 for required
        /// </summary>
        /// <param name="_MCommand">M command to check</param>
        /// <returns></returns>
        public static int SupportP(int _MCommand)
        {
            string name = Enum.GetName(typeof(Command.MList), _MCommand);
            string[] nameSplit = name.Split('0');

            if (name.Contains('0') && nameSplit[1].Contains("p"))
            {
                return 1;
            }
            if (name.Contains('0') && nameSplit[1].Contains("P"))
            {
                return 2;
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// Check if this M command supports or requires S input.
        /// 0 for not support, 1 for optional, 2 for required
        /// </summary>
        /// <param name="_MCommand">M command to check</param>
        /// <returns></returns>
        public static int SupportS(int _MCommand)
        {
            string name = Enum.GetName(typeof(Command.MList), _MCommand);
            string[] nameSplit = name.Split('0');

            if (name.Contains('0') &&  nameSplit[1].Contains("s"))
            {
                return 1;
            }
            if (name.Contains('0') &&  nameSplit[1].Contains("S"))
            {
                return 2;
            }
            else
            {
                return 0;
            }
        }

        public static List<string> GetNames(Type _type)
        {
            List<string> names = new List<string>();
            Array ar = Enum.GetNames(_type);
            for(int i = 0; i < ar.Length; i++)
            {
                string n = (string)ar.GetValue(i);
                if (n.Contains('0'))
                {
                    string[] sn = n.Split('0');
                    names.Add(sn[0]);
                }
                else
                {
                    names.Add(n);
                }
                //names.Add((string)ar.GetValue(i));
            }
            return names;
        }

        public static List<string> GetValues(Type _type)
        {
            List<string> values = new List<string>();                   //M command values or 
            //Array ar = Enum.GetValues(_type);

            ////Array arr = Enum.GetValues
            //for (int i = 0; i < ar.Length; i++)
            //{
            //    string n = ar.GetValue(i).ToString();
            //    values.Add(n);
            //}

            foreach(int i in Enum.GetValues(_type))
            {
                values.Add(i.ToString());
            }
            return values;
        }

    }
    
    //public class Travel
    //{
    //    public 
    //}

    public class Path  //contain list of Blocks ===========================================================================================
    {
        public List<Block> blocks = new List<Block>(); //...default empty value
        public int? startCoori;
        public int? endCoori;
        public Color DefaultColor = Color.Red;
        public Config config = new Config();
        public List<int> Servo = new List<int>();

        public Path(Block _block)
        {
            this.blocks = new List<Block>();
            this.blocks.Add(_block);
            if (_block.coordinate.HasValue)
            {
                startCoori = 0;
                endCoori = 0;
            }
        }
        public Path(Block _block, Config _config)
        {
            this.blocks = new List<Block>();
            this.blocks.Add(_block);
            if (_block.coordinate.HasValue)
            {
                startCoori = 0;
                endCoori = 0;
            }
            config = _config;
        }

        public Path(List<Block> _wayPoints) //..............construct path from way points
        {
            this.blocks = _wayPoints;
           
            int nowStarti = -1;
            do
            {
                nowStarti++;
            } while (!_wayPoints[nowStarti].coordinate.HasValue && nowStarti < _wayPoints.Count -1);

            if (nowStarti == _wayPoints.Count - 1)  //none of the blocks contains coordinate, just command line
            {
                startCoori = null;
                endCoori = null;
            }
            else
            {
                startCoori = nowStarti;

                int nowEndi = _wayPoints.Count;
                do
                {
                    nowEndi--;
                } while (!_wayPoints[nowEndi].coordinate.HasValue && nowEndi > 0);

                endCoori = nowEndi;
            }
        }

        public Path(List<Block> _wayPoints, Config _config) //..............construct path from way points
        {
            this.blocks = _wayPoints;

            int nowStarti = -1;
            do
            {
                nowStarti++;
            } while (_wayPoints.Count > 0 && !_wayPoints[nowStarti].coordinate.HasValue && nowStarti < _wayPoints.Count - 1);

            if (nowStarti == _wayPoints.Count - 1)  //none of the blocks contains coordinate, just command line
            {
                startCoori = null;
                endCoori = null;
            }
            else
            {
                startCoori = nowStarti;

                int nowEndi = _wayPoints.Count;
                do
                {
                    nowEndi--;
                } while (_wayPoints.Count > 0 && !_wayPoints[nowEndi].coordinate.HasValue && nowEndi > 0);

                endCoori = nowEndi;
            }
            config = _config;
        }

        public Path(Path _other) //.........................construct path from another curve
        {
            blocks = new List<Block>(_other.blocks);
            startCoori = _other.startCoori;
            endCoori = _other.endCoori;
            config = _other.config;
        }

        public Path() { } //................................empty constructor

        public int Length
        {
            get { return this.blocks.Count; }
        }

        public Point3d StartCoor
        {
            get { return new Point3d(this.blocks[this.startCoori.Value].coordinate.Value); }
        }

        public Point3d EndCoor
        {
            get { return new Point3d(this.blocks[this.endCoori.Value].coordinate.Value); }
        }

        public void Flip() //...............................flip path direction
        {
            blocks.Reverse();


            var temp = startCoori;
            startCoori = blocks.Count-1 - endCoori;
            endCoori = blocks.Count -1 - temp;
        }

        public void Add(Block _block)
        {
            this.blocks.Add(_block);
            if (startCoori == null && _block.coordinate.HasValue) //if current has  no start and end
            {
                this.startCoori = this.blocks.Count - 1;
            }
            if (startCoori != null && _block.coordinate.HasValue) //if this point is add to path with start
            {
                this.endCoori = this.blocks.Count - 1;
            }
        }

        public void Add(Path _path) // .....................add path behind current path
        {
            this.Insert(_path, this.blocks.Count);
        }

        public void Insert(Block _block, int i)
        {
            if (startCoori != null) //if current has start and end
            {
                if (i < startCoori && _block.coordinate.HasValue)  //a waypoint is added at front
                {
                    startCoori = i;
                }
                if (i > endCoori && _block.coordinate.HasValue)  //a waypoint is added at back
                {
                    endCoori = i;
                }
                else
                {
                    if (startCoori != null)  //a command line is added
                    {
                        endCoori += 1;
                    }
                }
            }
            else // if current is just command line
            {
                if (_block.coordinate.HasValue)
                {
                    startCoori = i;
                    endCoori = i;
                }
            }
            blocks.Insert(i, _block);
        }

        public void Insert(Path _path, int i)
        {
            

            if (startCoori.HasValue && _path.startCoori.HasValue) //if this path and add path has coor, adjust start/end coor
            {
                if (i < endCoori && i > startCoori) //path is added in middle
                {
                    endCoori += _path.blocks.Count;
                }
                if (i <= startCoori)                 //path is added at beginning
                {
                    startCoori = _path.startCoori;
                    endCoori += (_path.blocks.Count -1);
                }
                if (i == endCoori)                    //path is added at end
                {
                    //Debug.WriteLine("path count = "+ this.blocks.Count.ToString());
                    endCoori = this.blocks.Count + _path.endCoori;
                }
            }
            if (!startCoori.HasValue && _path.startCoori.HasValue)
            {
                this.startCoori = i + _path.startCoori;
                this.endCoori = i + _path.endCoori;
            }
            if (this.startCoori.HasValue && !_path.startCoori.HasValue)
            {
                if (i < this.endCoori)
                {
                    endCoori += _path.blocks.Count;
                }
                if (i < this.startCoori)
                {
                    startCoori += _path.blocks.Count;
                }
            }

            blocks.InsertRange(i, _path.blocks);
        }

        public static Interval FeedRange(List<Path> _pathList)
        {
            Interval fRange = new Interval();
            for (int i = 0; i < _pathList.Count; i++)
            {

                Interval iv = _pathList[i].FeedRange();
                if (i == 0)
                {
                    fRange = iv; 
                }
                else
                {
                    fRange = Interval.FromUnion(iv, fRange);
                }
            }
            return fRange;
        }

        public Interval FeedRange()
        {
            Interval fRange = new Interval(0,0);
            if (this.startCoori.HasValue && this.blocks.Count > 0)
            {
                double low = (double)this.blocks[(int)this.startCoori].F;
                double high = (double)this.blocks[(int)this.startCoori].F;
                for (int i = 0; i < this.Length; i++)
                {
                    if (this.blocks[i].F.HasValue && this.blocks[i].F > high)
                    {
                        high = (double)this.blocks[i].F;
                    }
                    if (this.blocks[i].F.HasValue && this.blocks[i].F < low)
                    {
                        low = (double)this.blocks[i].F;
                    }
                }

                fRange = new Interval(low, high);
            }
            return fRange;
        }

        public static Interval ESRange(List<Path> _pathList)
        {
            Interval esRange = new Interval();
            for (int i = 0; i < _pathList.Count; i++)
            {

                Interval iv = _pathList[i].ESRange();
                if (i == 0)
                {
                    esRange = iv; 
                }
                else
                {
                    esRange = Interval.FromUnion(iv, esRange);
                }
            }
            return esRange;
        }
        public Interval ESRange()
        {
            Interval esRange = new Interval(0, 0);
            if (this.startCoori.HasValue)
            {
                double low = (double)this.blocks[(int)this.startCoori].ER;
                double high = (double)this.blocks[(int)this.startCoori].ER;
                for (int i = 0; i < this.Length; i++)
                {
                    if (this.blocks[i].ER.HasValue && this.blocks[i].ER > high)
                    {
                        high = (double)this.blocks[i].ER;
                    }
                    if (this.blocks[i].ER.HasValue && this.blocks[i].ER < low)
                    {
                        low = (double)this.blocks[i].ER;
                    }
                }

                esRange = new Interval(low, high);
            }
            return esRange;
        }

        /// <summary>
        /// Join two paths together
        /// </summary>
        /// <param name="_other">Other path to join</param>
        /// <param name="_tolerance">start/end point tolerance. Set to double.Max to ignore start/end point proximity, and add all blocks in list order</param>
        /// <param name="_preserveDirection">if true, path that meet but in wrong direction will not be joined</param>
        /// <returns></returns>
        public bool Join(Path _other, double _tolerance, bool _preserveDirection) //............................join one path to other if path touch, flip direction if need
        {
            bool success = false;

            if (this.startCoori != null && _other.startCoori != null)
            {

                Point3d nowStart = this.StartCoor;
                Point3d nowEnd = this.EndCoor;
                Point3d nextStart = _other.StartCoor;
                Point3d nextEnd = _other.EndCoor;

                if (nowStart.DistanceTo(nextEnd) < _tolerance)//add path in front
                {
                    this.blocks.RemoveAt(0);
                    this.Insert(_other, 0);
                    success = true;
                }
                if (nowEnd.DistanceTo(nextStart) < _tolerance)//add path behind
                {
                    this.blocks.RemoveAt(this.endCoori.Value);
                    this.Add(_other);
                    success = true;
                }
                if (nowStart.DistanceTo(nextStart) < _tolerance && !_preserveDirection)//flip and add path in front
                {
                    _other.Flip();
                    this.blocks.RemoveAt(0);
                    this.Insert(_other, 0);
                    success = true;
                }

                if (nowEnd.DistanceTo(nextEnd) < _tolerance && !_preserveDirection)//flip and add path behind
                {
                    _other.Flip();
                    this.blocks.RemoveAt(this.endCoori.Value);
                    this.Add(_other);
                    success = true;
                }
            }
            return success;
        }


        /// <summary>
        /// Join multiple path together. First path's configuration will be used
        /// </summary>
        /// <param name="_paths">List of path to join togather</param>
        /// <param name="_tolerance">start/end point tolerance. Set to double.Max to ignore start/end point proximity, and add all blocks in list order</param>
        /// <param name="_preserveDirection">if true, path that meet but in wrong direction will not be joined</param>
        /// <returns></returns>
        public static List<Path> Join(List<Path> _paths, double _tolerance, bool _preserveDirection)
        {
            System.Diagnostics.Debug.WriteLine("JoinPath::join begins");
            List<Path> pathsOut = new List<Path>(); //Output collection of paths. the active/current path to add to
            List<Path> pathsIn = new List<Path>(_paths);//make a copy of _paths. find the next path in here to add to PathOut
            {
                int x = pathsIn.Count; //remaining path count
                int j = 0; //current join curve counter
                int d = 0; //debug
                bool terminate = false;
                pathsOut.Add(new Path(pathsIn[0])); //add first path
                pathsIn.RemoveAt(0);
                do
                {
                    x = pathsIn.Count;
                    System.Diagnostics.Debug.WriteLine("run do loop");
                    for (int i = 0; i < x; i++)
                    {
                        bool joinSuccess = pathsOut[j].Join(pathsIn[i], _tolerance, _preserveDirection);
                        if (joinSuccess)
                        {
                            pathsIn.RemoveAt(i);
                            System.Diagnostics.Debug.WriteLine("join once");
                            break;
                            
                        }
                        if(!joinSuccess && i == x-1)//reach end of list without finindg partner
                        {
                            pathsOut.Add(new Path(pathsIn[0]));
                            j++;
                            System.Diagnostics.Debug.WriteLine("no more adjacent curve");
                            if (x == 1)//also not more next curve to add
                            {
                                d = 10001;//terminate
                                terminate = false;
                                Debug.WriteLine("terminate join");
                            }
                            else
                            {
                                
                            }
                            pathsIn.RemoveAt(0);
                        }
                    }
                    d++;
                    

                } while (x > 0 && pathsIn.Count > 0 && !terminate && d < 10000);
            }
            System.Diagnostics.Debug.WriteLine("finished joining\n....");
            return pathsOut;
        }

        //public void Preview(int _colorIndex, Path _path)//.....................................................custom preview for path
        //{
        //    if (_path.startCoori.HasValue) //is a path with way point
        //    {
        //        Point3d lastPt = new Point3d(_path.blocks[_path.startCoori.Value].coordinate.Value);
        //        for (int i = _path.startCoori.Value + 1; i < _path.endCoori.Value; i++)
        //        {
        //            //List<Line> lines = new List<Line>();

        //            if (_path.blocks[i].coordinate.HasValue)
        //            {
        //                Line ln = new Line(lastPt, _path.blocks[i].coordinate.Value);
        //                //GH_PreviewWireArgs args = new GH_PreviewWireArgs(



        //            }
        //        }
        //    }
        //}
        //public static List<Path> JoinOld(List<Path> _paths, double _tolerance, bool _preserveDirection)  //........join Multiple paths , need add function to arc
        //{
        //    List<Path> pathsOut = new List<Path>(); //Output collection of paths. the active/current path to add to
        //    List<Path> pathsIn = new List<Path>(_paths);//make a copy of _paths. find the next path in here to add to PathOut
        //    {
        //        int x = pathsIn.Count; //remaining path count
        //        int j = 0; //current join curve counter
        //        int f = 0; //flip warning
        //        int d = 0; //debug
        //        pathsOut.Add(new Path(pathsIn[0])); //add first path
        //        pathsIn.RemoveAt(0);
        //        do
        //        {
        //            //x = pathsIn.Count;
        //            for (int i = 0; i < x; i++)
        //            {
        //                if(!pathsOut[j].startCoori.HasValue || !pathsOut[j].endCoori.HasValue)  //current path does not contain coordinate, just add next
        //                {
        //                    pathsOut[j].Add(pathsIn[i]);
        //                    pathsIn.RemoveAt(i);
        //                    x--;
        //                    break;
        //                }

        //                if (!pathsIn[i].startCoori.HasValue || !pathsIn[i].endCoori.HasValue)  //next path does not contain coordinate, just add next
        //                {
        //                    pathsOut[j].Add(pathsIn[i]);
        //                    pathsIn.RemoveAt(i);
        //                    x--;
        //                    break;
        //                }

        //                Point3d nowStart = pathsOut[j].StartCoor;
        //                Point3d nowEnd = pathsOut[j].EndCoor;
        //                Point3d nextStart = pathsIn[i].StartCoor;
        //                Point3d nextEnd = pathsIn[i].EndCoor;

        //                if (nowStart.DistanceTo(nextEnd) < _tolerance)//add path in front
        //                {
        //                    //pathsOut[j].blocks.RemoveAt(0); //remove overlap 
        //                    pathsOut[j].Insert(pathsIn[i], 0);
        //                    pathsIn.RemoveAt(i); //remove from input list
        //                    x--;
        //                    break;
        //                }
        //                if (nowEnd.DistanceTo(nextStart) < _tolerance)//add path behind
        //                {
        //                    //pathsOut[j].blocks.RemoveAt(pathsOut[j].blocks.Count - 1);
        //                    pathsOut[j].Add(pathsIn[i]);
        //                    pathsIn.RemoveAt(i);
        //                    x--;
        //                    break;
        //                }
        //                if (nowStart.DistanceTo(nextStart) < _tolerance && !_preserveDirection)//flip and add path in front
        //                {
        //                    //pathsOut[j].blocks.RemoveAt(0);
        //                    pathsIn[i].Flip();
        //                    pathsOut[j].Insert(pathsIn[i], 0);
        //                    pathsIn.RemoveAt(i);
        //                    x--;
        //                    f++;
        //                    break;
        //                }
        //                if (nowEnd.DistanceTo(nextEnd) < _tolerance && !_preserveDirection)//flip and add path behind
        //                {
        //                    //pathsOut[j].blocks.RemoveAt(pathsOut[j].blocks.Count - 1);
        //                    pathsIn[i].Flip();
        //                    pathsOut[j].Add(pathsIn[i]);
        //                    pathsIn.RemoveAt(i);
        //                    x--;
        //                    f++;
        //                    break;
        //                }
        //                else
        //                {
        //                    j++; //no more adjacent paths. 
        //                    if (pathsIn.Count > 0)
        //                    {
        //                        pathsOut.Add(pathsIn[i]); //Start new path
        //                        pathsIn.RemoveAt(i);
        //                        x--;
        //                    }
        //                    break;
        //                }
        //                //d++;

        //            }

        //        } while (x>1 && pathsIn.Count > 1 && d< 100000);

        //    }
        //    return pathsOut;
        //}

        //public static List<Path> JoinOld2(List<Path> _paths, double _tolerance, bool _preserveDirection)  //.......join multiple paths together
        //{
        //    List<Path> p = new List<Path>(); //Output collection of paths
        //    List<Path> paths = new List<Path>(_paths);//make a copy of _paths
        //    {
        //        int x = paths.Count; //remaining path count
        //        int j = 0; //current join curve counter
        //        int f = 0; //flip warning
        //        int d = 0; //debug
        //        p.Add(new Path(paths[0]));
        //        do
        //        {
        //            for(int i = 0; i < x; i++)
        //            {
        //                //now path index------------------------------------------------------------------
        //                int nowStarti = 0;
        //                do 
        //                {
        //                    nowStarti++;
        //                } while (!paths[0].blocks[nowStarti].coordinate.HasValue && nowStarti < paths[0].blocks.Count);

        //                if (nowStarti == paths[0].blocks.Count - 1)  //does not contain coordinate, just add to path
        //                {
        //                    p[j].blocks.AddRange(paths[0].blocks);
        //                    paths.RemoveAt(0);
        //                    break;
        //                }

        //                int nowEndi = paths[0].blocks.Count - 1;
        //                do
        //                {
        //                    nowEndi--;
        //                } while (!paths[0].blocks[nowEndi].coordinate.HasValue && nowEndi > 0);

        //                //next path index index---------------------------------------------------------------------------
        //                int nextStarti = 0;
        //                do
        //                {
        //                    nextStarti++;
        //                } while (!paths[i].blocks[nextStarti].coordinate.HasValue && nextStarti < paths[i].blocks.Count);

        //                if (nextStarti == paths[i].blocks.Count - 1)  //does not contain coordinate, just add to path
        //                {
        //                    p[j].blocks.AddRange(paths[i].blocks);
        //                    paths.RemoveAt(i);
        //                    break;
        //                }

        //                int nextEndi = paths[i].blocks.Count - 1;
        //                do
        //                {
        //                    nextEndi--;
        //                } while (!paths[i].blocks[nextEndi].coordinate.HasValue && nextEndi > 0);


        //                    Point3d nowStart = new Point3d(paths[0].blocks[0].coordinate.Value);
        //                    Point3d nowEnd = new Point3d(paths[0].blocks[paths[0].blocks.Count - 1].coordinate.Value);
        //                    Point3d nextStart = new Point3d(paths[i].blocks[0].coordinate.Value);
        //                    Point3d nextEnd = new Point3d(paths[i].blocks[paths[i].blocks.Count - 1].coordinate.Value);

        //                    if (nowStart.DistanceTo(nextEnd) < _tolerance)//add path in front
        //                    {
        //                        p[j].blocks.RemoveAt(0); //remove overlap point
        //                        p[j].blocks.InsertRange(0, paths[i].blocks); //insert at front
        //                        paths.RemoveAt(i); //remove from input list
        //                        x--;
        //                    }
        //                    if (nowEnd.DistanceTo(nextStart) < _tolerance)//add path behind
        //                    {
        //                        p[j].blocks.RemoveAt(p[j].blocks.Count - 1);
        //                        p[j].blocks.AddRange(paths[i].blocks);
        //                        paths.RemoveAt(i);
        //                        x--;
        //                    }
        //                    if (nowStart.DistanceTo(nextStart) < _tolerance && !_preserveDirection)//flip and add path in front
        //                    {
        //                        p[j].blocks.RemoveAt(0);
        //                        paths[i].blocks.Reverse();
        //                        p[j].blocks.InsertRange(0, paths[i].blocks);
        //                        paths.RemoveAt(i);
        //                        x--;
        //                        f++;
        //                    }
        //                    if (nowEnd.DistanceTo(nextEnd) < _tolerance && !_preserveDirection)//flip and add path behind
        //                    {
        //                        p[j].blocks.RemoveAt(p[j].blocks.Count - 1);
        //                        paths[i].blocks.Reverse();
        //                        p[j].blocks.AddRange(paths[i].blocks);
        //                        paths.RemoveAt(i);
        //                        x--;
        //                        f++;
        //                    }
        //                    else
        //                    {
        //                        j++; //no more adjacent paths. 
        //                        p.Add(new Path(paths[0])); //Start new path
        //                    }
        //                    d++;

        //            }

        //        } while (x>0 && d < 100000);

        //    }
        //    return p;
        //}

        public static List<Path> Bridge(List<Path> _paths)  //..................................bridge and join path in order of Path list
        {
            List<Path> paths = new List<Path>();

            return paths;
        }

        public static List<Path> TravelBridge(List<Path> _paths, Config _config)  //............................create travel moves between paths
        {
            List<Path> paths = new List<Path>();

            return paths;
        }

        

        public static List<string> ToString(Path _path, Config _config) //..................................................convert path to strings
        {
            List<string> lines = new List<string>();

            return lines;
        }

        public Polyline ToPolyline()
        {
            Polyline pl = new Polyline();
            List<Point3d> pts = new List<Point3d>();

            for (int i=0; i < this.blocks.Count; i++)
            {
                if (this.blocks[i].coordinate.HasValue)
                {
                    pts.Add(this.blocks[i].coordinate.Value);
                }
            }

            return new Polyline(pts);
        }


    }

  

    public class Gcode  //Gcode compiler and checker, contain collection of path...........................................................
    {
        readonly public List<string> lines = new List<string>();  //lines of code
        public List<Path> paths = new List<Path>();
        public Config config;
        public string psc; //precision
        public bool absPos = true;
        public bool absE = true;
        public string msg = "";
        

        public Gcode(List<string> _line)
        {
            lines = _line;
        }

        public Gcode() { } //empty constructor

        /// <summary>
        /// Create gcode from path
        /// </summary>
        /// <param name="_path">Path to convert to gcode</param>
        /// <param name="_psc">Precision of data. Number of decimal place</param>
        /// <param name="_axesCount">Number of axes to output, between 3-6</param>
        /// <param name="_absPos">Use absolute position</param>
        /// <param name="_absE">Use absolute E</param>
        /// <param name="_config">Configuration</param>
        /// <param name="msg">Message. For checking</param>
        public Gcode(Path _path, int _psc, bool _absPos, bool _absE, Config _config) //3axis
        {

            /*double tol = Convert.ToDouble(_config.Settings["xy_tolerance"]);  //machine tolerance
            //int psc = tol.ToString().Length - 2;
            int precision = tol.ToString().Length - 2; ;  //number of X,Y coordinate decimal place to retain
            int precisionZ = precision; //number of Z coordinate decimal place to retain
            int precisionE = precision; //number of E coordinate decimal place to retain
            string pcs = "F" + precision.ToString();
            string pcsZ = "F" + precisionZ.ToString();
            string pcsE = "F" + precisionE.ToString();
            double minS = 500;  //minimum speed allowed (mm/min)

            //double CR = (double)Config.PrintSetting.corner_pause;
            double CR = (double)_config.Settings["corner_threshold"];
            double corner = Math.Cos(CR * Math.PI / 180); //turning threshold. Stop and extrude extra E. Value eaquals to cos x. Rmb for dot product, 90 degree =0, straight = -1
            double cornE = 0.004;  //coefficient for extra bit to extrude at corner
            int checkZ = 0;
            int checkL = 0;

            List<string> gCode = new List<string>();
            List<int> cornerInd = new List<int>();

            List<double> uniqueZ = new List<double>();
            List<double> allZ = new List<double>();
            foreach (Point3d pZ in V) { allZ.Add(pZ.Z); }
            uniqueZ = sortUnique(allZ);   //all unique Z val for all vertex, i.e. non-repeating layer height value

            double lastLayerZ = uniqueZ[0] + tol;        //Z value of last layer, to cal layer height(dz)
            double firstZ = uniqueZ[0] + tol;            //first layer height,i.e. smallest Z of all vertices
            double lastZ = uniqueZ[0] + tol;             //Z value of last vertex, updated every i loop
            double lastE = 0.0;
            double fLength = 0.0;
            double time = 0.0;*/

            this.paths.Add(_path);

            this.absPos = _absPos;
            this.absE = _absE;
            this.config = _path.config;
            this.psc = "F" + _psc.ToString();
            this.config = _config;
            //this.UpdateLines(out _);


            //int pscUnderCount = 0; //check if more precision needed. number of times point distance smaller than psc
            //double tol = Pow(10, -1 * _psc);
            //double lastX = 0;
            //double lastY = 0;
            //double lastZ = 0;
            //Point3d lastPt = new Point3d(_config.Machine.parkPos);

            ////double lastA = 0;
            ////double lastB = 0;
            ////double lastC = 0;

            //double lastF = 0;
            //double lastS = 0;
            //double lastE = 0;

            //if (_absPos)
            //{
            //    lines.Add("G90");
            //}
            //else
            //{
            //    lines.Add("G91");
            //}
            //if (_absE)
            //{
            //    lines.Add("M82");
            //}
            //else
            //{
            //    lines.Add("M83");
            //}

            //msg = "";

            //for (int j = 0; j < _path.blocks.Count; j++)
            //{

            //    Block nowBlk = _path.blocks[j];

            //    string text = "";
            //    bool moved = false;

            //    if (nowBlk.G.HasValue)
            //    {
            //        text += "G";
            //        text += nowBlk.G.Value.ToString();
                    
            //    }
            //    if (nowBlk.coordinate.HasValue && _absPos)
            //    {
            //        if (Abs(lastX - nowBlk.coordinate.Value.X) > tol)
            //        {
            //            text += " X";
            //            text += nowBlk.coordinate.Value.X.ToString(psc);
            //            lastX = nowBlk.coordinate.Value.X;
            //            moved = true;
            //        }
            //        if (Abs(lastY - nowBlk.coordinate.Value.Y) > tol)
            //        {
            //            text += " Y";
            //            text += nowBlk.coordinate.Value.Y.ToString(psc);
            //            lastY = nowBlk.coordinate.Value.Y;
            //            moved = true;
            //        }
            //        if (Abs(lastZ - nowBlk.coordinate.Value.Z) > tol)
            //        {
            //            text += " Z";
            //            text += nowBlk.coordinate.Value.Z.ToString(psc);
            //            lastZ = nowBlk.coordinate.Value.Z;
            //            moved = true;
            //        }
            //        else
            //        {
            //            if(lastPt.DistanceTo(nowBlk.coordinate.Value) < tol) //moved smaller than tolerance
            //            {
            //                pscUnderCount += 1;
            //                moved = true;
            //            }
            //        }
            //    }

            //    if (nowBlk.coordinate.HasValue && !_absPos)
            //    {
            //        double moveX = Abs(lastX - nowBlk.coordinate.Value.X);
            //        double moveY = Abs(lastY - nowBlk.coordinate.Value.Y);
            //        double moveZ = Abs(lastZ - nowBlk.coordinate.Value.Z);

            //        if (moveX > tol)
            //        {
            //            text += " X";
            //            text += moveX.ToString(psc);
            //            lastX = nowBlk.coordinate.Value.X;
            //            moved = true;
            //        }
            //        if (moveY > tol)
            //        {
            //            text += " Y";
            //            text += nowBlk.coordinate.Value.Y.ToString(psc);
            //            lastY = nowBlk.coordinate.Value.Y;
            //            moved = true;
            //        }
            //        if (moveZ > tol)
            //        {
            //            text += " Z";
            //            text += nowBlk.coordinate.Value.Z.ToString(psc);
            //            lastZ = nowBlk.coordinate.Value.Z;
            //            moved = true;
            //        }

            //        lastX += moveX;
            //        lastY += moveY;
            //        lastZ += moveZ;
            //    }

            //    if (nowBlk.orientation.HasValue)
            //    {
            //        //if(_axesCount == 4)
            //        //{
            //        //    text += " A";
            //        //    text += nowBlk.orientation.Value.
            //        //}
            //        //if (_axesCount == 5)
            //        //{
            //        //    text += " B";
            //        //    text += nowBlk.orientation.Value.
            //        //}
            //        //if (_axesCount == 6)
            //        //{
            //        //    text += " C";
            //        //    text += nowBlk.orientation.Value.
            //        //}
            //    }

            //    if (nowBlk.F.HasValue)
            //    {
            //        if (Abs(lastF - nowBlk.F.Value) > tol)
            //        {
            //            text += " F";
            //            text += nowBlk.F.Value.ToString("F0");
            //            lastF = nowBlk.F.Value;
            //        }
            //    }


            //    if (nowBlk.ER.HasValue && moved && _absE) //using abs E
            //    {
            //        double er = nowBlk.ER.Value;
            //        double dist = lastPt.DistanceTo(nowBlk.coordinate.Value);
            //        double e = er / dist;
            //        lastE += e;

            //        text += " E";
            //        text += lastE.ToString();
            //    }

            //    if (nowBlk.ER.HasValue && moved && !_absE) //not using abs E ie. incremental E
            //    {
            //        double er = nowBlk.ER.Value;
            //        double dist = lastPt.DistanceTo(nowBlk.coordinate.Value);
            //        double e = er / dist;

            //        text += " E";
            //        text += e.ToString();
            //    }


            //    if (nowBlk.SS.HasValue && Abs(lastS - nowBlk.SS.Value) > tol)
            //    {
            //        text += " S";
            //        text += nowBlk.SS.Value.ToString();
            //        lastS = nowBlk.SS.Value;
            //    }

            //    if (nowBlk.M.Count > 0)
            //    {
            //        for (int i = 0; i < nowBlk.M.Count; i++)
            //        {
            //            if (nowBlk.M[i].HasValue)
            //            {
            //                text += "M";
            //                text += nowBlk.M[i].Value.ToString();
            //                text += " ";
            //            }
            //            if (nowBlk.P[i].HasValue)
            //            {
            //                text += "P";
            //                text += nowBlk.P[i].Value.ToString();
            //                text += " ";
            //            }
            //            if (nowBlk.S[i].HasValue)
            //            {
            //                text += "S";
            //                text += nowBlk.S[i].Value.ToString();
            //                text += " ";
            //            }
            //            //text += "\n";
            //        }
            //    }
            //    if (moved)
            //    {
            //        lastPt = nowBlk.coordinate.Value;
            //    }


            //    //if (this.T.HasValue)
            //    //{
            //    //    text += "T";
            //    //    text += this.T.Value.ToString();
            //    //    text += " ";
            //    //}

            //    this.lines.Add(text);
            //}

            //if(pscUnderCount > 0)
            //{
            //    msg += "warning! points get closer than the precision allowed " + psc.ToString() + " times! Consider higher precision.";
            //}

        }

        /// <summary>
        /// Internally convert beaver path to string. Then use ToString to print out lines
        /// </summary>
        /// <param name="_msg"></param>
        public void UpdateLines(bool _ignoreOS, out string _msg)
        {
            _msg = "";
            int _psc = Convert.ToInt32(psc.Replace("F", ""));
            
            int pscUnderCount = 0; //check if more precision needed. number of times point distance smaller than psc
            double tol = Pow(10, -1 * _psc);
            double lastX = -99999;
            double lastY = -99999;
            double lastZ = -99999;
            Point3d lastPt = new Point3d(config.Machine.parkPos);
            int axesCount = config.Machine.AxisCount;
            //double lastA = 0;
            //double lastB = 0;
            //double lastC = 0;

            double lastF = -99999;
            double lastS = -99999;
            double lastE = -99999;


            
            double scale = 1.0;
            Rhino.RhinoDoc doc = Rhino.RhinoDoc.ActiveDoc;
            Rhino.UnitSystem system = doc.ModelUnitSystem;

            #region scale to match machine unit and rhino unit..........
            if (system.ToString() == "Meters")
            {
                scale = 1000;
                _msg += "Rhino unit = Meters. All points scaled 1000x\n";
            }
            if (system.ToString() == "Millimeters")
            {
                scale = 1;
                _msg += "Rhino unit = Millimeters.\n";
            }
            if (system.ToString() == "Centimeters")
            {
                scale = 10;
                _msg += "Rhino unit = Centimeters. All points scaled 10x\n";
            }
            if (system.ToString() == "Inches")
            {
                scale = 25.4;
                _msg += "Rhino unit = Inches. All points scaled 25.4x\n";
            }
            if (system.ToString() == "Feet")
            {
                scale = 304.8;
                _msg += "Rhino unit = Feet. All points scaled 304.8x\n";
            }
            #endregion


            if (absPos)
                lines.Add("G90 ; Use absolute position");
            else
                lines.Add("G91 ; Use relative position");
            
            if (absE)
                lines.Add("M82 ; Use absolute E");
            else
                lines.Add("M83 ; Use relative E");
            


            for (int k = 0; k < paths.Count; k++) //for each path in gcode
            {

                if(this.config.Machine.Volume == null)
                    msg += "No machine configuration detected";
                

                for (int j = 0; j < paths[k].blocks.Count; j++)
                {

                    Block nowBlk = paths[k].blocks[j];

                    string text = "";
                    bool moved = false;

                    bool go = true;
                    if (nowBlk.coordinate.HasValue && this.config.Machine.Volume != null)
                    {
                        go = this.config.Machine.Volume.IsPointInside(nowBlk.coordinate.Value, tol, true); //test for point inside
                    }
                    if (!go) //stop iteration if no go
                    {
                        _msg += "Skipped point[" + j.ToString() + "]. Reason: Outside machine boundary.\n";
                        continue;
                    }

                    if (nowBlk.G.HasValue)
                    {
                        text += "G";
                        text += nowBlk.G.Value.ToString();

                    }

                    if(this.config.Machine.rAxes.type != RAxesType.None && nowBlk.coordinate.HasValue)
                    {
                        switch (this.config.Machine.rAxes.type)
                        {
                            case RAxesType.SPM:
                                this.config.Machine.rAxes.spm.UpdateConfig(nowBlk.orientation.Value, true);
                                nowBlk.coordinate = this.config.Machine.rAxes.spm.mountPlane.Origin;
                                break;
                            case RAxesType.SphericalAxes:
                                this.config.Machine.rAxes.sa.UpdateConfig(nowBlk.orientation.Value);
                                nowBlk.coordinate = this.config.Machine.rAxes.sa.mountPlane.Origin;
                                break;
                        }
                    }

                    if (nowBlk.coordinate.HasValue && absPos)
                    {

                        if (Abs(lastX - nowBlk.coordinate.Value.X) > tol)
                        {
                            text += " X";
                            text += nowBlk.coordinate.Value.X.ToString(psc);
                            lastX = nowBlk.coordinate.Value.X;
                            moved = true;
                        }
                        if (Abs(lastY - nowBlk.coordinate.Value.Y) > tol)
                        {
                            text += " Y";
                            text += nowBlk.coordinate.Value.Y.ToString(psc);
                            lastY = nowBlk.coordinate.Value.Y;
                            moved = true;
                        }
                        if (Abs(lastZ - nowBlk.coordinate.Value.Z) > tol)
                        {
                            text += " Z";
                            text += nowBlk.coordinate.Value.Z.ToString(psc);
                            lastZ = nowBlk.coordinate.Value.Z;
                            moved = true;
                        }
                        else
                        {
                            if (lastPt.DistanceTo(nowBlk.coordinate.Value) < tol) //moved smaller than tolerance
                            {
                                pscUnderCount += 1;
                                moved = true;
                            }
                        }
                    }

                    if (nowBlk.coordinate.HasValue && !absPos)
                    {
                        double moveX = Abs(lastX - nowBlk.coordinate.Value.X);
                        double moveY = Abs(lastY - nowBlk.coordinate.Value.Y);
                        double moveZ = Abs(lastZ - nowBlk.coordinate.Value.Z);

                        if (moveX > tol)
                        {
                            text += " X";
                            text += moveX.ToString(psc);
                            lastX = nowBlk.coordinate.Value.X;
                            moved = true;
                        }
                        if (moveY > tol)
                        {
                            text += " Y";
                            text += nowBlk.coordinate.Value.Y.ToString(psc);
                            lastY = nowBlk.coordinate.Value.Y;
                            moved = true;
                        }
                        if (moveZ > tol)
                        {
                            text += " Z";
                            text += nowBlk.coordinate.Value.Z.ToString(psc);
                            lastZ = nowBlk.coordinate.Value.Z;
                            moved = true;
                        }

                        lastX += moveX;
                        lastY += moveY;
                        lastZ += moveZ;
                    }

                    if (nowBlk.orientation.HasValue)
                    {
                        
                        if(config.Machine.rAxes.type == RAxesType.SPM)
                        {
                            SPM s1 = config.Machine.rAxes.spm;
                            //List<double> angles = s1.InputAngle(nowBlk.orientation.Value);

                            s1.UpdateConfig(nowBlk.orientation.Value, false);

                            List<double> angles = new List<double>();
                            for (int i = 0; i < 3; i++)
                            {
                                double ia = s1.InputAngle(s1.axesV[i], i);
                                ia *= 180 / Math.PI; //from radian to degree output
                                angles.Add(ia);
                            }

                            text += " A";
                            text += angles[0].ToString(psc);
                            text += " B";
                            text += angles[1].ToString(psc);
                            text += " C";
                            text += angles[2].ToString(psc);
                        }
                        if(config.Machine.rAxes.type == RAxesType.None)
                        {
                            if (nowBlk.orientation.Value != Plane.WorldXY)
                            {
                                msg += "A,B,C value cannot be generated because rotational axes mechanism was not specified.";
                            }
                        }
                    }

                    if (nowBlk.F.HasValue)
                    {
                        if (Abs(lastF - nowBlk.F.Value) > tol)
                        {
                            text += " F";
                            text += nowBlk.F.Value.ToString("F0");
                            lastF = nowBlk.F.Value;
                        }
                    }


                    if (nowBlk.ER.HasValue && moved && absE) //using abs E
                    {
                        double er = nowBlk.ER.Value;
                        double dist = lastPt.DistanceTo(nowBlk.coordinate.Value);
                        double e = er / dist;
                        lastE += e;

                        text += " E";
                        text += lastE.ToString();
                    }

                    if (nowBlk.ER.HasValue && moved && !absE) //not using abs E ie. incremental E
                    {
                        double er = nowBlk.ER.Value;
                        double dist = lastPt.DistanceTo(nowBlk.coordinate.Value);
                        double e = er / dist;

                        text += " E";
                        text += e.ToString();
                    }


                    if (nowBlk.SS.HasValue && Abs(lastS - nowBlk.SS.Value) > tol)
                    {
                        text += " S";
                        text += nowBlk.SS.Value.ToString();
                        lastS = nowBlk.SS.Value;
                    }

                    if (nowBlk.M.Count > 0)
                    {
                        for (int i = 0; i < nowBlk.M.Count; i++)
                        {
                            if (nowBlk.M[i].HasValue)
                            {
                                if (nowBlk.M[i].Value >= 0)
                                {
                                    text += " M";
                                    text += nowBlk.M[i].Value.ToString();
                                }
                                else
                                {
                                    text += "G";
                                    text += (nowBlk.M[i].Value*-1).ToString();
                                }
                                
                                
                                text += " ";
                            }
                            if (nowBlk.P[i].HasValue)
                            {
                                text += "P";
                                text += nowBlk.P[i].Value.ToString();
                                text += " ";
                            }
                            if (nowBlk.S[i].HasValue)
                            {
                                text += "S";
                                text += nowBlk.S[i].Value.ToString();
                                text += " ";
                            }
                            //text += "\n";
                        }
                    }
                    if (moved)
                    {
                        lastPt = nowBlk.coordinate.Value;
                    }


                    //if (this.T.HasValue)
                    //{
                    //    text += "T";
                    //    text += this.T.Value.ToString();
                    //    text += " ";
                    //}

                    this.lines.Add(text);
                }
            }

            if (pscUnderCount > 0)
            {
                _msg += "warning! points get closer than the precision allowed " + psc.ToString() + " times! Consider higher precision.";
            }
        }

        /// <summary>
        /// Print out string stored in gcode. Make sure you have used UpdateLines before using ToString
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string txt = "";
            for(int i = 0; i < this.lines.Count; i++)
            {
                txt += this.lines[i];
                txt += "\n";
            }
            return txt;
        }

        public static string AddCheckSum(string _line)
        {
            int cs = 0;
            for (int i = 0; i < _line.Length; i++)
                cs ^= _line[i];
            cs &= 0xff;

            return _line + "*" + cs.ToString();
        }

        /// <summary>
        /// Add line number and check sum to each line of gcode string.
        /// </summary>
        /// <param name="_line">Single line of code.</param>
        /// <param name="_lineNum">Line number. Updated per line written.</param>
        /// <returns></returns>
        public static string LineSyntax(string _line, ref int _lineNum)
        {
            string ln = $"N{_lineNum} {_line}";
            _lineNum++;
            return AddCheckSum(ln);
        }

        public static string SaveFile(string loc, string name, string ext, List<string> lines, bool overwrite, bool write)
        {
            string[] ln = lines.ToArray();
            
            //file path.................................................
            if (loc[loc.Length - 1] != '\\')
            { //if user did not add backslash
                loc += "\\";
            }
            if (ext[0] != '.')
            {      //if user did not add . in front of ext
                ext = "." + ext;
            }
            if (!Directory.Exists(loc))
            { //if folder doesn't exist, create one
                Directory.CreateDirectory(loc);
            }

            string msg = "Ready to write file";

            string path = loc + name + ext;

            if (write)
            {
                if (!System.IO.File.Exists(path))
                {  //if file does not exist, create new
                    System.IO.File.WriteAllLines(path, ln);
                    //DA.SetData(0, "File saved to " + path);
                    msg = "File saved to " + path;
                }
                else
                {             //if file path exist
                    if (overwrite)
                    {  //overwrite
                        System.IO.File.WriteAllLines(path, ln);
                        //DA.SetData(0, "File overwritten, saved to " + path);
                        msg = "File overwritten, saved to " + path;
                    }
                    else
                    {
                        {
                            int i = 1; bool saved = false;
                            do
                            {
                                string pathi = loc + name + "_" + i.ToString() + ext;
                                if (!System.IO.File.Exists(pathi))
                                {
                                    System.IO.File.WriteAllLines(pathi, ln);
                                    saved = true;
                                    //DA.SetData(0, "File saved to " + path);
                                    msg = "File saved incrementally to " + path;
                                }
                                else
                                {
                                    i++;
                                }
                            } while (!saved);
                        }
                    }
                }
            }
            return msg;
        }
    }


    
}


