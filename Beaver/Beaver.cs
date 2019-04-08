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
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Beaver
{
    

    public enum GList  //supported G format
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

    public enum MList  //supported M format
    {
        End = 2,
        SpindleOnCW = 3,
        SpindleOnCCW = 4,
        SpindleOff = 5,
        AutoToolChange = 6,
        CoolantOnMist = 7,
        CoolantOnFlood = 8,
        CoolantOff = 9,


    }
    public enum ToolType
    {
        extruder,
        mill
    }

    public enum ToolShape
    {
        flat,
        ball,
        custom
    }

    public class Tool
    {
        public static List<string> ShapeList()
        {
            List<string> shp = new List<string>();
            //for (int i = 0; i < ToolShap)
            return shp;
        }
        //all tool properties
        public string ToolName = "";
        public ToolType Type;
        public string Descriptions = "";
        public double Diameter = -1;
        public double OffsetX = -1;
        public double OffsetY = -1;
        public double OffsetZ = -1;
        public double Max = -1;
        public double Min = -1;
        public List<string> ToolChange = new List<string>();

        //mill properties
        public ToolShape Shape;
        public bool RightHand; //true for right hand tool, false for left hand tool
        public double Feed;
        public double SpindleSpeed;
        public double PlungeRate;
        public double RetractRate;

        /*public Tool(string _toolName, ToolType _toolType)
        {
            ToolName = _toolName;
            Type = _toolType;
        }

        public Tool(string _toolName, ToolType _toolType, double _diameter, double _offsetZ)
        {
            ToolName = _toolName;
            Type = _toolType;
            Diameter = _diameter;
            OffsetZ = _offsetZ;
        }*/

        /// <summary>
        /// Extruder tool constructor only
        /// </summary>
        
        public Tool(string _toolName, ToolType _extruder, double _diameter, double _offsetX, double _offsetY, double _offsetZ)
        {
            
            ToolName = _toolName;
            Type = _extruder;
            Diameter = _diameter;
            OffsetX = _offsetX;
            OffsetY = _offsetY;
            OffsetZ = _offsetZ;
        }

        /// <summary>
        /// Mill tool constructor only
        /// </summary>

        public Tool(string _toolName, ToolType _mill, double _diameter, double _offsetX, double _offsetY, double _offsetZ, ToolShape _shape, double _feed, double _spindleSpeed, double _plungeRate, double _retractRate)
        {
            ToolName = _toolName;
            Type = _mill;
            Shape = _shape;
            Diameter = _diameter;
            OffsetX = _offsetX;
            OffsetY = _offsetY;
            OffsetZ = _offsetZ;
            Feed = _feed;
            SpindleSpeed = _spindleSpeed;
            PlungeRate = _plungeRate;
            RetractRate = _retractRate;
        }

        

        public override string ToString()
        {
            string msg = "";

            if (ToolName.Length > 0)
            {
                msg += "tool name : " + ToolName + "\n";
            }
            if(IsExtruder)
            {
                msg += "tool type : 3D printing extruder\n";
                if (Descriptions.Length > 0)
                {
                    msg += "tool descriptions : " + Descriptions + "\n";
                }
                msg += "tool noozle diameter : " + Diameter.ToString() + "mm\n"; 
                msg += "tool offset X : " + OffsetX.ToString() + "mm\n"; 
                msg += "tool offset Y : " + OffsetY.ToString() + "mm\n";
                msg += "tool offset Z : " + OffsetZ.ToString() + "mm\n";

            }
            if (IsMill)
            {
                msg += "tool type : milling bit\n";
                if (Descriptions.Length > 0)
                {
                    msg += "tool descriptions : " + Descriptions + "\n";
                }
                msg += "tool bit diameter : " + Diameter.ToString() + "mm\n"; 
                msg += "tool offset X : " + OffsetX.ToString() + "mm\n";
                msg += "tool offset Y : " + OffsetY.ToString() + "mm\n";
                msg += "tool offset Z : " + OffsetZ.ToString() + "mm\n";
                msg += "tool feed : " + Feed.ToString() + "mm/min\n";
                msg += "tool spindle speed : " + SpindleSpeed.ToString() + "rpm\n";
                msg += "tool plunge rate : " + PlungeRate.ToString() + "mm/min\n";
                msg += "tool retract rate : " + RetractRate.ToString() + "mm/min\n";
            }

            return msg;
        }

        public bool IsMill
        {
            get { return Type == ToolType.mill; }
        }
        public bool IsExtruder
        {
            get { return Type == ToolType.extruder; }
        }
    }

    public enum BedShape
    {
        rectangular,
        circular,
        custom
    }

    public class Machine //set printer info........................................................
    {
        readonly public Brep Volume;
        public Interval SizeX;
        public Interval SizeY;
        public Interval SizeZ;
        public int AxisCount;
        readonly public BedShape Shape;
        readonly public Curve BedShape;
        public List<Tool> Tools;
        public string name = "";
        public bool hasCoolant;
        

        public Machine(double _x, double _y, double _z, List<Tool> _tools)
        {
            SizeX = new Interval(0, _x);
            SizeY = new Interval(0, _y);
            SizeZ = new Interval(0, _z);

            Volume = new Box(Plane.WorldXY, SizeX, SizeY, SizeZ).ToBrep();
            Shape = Beaver.BedShape.rectangular;
            BedShape = new Rectangle3d(Plane.WorldXY, SizeX, SizeY).ToNurbsCurve();
            Tools = _tools;
        }

        public Machine(Interval _x, Interval _y, Interval _z, List<Tool> _tools)
        {
            SizeX = _x;
            SizeY = _y;
            SizeZ = _z;

            Volume = new Box(Plane.WorldXY, SizeX, SizeY, SizeZ).ToBrep();
            Shape = Beaver.BedShape.rectangular;
            BedShape = new Rectangle3d(Plane.WorldXY, SizeX, SizeY).ToNurbsCurve();
            Tools = _tools;
        }

        public Machine(Interval _x, Interval _z, List<Tool> _tools)  //Circular printer, origin at center
        {

            SizeX = _x;
            double _diameter = _x.Max - _x.Min;
            SizeY = SizeX;
            SizeZ = _z;
            Plane pp = new Plane(new Point3d(0, 0, _z.Min), Vector3d.ZAxis);

            var cr = new Circle(pp , _diameter * 0.5);
            //var cr = new Circle(Plane.WorldXY, _diameter * 0.5);
            Volume = Brep.CreateFromCylinder(new Cylinder(cr, _z.Max-_z.Min), true, true);
            BedShape = cr.ToNurbsCurve();
            Shape = Beaver.BedShape.circular;
            Tools = _tools;
        }

        public override string ToString()
        {
            string msg = "";
            if (name.Length > 0)
            {
                msg += "machine name : " + name + "\n";
            }
            if (IsCircular)
            {
                msg += "machine shape : circular\n";
                double diameter = SizeX.Max - SizeX.Min;
                msg += "machine radius : " + diameter.ToString() + "mm\n";
                msg += "machine height : " + SizeZ.ToString() + "mm\n";
            }
            if (IsRectangular)
            {
                msg += "machine shape : rectangular\n";
                msg += "machine x size : " + SizeX.ToString() + "mm\n"; 
                msg += "machine y size : " + SizeX.ToString() + "mm\n";
                msg += "machine y size  : " + SizeZ.ToString() + "mm\n";
            }

            for(int i = 0; i < Tools.Count-1; i++)
            {
                msg += "\ntool 0 ::\n";
                msg += Tools[i].ToString() + "\n";
            }

            return msg;
        }

        public bool IsCircular
        {
            get { return Shape == Beaver.BedShape.circular; }
        }
        public bool IsRectangular
        {
            get { return Shape == Beaver.BedShape.rectangular; }
        }
    }

    public class Config //read and set configuration...............................................
    {
        public Hashtable Settings;
        public Machine Machine;

        public Config()
        {

        }

        public Config(List<string> _config) //construct config instance with setting strings
        {
            Settings = Config.ToHashtable(_config);
        }

        public static Hashtable ToHashtable(List<string> _config)
        {
            Hashtable s = new Hashtable();
            foreach (string cc in _config)
            {
                if (!cc.Contains("[") && !s.ContainsKey(GetSettingName(cc)))
                {
                    s.Add(GetSettingName(cc), GetSettingValue(cc));
                }
            }
            return s;
        }

        public static void CreateSlider(Config _config, GH_Component _this, bool _createZero)  //create slider if find setting
        {
            Hashtable cfg = _config.Settings;

            for (int i = 0; i < _this.Params.Input.Count - 1; i++)
            {
                string key = _this.Params.Input[i].Name;
                if (_this.Params.Input[i].SourceCount == 0)
                {
                    if (cfg.ContainsKey(key))
                    {
                        string value = cfg[key].ToString();
                        if (value.Contains("%"))
                        {
                            //value.Trim('%');
                            double v = Convert.ToDouble(value.Trim('%')) / 100;
                            value = v.ToString();
                        }
                        if (value.Contains(","))
                        {
                            //value.Trim('%');
                            double v = Convert.ToDouble(value.Split(',')[0]) / 100;
                            value = v.ToString();
                        }
                        Extension.Slider(i, key, Convert.ToDouble(value), _this);
                    }
                    else
                    {
                        if (_createZero)
                        { 
                            //Extension.Slider(i, key, 0, 2, _this);
                            Extension.Slider(i, key, -1, -1, 100, 2, _this);
                        }
                    }
                }
            }
        }

        public static List<string> UpdateConfig(List<string> _config, GH_Component _this) //update input config string by checking component param input
        {
            List<string> cfgOut = new List<string>();
            Hashtable cfg = Config.ToHashtable(_config);

            for (int i = 0; i < _this.Params.Input.Count - 1; i++)
            {
                string key = _this.Params.Input[i].Name;
                if (_this.Params.Input[i].SourceCount == 0)
                {
                    if (cfg.ContainsKey(key))
                    {
                        string value = cfg[key].ToString();
                        if (value.Contains("%"))
                        {
                            //value.Trim('%');
                            double v = Convert.ToDouble(value.Trim('%')) / 100;
                            value = v.ToString();
                        }
                        if (value.Contains(","))
                        {
                            //value.Trim('%');
                            double v = Convert.ToDouble(value.Split(',')[0]) / 100;
                            value = v.ToString();
                        }

                        Extension.Slider(i, key, Convert.ToDecimal(value), 2, _this);
                        string s = key + " = " + value;
                        cfgOut.Add(s);
                    }
                    else
                    {
                        Extension.Slider(i, key, 0, 2, _this);
                        string s = key + " = " + _this.Params.Input[i].VolatileData.AllData(false).ToString();
                        //string s = key + " = " + _this.DA.GetData

                        cfgOut.Add(s);
                    }
                }
                else
                {
                    string s = key + " = " + _this.Params.Input[i].VolatileData.AllData(false).ToString();
                    cfgOut.Add(s);
                }
            }

            return cfgOut;
        }

        public static string[] GetSetting(string _setting)
        {
            string[] setting = new string[2];

            if (_setting.Contains("="))
            {
                string[] str = _setting.Split('=');
                setting[0] = str[0].Replace(" ", "");
                setting[1] = str[1].Replace(" ", "");
                return setting;
            }
            else
            {
                return null;
            }
        }

        public static string GetSettingName(string _setting)
        {
            if (_setting.Contains("="))
            {
                    string[] str = _setting.Split('=');
                    string val = str[0].Replace(" ", "");
                    return val;
            }
            else
            {
                return null;
            }

         }

        public static string GetSettingValue(string _setting)
        {
            if (_setting.Contains("="))
            {
                string[] str = _setting.Split('=');
                string val = str[1].Replace(" ", "");
                return val;
            }
            else
            {
                return null;
            }
        }

        public enum PrinterSetting //setting to use from ini file
        {
            print_shape,
            printer_x,
            printer_y,
            printer_height,
            allow_negavtive_x,
            allow_negavtive_y,
            allow_negavtive_z
        }


        public enum RouterSetting
        {
            x_size,
            y_size,
            z_size
        }

        public enum PrintSetting
        {
            xy_tolerance,
            layer_height,
            first_layer_height,
            disable_fan_first_layers,
            extrusion_width,
            infill_extrusion_width,
            first_layer_extrusion_multiplier,
            extrusion_multiplier,
            infill_overlap,
            perimeter,
            fill_density,
            print_speed,
            perimeter_speed,
            external_perimeter_speed,
            small_perimeter_speed,
            corner_pause,
            corner_threshold,
            infill_speed,
            first_layer_speed,
            travel_speed,
            speed_multiplier,
            raft_layers
        }

        public enum FilamentSetting
        {
            filament_name,
            filament_diameter,
            retract_speed,
            retract_length,
            filament_density,
            filament_cost,
            disable_fan_first_layers,
            temperature,
            bed_temperature
        }
    } 

    public class Block //contains location and speed, etc of gcode point ie each line of code........................
    {
        public Point3d? coordinate;
        public Vector3d? direction;  //six axis milling
        public Arc? arc;
        public double? F;            //Feed Rate mm/min
        public double? ES;        //Extrusion Rate mm3/min, can only calculate E from ERate between 2 points OR Speed if spindle
        //public int? S;
        public int? G;
        public int? M;
        public int? N;
        public int? P;
        public int? T;
        public string comment = "";

        public Block(Point3d _location, double _feedRate, int _toolIndex) //...........waypoint rapid move 
        {
            G = 0;
            coordinate = _location;
            F = _feedRate;
            T = _toolIndex;
        }

        public Block(Point3d _location, double _feedRate, double? _extrusionRate, int _toolIndex) //...........waypoint 
        {
            G = 1;
            coordinate = _location;
            F = _feedRate;
            ES = _extrusionRate;
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

        public Block(Point3d? _location, Vector3d? _direction, double? _feedRate, double? _ES, int _toolIndex) //generic wayPoint for printing and milling
        {
            if (_feedRate == null)
            {
                Console.WriteLine("must provide feed rate for way point");
                return;
            }
            if(_ES == null)
            {
                G = (int)GList.RapidPosition;
            }
            else
            {
                G = (int)GList.LinearPosition;
                ES = _ES;
            }

            coordinate = _location;
            direction = _direction;
            F = _feedRate;
            T = _toolIndex;
        }

        

        public Block(Point3d? _centre, Arc _arc, double _feedRate, double _ES, int _toolIndex) //..........arc move
        {
            coordinate = _centre;
            arc = _arc;
            F = _feedRate;
            ES = _ES;
            T = _toolIndex;
        }

        public Block(int _G, int _M, int _N, int _S, int _P)  //..................command block
        {
            int G = _G;
            int M = _M;
            int N = _N;
            int S = _S;
            int P = _P;
        }

        //public Block()
        public string GetGCode(bool _getN)
        {
            string text = "";
            if (_getN && this.N.HasValue)
            {
                text += "N";
                text += this.N.Value;
                text += " ";
            }
            if (this.G.HasValue)
            {
                text += "G";
                text += this.G.Value.ToString();
                text += " ";
            }
            if (this.M.HasValue)
            {
                text += "M";
                text += this.M.Value.ToString();
                text += " ";
            }
            if (this.P.HasValue)
            {
                text += "P";
                text += this.P.Value.ToString();
                text += " ";
            }
            if (this.T.HasValue)
            {
                text += "T";
                text += this.T.Value.ToString();
                text += " ";
            }
            return text;
        }
    } 

    public class Path  //contain list of Blocks ===========================================================================================
    {
        public List<Block> blocks = new List<Block>(); //...default empty value
        public int? startCoori;
        public int? endCoori;
        public Color DefaultColor = Color.Red;

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

        public Path(Path _other) //.........................construct path from another curve
        {
            blocks = new List<Block>(_other.blocks);
            startCoori = _other.startCoori;
            endCoori = _other.endCoori;
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

        public Interval FeedRange()
        {
            Interval fRange = new Interval(0,0);
            if (this.startCoori.HasValue)
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

        public Interval ESRange()
        {
            Interval esRange = new Interval(0, 0);
            if (this.startCoori.HasValue)
            {
                double low = (double)this.blocks[(int)this.startCoori].ES;
                double high = (double)this.blocks[(int)this.startCoori].ES;
                for (int i = 0; i < this.Length; i++)
                {
                    if (this.blocks[i].ES.HasValue && this.blocks[i].ES > high)
                    {
                        high = (double)this.blocks[i].ES;
                    }
                    if (this.blocks[i].ES.HasValue && this.blocks[i].ES < low)
                    {
                        low = (double)this.blocks[i].ES;
                    }
                }

                esRange = new Interval(low, high);
            }
            return esRange;
        }

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

        public void Preview(int _colorIndex, Path _path)//.....................................................custom preview for path
        {
            if (_path.startCoori.HasValue) //is a path with way point
            {
                Point3d lastPt = new Point3d(_path.blocks[_path.startCoori.Value].coordinate.Value);
                for (int i = _path.startCoori.Value+1; i < _path.endCoori.Value; i++)
                {
                    //List<Line> lines = new List<Line>();
                    
                    if (_path.blocks[i].coordinate.HasValue)
                    {
                        Line ln = new Line(lastPt, _path.blocks[i].coordinate.Value);
                         //GH_PreviewWireArgs args = new GH_PreviewWireArgs(
                        


                    }
                }
            }
        }

        public static List<Path> Join(List<Path> _paths, double _tolerance, bool _preserveDirection)
        {
            System.Diagnostics.Debug.WriteLine("\njoin begins");
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


    }

    public class Gcode  //Gcode compiler and checker, contain collection of path...........................................................
    {
        readonly public List<string> line = new List<string>();  //lines of code
        public List<Path> paths = new List<Path>();
        public Config config;

        public Gcode(List<string> _line)
        {
            line = _line;
        }

        public Gcode() { } //empty constructor

        public Gcode(Path _path, Config _config)
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

    public static class Extension
    {
        public static void Slider(int _input, string _name, Decimal _defVal, int _psc, GH_Component _this) //Add Slider 
        {
            if (_this.Params.Input[_input].SourceCount == 0) //only add slider if no source input
            {
                var slider = new Grasshopper.Kernel.Special.GH_NumberSlider();
                slider.CreateAttributes();
                slider.Name = _name;
                slider.NickName = _name;

                
                slider.Slider.Maximum = _defVal;
                slider.Slider.DecimalPlaces = _psc;
                slider.SetSliderValue(_defVal);

                slider.Attributes.Pivot = new PointF((float)_this.Attributes.DocObject.Attributes.Bounds.Left - slider.Attributes.Bounds.Width - 70, (float)_this.Params.Input[_input].Attributes.Bounds.Y + 10);
                _this.OnPingDocument().AddObject(slider, false);

                _this.Params.Input[_input].AddSource(slider);
                slider.ExpireSolution(true);
            }
        }

        public static void Slider(int _inputIndex, string _name, double _defaultValue, double _min, double _max, int _precision, GH_Component _this) //Add Slider 
        {
            if (_this.Params.Input[_inputIndex].SourceCount == 0) //only add slider if no source input
            {
                var slider = new Grasshopper.Kernel.Special.GH_NumberSlider();
                slider.CreateAttributes();
                slider.Name = _name;
                slider.NickName = _name;
                
                slider.Slider.Maximum = Convert.ToDecimal(_max);
                slider.Slider.Minimum = Convert.ToDecimal(_min);
                slider.Slider.DecimalPlaces = _precision;
                slider.SetSliderValue(Convert.ToDecimal(_defaultValue));

                slider.Attributes.Pivot = new PointF((float)_this.Attributes.DocObject.Attributes.Bounds.Left - slider.Attributes.Bounds.Width - 70, (float)_this.Params.Input[_inputIndex].Attributes.Bounds.Y + 10);
                _this.OnPingDocument().AddObject(slider, false);

                _this.Params.Input[_inputIndex].AddSource(slider);
                slider.ExpireSolution(true);
            }
        }

        public static void Slider(int _inputIndex, string _name, double _defaultValue, GH_Component _this) //Add Slider 
        {
            double _max;
            double _min;
            int _precision;
            int dPlace = 0;
            if (Math.Abs(_defaultValue) < 1)
            {
                dPlace = 0;
            }
            else
            {
                dPlace = Convert.ToInt32(Math.Abs(_defaultValue)).ToString().Length;
            }

            if (_defaultValue > 0)
            {
                _max = Math.Pow(10, dPlace);
                _min = 0;
            }
            else
            {
                _min = Math.Pow(10, dPlace) * -1;
                _max = 0;
            }

            string p = _defaultValue.ToString();
            if (p.Contains("."))
            {
                _precision = p.Split('.')[1].Length;
            }
            else
            {
                _precision = 0;
            }

            Extension.Slider(_inputIndex, _name, _defaultValue, _min, _max, _precision, _this);
        }

        public static void DropDown(int _input, string _name, List<string> _items, GH_Component _this) //Add Dropdown menue
        {
            if (_this.Params.Input[_input].SourceCount == 0 && _this.Params.Input[0].SourceCount > 0)
            {
                var vallist = new Grasshopper.Kernel.Special.GH_ValueList();
                vallist.CreateAttributes();
                vallist.Name = _name;
                vallist.NickName = _name;
                vallist.ListMode = Grasshopper.Kernel.Special.GH_ValueListMode.DropDown;

                int inputcount = _this.Params.Input[_input].SourceCount;
                vallist.Attributes.Pivot = new PointF((float)_this.Attributes.DocObject.Attributes.Bounds.Left - vallist.Attributes.Bounds.Width - 100, (float)_this.Params.Input[_input].Attributes.Bounds.Y);

                vallist.ListItems.Clear();

                for (int i = 0; i < _items.Count; i++)
                {
                    vallist.ListItems.Add(new Grasshopper.Kernel.Special.GH_ValueListItem(_items[i], i.ToString()));
                }
                vallist.Description = _items.Count.ToString() + " ";

                _this.OnPingDocument().AddObject(vallist, false);

                _this.Params.Input[_input].AddSource(vallist);
                vallist.ExpireSolution(true);


                //_this.AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "dropdown added");
            }
        }
    }

    public class Resources
    {
        private static ResourceManager resourceMan;

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public static ResourceManager ResourceManager
        {
            get
            {
                if (resourceMan == null)
                {
                    ResourceManager resourceManager = resourceMan = new ResourceManager("Beaver.Properties.Resources", typeof(Resources).Assembly);
                }
                return resourceMan;
            }
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public static CultureInfo Culture { get; set; }

        public static Bitmap joker
        {
            get
            {
                object obj = ResourceManager.GetObject("joker2", Culture);
                return (Bitmap)obj;
            }
        }
    }
}


