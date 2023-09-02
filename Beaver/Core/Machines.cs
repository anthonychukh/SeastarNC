using Grasshopper.Kernel.Data;
using Grasshopper;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Math;

namespace Seastar.Core
{

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

    public interface ITool
    {
        string ToolName { get; }
        string Descriptions { get; set; }
        /// <summary> Tool offset from end effector origin. </summary>
        double OffsetX { get; set; }
        /// <summary> Tool offset from end effector origin. </summary>
        double OffsetY { get; set; }
        /// <summary> Tool offset from end effector origin. </summary>
        double OffsetZ { get; set; }
        /// <summary> Tool changing gcode. </summary>
        string ToolChangeGcode { get; set; }
        Guid Guid { get; }
    }

    public class Extruder : ITool
    {
        string toolName = "";
        string descriptions = "";
        double diameter = 0;
        double offsetX = 0;
        double offsetY = 0;
        double offsetZ = 0;
        string toolChangeGCode = "";

        public string ToolName { get { return toolName; } }
        public string Descriptions { get { return descriptions; } set { descriptions = value; } }
        public double OffsetX { get { return offsetX; } set { offsetX = value; } }
        public double OffsetY { get { return offsetY; } set { offsetY = value; } }
        public double OffsetZ { get { return offsetZ; } set { offsetZ = value; } }
        public string ToolChangeGcode { get { return toolChangeGCode; } set { toolChangeGCode = value; } }
        public Guid Guid => new Guid();

        public double Diameter { get { return diameter; } set { diameter = value; } }
        /// <summary> List of all filament this tool can use. </summary>
        public Filament Filaments { get; set; }

        public Extruder(string _name,  string _description, double _diameter, Filament _filaments)
        {
            toolName = _name;
            descriptions = _description;
            diameter = _diameter;
            Filaments = _filaments;
        }
    }

    /*
 * filament_name,
        filament_diameter,
        retract_speed,
        retract_length,
        filament_density,
        filament_cost,
        disable_fan_first_layers,
        temperature,
        bed_temperature
 */
    public class Filament
    {
        public string Name { get;}
        public double Diameter { get; }
        public double Temperature { get; set; } = 0.0;
        public double BedTemperature { get; set; } = 0.0;
        public double RetractSpeed { get; set; } = 0.0;
        public double RetractDistance { get; set; } = 0.0;
        public string MaterialChangeGCode { get; set; } = "";
        public Guid Guid => new Guid();

        public Filament(string _name, double _diameter, double _temperature, double _bedTemp)
        {
            Name = _name;
            Diameter = _diameter;
            Temperature = _temperature;
            BedTemperature = _bedTemp;
        }
    }

    public class Spindle : ITool
    {
        string toolName = "";
        string descriptions = "";
        double diameter = 0;
        double offsetX = 0;
        double offsetY = 0;
        double offsetZ = 0;
        string toolChangeGCode = "";

        public string ToolName { get { return toolName; } }
        public string Descriptions { get { return descriptions; } set { descriptions = value; } }
        public double OffsetX { get { return offsetX; } set { offsetX = value; } }
        public double OffsetY { get { return offsetY; } set { offsetY = value; } }
        public double OffsetZ { get { return offsetZ; } set { offsetZ = value; } }
        public string ToolChangeGcode { get { return toolChangeGCode; } set { toolChangeGCode = value; } }
        public Guid Guid => new Guid();

        public double MaxSpeed { get; set; }
        public double NorminalDiameter { get; set; }
        public double PlungeRate { get; set; }
        public double RetractRate { get; set; }
        public Spindle() { }
    }

    public class GenericTool : ITool
    {
        string toolName = "";
        string descriptions = "";
        double diameter = 0;
        double offsetX = 0;
        double offsetY = 0;
        double offsetZ = 0;
        string toolChangeGCode = "";

        public string ToolName { get { return toolName; } }
        public string Descriptions { get { return descriptions; } set { descriptions = value; } }
        public double OffsetX { get { return offsetX; } set { offsetX = value; } }
        public double OffsetY { get { return offsetY; } set { offsetY = value; } }
        public double OffsetZ { get { return offsetZ; } set { offsetZ = value; } }
        public string ToolChangeGcode { get { return toolChangeGCode; } set { toolChangeGCode = value; } }
        public Guid Guid => new Guid();
    }
    /// <summary>
    /// Tools contains properties for milling and extruder, such as offset.
    /// </summary>
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
        public ToolType ToolType;
        public string Descriptions = "";
        public double Diameter = 0;
        public double OffsetX = 0;
        public double OffsetY = 0;
        public double OffsetZ = 0;
        public double Max = double.MaxValue;
        public double Min = 0;
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


        public Tool()
        {

        }

        /// <summary>
        /// Extruder tool constructor only
        /// </summary>
        public Tool(string _toolName, ToolType _extruder, double _diameter, double _offsetX, double _offsetY, double _offsetZ)
        {

            ToolName = _toolName;
            ToolType = _extruder;
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
            ToolType = _mill;
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
            if (IsExtruder)
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

        //public override string ToString()
        //{
        //    return "Seastar Machine";
        //}

        public bool IsMill
        {
            get { return ToolType == ToolType.mill; }
        }
        public bool IsExtruder
        {
            get { return ToolType == ToolType.extruder; }
        }
    }

