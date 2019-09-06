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
 * Seastar, formerly known as Beaver is a library written for Rhino Common and Grasshopper for 3d printer control
 * It support real-time connection between Grasshopper and open-source 3D printing firmware
 * This project was made possible by the generous support of SPACE10 through a research resident program in summer 2019
 * 
 * 
 * -------------------Disclaimer--------------------------
 * This library and Grasshopper plugin was created out of good intension for the promotion and education of technology
 * User should take their own caution and risk when using this library
 * The creator(s) of this library do not provide any garantee and waranty to use of this library and digital tool derives from this library
  

 * --------------------CAUTION---------------------------
 * Working with electronic devices could be dangerous 
 * Always take precaution when working on electronic devices
 * Make sure you are well trained and informed to work on the specific system
 * Failure to appropriately operate your machine could lead to hardware damage and/or serious injuries
 * 
*/

namespace Beaver
{
    //public class Empty : GH_Component
    //{
    //    public Empty()
    //      : base("Insert Paths", "PathInsert",
    //          "Insert Path and command at specific index",
    //          "Beaver", "Path")
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

        //public override string ToString()
        //{
        //    return "Beaver Machine";
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
                switch(i)
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
            v.Rotate(-1*n, Vector3d.ZAxis); // return v to leg 1 position

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

                
                if(double.IsNaN(this.InputAngle(v, i)))
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

            DataTree <Arc> la = new DataTree<Arc>();

            for (int i = 0; i < 3; i++) {

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
            Interval range = new Interval(_intervalMax*-1, _intervalMax);


            List<Point3d> validPts = new List<Point3d>();
            double dt = (range.Max - range.Min) / res;
            for(int i = 0; i < res; i++)
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
        public SphericalAxes(Plane _homePlane,  int _system, Interval _aRange, Interval _bRange, Interval _cRange, Vector3d _planeOffset, Vector3d _mountOffset)
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
            if(!GetPlaneRotation(out angleA, out angleB, out angleC))
            {
                return;
            }
            UpdateHomeMountPlane();
        }

        public void RotatePlane(double _a, double _b, double _c)
        {
            if(system == 72) //is xyz
            {
                this.targetPlane = RotationAxes.RotateXYZ(this.homePlane, _a, _b, _c);
            }
            if(system == 8) //is zxz
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
        public RotationAxes rAxes;

        public bool IsDelta = false;
        readonly public Point3d[] dCol = new Point3d[3]; //contain 3 array of x,y,z of col position. ie 3 points. only represent kinematics
        private double Ae; //END_EFFECTOR_HORIZONTAL_OFFSET
        private double Ar; //DELTA_RADIUS
        private double L;  //DELTA_DIAGONAL_ROD
        private double Acz; //DELTA_Height
        private double Aco; //CARRIAGE_HORIZONTAL_OFFSET
        //private double Hez; //Tool tip offset from end effector
        private double[] ColAngle = new double[3] { 210 * PI / 180, 330 * PI / 180, 90 * PI / 180 }; //column angle. See below


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
        //    Shape = Beaver.BedShape.rectangular;
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
            Shape = Beaver.BedShape.rectangular;
            BedShape = new Rectangle3d(Plane.WorldXY, SizeX, SizeY).ToNurbsCurve();
            Tools = _tools;
            system = DriveSystem.cartesian;
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
            Shape = Beaver.BedShape.circular;
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

            Shape = Beaver.BedShape.circular;
            Tools = _tools;

            Ae = _Ae;
            Ar = _Ar;
            Aco = _Aco;
            L = _L;
            Acz = Sqrt(L * L - Ar * Ar);
           // double hez = _hez;

            for(int i = 0; i < ColAngle.Length; i++)
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
            if(rAxes.type != RAxesType.None)
            {
                msg += rAxes.ToString();
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
            for(int j = 0; j < dColP.Length; j++)
            {
                dColP[j].Transform(cb);
            }

            double x = dCol[1].X * 0.5;
            double V2 = Sqrt(dCol[2].X * dCol[2].X + dCol[2].Y * dCol[2].Y);
            double y = V2 - 2 * dColP[2].X * x / (2 * dColP[2].Y);
            double z = -1* Sqrt(L * L - x * x - y * y);
            Point3d cartesian = new Point3d(x, y, z);
            cartesian.Transform(cbb);
            return cartesian;
        }

    }

