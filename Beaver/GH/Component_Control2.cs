//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.IO.Ports;
//using Grasshopper.Kernel;
//using Rhino.Geometry;

//namespace BeaverGrasshopper
//{
//    public class SerialPortWrapper
//    {
//        public SerialPort SerialPort;
//        public string PortName;
//        public bool DataIn;
//        public string PortMsg;
//        public int Braudrate;
//        public bool IsOpen => SerialPort.IsOpen;

//        public SerialPortWrapper()
//        {
//            this.SerialPort = null;
//            PortName = "";
//        }
//    }

//    //public SerialPortWrapper myPort = new SerialPortWrapper();





//    public class Component_Control : GH_Component
//    {
//        /// <summary>
//        /// Initializes a new instance of the MyComponent1 class.
//        /// </summary>
//        public Component_Control()
//          : base("Machine Control", "05 | Connect",
//              "Send command to the machine??",
//              "Seastar", "05 | Connect")
//        {
//        }

//        /// <summary>
//        /// Registers all the input parameters for this component.
//        /// </summary>
//        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
//        {
//            pManager.AddBooleanParameter("Start", "Start", "Start sending message", GH_ParamAccess.item);
//            pManager.AddTextParameter("Port Name", "name", "COM port name", GH_ParamAccess.item);
//            pManager.AddIntegerParameter("Braudrate", "braud", "Braudrate", GH_ParamAccess.item);
//            pManager.AddTextParameter("Lines", "send", "Lines to send", GH_ParamAccess.list, "default");
//        }

//        /// <summary>
//        /// Registers all the output parameters for this component.
//        /// </summary>
//        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
//        {
//            pManager.AddTextParameter("Message", "msg", "Message for you", GH_ParamAccess.item);
//            pManager.AddTextParameter("Received", "msgIn", "Message receiving from port", GH_ParamAccess.list);
//        }

//        /// <summary>
//        /// This is the method that actually does the work.
//        /// </summary>
//        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
//        protected override void SolveInstance(IGH_DataAccess DA)
//        {
//            bool start = false;
//            string name = "";
//            int braud = 0;
//            List<string> lines = new List<string>();
//            DA.GetData(0, ref start);
//            DA.GetData(1, ref name);
//            DA.GetData(2, ref braud);
//            DA.GetDataList<string>(3, lines);

            
//            SerialPort port = new SerialPort(name, braud, Parity.None, 8, StopBits.One);
//            port.DtrEnable = true;
//            if (!port.IsOpen && start)
//            {
//                port.Open();
//                message.Add("Port open");
//                Debug.WriteLine("port open");
//            }
//            if (start)
//            {
//                for (int i = 0; i < lines.Count; i++)
//                {
//                    port.Write(lines[i]);
//                    Debug.WriteLine("port write line");
//                }
//                port.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
//                DA.SetDataList(1, myReceivedLines);
//            }
//            if(!start && port.IsOpen)
//            {
//                port.Close();
//                Debug.WriteLine("port closed");
//            }
//        }

//        List<string> myReceivedLines = new List<string>();
//        List<string> message = new List<string>();

//        private void DataReceivedHandler(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
//        {
//            SerialPort sp = (SerialPort)sender;
//            while (sp.BytesToRead > 0)
//            {
//                try
//                {
//                    myReceivedLines.Add(sp.ReadLine());
//                }
//                catch (TimeoutException)
//                {
//                    break;
//                }
//            }
//        }
//        /// <summary>
//        /// Provides an Icon for the component.
//        /// </summary>
//        protected override System.Drawing.Bitmap Icon
//        {
//            get
//            {
//                //You can add image files to your project resources and access them like this:
//                // return Resources.IconForThisComponent;
//                return null;
//            }
//        }

//        /// <summary>
//        /// Gets the unique ID for this component. Do not change this ID after release.
//        /// </summary>
//        public override Guid ComponentGuid
//        {
//            get { return new Guid("b9c33183-f1fe-4e09-8369-d39cef2d28fd"); }
//        }
//    }
//}