    /// <summary>
    /// SPM axes to be added to RotationalAxes Class
    /// </summary>
    public class SPM
    {
        //SPM config, assume η is equally divided
        readonly public double alpha1; //radian
        readonly public double alpha2;
        readonly public double gamma;
        readonly public double beta;
        readonly public double radius;      //radius of base/chasis, from axis to leg
        public Plane homePlane = Plane.WorldXY;    //also centre of rotation
        public Plane targetPlane = Plane.WorldXY;
        public Plane mountPlane = Plane.WorldXY;
        private double minVal = 0.001;


        public readonly double offsetX = 0;   //end effector offset from center
        public readonly double offsetY = 0;
        public readonly double offsetZ = 0;
        public readonly double offsetXmount = 0;   //mount offset from center
        public readonly double offsetYmount = 0;
        public readonly double offsetZmount = 0;

        public List<Vector3d> axesU; //axes 
        public List<Vector3d> axesW;
        public List<Vector3d> axesV;

        public double angleAHome = 0; //home state
        public double angleBHome = 0;
        public double angleCHome = 0;
        public double angleA; //current state
        public double angleB;
        public double angleC;

        public SPM()
        {

        }

        public SPM(SPM _other)
        {
            homePlane = _other.homePlane;
            alpha1 = _other.alpha1;
            alpha2 = _other.alpha2;
            gamma = _other.gamma;
            beta = _other.beta;
            radius = _other.radius;
        }
        public SPM(Plane _homePlane, double _alpha1, double _alpha2, double _gamma, double _beta, double _radius)
        {
            if (_alpha1 <= minVal) { _alpha1 = minVal; }
            if (_alpha2 <= minVal) { _alpha2 = minVal; }
            if (_gamma <= minVal) { _gamma = minVal; }
            if (_beta <= minVal) { _beta = minVal; }
            if (_radius <= minVal) { _radius = minVal; }

            homePlane = _homePlane;
            alpha1 = _alpha1;
            alpha2 = _alpha2;
            gamma = _gamma;
            beta = _beta;
            radius = _radius;
        }
        public SPM(Plane _homePlane, double _alpha1, double _alpha2, double _gamma, double _beta, double _radius, Vector3d _planeOffset, Vector3d _mountOffset)
        {
            if (_alpha1 <= minVal) { _alpha1 = minVal; }
            if (_alpha2 <= minVal) { _alpha2 = minVal; }
            if (_gamma <= minVal) { _gamma = minVal; }
            if (_beta <= minVal) { _beta = minVal; }
            if (_radius <= minVal) { _radius = minVal; }

            homePlane = _homePlane;
            alpha1 = _alpha1;
            alpha2 = _alpha2;
            gamma = _gamma;
            beta = _beta;
            radius = _radius;
            offsetX = _planeOffset.X;
            offsetY = _planeOffset.Y;
            offsetZ = _planeOffset.Z;
            offsetXmount = _mountOffset.X;
            offsetYmount = _mountOffset.Y;
            offsetZmount = _mountOffset.Z;
        }


        public double C(double _angle)
        {
            return Math.Cos(_angle);
        }

        public double S(double _angle)
        {
            return Math.Sin(_angle);
        }

        /// <summary>
        /// set plane offset
        /// </summary>
        /// <param name="_x">x</param>
        /// <param name="_y">y</param>
        /// <param name="_z">z</param>
        //public void SetPlaneOffset(double _x, double _y, double _z)
        //{
        //    offsetX = _x;
        //    offsetY = _y;
        //    offsetZ = _z;
        //}

        /// <summary>
        /// set plane offset
        /// </summary>
        /// <param name="_offset"></param>
        //public void SetPlaneOffset(Vector3d _offset)
        //{
        //    offsetX = _offset.X;
        //    offsetY = _offset.Y;
        //    offsetZ = _offset.Z;
        //}

        /// <summary>
        /// Return offset plane
        /// </summary>
        /// <returns></returns>
        public Plane offsetPlane()
        {
            Vector3d ov = new Vector3d(0, 0, 0);
            ov += targetPlane.XAxis * offsetX;
            ov += targetPlane.YAxis * offsetY;
            ov += targetPlane.ZAxis * offsetZ;
            Plane op = new Plane(targetPlane);
            op.Origin += ov;

            return op;
        }

        public Vector3d offsetVector()
        {
            Vector3d ov = new Vector3d(0, 0, 0);
            ov += targetPlane.XAxis * offsetX;
            ov += targetPlane.YAxis * offsetY;
            ov += targetPlane.ZAxis * offsetZ;
            ov *= -1;

            return ov;
        }

        public Vector3d offsetMountVector()
        {
            Vector3d ov = new Vector3d(0, 0, 0);
            ov += homePlane.XAxis * offsetXmount;
            ov += homePlane.YAxis * offsetYmount;
            ov += homePlane.ZAxis * offsetZmount;
            //ov *= -1;

            return ov;
        }
        /// <summary>
        /// offset home plane and mount plane base on new target and offset values
        /// </summary>
        public void UpdateHomeMountPlane()
        {
            this.homePlane = new Plane(this.targetPlane.Origin + this.offsetVector(), Vector3d.ZAxis); //offset plane base on new target
            this.mountPlane = new Plane(this.homePlane.Origin + this.offsetMountVector(), Vector3d.ZAxis);
        }

