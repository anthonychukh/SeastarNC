using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Beaver;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.Display;
using Rhino.Geometry;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using Firefly_Bridge;

namespace SeastarGrasshopper
{

    public class Camera : GH_Component
    {
        public Camera()
          : base("Camera", "Camera",
              "Define Camera to use",
              "Beaver", "CV")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("run", "run", "runrunrun", GH_ParamAccess.item, false);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Message", "msg", "Message", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool run = false;
            DA.GetData<bool>(0, ref run);
            Bitmap bm;

            if (run)
            {
                //..open image in new window..........................
                //var src = new Mat(@"C:\Users\antho\Google Drive (anthony@space10.io)\Kai-Hong Anthony Chu\Resources\4622_large_arduino_uno_main_board.jpg", ImreadModes.Unchanged);
                //var window = new Window("window", image: src, flags: WindowMode.AutoSize);
                //Cv2.WaitKey();

                //turn on webCam........................................
                //var capture = new VideoCapture(@"C:\Users\antho\Google Drive (anthony@space10.io)\Kai-Hong Anthony Chu\Resources\4_6s.mp4");
                
                VideoCapture capture = new VideoCapture();
                capture.Open(1);
                int sleepTime = (int)Math.Round(1000 / capture.Fps);
                Debug.WriteLine("CV::sleepTime = {0}", sleepTime);
                
                int f = 0;
                //using (var window = new Window("window"))
                using (Mat img = new Mat())
                {
                    

                    while (f < 200)
                    {
                        
                        double fps = capture.Fps;


                        capture.Read(img);
                        Debug.WriteLine("CV::read");
                        if (img.Empty())
                        {
                            Debug.WriteLine("CV::image empty");
                            return;
                        }

                        bm = BitmapConverter.ToBitmap(img);
                        Firefly_Bitmap fbm = new Firefly_Bitmap();
                        
                        
                       // window.ShowImage(img);
                        


                        Cv2.WaitKey(sleepTime);
                        f++;
                        Debug.WriteLine("CV::frame = {0}", f);
                        
                    }
                    
                }
                capture.Release();
                DA.SetData(0, f.ToString());
                Cv2.DestroyAllWindows();
                Debug.WriteLine("CV::CV instance end");
            }

            
        }

        protected override System.Drawing.Bitmap Icon => Resources.joker;

        public override Guid ComponentGuid
        {
            get { return new Guid("986ca318-c991-411b-8287-e20870790439"); }
        }
    }
}