    public class Config //read and set configuration...............................................
    {
        public Hashtable Settings;
        public Machine Machine = new Machine();

        public Config()
        {

        }

        public Config(Machine _machine)
        {
            Machine = _machine;
            
            //add raxes
        }
        public Config(List<string> _config) //construct config instance with setting strings
        {
            Settings = Config.ToHashtable(_config);
        }

        public Config(List<string> _config, Machine _machine) //construct config instance with setting strings
        {
            Settings = Config.ToHashtable(_config);
            Machine = _machine;
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

        /// <summary>
        /// Append a slider to cooresponding input node if a value can be found
        /// </summary>
        /// <param name="_config">configuration</param>
        /// <param name="_this">this component</param>
        /// <param name="_createZero">If True, a slider of 0 value will be appended to input node where value is NOT found.\nIf false, no slider will be created for that node</param>
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


        /// <summary>
        /// Get the name of this setting string. Setting would be in format of {name} = {value}
        /// </summary>
        /// <param name="_setting">setting</param>
        /// <returns></returns>
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


        /// <summary>
        /// Get the value of this setting string. Setting would be in format of {name} = {value}
        /// </summary>
        /// <param name="_setting"></param>
        /// <returns></returns>
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

        
        
        //Settings to ask for.................................................................................

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
    public class Block //contains location and speed, etc of gcode point ie each line of code........................
    {
        public Point3d? coordinate;
        //public Vector3d? direction;  //six axis milling
        public Plane? orientation; //six axis
        public Arc? arc;
        public double? F;            //Feed Rate mm/min
        public double? ER;        //Extrusion Rate mm3/min, can only calculate E from ERate between 2 points OR Speed if spindle
        public int? SS; //Spindle speed
        public int? G;
        public int? T;  //tool to use
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
            if(_M.Count != _S.Count || _M.Count != _P.Count)
            {
                Console.WriteLine("Block(List<int?> _M, List<int?> _S, List<int?> _P), all list should be same length");
                return;
            }
            M = _M;
            S = _S;
            P = _P;
        }

        //public Block(int _E, int _S, int _T, int _A, int B, int _C)
        //{

        //}

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
            if(!this.G.HasValue||_overwrite && _block.G.HasValue)
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
                for(int i = 0; i < M.Count; i++)
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



            if (absPos)
            {
                lines.Add("G90 ; Use absolute position");
            }
            else
            {
                lines.Add("G91 ; Use relative position");
            }
            if (absE)
            {
                lines.Add("M82 ; Use absolute E");
            }
            else
            {
                lines.Add("M83 ; Use relative E");
            }


            for (int k = 0; k < paths.Count; k++) //for each path in gcode
            {

                if(this.config.Machine.Volume == null)
                {
                    msg += "No machine configuration detected";
                }

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

        //public void AddCheckSum()
        //{
        //    int cs = 0;
        //    for (int i = 0; i < _line.Length; i++)
        //        cs ^= _line[i];
        //    cs &= 0xff;

        //    return _line + "*" + cs.ToString();
        //}


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


    public class Connect
    {


    }

    public enum Info
    {
        channel =1,
        note = 2,
        time,
        duration
    }
    public class MidiExtension
    {
        /// <summary>
        /// Get information of note.
        /// </summary>
        /// <param name="_note"></param>
        /// <returns></returns>
        public static string NoteInfo(Note _note)
        {
            string info = "";
            info += _note.Channel.ToString() + " ";
            info += _note.NoteNumber.ToString() + " ";
            info += _note.Time.ToString() + " ";
            info += _note.Length.ToString();
            //info += _note.

            return info;
        }

        public static string NoteInfo(Note _note, Info _info)
        {
            string info = "";
            switch (_info)
            {
                case Info.channel:
                    info += _note.Channel.ToString() + " ";
                    break;
                case Info.note:
                    info += _note.NoteNumber.ToString() + " ";
                    break;
                case Info.time:
                    info += _note.Time.ToString() + " ";
                    break;
                case Info.duration:
                    info += _note.Length.ToString() + " ";
                    break;
            }
            

            return info;
        }

        public static GH_Structure<GH_String> NoteInfo(List<Note> _noteList)
        {
            GH_Structure<GH_String> channelStrings = new GH_Structure<GH_String>();
            for (int i = 0; i < _noteList.Count; i++)
            {
                Note thisNote = _noteList[i];
                GH_Path pth = new GH_Path(thisNote.Channel);

                //.....to string.....................
                string noteInfo = MidiExtension.NoteInfo(thisNote);
                channelStrings.Append(new GH_String(noteInfo), pth);
            }

            return channelStrings;
        }

        public static GH_Structure<GH_String> NoteInfo(List<Note> _noteList, Info _info)
        {
            GH_Structure<GH_String> channelStrings = new GH_Structure<GH_String>();
            for (int i = 0; i < _noteList.Count; i++)
            {
                Note thisNote = _noteList[i];
                GH_Path pth = new GH_Path(thisNote.Channel);

                //.....to string.....................
                string noteInfo = MidiExtension.NoteInfo(thisNote, _info);
                channelStrings.Append(new GH_String(noteInfo), pth);
            }

            return channelStrings;
        }


        public static DataTree<Note> ChannelTree(List<Note> _noteList)
        {
            DataTree<Note> nT = new DataTree<Note>();
            for (int i = 0; i < _noteList.Count; i++)
            {
                Note thisNote = _noteList[i];
                GH_Path pth = new GH_Path(thisNote.Channel);
                nT.Add(thisNote, pth);  //Branch index == channel
            }
            return nT;
        }
        public static List<long> AllUniqueTime(List<Note> _notes)
        {
            List<long> uTime = new List<long>();
            List<long> allTime = new List<long>();

            for (int i = 0; i < _notes.Count; i++)
            {
                allTime.Add(_notes[i].Time);
            }
            allTime.Sort();
            long lastTime = 0;
            uTime.Add(lastTime);

            for (int j = 0; j < allTime.Count; j++)
            {
                long thisTime = allTime[j];
                if (thisTime == lastTime)
                {

                }
                else
                {
                    lastTime = thisTime;
                    uTime.Add(thisTime);
                }
            }

            return uTime;
        }

        /// <summary>
        /// Break down a note to equal time segment, listing note number at every time step
        /// </summary>
        /// <param name="_totalTime">Total time this note last until next note begin, including play time and silence between notes</param>
        /// <param name="_playTime">Time this note is played</param>
        /// <param name="_note">Note number</param>
        /// <param name="step">Time step. Good number is 20(ms)</param>
        /// <returns></returns>
        public static IEnumerable<GH_Integer> Segment(long _totalTime, long _playTime, int _note, int step)
        {
            List<GH_Integer> segs = new List<GH_Integer>();
            for (int i = 0; i * step < _totalTime; i++)
            {
                if (i * step < _playTime)
                {
                    GH_Integer gnote = new GH_Integer();
                    GH_Convert.ToGHInteger(_note, GH_Conversion.Primary, ref gnote);
                    segs.Add(gnote);
                }
                else
                {
                    segs.Add(new GH_Integer(0));
                }
            }
            return segs;
        }

        /// <summary>
        /// Divide notes into segment of equal time setp and list out note number played at each step
        /// </summary>
        /// <param name="_noteTree">Data tree of Notes to segment</param>
        /// <param name="_stepSize">Size of each time step in ms. Good number is 20(ms)</param>
        /// <param name="_channels">List of channels to export</param>
        /// <returns></returns>
        public static GH_Structure<GH_Integer> TimeStep(DataTree<Note> _noteTree, int _stepSize, List<int> _channels)
        {
            GH_Structure<GH_Integer> channelTimeStep = new GH_Structure<GH_Integer>();
            for (int i = 0; i < _channels.Count; i++)
            {
                int thisChannel = _channels[i];
                GH_Path pth = new GH_Path(thisChannel);

                if (!_noteTree.PathExists(pth))
                {
                    Debug.WriteLine("MidiExtension::TimeStep::Channel" + pth.ToString() + " does not exist");
                    continue;
                }


                long time = 0;
                long lastTime = 0;
                var notes = _noteTree.Branch(thisChannel);
                long firstTimeLapse = notes[0].Time;

                channelTimeStep.AppendRange(MidiExtension.Segment(firstTimeLapse, notes[0].Length, notes[0].NoteNumber, _stepSize), pth);

                for (int j = 0; j < notes.Count; j++)
                {
                    time = notes[j].Time;
                    long timeLapse = notes[j].Time - lastTime;
                    channelTimeStep.AppendRange(MidiExtension.Segment(timeLapse, notes[j].Length, notes[j].NoteNumber, _stepSize), pth);
                    lastTime = notes[j].Time;
                }
            }

            return channelTimeStep;
        }

        /// <summary>
        /// Divide notes into segment of equal time setp and list out note number played at each step
        /// </summary>
        /// <param name="_noteTree">Data tree of Notes to segment</param>
        /// <param name="_stepSize">Size of each time step in ms. Good number is 20(ms)</param>
        /// <param name="_channels">Number of channels to export, starting at channel 0</param>
        /// <returns></returns>
        public static GH_Structure<GH_Integer> TimeStep(DataTree<Note> _noteTree, int _stepSize, int _channels)
        {
            List<int> _ch = new List<int>();
            for (int i = 0; i < _channels; i++)
            {
                _ch.Add(i);
            }
            return TimeStep(_noteTree, _stepSize, _ch);
        }
    }

    public static class Extension
    {
        //public static GH_Structure<T> ToGHStructure<T, U>(DataTree<U> _data) where T : IComparable<T>
        //{
        //    GH_Structure<T> dataOut = new GH_Structure<T>();
        //    for(int i = 0; i < _data.BranchCount; i++)
        //    {
        //        for(int j = 0; j< _data.Branch(i).Count; j++)
        //        {
        //            var obj = GH_Convert.ToVariant(_data.Branch(i)[j]);
        //            if(obj.GetType() == T)
        //            {

        //            }
        //        }
        //    }
        //}

        public static List<T> SetListLength<T>(List<T> _list, int _targetLength,  T _replacement) where T : IComparable<T>
        {
            List<T> newList = new List<T>();

            for (int i = 0; i < _targetLength; i++)
            {
                if (i < _list.Count)
                {
                    newList.Add(_list[i]);
                }
                else
                {
                    newList.Add(_replacement);
                }
            }

            return newList;
        }

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

        /// <summary>
        /// Create and append dropdown value list to this component
        /// </summary>
        /// <param name="_input">Index of input to connect to</param>
        /// <param name="_name">Name of the value list</param>
        /// <param name="_items">List of item names</param>
        /// <param name="_values">Corresponding list of value</param>
        /// <param name="_this">This component</param>
        public static void DropDown(int _input, string _name, List<string> _items, List<string> _values, GH_Component _this) //Add Dropdown menue
        {
            if (_this.Params.Input[_input].SourceCount == 0)
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
                    vallist.ListItems.Add(new Grasshopper.Kernel.Special.GH_ValueListItem(_items[i], _values[i]));
                }
                vallist.Description = _items.Count.ToString() + " ";


                _this.OnPingDocument().AddObject(vallist, false);

                _this.Params.Input[_input].AddSource(vallist);
                vallist.ExpireSolution(true);


                //_this.AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "dropdown added");
            }
        }

        /// <summary>
        /// Expire another component that is on the same document as this
        /// </summary>
        /// <param name="_otherName">Other component's name</param>
        /// <param name="_this">this component</param>
        public static void expireOthers(string _otherName, GH_Component _this)
        {
            foreach (IGH_DocumentObject obj in _this.OnPingDocument().Objects)
            {
                if (obj.Name == _otherName)
                {
                    obj.ExpireSolution(true);
                    break;
                }
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