        public List<double> GetAnglesList(bool useDegree)
        {
            List<double> angles = new List<double>();
            if (useDegree)
            {
                angles.Add(angleA * 180 / Math.PI);
                angles.Add(angleB * 180 / Math.PI);
                angles.Add(angleC * 180 / Math.PI);
            }
            else
            {
                angles.Add(angleA);
                angles.Add(angleB);
                angles.Add(angleC);
            }
            return angles;
        }

        /*---------------
         * Following kinemetics from paper bellow
         * 
         * [Optimum design of spherical parallel manipulators 
         * for a prescribed workspace]
         * 
         * Shaoping Bai *
         * Department of Mechanical Engineering, Aalborg University, Denmark
         * 
         * */

        /// <summary>
        /// Initiate internal configuration. Update it with UpdateConfig() 
        /// </summary>
        /// <param name="_moveToTarget">this should match UpdateConfig()</param>
        public void ComputeInitialConfig(bool _moveToTarget)  //use beta to find initial configuration, Chasis flat at initial state
        {
            List<Vector3d> lnU = new List<Vector3d>();
            List<Vector3d> lnW = new List<Vector3d>();
            List<Vector3d> lnV = new List<Vector3d>();
            //double length = radius / Math.Sin(gamma);
            Plane tempHome = homePlane;
            tempHome.Origin = Point3d.Origin;

            if (_moveToTarget)
            {
                tempHome.YAxis = tempHome.YAxis * -1;
                tempHome.XAxis = tempHome.XAxis * -1;
                tempHome.ZAxis = tempHome.ZAxis * -1;
            }

            Transform cb = Transform.ChangeBasis(tempHome, Plane.WorldXY);


            for (int i = 0; i < 3; i++)
            {
                double n = 2 * i * Math.PI / 3;

                Vector3d v = new Vector3d(-1 * S(n) * S(beta), C(n) * S(beta), C(beta));
                v.Unitize();
                v.Transform(cb);

                lnV.Add(v);

                double _initialA = this.InputAngle(v, i);

                switch (i)
                {
                    case 0:
                        angleAHome = _initialA; // - PI*0.5;
                        break;
                    case 1:
                        angleBHome = _initialA; // - PI*0.5 - PI*2/3;
                        break;
                    case 2:
                        angleCHome = _initialA; // - PI * 0.5 + PI * 2 / 3;
                        break;
                }


                Vector3d u = new Vector3d(-1 * S(n) * S(gamma), C(n) * S(gamma), -1 * C(gamma));
                u.Unitize();
                lnU.Add(u);

                Plane pln = new Plane(homePlane.Origin, u, Vector3d.ZAxis);
                pln.Rotate(_initialA, u);
                Vector3d w = new Vector3d(u);
                w.Rotate(alpha1, pln.ZAxis);

                w.Unitize();
                lnW.Add(w);
            }

            this.axesU = lnU;
            this.axesW = lnW;
            this.axesV = lnV;
        }


        /// <summary>
        /// Update and save current position internally
        /// </summary>
        /// <param name="_target">target plane of end effector</param>
        /// <param name="_moveToTarget">if true, the SPM will move to target location and default Z axis point downward. if false, SPM will be at origin and default Z point up</param>
        public bool UpdateConfig(Plane _target, bool _moveToTarget)
        {
            this.targetPlane = _target;
            List<Vector3d> lnU = new List<Vector3d>();
            List<Vector3d> lnW = new List<Vector3d>();
            List<Vector3d> lnV = new List<Vector3d>();
            //double length = radius / Math.Sin(gamma);

            //Plane flipXY = new Plane(Plane.WorldXY);
            //flipXY.Flip();
            //flipXY.Origin = _target.Origin;
            //flipXY.XAxis = flipXY.XAxis * -1;
            //flipXY.YAxis = flipXY.YAxis * -1;
            //Plane flipTarget = new Plane(_target);
            //flipTarget.Flip();

            //Plane targetXY = new Plane(flipXY);
            //targetXY.Origin = _target.Origin;

            //move target orgin to worldXY and flip
            //in calculation, default SPM position with Z axis align with world Z
            targetPlane.Origin = Point3d.Origin;
            //targetPlane.Rotate(PI, Vector3d.XAxis);
            //targetPlane.Flip();
            if (_moveToTarget)
            {
                targetPlane.YAxis = targetPlane.YAxis * -1;
                targetPlane.XAxis = targetPlane.XAxis * -1;
                targetPlane.ZAxis = targetPlane.ZAxis * -1;
            }

            Transform cb = Transform.ChangeBasis(targetPlane, Plane.WorldXY);
            Transform cbb = Transform.ChangeBasis(targetPlane, _target); //should just be a flip and translation

            for (int i = 0; i < 3; i++)
            {
                double n = 2 * i * Math.PI / 3;

                Vector3d v = new Vector3d(-1 * S(n) * S(beta), C(n) * S(beta), C(beta));
                v.Unitize();

                v.Transform(cb); //move v to target plane

                double _initialA = this.InputAngle(v, i);
                if (double.IsNaN(_initialA))
                {
                    return false;
                }
                switch (i)
                {
                    case 0:
                        angleA = _initialA;
                        break;
                    case 1:
                        angleB = _initialA;
                        break;
                    case 2:
                        angleC = _initialA;
                        break;
                }

                Vector3d u = new Vector3d(-1 * S(n) * S(gamma), C(n) * S(gamma), -1 * C(gamma)); //this work for world XY
                u.Unitize();

                Plane pln = new Plane(Point3d.Origin, u, Vector3d.ZAxis);
                pln.Rotate(_initialA, u);
                Vector3d w = new Vector3d(u);
                w.Rotate(alpha1, pln.ZAxis);
                w.Unitize();

                if (_moveToTarget)
                {
                    v.Transform(cbb);
                    u.Transform(cbb);
                    w.Transform(cbb);
                    //Transform ct = Transform.ChangeBasis(targetXY, Plane.WorldXY);
                    targetPlane = _target;
                }

                lnV.Add(v);
                lnU.Add(u);
                lnW.Add(w);
            }

            this.axesU = lnU;
            this.axesW = lnW;
            this.axesV = lnV;

            this.UpdateHomeMountPlane();
            return true;
        }


