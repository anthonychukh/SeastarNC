using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Seastar;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.Display;
using Rhino.Geometry;
using OpenCvSharp;
using System.Threading;
using Firefly_Bridge;
using System.Drawing.Imaging;
using System.Device;

using WIA;
using Grasshopper.GUI;

namespace SeastarGrasshopper
{

    //public class Camera : GH_Component
    //{
    //    public static bool run = false;
    //    public static bool openThread = true;

    //    public static int ci = 1;
    //    public static int count = 0;
    //    public static string msg = "Ready to connect";

    //    public static VideoCapture capture = new VideoCapture();
    //    public static Mat img;

    //    public static Firefly_Bitmap fbmp = new Firefly_Bitmap();
    //    public static Bitmap bmp;
    //    public static int fps = 25;

    //    public Camera()
    //      : base("Camera", "Camera",
    //          "Define Camera to use",
    //          "Seastar", "07 | CV")
    //    {
    //    }

    //    public override GH_Exposure Exposure => GH_Exposure.primary;

    //    protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
    //    {
    //        pManager.AddBooleanParameter("run", "run", "runrunrun", GH_ParamAccess.item, false);
    //        pManager.AddIntegerParameter("Camera index", "i", "Index of Camera to use", GH_ParamAccess.item, 1);
    //        pManager.AddIntegerParameter("FPS", "f", "Frame per second\n" +
    //            "Reduce FPS for performance", GH_ParamAccess.item, 18);
    //    }

    //    protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
    //    {
    //        pManager.AddGenericParameter("OpenCV Mat", "M", "OpenCV Mat", GH_ParamAccess.item);
    //        pManager.AddTextParameter("Message", "msg", "Message", GH_ParamAccess.item);
            
    //    }

    //    protected override void SolveInstance(IGH_DataAccess DA)
    //    {
            
    //        DA.GetData(0, ref run);
    //        DA.GetData("Camera index", ref ci);
    //        DA.GetData("FPS", ref fps);

    //        //..open image in new window..........................
    //        //var src = new Mat(@"C:\Users\antho\Google Drive (anthony@space10.io)\Kai-Hong Anthony Chu\Resources\4622_large_arduino_uno_main_board.jpg", ImreadModes.Unchanged);
    //        //var window = new Window("window", image: src, flags: WindowMode.AutoSize);
    //        //Cv2.WaitKey();

    //        //turn on webCam........................................
    //        //var capture = new VideoCapture(@"C:\Users\antho\Google Drive (anthony@space10.io)\Kai-Hong Anthony Chu\Resources\4_6s.mp4");


    //        if (Camera.openThread && Camera.img == null && Camera.run) //start thread if img not set and set to run
    //        {
    //            Camera.openThread = false;
    //            msg += "main thread open";
    //            WindowThread obj = new WindowThread();
    //            Thread thr = new Thread(new ThreadStart(obj.OpenWindow));
    //            thr.Start();
    //        }



    //        if (Camera.run)
    //        {
    //            //count++;//test counter
    //            //msg += count.ToString();
    //            //msg += "\n";
                
    //            DA.SetData("Message", msg);
    //            DA.SetData(0, img);
    //            this.ExpireSolution(true);
    //        }
    //        else  //reset for next
    //        {
    //            count = 0;
    //            msg = "Ready to connect";
    //            DA.SetData("Message", msg);
    //        }
    //    }
        


    //    protected override System.Drawing.Bitmap Icon => Seastar.Properties.Resources.cvCamera;

    //    public override Guid ComponentGuid
    //    {
    //        get { return new Guid("986ca318-c991-411b-8287-e20870790439"); }
    //    }
    //}

    public class Camera2 : GH_Component
    {

        public Camera2()
          : base("Camera2", "Camera2",
              "Define Camera to use",
              "Seastar", "07 | CV")
        {
        }
        public override GH_Exposure Exposure => GH_Exposure.primary;
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Run", "run", "Start camera capturing", GH_ParamAccess.item, false);
            pManager.AddIntegerParameter("Camera index", "i", "Index of Camera to use", GH_ParamAccess.item, 1);
            
            pManager.AddGenericParameter("Camera Settings", "cs", "CV camera settings\n" +
                "Filter and process will be processed in order of list input", GH_ParamAccess.list);
            pManager[2].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("OpenCV Mat", "mat", "OpenCV Mat", GH_ParamAccess.item);
            pManager.AddGenericParameter("Seastar Camera", "C", "Seastar Camera\nEmbedded with detected geometries\nUse Decompose Camera to access these geometries", GH_ParamAccess.item);
            pManager.AddTextParameter("Message", "msg", "Message", GH_ParamAccess.item);
        }

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalComponentMenuItems(menu);
            Menu_AppendItem(menu, "Update interval:").Font = GH_FontServer.StandardItalic;
            Menu_AppendItem(menu, "25 ms", (sender, e) => Menu_SelectFPS(sender, e, 25), true, CheckFPS(25));
            Menu_AppendItem(menu, "50 ms", (sender, e) => Menu_SelectFPS(sender, e, 50), true, CheckFPS(50));
            Menu_AppendItem(menu, "100 ms", (sender, e) => Menu_SelectFPS(sender, e, 100), true, CheckFPS(100));
            Menu_AppendItem(menu, "1 s", (sender, e) => Menu_SelectFPS(sender, e, 1000), true, CheckFPS(1000));
            Menu_AppendItem(menu, "5 s", (sender, e) => Menu_SelectFPS(sender, e, 5000), true, CheckFPS(5000));
            var tBox = Menu_AppendTextItem(menu, frameInterval.ToString() + " ms", null, TextChange, true);

            if (frameInterval == 25 || frameInterval == 50 || frameInterval == 100 || frameInterval == 1000 || frameInterval == 5000) tBox.ForeColor = Color.Gray;
            else tBox.ForeColor = Color.Black;

            Menu_AppendSeparator(menu);
            Menu_AppendItem(menu, "Video resolution:").Font = GH_FontServer.StandardItalic;
            Vector2d r480 = new Vector2d(480, 640);
            Vector2d r360 = new Vector2d(360, 480);
            Vector2d r240 = new Vector2d(240, 320);
            Vector2d r120 = new Vector2d(120, 160);
            Vector2d r60 = new Vector2d(60, 80);
            Menu_AppendItem(menu, "640 x 480", (sender, e) => Menu_SelectRes(sender, e, r480), true, CheckRes(r480));
            Menu_AppendItem(menu, "480 x 360", (sender, e) => Menu_SelectRes(sender, e, r360), true, CheckRes(r360));
            Menu_AppendItem(menu, "320 x 240", (sender, e) => Menu_SelectRes(sender, e, r240), true, CheckRes(r240));
            Menu_AppendItem(menu, "160 x 120", (sender, e) => Menu_SelectRes(sender, e, r120), true, CheckRes(r120));
            Menu_AppendItem(menu, "60 x 80", (sender, e) => Menu_SelectRes(sender, e, r60), true, CheckRes(r60));