        /// <summary>
        /// Calculate input angle with the current internal configuration. Works in worldXY only
        /// </summary>
        /// <param name="v"></param>
        /// <param name="i"></param>
        /// <returns></returns>
        public double InputAngle(Vector3d v, int i)
        {
            double n = 2 * i * Math.PI / 3;
            v.Rotate(-1 * n, Vector3d.ZAxis); // return v to leg 1 position

            double a = v.Y * (S(gamma) * C(alpha1) - C(gamma) * S(alpha1)) - v.Z * (C(gamma) * C(alpha1) + S(gamma) * S(alpha1)) - C(alpha2);
            double b = v.X * S(alpha1);
            double c = v.Y * (S(gamma) * C(alpha1) + C(gamma) * S(alpha1)) - v.Z * (C(gamma) * C(alpha1) - S(gamma) * S(alpha1)) - C(alpha2);

            double t = (-2 * b - Math.Sqrt(4 * b * b - 4 * a * c)) / (2 * a);

            double _initialA = 2 * Math.Atan(t);
            return _initialA;
        }


        public List<double> InputAngle()
        {
            List<double> ial = new List<double>();
            for (int i = 0; i < 3; i++)
            {
                double ia = InputAngle(this.axesV[i], i);
                ial.Add(ia);
            }
            return ial;
        }

        /// <summary>
        /// Get absolute input angle. This will not update internal config
        /// </summary>
        /// <param name="_target">target plane</param>
        /// <returns></returns>
        public List<double> InputAngle(Plane _target)
        {
            List<double> ial = new List<double>();
            for (int i = 0; i < 3; i++)
            {
                double n = 2 * i * Math.PI / 3;

                Vector3d v = new Vector3d(-1 * S(n) * S(beta), C(n) * S(beta), C(beta));
                v.Unitize();
                Transform cb = Transform.ChangeBasis(_target, Plane.WorldXY);
                v.Transform(cb);

                ial.Add(this.InputAngle(v, i));
            }
            return ial;
        }

        public bool TestInputAngle(Plane _target)
        {
            List<double> ial = new List<double>();
            for (int i = 0; i < 3; i++)
            {
                double n = 2 * i * Math.PI / 3;

                Vector3d v = new Vector3d(-1 * S(n) * S(beta), C(n) * S(beta), C(beta));
                v.Unitize();
                Transform cb = Transform.ChangeBasis(_target, Plane.WorldXY);
                v.Transform(cb);


                if (double.IsNaN(this.InputAngle(v, i)))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Draw the arcs that represent the SPM arms. Make sure internal configuration has been updated.
        /// </summary>
        /// <returns></returns>
        public DataTree<Arc> LinkArc()
        {
            //Vector3d os = new Vector3d(targetPlane.XAxis * offsetX + targetPlane.YAxis * offsetY+ targetPlane.ZAxis * offsetZ);
            Vector3d os = this.offsetVector();
            Transform ost = Transform.Translation(os * -1);

            DataTree<Arc> la = new DataTree<Arc>();

            for (int i = 0; i < 3; i++)
            {

                GH_Path pth = new GH_Path(i);

                Point3d spt = new Point3d(this.homePlane.Origin + this.axesU[i] * radius);
                Point3d ept = new Point3d(this.homePlane.Origin + this.axesW[i] * radius);
                Vector3d bi = new Vector3d((this.axesU[i] + this.axesW[i]) / 2);
                bi.Unitize();
                Point3d mpt = new Point3d(this.homePlane.Origin + bi * radius);
                Arc a = new Arc(spt, mpt, ept);
                //a.Transform(ost);
                la.Add(a, pth);

                Point3d sptb = new Point3d(this.homePlane.Origin + this.axesW[i] * radius);
                Point3d eptb = new Point3d(this.homePlane.Origin + this.axesV[i] * radius);
                Vector3d bib = new Vector3d((this.axesV[i] + this.axesW[i]) / 2);
                bib.Unitize();
                Point3d mptb = new Point3d(this.homePlane.Origin + bib * radius);
                Arc b = new Arc(sptb, mptb, eptb);
                //b.Transform(ost);
                la.Add(b, pth);
            }
            return la;
        }

        //public DataTree<Arc> LinkArc(bool _transform)
        //{

        //}


        /// <summary>
        /// Work Space of this SPM
        /// </summary>
        /// <param name="_res">resolution along each axis</param>
        /// <param name="_intervalMax">Max value of angle interval to check</param>
        /// <returns></returns>
        public List<Point3d> WorkSpace(int _res, double _intervalMax) //find possible config/boundary of work space
        {
            int res = _res;
            Interval range = new Interval(_intervalMax * -1, _intervalMax);


            List<Point3d> validPts = new List<Point3d>();
            double dt = (range.Max - range.Min) / res;
            for (int i = 0; i < res; i++)
            {
                for (int j = 0; j < res; j++)
                {
                    for (int k = 0; k < res; k++)
                    {
                        Plane testPln = RotationAxes.RotateXYZ(Plane.WorldXY, i * dt, j * dt, k * dt);
                        if (TestInputAngle(testPln))
                        {
                            validPts.Add(new Point3d(i * dt, j * dt, k * dt));
                        }
                    }
                }
            }
            return validPts;
        }
    }
    /// <summary>
    /// Spherical axes to be added to RotationalAxes Class
    /// </summary>
    public class SphericalAxes
    {
        //public readonly double radius;
        public readonly Interval aRange;
        public readonly Interval bRange;
        public readonly Interval cRange;
        public Plane homePlane = new Plane(Plane.WorldXY); //origin
        public Plane targetPlane = new Plane(Plane.WorldXY);
        public Plane mountPlane;
        public byte system;
        public List<Line> axes = new List<Line>();

        //00 - 00(first xy) - 00(second xy) - 00(third xy)

        //00100100 = xyz = 72
        //00100001 = xzy = 33
        //00011000 = yxz = 24
        //00010010 = yzx = 18
        //00001001 = zxy = 9
        //00001000 = zxz = 8
        //00000110 = zyx = 6

        public readonly double offsetX = 0;      //end effector offset from center plane
        public readonly double offsetY = 0;
        public readonly double offsetZ = 0;
        public readonly double offsetXmount = 0; //mount offset from center plane
        public readonly double offsetYmount = 0;
        public readonly double offsetZmount = 0;

        //public double angleAHome = 0;   //home state
        //public double angleBHome = 0;
        //public double angleCHome = 0;
        public double angleA;           //current state
        public double angleB;
        public double angleC;


        public SphericalAxes() { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_homePlane"></param>
        /// <param name="_system">xyz = 72, zxz = 16, zyx = 10</param>
        /// <param name="_aRange"></param>
        /// <param name="_bRange"></param>
        /// <param name="_cRange"></param>
        /// <param name="_planeOffset"></param>
        /// <param name="_mountOffset"></param>
        public SphericalAxes(Plane _homePlane, int _system, Interval _aRange, Interval _bRange, Interval _cRange, Vector3d _planeOffset, Vector3d _mountOffset)
        {
            homePlane = _homePlane;
            targetPlane = _homePlane;
            //radius = _radius;
            system = (byte)_system;
            aRange = _aRange;
            bRange = _bRange;
            cRange = _cRange;

            offsetX = _planeOffset.X;
            offsetY = _planeOffset.Y;
            offsetZ = _planeOffset.Z;
            offsetXmount = _mountOffset.X;
            offsetYmount = _mountOffset.Y;
            offsetZmount = _mountOffset.Z;
        }

        /// <summary>
        /// offset vector pointing from origin to end end effector
        /// </summary>
        /// <returns></returns>
        public Vector3d offsetVector()
        {
            Vector3d ov = new Vector3d(0, 0, 0);
            ov += targetPlane.XAxis * offsetX;
            ov += targetPlane.YAxis * offsetY;
            ov += targetPlane.ZAxis * offsetZ;
            ov *= -1;

            return ov;
        }

        public Vector3d offsetMountVector()
        {
            Vector3d ov = new Vector3d(0, 0, 0);
            ov += homePlane.XAxis * offsetXmount;
            ov += homePlane.YAxis * offsetYmount;
            ov += homePlane.ZAxis * offsetZmount;
            //ov *= -1;

            return ov;
        }

        /// <summary>
        /// offset home plane and mount plane and axes line base on new target and offset values
        /// </summary>
        public void UpdateHomeMountPlane()
        {
            this.homePlane = new Plane(this.targetPlane.Origin + this.offsetVector(), Vector3d.ZAxis); //offset plane base on new target
            this.mountPlane = new Plane(this.homePlane.Origin + this.offsetMountVector(), Vector3d.ZAxis);

            this.axes.Clear();
            Line a1 = new Line(homePlane.Origin, targetPlane.ZAxis * offsetZ);
            Line a2 = new Line(homePlane.Origin, mountPlane.ZAxis * offsetZmount);
            this.axes.Add(a1);
            this.axes.Add(a2);
        }

        public List<double> GetAnglesList(bool useDegree)
        {
            List<double> angles = new List<double>();
            if (useDegree)
            {
                angles.Add(angleA * 180 / Math.PI);
                angles.Add(angleB * 180 / Math.PI);
                angles.Add(angleC * 180 / Math.PI);
            }
            else
            {
                angles.Add(angleA);
                angles.Add(angleB);
                angles.Add(angleC);
            }
            return angles;
        }

        public void UpdateConfig(Plane _target)
        {
            this.targetPlane = _target;
            if (!GetPlaneRotation(out angleA, out angleB, out angleC))
            {
                return;
            }
            UpdateHomeMountPlane();
        }

        public void RotatePlane(double _a, double _b, double _c)
        {
            if (system == 72) //is xyz
            {
                this.targetPlane = RotationAxes.RotateXYZ(this.homePlane, _a, _b, _c);
            }
            if (system == 8) //is zxz
            {
                this.targetPlane = RotationAxes.RotateZXZ(this.homePlane, _a, _b, _c);
            }
        }

        public bool GetPlaneRotation(out double _a, out double _b, out double _c)
        {
            if (system == 72) //is xyz
            {
                RotationAxes.GetXYZAngles(this.homePlane, this.targetPlane, out _a, out _b, out _c);
                return true;
            }
            if (system == 8) //is zxz
            {
                RotationAxes.GetZXZAngles(this.homePlane, this.targetPlane, out _a, out _b, out _c);
                return true;
            }
            else
            {
                _a = double.NaN;
                _b = double.NaN;
                _c = double.NaN;
                return false;
            }
        }

    }

    public enum RAxesType
    {
        None,
        SPM,
        SphericalAxes
    }

    /// <summary>
    /// RotationalAxes contains classes like SPM, Spherical class, etc. 
    /// </summary>
    public class RotationAxes
    {
        public SPM spm;
        public SphericalAxes sa;
        public RAxesType type;


        public RotationAxes()
        {
            type = RAxesType.None;
        }
        public RotationAxes(SPM _spm)
        {
            spm = _spm;
            type = RAxesType.SPM;
        }

        public RotationAxes(SphericalAxes _sa)
        {
            sa = _sa;
            type = RAxesType.SphericalAxes;
        }


        /// <summary>
        /// Return a rotated plane according to Euler Angle/ZXZ
        /// </summary>
        /// <param name="_pln">Plane to rotate</param>
        /// <param name="_alpha">alpha</param>
        /// <param name="_beta">beta</param>
        /// <param name="_gamma">gamma</param>
        /// <returns></returns>
        public static Plane RotateZXZ(Plane _pln, double _alpha, double _beta, double _gamma) //find tilt by Euler Angle
        {
            //Plane pln = new Plane(Plane.WorldXY);
            _pln.Rotate(_alpha, _pln.ZAxis);
            _pln.Rotate(_beta, _pln.XAxis);
            _pln.Rotate(_gamma, _pln.ZAxis);

            return _pln;
        }

        public static void GetZXZAngles(Plane _home, Plane _target, out double _z0, out double _x, out double _z1)
        {
            _home.Origin = Point3d.Origin;
            _target.Origin = Point3d.Origin;

            Transform e = Transform.ChangeBasis(_home, _target);

            _x = Acos(e.M33);
            if (_x == 0 || _x % PI == 0) //singularity
            {
                _z1 = 0;
                _z0 = Vector3d.VectorAngle(_home.XAxis, _target.XAxis);
            }
            else
            {
                _z0 = Acos(-1 * e.M23 / Sin(_x));
                _z1 = Acos(e.M32 / Sin(_x));
            }
        }

        /// <summary>
        /// Return a rotated plane by its local x, y, z axes in this order
        /// </summary>
        /// <param name="_pln">Plane to rotate</param>
        /// <param name="_x">x</param>
        /// <param name="_y">y</param>
        /// <param name="_z">z</param>
        /// <returns></returns>
        public static Plane RotateXYZ(Plane _pln, double _x, double _y, double _z) //rotate plane by local x y z axis in this order
        {
            _pln.Rotate(_x, _pln.XAxis);
            _pln.Rotate(_y, _pln.YAxis);
            _pln.Rotate(_z, _pln.ZAxis);

            return _pln;
        }

        /// <summary>
        /// Get the xyz rotation for this rotational transformation
        /// </summary>
        /// <param name="_home">Original plane</param>
        /// <param name="_target">Rotated plane</param>
        /// <param name="_x">rotation around x axis</param>
        /// <param name="_y">rotation around y axis</param>
        /// <param name="_z">rotation around z axis</param>
        public static void GetXYZAngles(Plane _home, Plane _target, out double _x, out double _y, out double _z)
        {
            _home.Origin = Point3d.Origin;
            _target.Origin = Point3d.Origin;

            Transform e = Transform.ChangeBasis(_home, _target);
            _x = -1 * Asin(e.M33);
            _y = Atan2(e.M32 / Cos(_x), e.M33 / Cos(_x));
            _z = Atan2(e.M21 / Cos(_x), e.M11 / Cos(_x));
        }


        /// <summary>
        /// Return a rotated plane by its local x, y, z axes in this order
        /// </summary>
        /// <param name="_pln">Plane to rotate</param>
        /// <param name="_a">First rotation</param>
        /// <param name="_b">Second rotation</param>
        /// <param name="_c">Third rotation</param>
        /// <param name="_mode">Rotate axes order. 00 - 00(first xy) - 00(second xy) - 00(third xy) </param>
        /// <returns></returns>
        public static Plane RotatePlane(Plane _pln, double _a, double _b, double _c, int _mode) //rotate plane by local x y z axis in this order
        {
            // 00 - 00(first xy) - 00(second xy) - 00(third xy)

            byte flag = (byte)_mode;
            //first rotation
            if ((flag & 48) == 32) _pln.Rotate(_a, _pln.XAxis);
            if ((flag & 48) == 16) _pln.Rotate(_a, _pln.YAxis);
            if ((flag & 48) == 0) _pln.Rotate(_a, _pln.ZAxis);
            //second rotation
            if ((flag & 12) == 8) _pln.Rotate(_a, _pln.XAxis);
            if ((flag & 12) == 4) _pln.Rotate(_a, _pln.YAxis);
            if ((flag & 12) == 0) _pln.Rotate(_a, _pln.ZAxis);
            //third rotation
            if ((flag & 3) == 2) _pln.Rotate(_a, _pln.XAxis);
            if ((flag & 3) == 1) _pln.Rotate(_a, _pln.YAxis);
            if ((flag & 3) == 0) _pln.Rotate(_a, _pln.ZAxis);

            return _pln;
        }

        public override string ToString()
        {
            switch (this.type)
            {
                case RAxesType.SPM:
                    return "Rotational axis: SPM";
                case RAxesType.SphericalAxes:
                    return "Rotational axis: Spherical Axes";
                case RAxesType.None:
                    return "Rotational axis: None";
            }

            return "Rotational axis type returns null";
        }
    }

    public enum DriveSystem
    {
        cartesian,
        delta
    }
    public enum BedShape
    {
        rectangular,
        circular,
        custom
    }

    public enum MachineDefault
    {
        dFeedrate = 40

    }

    /// <summary>
    /// Machine class contains machine/printer info, including axes class, tool class, dimension, etc
    /// </summary>
    public class Machine //set printer info........................................................
    {
        public Brep Volume;
        public Interval SizeX;
        public Interval SizeY;
        public Interval SizeZ;
        public int AxisCount = 3;
        readonly public BedShape Shape;
        readonly public Curve BedShape;
        readonly public DriveSystem system;
        public List<Tool> Tools;
        public string name = "";
        public bool hasCoolant;
        public Point3d parkPos = new Point3d(0, 0, 0);
        public RotationAxes rAxes = new RotationAxes();

        public bool IsDelta = false;
        readonly public Point3d[] dCol = new Point3d[3]; //contain 3 array of x,y,z of col position. ie 3 points. only represent kinematics
        private double Ae; //END_EFFECTOR_HORIZONTAL_OFFSET
        private double Ar; //DELTA_RADIUS
        private double L;  //DELTA_DIAGONAL_ROD
        private double Acz; //DELTA_Height
        private double Aco; //CARRIAGE_HORIZONTAL_OFFSET
        //private double Hez; //Tool tip offset from end effector
        private double[] ColAngle = new double[3] { 210 * PI / 180, 330 * PI / 180, 90 * PI / 180 }; //column angle. See below
        public string startCode = "";
        public string endCode = "";

        public Machine()
        {

        }
        /*  =========== Parameter essential for delta calibration ===================

                    C, Y-Axis
                    |                      _  |___| CARRIAGE_HORIZONTAL_OFFSET
                    |                     |   |   \
                    |_________ X-axis     |   |    \
                   / \                    |   |     \  DELTA_DIAGONAL_ROD
                  /   \      DELTA_HEIGHT |          \
                 /     \                  |           \    Carriage is at printer center!
                 A      B                 |_           \_____/
                              Tool_H      |            |--| END_EFFECTOR_HORIZONTAL_OFFSET
                                          |_      |----| DELTA_RADIUS
                                              |-----------| PRINTER_RADIUS

            Column angles are measured from X-axis counterclockwise
            "Standard" positions: alpha_A = 210, alpha_B = 330, alpha_C = 90
        */



        //public Machine(double _x, double _y, double _z, List<Tool> _tools)
        //{
        //    SizeX = new Interval(0, _x);
        //    SizeY = new Interval(0, _y);
        //    SizeZ = new Interval(0, _z);

        //    Volume = new Box(Plane.WorldXY, SizeX, SizeY, SizeZ).ToBrep();
        //    Shape = Seastar.BedShape.rectangular;
        //    BedShape = new Rectangle3d(Plane.WorldXY, SizeX, SizeY).ToNurbsCurve();
        //    Tools = _tools;
        //}

        /// <summary>
        /// Create a cartesian machine 
        /// </summary>
        /// <param name="_x">Print vol x domain</param>
        /// <param name="_y">Print vol y domain</param>
        /// <param name="_z">Print vol z domain</param>
        /// <param name="_tools">List of tools</param>
        public Machine(Interval _x, Interval _y, Interval _z, List<Tool> _tools)
        {
            SizeX = _x;
            SizeY = _y;
            SizeZ = _z;

            Volume = new Box(Plane.WorldXY, SizeX, SizeY, SizeZ).ToBrep();
            Shape = Seastar.Core.BedShape.rectangular;
            BedShape = new Rectangle3d(Plane.WorldXY, SizeX, SizeY).ToNurbsCurve();
            Tools = _tools;
            system = DriveSystem.cartesian;
            IsDelta = false;
        }

        /// <summary>
        /// Create a simple delta machine
        /// </summary>
        /// <param name="_d">Print volumn diamter interval</param>
        /// <param name="_z">Print volumn z domain</param>
        /// <param name="_tools">List of tools</param>
        public Machine(Interval _d, Interval _z, List<Tool> _tools)
        {
            SizeX = _d;
            double _diameter = _d.Max - _d.Min;
            double r = _diameter * 0.5;
            SizeY = SizeX;
            SizeZ = _z;
            Plane pp = new Plane(new Point3d(0, 0, _z.Min), Vector3d.ZAxis);

            var cr = new Circle(pp, r);
            //var cr = new Circle(Plane.WorldXY, _diameter * 0.5);
            Volume = Brep.CreateFromCylinder(new Cylinder(cr, _z.Max - _z.Min), true, true);
            BedShape = cr.ToNurbsCurve();


            IsDelta = true;
            Shape = Seastar.Core.BedShape.circular;
            Tools = _tools;
        }

        /// <summary>
        /// Create a delta machine
        /// </summary>
        /// <param name="_d">Print volumn diamter interval</param>
        /// <param name="_z">Print volumn height interval</param>
        /// <param name="_Ae">End effector horizontal offset</param>
        /// <param name="_Ar">Delta radius</param>
        /// <param name="_Aco">Carriage horizontal offset</param>
        /// <param name="_L">Diagonal rod length</param>
        /// <param name="_hez">Tool tip offset from end effector</param>
        /// <param name="_tools">List of tools</param>
        /// 
        public Machine(Interval _d, Interval _z, double _Ae, double _Ar, double _Aco, double _L, List<Tool> _tools, RotationAxes _axes)
        {

            SizeX = _d;
            double _diameter = _d.Max - _d.Min;
            double r = _diameter * 0.5;
            SizeY = SizeX;
            SizeZ = _z;
            Plane pp = new Plane(new Point3d(0, 0, _z.Min), Vector3d.ZAxis);

            var cr = new Circle(pp, r);
            //var cr = new Circle(Plane.WorldXY, _diameter * 0.5);
            Volume = Brep.CreateFromCylinder(new Cylinder(cr, _z.Max - _z.Min), true, true);
            BedShape = cr.ToNurbsCurve();
            rAxes = _axes;


            IsDelta = true;
            system = DriveSystem.delta;

            Shape = Seastar.Core.BedShape.circular;
            Tools = _tools;

            Ae = _Ae;
            Ar = _Ar;
            Aco = _Aco;
            L = _L;
            Acz = Sqrt(L * L - Ar * Ar);
            // double hez = _hez;

            for (int i = 0; i < ColAngle.Length; i++)
            {
                dCol[i] = new Point3d(Ar * Cos(ColAngle[i]), Ar * Sin(ColAngle[i]), _z.Min);

            }


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
            if (rAxes.type != RAxesType.None)
            {
                msg += rAxes.ToString();
            }
            for (int i = 0; i < Tools.Count - 1; i++)
            {
                msg += "\ntool 0 ::\n";
                msg += Tools[i].ToString() + "\n";
            }

            return msg;
        }

        public bool IsCircular
        {
            get { return Shape == Seastar.Core.BedShape.circular; }
        }
        public bool IsRectangular
        {
            get { return Shape == Seastar.Core.BedShape.rectangular; }
        }

        //using formular in  en.wikipedia.org/wiki/True_range_multilateration
        public Point3d DeltaForwardKinematics(double[] dZ) //dz is array of z position of carriage
        {

            Point3d[] dColP = new Point3d[3];
            for (int ind = 0; ind < 3; ind++)
            {
                dColP[ind] = new Point3d(dCol[ind].X, dCol[ind].Y, dZ[ind]);
            }

            Plane tPln = new Plane(dColP[0], dColP[1], dColP[2]);
            Transform cb = Transform.ChangeBasis(Plane.WorldXY, tPln);
            Transform cbb = Transform.ChangeBasis(tPln, Plane.WorldXY);

            Point3d[] tColP = new Point3d[3];
            for (int j = 0; j < dColP.Length; j++)
            {
                dColP[j].Transform(cb);
            }

            double x = dCol[1].X * 0.5;
            double V2 = Sqrt(dCol[2].X * dCol[2].X + dCol[2].Y * dCol[2].Y);
            double y = V2 - 2 * dColP[2].X * x / (2 * dColP[2].Y);
            double z = -1 * Sqrt(L * L - x * x - y * y);
            Point3d cartesian = new Point3d(x, y, z);
            cartesian.Transform(cbb);
            return cartesian;
        }

    }

}