            //Menu_AppendSeparator(menu);
            //Menu_AppendItem(menu, "Select color space:").Font = GH_FontServer.StandardItalic;
            //Menu_AppendItem(menu, "Full RBG", (sender, e) => Menu_SelectColorSpace(sender, e, 0), true, CheckColorSpace(0));
            //Menu_AppendItem(menu, "Grayscale", (sender, e) => Menu_SelectColorSpace(sender, e, 1), true, CheckColorSpace(1));
            //Menu_AppendItem(menu, "Red", (sender, e) => Menu_SelectColorSpace(sender, e, 2), true, CheckColorSpace(2));
            //Menu_AppendItem(menu, "Green", (sender, e) => Menu_SelectColorSpace(sender, e, 3), true, CheckColorSpace(3));
            //Menu_AppendItem(menu, "Blue", (sender, e) => Menu_SelectColorSpace(sender, e, 4), true, CheckColorSpace(4));
        }
        private void TextChange(GH_MenuTextBox sender, string text)
        {
            string temp = text;
            if (temp.Contains("ms"))
            {
                temp.Replace("ms", "");
            }
            if (temp.Contains(" "))
            {
                temp.Replace(" ", "");
            }
            try
            {
                frameInterval = Convert.ToInt32(temp);
            }
            catch
            {
                sender.TextBoxItem.ForeColor = Color.Red;
                frameInterval = Convert.ToInt32(sender.OriginalText);
            }
        }
        public void Menu_SelectFPS(object sender, EventArgs e, double _frameInterval)
        {
            frameInterval = _frameInterval;
        }
        public bool CheckFPS(double _frameInterval)
        {
            return (_frameInterval == frameInterval);
        }

        public void Menu_SelectRes(object sender, EventArgs e, Vector2d _res)
        {
            res = _res;
        }
        public bool CheckRes(Vector2d _res)
        {
            return (_res.Equals(res));
        }
        //public void Menu_SelectColorSpace(object sender, EventArgs e, int _colorSpace )
        //{
        //    colorSpace = _colorSpace;
        //}
        //public bool CheckColorSpace(int _colorSpace)
        //{
        //    return (_colorSpace == colorSpace);
        //}
        
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            cm2 = this;
            bool run = false;
            int ci = 1; //camera index
            List<SSCamSetting> camSettings = new List<SSCamSetting>();

            DA.GetData(0, ref run);
            DA.GetData("Camera index", ref ci);
            DA.GetDataList<SSCamSetting>(2, camSettings);

            ///start/end thread...................................
            if ((thr == null || !thr.IsAlive) && run)
            {
                double fps = 1000 / frameInterval;
                sc = new SSCamera(ci, fps, res, camSettings);
                sc.run = run;
                //sc.da = DA;
                //sc.component = this;

                

                thr = new Thread(new ThreadStart(Initiate));
                thr.Start();
                thr.IsBackground = true;
                Debug.WriteLine("Camera2 thread start");
            }

            //display error.......................................
            if (sc != null && sc.errorFlag)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, sc.msg);
                DA.SetData("Messgae", sc.msg);
                return;
            }

            ///refresh for input/output...........................
            if (run && matOut != null)
            {
                sc.camSetting = camSettings;
                sc.fps = 1000 / frameInterval;
                DA.SetData(0, matOut);
                DA.SetData("Seastar Camera", sc);
                DA.SetData("Message", sc.msg);
                
               // this.ExpireSolution(true);
            }
            else
            {
                if (sc != null ) sc.run = false;
                DA.SetData("Message", "Ready to connect");
            }
            
            
        }

        public static SSCamera sc;
        public static Thread thr;
        public static string msg;
        //public static int colorSpace = 0;
        public static double frameInterval = 50;
        public static GH_Component cm2;
        public static Vector2d res = new Vector2d(480, 640);
        public static Mat matOut = new Mat();

        protected override System.Drawing.Bitmap Icon => Seastar.Properties.Resources.cvCamera;

        public override Guid ComponentGuid
        {
            get { return new Guid("7b823abf-cc54-4228-b5d2-c9ae07f95667"); }
        }

        //public static void expireThis()
        //{
        //    cm2.ExpireSolution(true);
        //}
        private void UpdateSetData(GH_Document gh)
        {
            ExpireSolution(false);
        }


        public void Initiate()
        {
            sc.OpenCamera();

            var sw = Stopwatch.StartNew();
            double lastMS = 0;

            using (var window = new Window("Seastar Camera"))
            {
                
                window.SetProperty(WindowPropertyFlags.AutoSize, 1);
                while (sc.run)
                {
                    matOut = sc.ReadCamera();

                    //.fps count..............................
                    double thisMS = sw.ElapsedMilliseconds;
                    double frame = 1000 / (thisMS - lastMS);
                    lastMS = thisMS;
                    //........................................

                    //Mat mat3 = this.ApplyFilter(mat2);
                    sc.ApplyFilter(ref matOut);
                    sc.ShowCamera(matOut, window, frame);

                    var calllater = new GH_Document.GH_ScheduleDelegate(UpdateSetData);
                    Grasshopper.Instances.ActiveCanvas.Document.ScheduleSolution(1, calllater);
                }
                sc.DestroyCamera();
            }
        }

     
    }

    public class ColorSpace : GH_Component
    {
        public ColorSpace()
          : base("Color Space", "ColorSpace",
              "Select the color space for video capture",
              "Seastar", "07 | CV")
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {

        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Camera Settings", "cs", "Camera settings for color space\n" +
                "Connect to Camera component", GH_ParamAccess.item);
            pManager.AddTextParameter("Message", "msg", "Message", GH_ParamAccess.item);
        }

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalComponentMenuItems(menu);
            Menu_AppendItem(menu, "Select color space:").Font = GH_FontServer.StandardItalic;
            Menu_AppendItem(menu, "Full RBG", (sender, e) => Menu_SelectColorSpace(sender, e, 0), true, CheckColorSpace(0));
            Menu_AppendItem(menu, "Grayscale", (sender, e) => Menu_SelectColorSpace(sender, e, 1), true, CheckColorSpace(1));
            Menu_AppendItem(menu, "Red", (sender, e) => Menu_SelectColorSpace(sender, e, 2), true, CheckColorSpace(2));
            Menu_AppendItem(menu, "Green", (sender, e) => Menu_SelectColorSpace(sender, e, 3), true, CheckColorSpace(3));
            Menu_AppendItem(menu, "Blue", (sender, e) => Menu_SelectColorSpace(sender, e, 4), true, CheckColorSpace(4));
        }

        public void Menu_SelectColorSpace(object sender, EventArgs e, int _colorSpace)
        {
            colorSpace = _colorSpace;
            this.ExpireSolution(true);
        }
        public bool CheckColorSpace(int _colorSpace)
        {
            return (_colorSpace == colorSpace);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //string msg = "";
            
            SSCamSetting cs = new SSCamSetting();
            cs.colorSpace = colorSpace;
            DA.SetData(0, cs);
            DA.SetData(1, colorSpace.ToString());
        }

        public static int colorSpace = 0;
        protected override System.Drawing.Bitmap Icon => Seastar.Properties.Resources.cvColSpace;

        public override Guid ComponentGuid
        {
            get { return new Guid("cb7dd3fd-0630-4823-ac0b-328000c287f6"); }
        }
    }


    public class EdgeDetect : GH_Component
    {
        public EdgeDetect()
          : base("Edge Detection", "EdgeDetect",
              "Edge detection",
              "Seastar", "07 | CV")
        {
        }
        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Param A", "a", "Param A", GH_ParamAccess.item);
            pManager.AddNumberParameter("Param B", "b", "Param B", GH_ParamAccess.item);
            pManager.AddNumberParameter("Param C", "c", "Param C", GH_ParamAccess.item);
            this.Params.Input[0].Optional = true;
            this.Params.Input[1].Optional = true;
            this.Params.Input[2].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Camera Settings", "cs", "Camera settings for edge detection\n" +
                "Connect to Camera component", GH_ParamAccess.item);
            pManager.AddTextParameter("Message", "msg", "Message", GH_ParamAccess.item);
        }

        public static int detectMethod = 0;

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalComponentMenuItems(menu);
            Menu_AppendItem(menu, "Edge detection method:").Font = GH_FontServer.StandardItalic;
            Menu_AppendItem(menu, "Canny Edge detection", (sender, e) => Menu_selectMethod(sender, e, 0), true, CheckMethod(0));
            Menu_AppendItem(menu, "Line detection", (sender, e) => Menu_selectMethod(sender, e, 1), true, CheckMethod(1));
            Menu_AppendItem(menu, "Line segment detection", (sender, e) => Menu_selectMethod(sender, e, 2), true, CheckMethod(2));
            Menu_AppendItem(menu, "Circle detection", (sender, e) => Menu_selectMethod(sender, e, 3), true, CheckMethod(3));
        }

        private void Menu_selectMethod(object sender, EventArgs e, int _method)
        {
            detectMethod = _method;
            
            this.ExpireSolution(true);
        }
        private bool CheckMethod(int _method)
        {
            return (_method == detectMethod);
        }
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string msg = "";
            double A = 0;
            double B = 0;
            double C = 0;
            SSCamSetting cs = new SSCamSetting();
            this.Params.Input[2].Description = "(No input required. Any input value will be ignored)";

            switch (detectMethod)
            {
                case 0: //canny
                    if (!DA.GetData(0, ref A)) this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Input parameter a failed to collect data");
                    if (!DA.GetData(1, ref B)) this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Input parameter b failed to collect data");
                    this.Params.Input[0].Description = "Threshold 1";
                    this.Params.Input[1].Description = "Threshold 2";
                    this.Description = "Canny Edge detection";
                    break;
                case 1: //line
                    if (!DA.GetData(0, ref A)) this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Input parameter a failed to collect data");
                    if (!DA.GetData(1, ref B)) this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Input parameter b failed to collect data");
                    if (!DA.GetData(2, ref C)) this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Input parameter c failed to collect data");
                    this.Params.Input[0].Description = "Rho_param\nThe resolution of the parameter r in pixels.";
                    this.Params.Input[1].Description = "Theta_param\nThe resolution of the parameter θ in radians.";
                    this.Params.Input[2].Description = "Threshold\nThe minimum number of intersections to detect a line.";
                    this.Description = "Line Detection [Hough Transform]";
                    break;
                case 2:  //line segment 
                    if (!DA.GetData(0, ref A)) this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Input parameter a failed to collect data");
                    if (!DA.GetData(1, ref B)) this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Input parameter b failed to collect data");
                    this.Params.Input[0].Description = "minLinLength\nThe minimum number of points that can form a line. Lines with less than this number of points are disregarded.";
                    this.Params.Input[1].Description = "maxLineGap\nThe maximum gap between two points to be considered in the same line.";
                    this.Description = "Line Segment Detection P [Hough Transform]";
                    break;
                case 3:  //circle detect
                    if (!DA.GetData(0, ref A)) this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Input parameter a failed to collect data");
                    if (!DA.GetData(1, ref B)) this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Input parameter b failed to collect data");
                    this.Params.Input[0].Description = "param1\nFirst method - specific parameter.In case of HOUGH_GRADIENT, it is the higher threshold of the two passed to the Canny edge detector(the lower one is twice smaller).";
                    this.Params.Input[1].Description = "param2\nSecond method - specific parameter.In case of HOUGH_GRADIENT, it is the accumulator threshold for the circle centers at the detection stage.The smaller it is, the more false circles may be detected.Circles, corresponding to the larger accumulator values, will be returned first.";
                    this.Params.Input[2].Description = "minRadius\nMinimum circle radius.";
                    this.Description = "dp  Inverse ratio of the accumulator resolution to the image resolution. For example, if dp = 1 , the accumulator has the same resolution as the input image. If dp = 2, the accumulator has half as big width and height.\n"+
                                "minDist Minimum distance between the centers of the detected circles. If the parameter is too small, multiple neighbor circles may be falsely detected in addition to a true one.If it is too large, some circles may be missed.";
                    break;
            }
            msg += this.Description;

            cs.detectMethod = detectMethod;
            if(DA.GetData(0, ref A)) cs.EDparamA = A;
            if(DA.GetData(1, ref B)) cs.EDparamB = B;
            if(DA.GetData(2, ref C)) cs.EDparamC = C;

            DA.SetData(0, cs);
            DA.SetData(1, msg);
        }

        protected override System.Drawing.Bitmap Icon => Seastar.Properties.Resources.cvEdge;

        public override Guid ComponentGuid
        {
            get { return new Guid("6aed8526-b932-4784-ae9e-5c7cf66474d5"); }
        }
    }

    public class Blur : GH_Component
    {
        public Blur()
          : base("Blur Filters", "Blur",
              "Blur Filters.\nChoose from filter selection in menu",
              "Seastar", "07 | CV")
        {
        }
        public override GH_Exposure Exposure => GH_Exposure.secondary;

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Param X", "x", "Parameter X\n" +
                "Blurring action on X/both direction(s)\n" +
                "Not all blur filer require this parameter, See message box for info", GH_ParamAccess.item);
            pManager.AddNumberParameter("Param Y", "y", "Parameter Y\n" +
                "Blurring action on Y direction\n" +
                "Not all blur filer require this parameter, See message box for info", GH_ParamAccess.item);
            pManager[0].Optional = true;
            pManager[1].Optional = true;
            

        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Camera Settings", "cs", "Camera settings for blur filter\n" +
                "Connect to Camera component", GH_ParamAccess.item);
            pManager.AddTextParameter("Message", "msg", "Message", GH_ParamAccess.item);
        }

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalComponentMenuItems(menu);
            Menu_AppendItem(menu, "Blur", (sender, e)=>Menu_SelectMode(sender, e, 0), true, IsBlurModeOn(0));
            Menu_AppendItem(menu, "Gaussian blur", (sender, e) => Menu_SelectMode(sender, e, 1), true, IsBlurModeOn(1));
            Menu_AppendItem(menu, "Median blur", (sender, e) => Menu_SelectMode(sender, e, 2), true, IsBlurModeOn(2));
            Menu_AppendItem(menu, "Bilateral blur", (sender, e) => Menu_SelectMode(sender, e, 3), true, IsBlurModeOn(3));
            Menu_AppendItem(menu, "Laplacian blur", (sender, e) => Menu_SelectMode(sender, e, 4), true, IsBlurModeOn(4));
        }

        public static int blurMode = 0;

        public bool IsBlurModeOn(int _blurMode)
        {
            if (_blurMode == blurMode) return true;
            else return false;
        }

        private void Menu_SelectMode(object Sender, EventArgs e, int _blurMode)
        {
            blurMode = _blurMode;
            this.ExpireSolution(true);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {

            SSCamSetting camSet = new SSCamSetting();
            string msg = "";
            msg += blurMode.ToString();
            double x = 0;
            double y = 0;

            switch (blurMode)
            {
                case 0: //blur

                    msg = "Simple blur\n" +
                        "Apply a simple blur filter where each pixel's color is replaced by the average of its neighbors\n" +
                          "in a blurX x blurY box region around it.";

                    if (!DA.GetData(0, ref x)) this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Input parameter x failed to collect data");
                    if (!DA.GetData(1, ref y)) this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Input parameter y failed to collect data");
                    this.Params.Input[0].Description = "size of averaging box in the x direction";
                    this.Params.Input[1].Description = "size of averaging box in the y direction";
                    this.Description = "Simple blur\n" + 
                        "Apply a simple blur filter where each pixel's color is replaced by the average of its neighbors\n" +
                          "in a blurX x blurY box region around it.";

                    camSet.blurX = x;
                    camSet.blurY = y;
                    //this.AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, msg);

                    break;

                case 1: //Gaussian blur
                    double xg = 0;
                    double yg = 0;
                    msg = "Gaussian blur\n" +
                        "Apply a gaussian blur filter where each pixel's color is replaced by the average of its neighbors in\n" +
                          "a blurX x blurY box region around it weighted using a gaussian kernel.\n";

                    if (!DA.GetData(0, ref x)) this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Input parameter x failed to collect data");
                    if (!DA.GetData(1, ref y)) this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Input parameter y failed to collect data");
                    this.Params.Input[0].Description = "size of averaging box in the x direction [MUST BE AN ODD NUMBER]";
                    this.Params.Input[1].Description = "size of averaging box in the y direction [MUST BE AN ODD NUMBER]";
                    this.Description = "Gaussian blur\n" + 
                        "Apply a gaussian blur filter where each pixel's color is replaced by the average of its neighbors in\n" +
                          "a blurX x blurY box region around it weighted using a gaussian kernel.\n";

                    camSet.GblurX = x;
                    camSet.GblurY = y;

                    break;

                case 2: //Median blur
                    double m = 0;
                    msg = "Median blur\n" +
                        "Apply a median smoothing filter to the image, replacing the color of each pixel with the median of its naighbours.\n" +
                          "This in effect removes speckles and noise from an image but canmaintain some edges\n";

                    if (!DA.GetData(0, ref x)) this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Input parameter x failed to collect data");
                    this.Params.Input[0].Description = "size of the smoothing box in pixels. [MUST BE AN ODD NUMBER]";
                    this.Params.Input[1].Description = "(No input required. Any input value will be ignored)";
                    this.Description = "Median blur\n" + "Apply a median smoothing filter to the image, replacing the color of each pixel with the median of its naighbours.\n" +
                          "This in effect removes speckles and noise from an image but canmaintain some edges\n";

                    //this.AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, msg);
                    camSet.mediumBsize = Convert.ToInt32(x);
                    break;

                case 3: //Bilateral blur

                    msg = "Bilateral blur\n" +
                        "The bilaterial filter is another smoothing filter that tries to preserve edges.It averages pixels\n" +
                          "based on both spatial proximity as well as color proximity. So two neighbouring pixels with\n" +
                          "very different colors (e.g. along an edge) won't be averaged.\n\n";

                    if (!DA.GetData(0, ref x)) this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Input parameter x failed to collect data");
                    if (!DA.GetData(1, ref y)) this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Input parameter y failed to collect data");
                    this.Params.Input[0].Description = "color similarity threshold. Pixels with values that differ more than this will not be averaged";
                    this.Params.Input[1].Description = "size in pixels of smoothing region";
                    this.Description = "Bilateral blur\n" + "The bilaterial filter is another smoothing filter that tries to preserve edges.It averages pixels\n" +
                          "based on both spatial proximity as well as color proximity. So two neighbouring pixels with\n" +
                          "very different colors (e.g. along an edge) won't be averaged.\n\n";

                    camSet.colorSimilarity = x;
                    camSet.smoothingSize = Convert.ToInt32(y);

                    break;

                case 4: // Laplacian blur

                    msg = "Laplacian blur\n" +
                        "Laplacian is a quick edge estimation filter. It basically computes the difference of a pixel with all of its neighbours\n" +
                            "so it would be maximum for a pixel that is a single speck and minimum for a pixel that lies within a\n" +
                            "homogeneous color region.";

                    if (!DA.GetData(0, ref x)) this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Input parameter x failed to collect data");

                    this.Params.Input[0].Description = "color similarity threshold. Pixels with values that differ more than this will not be averaged";
                    this.Params.Input[1].Description = "(No input required. Any input will be ignored)";
                    this.Description = "Laplacian blur\n" + "Laplacian is a quick edge estimation filter. It basically computes the difference of a pixel with all of its neighbours\n" +
                            "so it would be maximum for a pixel that is a single speck and minimum for a pixel that lies within a\n" +
                            "homogeneous color region.";

                    camSet.kernelSize = Convert.ToInt32(x);

                    break;

            }

            camSet.UpdateValue();
            DA.SetData(0, camSet);


            DA.SetData(1, msg);
        }

        protected override System.Drawing.Bitmap Icon => Seastar.Properties.Resources.cvBlur;

        public override Guid ComponentGuid
        {
            get { return new Guid("16f3bbd1-610f-4e13-9e52-f29a7c3366f0"); }
        }
    }

    public class Morphology : GH_Component
    {
        public Morphology()
          : base("Image Morphology", "Morphology",
              "OpenCV generalized morphological operators",
              "Seastar", "07 | CV")
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Size of window", "s", "Size of window", GH_ParamAccess.item, 5);
            pManager.AddNumberParameter("Iteration", "i", "Iteration", GH_ParamAccess.item, 2);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Camera Settings", "cs", "Camera settings for morphology filter\n" +
                "Connect to Camera component", GH_ParamAccess.item);
            pManager.AddTextParameter("Message", "msg", "Message", GH_ParamAccess.item);
        }

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalComponentMenuItems(menu);
            Menu_AppendItem(menu, "Erode", (sender, e) => Menu_SelectMode(sender, e, 0), true, IsMorpModeOn(0));
            Menu_AppendItem(menu, "Dilate", (sender, e) => Menu_SelectMode(sender, e, 1), true, IsMorpModeOn(1));
            Menu_AppendItem(menu, "Opening", (sender, e) => Menu_SelectMode(sender, e, 2), true, IsMorpModeOn(2));
            Menu_AppendItem(menu, "Closing", (sender, e) => Menu_SelectMode(sender, e, 3), true, IsMorpModeOn(3));
            Menu_AppendItem(menu, "Gradient", (sender, e) => Menu_SelectMode(sender, e, 4), true, IsMorpModeOn(4));
            Menu_AppendItem(menu, "Top Hat", (sender, e) => Menu_SelectMode(sender, e, 4), true, IsMorpModeOn(4));
            Menu_AppendItem(menu, "Black Hat", (sender, e) => Menu_SelectMode(sender, e, 4), true, IsMorpModeOn(4));
        }

        public int morpMode = 0;

        public bool IsMorpModeOn(int _morpMode)
        {
            if (_morpMode == morpMode) return true;
            else return false;
        }

        private void Menu_SelectMode(object Sender, EventArgs e, int _morpMode)
        {
            morpMode = _morpMode;
            this.ExpireSolution(true);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string msg = "";
            double s = 0;
            double i = 0;
            DA.GetData(0, ref s);
            DA.GetData(1, ref i);
            
            switch (morpMode)
            {
                case 0:
                    //msg = "Erosion is the opposite of dilation causing light regions to contract\n";
                    if (!DA.GetData(0, ref s)) this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Input parameter s failed to collect data");
                    if (!DA.GetData(1, ref i)) this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Input parameter i failed to collect data");
                    this.Description = "Erode\nReplaces value of each pixel by the mainimum value in its neighborhood\n" +
                                        "this will cause bright regions to contract and darker regions to expand.\n";

                    break;
                case 1:
                    //msg = "Dilation replaces the value of each pixel by the maximum value in its neighborhood\n" +
                         // "this will cause bright regions to expand and darker regions to contract.\n";
                    if (!DA.GetData(0, ref s)) this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Input parameter s failed to collect data");
                    if (!DA.GetData(1, ref i)) this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Input parameter i failed to collect data");
                    this.Description = "Dilate\nReplaces value of each pixel by the maximum value in its neighborhood\n" +
                                        "this will cause bright regions to expand and darker regions to contract.\n";
                    break;
                case 2:
                    //msg = "opening is the result of one erosion followed by one dilation of the image\n" +
                      //    "in effect it will eliminate bright regions smaller than the size of the operator.\n";
                    if (!DA.GetData(0, ref s)) this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Input parameter s failed to collect data");
                    if (!DA.GetData(1, ref i)) this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Input parameter i failed to collect data");
                    this.Description = "Opening\nOne erosion followed by one dilation of the image\n" +
                                        "This will eliminate bright regions smaller than the size of the operator.\n";
                    break;
                case 3:
                   // msg = "closing is the result of one dilation followed by one erosion of the image\n" +
                       //   "in effect it will eliminate dark regions regions smaller than the size of the operator.\n";
                    if (!DA.GetData(0, ref s)) this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Input parameter s failed to collect data");
                    if (!DA.GetData(1, ref i)) this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Input parameter i failed to collect data");
                    this.Description = "Closing\nOne dilation followed by one erosion of the image\n" +
                                       "This will eliminate dark regions regions smaller than the size of the operator.\n";
                    break;
                case 4: //gradient
                    if (!DA.GetData(0, ref s)) this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Input parameter s failed to collect data");
                    if (!DA.GetData(1, ref i)) this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Input parameter i failed to collect data");
                    this.Description = "Gradient";
                    break;
                case 5: //top hat
                    if (!DA.GetData(0, ref s)) this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Input parameter s failed to collect data");
                    if (!DA.GetData(1, ref i)) this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Input parameter i failed to collect data");
                    this.Description = "Top hat";
                    break;
                case 6: //black hat
                    if (!DA.GetData(0, ref s)) this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Input parameter s failed to collect data");
                    if (!DA.GetData(1, ref i)) this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Input parameter i failed to collect data");
                    this.Description = "Black hat";
                    break;
            }
            SSCamSetting cs = new SSCamSetting();

            cs.morphSize = s;
            cs.morphSize = i;
            cs.morphType = (MorphTypes)morpMode;

            DA.SetData(0, cs);
            DA.SetData(1, msg);

        }

        protected override System.Drawing.Bitmap Icon => Seastar.Properties.Resources.cvMorph;

        public override Guid ComponentGuid
        {
            get { return new Guid("b6bf1234-15dd-4c90-b76a-112ba7bd8b3c"); }
        }
    }

    public class ColorTrans : GH_Component
    {
        public ColorTrans()
          : base("Color Transformation", "ColorTrans",
              "Color transformation",
              "Seastar", "07 | CV")
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Brightness", "b", "Brightness offset value.\n Value of -1 means full darkness.", GH_ParamAccess.item, 0);
            pManager.AddNumberParameter("Contrast", "c", "Contrast offset value.", GH_ParamAccess.item, 0);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Camera Settings", "cs", "Camera settings for morphology filter\n" +
            "Connect to Camera component", GH_ParamAccess.item);
            pManager.AddTextParameter("Message", "msg", "Message", GH_ParamAccess.item);
        }

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalComponentMenuItems(menu);
            Menu_AppendItem(menu, "Draw histogram", Menu_SelectDrawHisto, true, drawHisto);
        }

        private bool drawHisto = false;

        private void Menu_SelectDrawHisto(object sender, EventArgs e)
        {
            drawHisto = !drawHisto;
            this.ExpireSolution(true);
        }
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            SSCamSetting cs = new SSCamSetting();
            string msg = "";

            double b = 0;
            double c = 0;
            DA.GetData("Brightness", ref b);
            DA.GetData("Contrast", ref c);

            cs.brightness = 1 + b;
            cs.contrast = c;
            cs.drawHistogram = drawHisto;

            DA.SetData(0, cs);
            DA.SetData(1, msg);
        }

        protected override System.Drawing.Bitmap Icon => Seastar.Properties.Resources.cvColTrans;

        public override Guid ComponentGuid
        {
            get { return new Guid("961a4fb2-44ee-4945-bf83-6f0ebe584680"); }
        }
    }

    public class MSER : GH_Component
    {
        public MSER()
          : base("Blob Detection MSER", "MSER",
              "Blob Detection MSER",
              "Seastar", "07 | CV")
        {
        }
        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {

        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {

        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {

        }

        protected override System.Drawing.Bitmap Icon => Seastar.Properties.Resources.cvMSER;

        public override Guid ComponentGuid
        {
            get { return new Guid("04a9f989-1151-4321-839d-735eaeb31e32"); }
        }
    }

    public class Face : GH_Component
    {
        public Face()
          : base("Face Detection", "Face",
              "Face Detection\nThis compoent works better with higher resolution.",
              "Seastar", "07 | CV")
        {
        }
        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {

        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Camera Setting", "cs", "Camera setting to face detection", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            SSCamSetting camSet = new SSCamSetting();
            string path = Environment.ExpandEnvironmentVariables("%APPDATA%");
            path += @"\Grasshopper\Libraries\haarcascade_frontalface_default.xml";
#if DEBUG
            this.AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, path);
#endif
            camSet.haarCascade = new CascadeClassifier(path);
            DA.SetData(0, camSet);
        }

        protected override System.Drawing.Bitmap Icon => Seastar.Properties.Resources.cvFace;

        public override Guid ComponentGuid
        {
            get { return new Guid("c5f44804-15b8-411d-a3a2-eaa352804b0e"); }
        }
    }


    public class DecomposeCamera : GH_Component
    {
        public DecomposeCamera()
          : base("Decompose Camera", "DecomposeCamera",
              "Decompose geometries embedded in camera detection funstions",
              "Seastar", "07 | CV")
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.septenary;

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Seastar Camera", "C", "Seastar camera object from Camera component", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddRectangleParameter("Picture Frame", "F", "Rectangle representing the picture frame of video capture", GH_ParamAccess.item);
            pManager.AddGeometryParameter("Faces", "f", "Ellipses representing faces detected", GH_ParamAccess.list);
            pManager.AddLineParameter("Lines", "l", "Lines representing line detected", GH_ParamAccess.list);
            pManager.AddLineParameter("Edges", "e", "Lines representing edges detected", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            SSCamera scD = new SSCamera();
            DA.GetData(0, ref scD);

            Rectangle3d frame = new Rectangle3d(Plane.WorldXY, scD.width, scD.height);
            DA.SetData("Picture Frame", frame);

            if (scD.faceRec.Count > 0)
            {
                DA.SetDataList("Face", scD.faceRec);
            }
            if(scD.edges.Count > 0)
            {
                DA.SetDataList("Edges", scD.edges);
            }
        }

        protected override System.Drawing.Bitmap Icon => Seastar.Properties.Resources.cvDeCam;

        public override Guid ComponentGuid
        {
            get { return new Guid("33ad0e02-5765-4265-95a4-0e83a127f34e"); }
        }
    }

    public class ToFireFly : GH_Component
    {
        public ToFireFly()
          : base("Mat to FireFly Bitmap", "ToFireFly",
              "Convert OpenCV Mat to FireFly bitmap",
              "Seastar", "07 | CV")
        {
        }
        public override GH_Exposure Exposure => GH_Exposure.septenary;

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("OpenCV Mat", "mat", "OpenCV Mat", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("FireFly Bitmap", "bmp", "FireFly bitmap\nConnect to Firefly vision components", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Mat mat = new Mat();
            DA.GetData(0, ref mat);
            DA.SetData(0, SSCamera.MatToFireFly(mat));
        }

        protected override System.Drawing.Bitmap Icon => Seastar.Properties.Resources.cvToFire;

        public override Guid ComponentGuid
        {
            get { return new Guid("cade43ad-44af-459b-98b4-19a9132bf2b8"); }
        }
    }

    public class SSCamSetting
    {
        //blur settings............................................................................................
        public double blurX = 0; //size of averaging box in the x direction
        public double blurY = 0;  //size of averaging box in the y direction
        public double GblurX = 0;  //Gaussian Blur. size of averaging box in the x direction [MUST BE AN ODD NUMBER]
        public double GblurY = 0; //Gaussian Blur. size of averaging box in the y direction [MUST BE AN ODD NUMBER]
        public int mediumBsize = 0; //Medium blue. //the size of the smoothing box in pixels. [MUST BE AN ODD NUMBER] 
        public double colorSimilarity = 0; //Bilateral. color similarity threshold. Pixels with values that differ more than this will not be averaged
        public int smoothingSize = 0; //Bilateral. size in pixels of smoothing region.
        public int kernelSize = 0; //the size of the filter window. [MUST BE AN ODD NUMBER] 

        //morphology settings............................................................................................
        public MorphTypes morphType;
        public double morphSize = 0; //size of window
        public int morpIterations = 0; //number of iterative applications to an image
        public bool drawHistogram = false;

        //color transform..............................................................................................
        public double brightness = 0;
        public double contrast = 0;

        public int colorSpace = 0; //0 = rgb, 1 = grey

        //Edge detection..............................................................................................//
        public int detectMethod = 0;
        public double EDparamA = 0;
        public double EDparamB = 0;
        public double EDparamC = 0;

        public CascadeClassifier haarCascade;

        public SSCamSetting() {
            
        }

        public void UpdateValue()
        {
            GblurX = CheckOdd(GblurX);
            GblurY = CheckOdd(GblurY);
            mediumBsize = Convert.ToInt32(CheckOdd(mediumBsize));
            colorSimilarity = CheckOdd(colorSimilarity);
            smoothingSize = Convert.ToInt32(CheckOdd(smoothingSize));
            kernelSize = Convert.ToInt32(CheckOdd(kernelSize));
        }
        public static double CheckOdd(double value)
        {
            double o = 0;

            if (value % 2 == 0 && value != 0)  o = value + 1;
            else o = value;

            return o;
        }

    }
    public class SSCamera
    {
        //Setup
        public VideoCapture capture;
        public int cameraIndex;
        public Mat mat = new Mat();
        public double fps;
        public int width;
        public int height;
        public int sleepTime;
        public bool run = true;
        public string msg = "";
        public string transMsg = "";
        public int timeout = 10000000;
        public List<SSCamSetting> camSetting = new List<SSCamSetting>();
        

        //run
        //public IGH_DataAccess da;
        //public GH_Component component; //the component this camera is on
        public bool errorFlag = false;

        //analysis result
        public List<Rectangle3d> faceRec = new List<Rectangle3d>();
        public List<Line> edges = new List<Line>();


        /// <summary>
        /// 
        /// </summary>
        /// <param name="_cameraIndex">Camera index</param>
        /// <param name="_fps">fps</param>
        /// <param name="_colorSpace">Color space. 0 = RGB, 1 = Gray</param>
        //public SSCamera(int _cameraIndex, double _fps, int _colorSpace, Vector2d _res, List<SSCamSetting> _camSetting)
        //{
        //    capture = new VideoCapture();
        //    cameraIndex = _cameraIndex;

        //    if (_fps == 0) fps = capture.Fps;
        //    else    fps = _fps;

        //    colorSpace = _colorSpace;
        //    camSetting = _camSetting;
        //    width = Convert.ToInt32(_res.Y);
        //    height = Convert.ToInt32(_res.X);
        //}

        public SSCamera(int _cameraIndex, double _fps, Vector2d _res, List<SSCamSetting> _camSetting)
        {
            capture = new VideoCapture();
            cameraIndex = _cameraIndex;

            if (_fps == 0) fps = capture.Fps;
            else fps = _fps;

            camSetting = _camSetting;
            width = Convert.ToInt32(_res.Y);
            height = Convert.ToInt32(_res.X);
        }

        public SSCamera() { }
      
        public void Initiate()
        {
            this.OpenCamera();

            var sw = Stopwatch.StartNew();
            double lastMS = 0;

            using (var window = new Window("Seastar Camera"))
            {
                while (run)
                {
                    Mat mat2 = this.ReadCamera();

                    //.fps count..............................
                    double thisMS = sw.ElapsedMilliseconds;
                    double frame = 1000 / (thisMS - lastMS);
                    lastMS = thisMS;
                    //........................................
                    msg = "";
                    //Mat mat3 = this.ApplyFilter(mat2);
                    this.ApplyFilter(ref mat2);
                    this.ShowCamera(mat2, window, frame);

                    
                }
                this.DestroyCamera();
            }
        }



        public void OpenCamera()
        {
            
            if (capture.IsOpened())
            {
                capture.Release();
                Cv2.DestroyAllWindows();
            }

            try
            {
                capture.Open(cameraIndex);
            }
            catch
            {
                msg = $"Failed to open camera {cameraIndex}\n";
                errorFlag = true;
            }

            Debug.WriteLine("CV::capture opened");
        }

        /// <summary>
        /// Read video capture. A mat is save into SSCamera.Mat and should not be used. Only use the clone returned from this function.
        /// </summary>
        /// <returns></returns>
        public Mat ReadCamera()
        {
            
            capture.Read(mat);
            Debug.WriteLine("CV::read");

            if (mat.Empty())
            {
                Debug.WriteLine("CV::image empty");
                msg = "Read camera return error\n";  //return error message.....
                errorFlag = true;
                return null;
            }


            Mat matOut = mat.Clone();
            OpenCvSharp.Size dim = new OpenCvSharp.Size(width, height);
            Cv2.Resize(mat, matOut, dim, 0,0 , InterpolationFlags.Linear);
            msg = "";
            msg += "Camera capturing.\n";

            //Get camera name.......................................
            List<string> names = CameraNames();
            msg += "names\n";
            foreach (string n in names)
            {
                msg += n;
                msg += "\n";
            }


            return matOut;
        }

        ///// <summary>
        ///// Read video capture. Mat is only save to ref _img. The function return a clone.
        ///// </summary>
        ///// <param name="_img">reference Mat</param>
        ///// <returns></returns>
        //public Mat ReadCamera(ref Mat _img)
        //{
        //    capture.Read(_img);
        //    Debug.WriteLine("CV::read");

        //    if (_img.Empty())
        //    {
        //        Debug.WriteLine("CV::image empty");
        //        msg = "Read camera return error.";
        //        errorFlag = true;
        //        return null;
        //    }
        //    msg += "Camera capturing";
        //    //img processing........................................

        //    //Color mode........
        //    switch (colorSpace)
        //    {
        //        case 0:
        //            break;
        //        case 1:
        //            _img = _img.CvtColor(ColorConversionCodes.RGB2GRAY);
        //            msg += "Grayscale filter applied";
        //            break;
        //    }


        //    return _img.Clone();
        //}

        /// <summary>
        /// Apply filter. SSCamera.Mat is called and a clone Mat is return.
        /// </summary>
        /// <returns></returns>
        //public Mat ApplyFilter()
        //{
        //    Mat matOut = mat.Clone();
        //    for (int i = 0; i < camSetting.Count; i++)
        //    {
        //        //Blur filters....................................................................................
        //        if (camSetting[i].blurX != 0 && camSetting[i].blurY != 0)
        //        {
        //            matOut = matOut.Blur(new OpenCvSharp.Size(camSetting[i].blurX, camSetting[i].blurY));
        //            msg += "Blur filter applied";
        //        }
        //        if (camSetting[i].GblurX != 0 && camSetting[i].GblurY != 0)
        //        {
        //            matOut = matOut.GaussianBlur(new OpenCvSharp.Size(camSetting[i].GblurX, camSetting[i].GblurY), 0, 0, BorderTypes.Default);
        //            msg += "Gaussian blur filter applied";
        //        }
        //        if (camSetting[i].mediumBsize != 0)
        //        {
        //            matOut = matOut.MedianBlur((int)camSetting[i].mediumBsize);
        //            msg += "Median blur filter applied";
        //        }
        //        if (camSetting[i].colorSimilarity != 0 && camSetting[i].smoothingSize != 0)
        //        {
        //            matOut = matOut.BilateralFilter(camSetting[i].smoothingSize, camSetting[i].colorSimilarity, 0, BorderTypes.Default);
        //            msg += "Bilateral filter applied";
        //        }

        //        //..............................................................................


        //    }
        //    //matOut = mat.Clone();
        //    return matOut;
        //}

        /// <summary>
        /// Apply filter. SSCamera.Mat is called and a clone Mat is return.
        /// </summary>
        /// <returns></returns>
        //public Mat ApplyFilter(Mat _mat)
        //{
        //    Mat matOut = _mat.Clone();
        //    for (int i = 0; i < camSetting.Count; i++)
        //    {
        //        //Blur filters....................................................................................
        //        if (camSetting[i].blurX != 0 && camSetting[i].blurY != 0)
        //        {
        //            matOut = matOut.Blur(new OpenCvSharp.Size(camSetting[i].blurX, camSetting[i].blurY));
        //            msg += "Blur filter applied\n";
        //        }
        //        if (camSetting[i].GblurX != 0 && camSetting[i].GblurY != 0)
        //        {
        //            matOut = matOut.GaussianBlur(new OpenCvSharp.Size(camSetting[i].GblurX, camSetting[i].GblurY), 0, 0, BorderTypes.Default);
        //            msg += "Gaussian blur filter applied\n";
        //        }
        //        if (camSetting[i].mediumBsize != 0)
        //        {
        //            matOut = matOut.MedianBlur((int)camSetting[i].mediumBsize);
        //            msg += "Median blur filter applied\n";
        //        }
        //        if (camSetting[i].colorSimilarity != 0 && camSetting[i].smoothingSize != 0)
        //        {
        //            matOut = matOut.BilateralFilter(camSetting[i].smoothingSize, camSetting[i].colorSimilarity, 0, BorderTypes.Default);
        //            msg += "Bilateral filter applied\n";
        //        }

        //        //..............................................................................


        //    }

        //    return matOut;
        //}


        //.....Preferred function..................


        /// <summary>
        /// Apply filter. Read setting in SScamSetting and modified ref Mat. Do not put in sc.Mat.
        /// </summary>
        /// /// <param name="matOut">ref mat to applied filter to.</param>
        /// <returns></returns>
        public void ApplyFilter(ref Mat matOut)
        {
            //Mat matOut = _mat.Clone();
            //Mat gray;
            
            //if (camSetting[i].colorSpace == 0) gray = matOut.CvtColor(ColorConversionCodes.RGB2GRAY);
            //else gray = matOut;

            for (int i = 0; i < camSetting.Count; i++)
            {
                //Color mode........

                switch (camSetting[i].colorSpace)
                {
                    case 0:
#if (DEBUG)
                        msg += "Full RGB.\n";
                        msg += $"Number of channel = {matOut.Channels().ToString()}\n";
#endif
                        break;
                    case 1:
                        matOut = matOut.CvtColor(ColorConversionCodes.RGB2GRAY);
                        msg += "Grayscale filter applied.\n";
#if (DEBUG)
                        msg += $"Number of channel = {matOut.Channels().ToString()}\n";
#endif
                        break;
                    case 2:
                        matOut = matOut.ExtractChannel(2);
                        msg += "Red filter applied.\n";
#if (DEBUG)
                        msg += $"Number of channel = {matOut.Channels().ToString()}\n";
#endif
                        break;
                    case 3:
                        matOut = matOut.ExtractChannel(1);
                        msg += "Green filter applied.\n";
#if (DEBUG)
                        msg += $"Number of channel = {matOut.Channels().ToString()}\n";
#endif
                        break;
                    case 4:
                        matOut = matOut.ExtractChannel(0);
                        msg += "Blue filter applied.\n";
#if (DEBUG)
                        msg += $"Number of channel = {matOut.Channels().ToString()}\n";
#endif
                        break;
                }


                //Blur filters....................................................................................
                if (camSetting[i].blurX != 0 && camSetting[i].blurY != 0)
                {
                    matOut = matOut.Blur(new OpenCvSharp.Size(camSetting[i].blurX, camSetting[i].blurY));
                    msg += "Blur filter applied\n";
                }
                if (camSetting[i].GblurX != 0 && camSetting[i].GblurY != 0)
                {
                    matOut = matOut.GaussianBlur(new OpenCvSharp.Size(camSetting[i].GblurX, camSetting[i].GblurY), 0, 0, BorderTypes.Default);
                    msg += "Gaussian blur filter applied\n";
                }
                if (camSetting[i].mediumBsize != 0)
                {
                    matOut = matOut.MedianBlur((int)camSetting[i].mediumBsize);
                    msg += "Median blur filter applied\n";
                }
                if (camSetting[i].colorSimilarity != 0 && camSetting[i].smoothingSize != 0)
                {
                    matOut = matOut.BilateralFilter(camSetting[i].smoothingSize, camSetting[i].colorSimilarity, 0, BorderTypes.Default);
                    msg += "Bilateral filter applied\n";
                }

                //Morphology......................................................................................

                if (camSetting[i].morphSize != 0)
                {
                    var kernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new OpenCvSharp.Size(camSetting[i].morphSize, camSetting[i].morphSize));
                    //Cv2.MorphologyEx(rgb, rgb2, MorphTypes.Open, kernel, null, iterations);
                    matOut = matOut.MorphologyEx(camSetting[i].morphType, kernel, null, camSetting[i].morpIterations);
                    string morpName = Enum.GetName(typeof(MorphTypes), camSetting[i].morphType);
                    msg += $"{morpName} morphology filter applied\n";
                }

                //Color transfomation.brightness, contrast.....................................................

                if(camSetting[i].brightness != 0)
                {
                    matOut *= camSetting[i].brightness;
                    msg += "Brightness transformation applied\n";
                }

                if (camSetting[i].contrast != 0)
                {
                    matOut += camSetting[i].contrast;
                    msg += "Contrast transformation applied\n";
                }

                if (camSetting[i].drawHistogram)
                {
                    Mat gray = GrayMat(matOut);
                    Cv2.EqualizeHist(gray, matOut);
                    
                    drawHistogram(matOut);

                }

                //Edge detection................................................................................
                if (camSetting[i].EDparamA != 0)
                {
                    Mat gray = GrayMat(matOut);

                    switch (camSetting[i].detectMethod)
                    {
                        case 0:
                            Cv2.EqualizeHist(gray, gray);
                            matOut = gray.Canny(camSetting[i].EDparamA, camSetting[i].EDparamB);
                            break;
                        case 1:

                            matOut = matOut.Canny(40, 210, 3, false);
                            double rho_param = camSetting[i].EDparamA;
                            double theta_param = camSetting[i].EDparamB;
                            int param = (int)camSetting[i].EDparamC;

                            LineSegmentPolar[] lines = Cv2.HoughLines(matOut, rho_param, theta_param, param, 0, 0);
                            int limit = Math.Min(lines.Length, 50);
                            for (int j = 0; j < limit; j++)
                            {
                                float rho = lines[j].Rho;
                                float theta = lines[j].Theta;
                                double a = Math.Cos(theta);
                                double b = Math.Sin(theta);
                                double x0 = a * rho;
                                double y0 = b * rho;
                                OpenCvSharp.Point pt1 = new OpenCvSharp.Point
                                {
                                    X = (int)Math.Round((float)(x0 + 1000 * (-b))),
                                    Y = (int)Math.Round((float)(y0 + 1000 * (a)))
                                };
                                OpenCvSharp.Point pt2 = new OpenCvSharp.Point
                                {
                                    X = (int)Math.Round((float)(x0 - 1000 * (-b))),
                                    Y = (int)Math.Round((float)(y0 - 1000 * (a)))
                                };
                                matOut.Line(pt1, pt2, new Scalar(0, 255, 255, 127), 1, LineTypes.AntiAlias, 0);
                            }
                            break;
                        case 2: //Line segment
                            Cv2.Canny(gray, gray, 50, 200, 3, false);

                            double minLinLength = camSetting[i].EDparamA;
                            double maxLineGap = camSetting[i].EDparamB;

                            LineSegmentPoint[] segs = Cv2.HoughLinesP(gray, 1.0, Math.PI / 180, 80, minLinLength, maxLineGap);
                            int lim = Math.Min(segs.Length, 50);
                            for (int k = 0; k < lim; k++)
                            {
                                edges.Clear();
                                matOut.Line(segs[k].P1, segs[k].P2, new Scalar(0, 255, 0, 127), 1, LineTypes.AntiAlias, 0);
                                edges.Add(new Line(CvPointToRhino(segs[k].P1), CvPointToRhino(segs[k].P2)));
                            }
                            break;
                        case 3:  //Circle detect
                            Cv2.MedianBlur(gray, gray, 5);

                            CircleSegment[] circs = Cv2.HoughCircles(gray, HoughModes.Gradient, 2.0, 50, camSetting[i].EDparamA * 50.0, camSetting[i].EDparamB * 20.0, 5, 1000);
                            int limm = Math.Min(circs.Length, 50);


                            //Cv2.CvtColor(gray, rgb2, ColorConversionCodes.GRAY2RGBA);
                            for (int n = 0; n < limm; n++)
                            {
                                matOut.Circle((OpenCvSharp.Point)circs[n].Center, (int)circs[n].Radius, new Scalar(0, 255, 255, 121));
                            }

                            break;
                    }
                    
                        
                    //Cv2.Canny(gray, gray, param*50.0, param2*100.0);
                }

                //Face detection..............................................................................
                if (camSetting[i].haarCascade != null && camSetting[i].haarCascade.CvPtr != System.IntPtr.Zero)
                {
                    msg += "Face detection applied\n";
                    Mat gray = GrayMat(matOut);
                    
                    OpenCvSharp.Rect[] faces = camSetting[i].haarCascade.DetectMultiScale(gray, 1.08, 3, HaarDetectionTypes.ScaleImage, new OpenCvSharp.Size(124, 124));

                    faceRec.Clear();
                    foreach (OpenCvSharp.Rect face in faces)
                    {
                        var center = new OpenCvSharp.Point
                        {
                            X = (int)(face.X + face.Width * 0.5),
                            Y = (int)(face.Y + face.Height * 0.5)
                        };
                        var axes = new OpenCvSharp.Size
                        {
                            Width = (int)(face.Width * 0.5),
                            Height = (int)(face.Height * 0.5)
                        };
                        var faceCol = new Scalar(0, 255, 0, 128);

                        //Cv2.Ellipse(matOut, center, axes, 0, 0, 360, faceCol, 4);
                        Cv2.Rectangle(matOut, face, faceCol, 1, LineTypes.Link8, 0);
                        msg += "face detected\n";

                        Rhino.Geometry.Point3d rhCenter = new Rhino.Geometry.Point3d(face.X, face.Y, 0);
                        Plane rP = new Plane(Plane.WorldXY);
                        rP.Origin = rhCenter;
                        Rectangle3d rhRec = new Rectangle3d(rP, face.Width, face.Height);
                        faceRec.Add(rhRec);
                    }

                }



            }
        }


        public void ShowCamera(Mat _img, Window _window, double frame)
        {
            sleepTime = (int)Math.Round(1000 / fps);
            Debug.WriteLine("CV::sleepTime = {0}", sleepTime);

            Mat toShow = new Mat();
            if (_img.Height < 400) Cv2.Resize(_img, toShow, new OpenCvSharp.Size(640, 480), 0, 0, InterpolationFlags.Linear);
            else toShow = _img;

            Cv2.PutText(_img, $"FPS:{frame:F}", new OpenCvSharp.Point(10, 40), HersheyFonts.HersheySimplex, 0.5, Scalar.Red);

            _window.ShowImage(toShow);

            Cv2.WaitKey(sleepTime);
        }

        public void DestroyCamera()
        {
            capture.Release();
            Cv2.DestroyAllWindows();

            Debug.WriteLine("CV::CV instance end");
        }

        public static Rhino.Geometry.Point3d CvPointToRhino(OpenCvSharp.Point _cvPt)
        {
            return new Rhino.Geometry.Point3d(_cvPt.X, _cvPt.Y, 0);
        }
        public static Firefly_Bitmap MatToFireFly(Mat _matIn)
        {
            Mat _mat = _matIn.Clone();
            Firefly_Bitmap ff = new Firefly_Bitmap();

            ff.SetSize(_mat.Width, _mat.Height);

            for (int y = 0; y < _mat.Height; y++)
            {
                for (int x = 0; x < _mat.Width; x++)
                {
                    Vec3b color = _mat.Get<Vec3b>(y, x);
                    byte temp = color.Item0;

                    int yi = _mat.Height -1 - y;

                    ff.pixels[x, yi].R = (byte)color.Item2;
                    ff.pixels[x, yi].G = (byte)color.Item1;
                    ff.pixels[x, yi].B = (byte)color.Item0;
                    ff.pixels[x, yi].A = byte.MaxValue;
                }
            }

            return ff;
        }
        public static Mat GrayMat(Mat matOut)
        {
            Mat gray;
            if (matOut.Channels() > 1) gray = matOut.CvtColor(ColorConversionCodes.RGB2GRAY);
            else gray = matOut;

            return gray;
        }
        static void drawHistogram(Mat img)
        {
            //................................................histogram
            //


            Mat hist = new Mat();
            int[] hdims = { 64 }; // Histogram size for each dimension
            Rangef[] ranges = { new Rangef(0, 256), }; // min/max 
            Cv2.CalcHist(
                new Mat[] { img },
                new int[] { 0 },
                null,
                hist,
                1,
                hdims,
                ranges);

            // Get the max value of histogram
            double minVal, maxVal;
            Cv2.MinMaxLoc(hist, out minVal, out maxVal);

            Scalar color = Scalar.All(255);
            // Scales and draws histogram
            hist = hist * (maxVal != 0 ? 0.5 * img.Height / maxVal : 0.0);
            double binW = (double)img.Width / hdims[0];
            for (int j = 0; j < hdims[0]; ++j)
            {
                double x = j * binW;
                int h = (int)(hist.Get<float>(j));
                if (h > img.Rows) h = img.Rows;
                img.Rectangle(
                    new OpenCvSharp.Point((int)(x), img.Rows - h),
                    new OpenCvSharp.Point((int)(x + binW), img.Rows),
                    new Scalar(j * 4),
                    -1);
            }
        }
        public static List<string> CameraNames()
        {
            List<string> str = new List<string>();
            
            
            var deviceManager1 = new DeviceManager();
            for (int i = 1; (i <= deviceManager1.DeviceInfos.Count); i++)
            {
                if (deviceManager1.DeviceInfos[i].Type == WiaDeviceType.CameraDeviceType || deviceManager1.DeviceInfos[i].Type == WiaDeviceType.VideoDeviceType)
                {
                    //Debug.WriteLine(deviceManager1.DeviceInfos[i].Properties.ToString());
                    str.Add( i.ToString());
                }
            }
            return str;
        }
    }



    //public class WindowThread
    //{
    //    public CascadeClassifier haarCascade;
        

    //    public void OpenWindow()
    //    {
    //      //  VideoCapture capture = new VideoCapture();
    //        Camera.capture.Open(Camera.ci);
            

    //        int sleepTime = (int)Math.Round(1000 / Camera.capture.Fps);
    //        Debug.WriteLine("CV::sleepTime = {0}", sleepTime);
            
    //        var sw = Stopwatch.StartNew();
    //        var counter = 0;
    //        int timeout = 100000;
    //        if (Camera.run)
    //        {
    //            using (var window = new Window("Seastar Camera"))
    //            using (Mat img = new Mat())
    //            {
    //                while (counter < timeout && Camera.run)
    //                {
    //                    //capture.Fps = Camera.fps;
    //                    //capture.Set(CaptureProperty.Fps, 1);
    //                    double fps = Camera.capture.Fps;
    //                    Camera.capture.Read(img);
    //                    Debug.WriteLine("CV::read");
    //                    if (img.Empty())
    //                    {
    //                        Debug.WriteLine("CV::image empty");
    //                        Camera.msg += "Read camera return error";
    //                        return;
    //                    }
    //                    counter++;
    //                    double frame = (double)counter / sw.ElapsedMilliseconds * 1000;
    //                    Cv2.PutText(img, $"FPS:{frame:F}", new OpenCvSharp.Point(10, 40), HersheyFonts.HersheySimplex, 1, Scalar.Red);
    //                    window.ShowImage(img);
    //                    Camera.img = img;
    //                    Camera.bmp = BitmapConverter.ToBitmap(Camera.img);
    //                    //Camera.fbmp = WindowThread.BitmapToFireFly(Camera.bmp);

    //                    Cv2.WaitKey(sleepTime);
    //                    Debug.WriteLine("CV::frame = {0}", counter);
    //                }

    //                Camera.capture.Release();
    //                Cv2.DestroyAllWindows();
    //                Camera.img = null;
    //                Debug.WriteLine("CV::CV instance end");
    //            }
    //        }
    //        Camera.openThread = true;
    //    }

    //    public static Firefly_Bitmap BitmapToFireFly(Bitmap bmp)
    //    {
    //        Firefly_Bitmap fbmp = new Firefly_Bitmap();
    //        fbmp.SetSize(bmp.Width, bmp.Height);

    //        for (int j = bmp.Height - 1; j >= 0; j--)
    //        {
    //            for (int i = 0; i < bmp.Width; i++)
    //            {
    //                fbmp.pixels[i, j].A = bmp.GetPixel(i, j).A;
    //                fbmp.pixels[i, j].R = bmp.GetPixel(i, j).R;
    //                fbmp.pixels[i, j].G = bmp.GetPixel(i, j).G;
    //                fbmp.pixels[i, j].B = bmp.GetPixel(i, j).B;
    //            }
    //        }
    //        return fbmp;
    //    }
    //}
}
