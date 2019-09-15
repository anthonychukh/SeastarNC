using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Windows.Forms;
using Seastar;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace SerialComponentLibrary
{
    
    public class Connect : GH_Component
    {

        public static readonly SerialPort thisPort = new SerialPort();
        public static int N = 0; //this Line count. Add one after writing
        public static int lastNsent = 0; //keep track of lastN sent, check with read ok
        public static string lastLine = "";
        public static int ok = 0; //last ok signal
        public static int? resend = null;
        public static bool busy = false;
        public static List<string> log = new List<string>();
        public static List<string> printerInfo = new List<string>();
        public static Queue<string> queue = new Queue<string>();
        //public static Queue<string> recentSent = new Queue<string>();
        public static bool printerReady = false;
        public static int queueSize = 5;  //desire queue size in printer memory. Ready to send more when ok [N] = lastNsent - queueSize
        public static string initCode =
            "M105 ; get extruder temp\n" +
            "M114 ; get position\n" +
            "T0\n" +
            "M20 ; SD card\n" +
            "M80 ; AXT power on\n" +
            "M220 S100 ; speed factor override\n" +
            "M221 S100 ; extrude factor override\n" +
            "M111 S6 ; Debug level 6\n" +
            "M155 S1 ; auto send temp\n";



        public Connect() : base("Connect to printer", "Connect", 
            "Connect to printer through Serial Port.", 
            "Seastar", "05 | Connect")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            
            pManager.AddTextParameter("Port", "P", "Port Name", GH_ParamAccess.item, "COM6");
            pManager.AddIntegerParameter("Baud", "B", "Baud Rate", GH_ParamAccess.item, 115200);
            pManager.AddBooleanParameter("Open", "O", "Open Port", GH_ParamAccess.item, false);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Message", "M", "Message", GH_ParamAccess.item);
            pManager.AddTextParameter("debug", "debug", "debug", GH_ParamAccess.list);
        }

        public bool includeInitCode = true;
        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalComponentMenuItems(menu);
            Menu_AppendItem(menu, "include initiation code", Menu_initCode, true, includeInitCode);
        }

        private void Menu_initCode(object Sender, EventArgs e)
        {
            includeInitCode = !includeInitCode;
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {


            string port = null;
            int baud = 0;
            bool handshake = true;
            bool open = false;
            string message = "";
            List<string> msgs = new List<string>();

            if (!DA.GetData(0, ref port)) { return; }
            if (!DA.GetData(1, ref baud)) { return; }
            if (!DA.GetData(2, ref open)) { return; }


            
            if (handshake == true)
            {
                thisPort.Handshake = System.IO.Ports.Handshake.XOnXOff;
            }
            else
            {
                thisPort.Handshake = System.IO.Ports.Handshake.None;
            }

            if(open == false && !thisPort.IsOpen)
            {
                string[] pNames = SerialPort.GetPortNames();
                string portNames = "";
                for (int i = 0; i < pNames.Length; i++)
                {
                    portNames += pNames[i];
                    if (i < pNames.Length - 1) portNames += ",";
                }
            
                message = "Ready to connect\nConnect toggle switch to input O\nAvailable port: " + portNames;

            }
            if(open == true && thisPort.IsOpen)
            {
                message = "Machine connected";
            }

            if (open == true && !thisPort.IsOpen)
            {
                try
                {
                    thisPort.PortName = port;
                    thisPort.BaudRate = baud;
                    int inbuf = thisPort.ReadBufferSize;
                    Debug.WriteLine(inbuf.ToString());
                    thisPort.ReadBufferSize = 32768;
                    thisPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);

                    log.Clear();
                    thisPort.Open();
                    message = "Machine connected";
                    
                    //initialised printer
                    thisPort.WriteLine(Gcode.AddCheckSum("N1 M110") +"\n"); //set line umber
                    thisPort.WriteLine(Gcode.AddCheckSum("N2 M115") + "\n"); //get firmware info
                    N = 3;
                    lastNsent = 2;
                }
                catch
                {
                    message = "Error opening Serial Port";
                }
            }
            if (open == false && thisPort.IsOpen)  //close port
            {
                thisPort.DiscardInBuffer();
                thisPort.DiscardOutBuffer();
                thisPort.Close();
                message = "Printer disconnected";
                log.Clear();
                queue.Clear();
                N = 0;
                lastNsent = 0;
                ok = 0;
                resend = null;
                busy = false;
                printerReady = false;
                lastLine = "";
                Seastar.Extension.expireOthers("WriteQueue", this);
            }


            //get firmware info from ok 2
            int infoi = 5;
            if(log.Count <= infoi)
            {
                string mInfo;
                int x = 0;
                do
                {
                    x++;

                    Debug.WriteLine("Looking for firmware info");
                } while (x < log.Count && log[x].Contains("FIRMWARE")); //loop until find ok 2
                x--;

                if (log.Count >= 1 && x >= 0  && log[x].Contains("FIRMWARE"))
                {
                    mInfo = log[x];
                    message += "\n\n";
                    message += "Firmware info : \n";
                    message += mInfo.Remove(0, 5);

                    if (includeInitCode)
                    {
                        string initCodee =
                            "M105 ; get extruder temp\n" +
                            "M114 ; get position\n" +
                            "T0\n" +
                            "M20 ; SD card\n" +
                            "M80 ; AXT power on\n" +
                            "M220 S100 ; speed factor override\n" +
                            "M221 S100 ; extrude factor override\n" +
                            "M111 S6 ; Debug level 6\n" +
                            "M155 S1 ; auto send temp\n" +
                            "G28;Home\n";

                        SerialComponentLibrary.Write2Queue.AddToQueue(initCodee, ref msgs);
                        Debug.WriteLine("Connect::Printer connected & initialised");
                        Connect.printerReady = true;
                    }
                    
                    Debug.WriteLine("Connect::Printer connected");
                }
                else
                {
                    if (open)
                    {
                        this.ExpireSolution(true);
                        Debug.WriteLine("Connect::Expire");
                    }
                }
            }

            DA.SetData(0, message);
            DA.SetDataList(1, log);
        }

        protected override System.Drawing.Bitmap Icon => Seastar.Properties.Resources.cntConnect;

        public override Guid ComponentGuid
        {
            get { return new Guid("2B62BEA7-CF4B-41ef-BC7F-72C47AD37431"); }
        }


        private static void DataReceivedHandler(
                        object sender,
                        SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            Debug.WriteLine("Data Received:");

            if (sp.IsOpen && sp.BytesToRead > 0)
            {
                
                string inda = sp.ReadExisting();
                string indata = inda;
                string[] nn = inda.Split('\n');

                //indata += DateTime.Now.ToString();
                log.Add(indata);
                Debug.Write("s::"+ indata + "::e");

                //find status from messga ereceived.............................
                for (int i = 0; i < nn.Length; i++)
                //for (int i = nn.Length - 1; i > 0; i--)
                {
                    if (nn[i].Contains("ok"))  //find ok. check length??? && nn[i].Length > 3
                    {
                        
                        try
                        {
                            string n = nn[i].Remove(0, 3);
                            Debug.WriteLine("Connect::OK Number:" + n);
                            ok = Convert.ToInt32(n);
                        }
                        catch
                        {
                            ok++;
                        }
                        //break;
                    }

                    if (nn[i].Contains("Resend"))     //find resend
                    {
                        string n = nn[i].Remove(0, 7);
                        Debug.WriteLine("Connect::Resend Number:" + n);
                        resend = Convert.ToInt32(n);
                        //break;
                    }
                    else
                    {
                        //resend = null;
                    }

                    if (nn[i].Contains("Busy") && !Connect.busy)  //find busy
                    {
                        Connect.busy = true;
                        //break;
                    }
                    if (Connect.busy && !nn[i].Contains("Busy"))
                    {
                        Connect.busy = false;
                        //break;
                    }
                }
            }
        }


        

        //private bool WaitForResult(ref string Result, int Timeout)
        //{
        //    bool read = false;
        //    double WaitTimeout = Timeout + DateTime.Now.TimeOfDay.TotalMilliseconds;
        //    while (!(DateTime.Now.TimeOfDay.TotalMilliseconds >= WaitTimeout))
        //    {
        //        int BytesToRead = thisPort.BytesToRead;
        //        if (BytesToRead > 0)
        //        {
        //            byte[] Bytes = new byte[BytesToRead];
        //            Result = thisPort.ReadLine();
        //            if (ResultComplete(Result) == true)
        //            {
        //                read = true;
        //            }
        //            else
        //            {
        //                //System.Windows.Forms.Application.DoEvents();
        //            }
        //        }
        //    }
        //    return read;
        //}

        //private bool ResultComplete(string TestData)
        //{
        //    bool ok = false;
        //    if (TestData.Contains("ok") || TestData.Contains("start"))
        //    {
        //        ok = true;
        //    }
        //    return ok;
        //}


    }

    public class ReadWrite : GH_Component
    {
        public ReadWrite() : base("ReadWrite", "RW", "Manage all data send between Grasshopper and printer\n" +
            "Reads printer log and sends command from queue to printer\n" +
            "Update and loop auotmatically", "Seastar", "05 | Connect")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            //pManager.AddBooleanParameter("On", "On", "Turn on Debugger", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Printer Logs", "L", "Printer logs and events", GH_ParamAccess.list);
            pManager.AddTextParameter("Queue", "Q", "Queue of commands waiting to be sent\n" +
                "Command can only be sent when the printer is ready to receive it\n" +
                "If queue get too long, reduce subdivision or increase interval between commands", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {

            int queueSize = 3;
            List<string> logtemp = new List<string>();
            //Resend..............................................................
            //if (Connect.resend.HasValue && !Connect.busy)
            //{
            //    Debug.WriteLine("ReadWrite::Resending");

            //    int nResend = Connect.resend.Value;
            //    string lineN = "N" + nResend.ToString();
            //    List<string> lineResend = new List<string>();
            //    int i = Connect.log.Count - 1;
            //    do
            //    {
            //        if (Connect.log[i].Contains("N"))
            //        {
            //            lineResend.Add(Connect.log[i]);
            //        }
            //        i--;
            //    } while (!Connect.log[i].Contains(lineN));

            //    lineResend.Reverse();
            //    for (int j = 0; j < lineResend.Count; j++)
            //    {
            //        try
            //        {
            //            SerialComponentLibrary.Connect.thisPort.WriteLine(lineResend[j]);
            //            Connect.log.Add(lineResend[j]);
            //            Connect.resend = null;
            //        }
            //        catch
            //        {
            //            this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Fail to resend");
            //            break;
            //        }
            //    }

            //}

            //write from queue...............................................................
            if (!Connect.busy && !Connect.resend.HasValue && Connect.queue.Count > 0)
            {
                DA.SetDataList(1, Connect.queue);
                
                //queue not empty yet && printer internal queue is less than queuSize i.e ready for more...................
                if (Connect.ok > Connect.lastNsent - queueSize)  
                {
                    string package = "";
                    int pSize = Connect.ok + queueSize - Connect.lastNsent;

                    for (int i = 0; i < Connect.queue.Count && i < pSize; i++) // package
                    {
                        string thisLine = Connect.queue.Dequeue();
                        if (!thisLine.Contains("*"))
                        {
                            thisLine = Gcode.AddCheckSum(thisLine);
                        }
                        package += thisLine;
                        package += "\n";
                        Connect.lastNsent++;
                    }

                    try
                    {
                        SerialComponentLibrary.Connect.thisPort.WriteLine(package);
                        Connect.log.Add(package);
                    }
                    catch
                    {
                        this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Fail to write to serial");
                    }
                }
                //logtemp = Connect.log;

            }

            //display log and queue........................................
            if (Connect.thisPort.IsOpen)
            {
                try
                {
                    DA.SetDataList(0, Connect.log);  //show log
                }
                catch
                {
                    DA.SetData(0, "standby");
                    Debug.WriteLine("ReadWrite::Connect,log thread overlap");
                }
            }
            else
            {
                DA.SetData(0, "No printer connected");
            }

            this.ExpireSolution(true);
        }

        protected override System.Drawing.Bitmap Icon => Seastar.Properties.Resources.cntLog;
        public override Guid ComponentGuid
        {
            get { return new Guid("52D22D10-A34C-4865-A9D4-EDF86EACA8DA"); }
        }

        
    }

    

    public class Write2Queue : GH_Component
    {
        //SerialComponentLibrary.SerialTest01 thisPort = new SerialComponentLibrary.SerialTest01();

        public Write2Queue()
            : base("WriteToQueue", "WQ", "This compoent ONLY write command to queue\n" +
                  "Use ReadWrite component to actually send command to printer", "Seastar", "05 | Connect")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("string", "T", "Text to Send", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Send", "S", "Set true to send command(s) to queue\n" +
                "Commands in queue will be sent asap", GH_ParamAccess.item, false);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Message", "M", "Message", GH_ParamAccess.list);
            
        }

       // public bool g90 = false;

       

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool send = false;
            DA.GetData<bool>(1, ref send);
            if (!send && !Connect.printerReady)
            {
                DA.SetData(0, "Printer not ready");
                return;  //Do not send
            }

            //string message = null;
            List<string> textin = new List<string>();
            List<string> text = new List<string>();
            List<string> msg = new List<string>();

            if (!DA.GetDataList(0, textin)) { return; }

            if (Connect.printerReady)
            {
                AddToQueue(textin, ref msg);
            }

            //not connected........................................................
            if (!SerialComponentLibrary.Connect.thisPort.IsOpen)
            {
                msg.Add("No printer connected");
            }

            DA.SetDataList(0, msg);
        }


        protected override System.Drawing.Bitmap Icon => Seastar.Properties.Resources.cntQueue;
        public override Guid ComponentGuid
        {
            get { return new Guid("87AC7B84-3103-44fb-91CA-488C5447DD0E"); }
        }


        /// <summary>
        /// Add strings to queue. Can be list<string> or string with \n
        /// </summary>
        /// <param name="_Data">Strings to add</param>
        /// <param name="msg">Message about this operation</param>
        public static void AddToQueue(List<string> _Data, ref List<string> msg)
        {
            //msg = new List<string>();
            //add \n if havent.................................................
            List<string> text = new List<string>();
            for (int i = 0; i < _Data.Count; i++)
            {
                if (_Data[i].Contains("\n"))
                {
                    string[] tt = _Data[i].Split('\n');
                    text.AddRange(tt.ToList());
                }
                else
                {
                    text.Add(_Data[i]);
                }
            }

            if (SerialComponentLibrary.Connect.thisPort.IsOpen)
            {
                //Add to queue......................................................
                for (int i = 0; i < text.Count; i++)
                {

                    if (text[i].Contains(";")) //remove comment
                    {
                        text[i] = text[i].Split(';')[0];
                    }
                    if (text[i].Length > 0)
                    {
                        text[i] = "N" + Connect.N.ToString() + " " + text[i];
                        text[i] = AddCheckSum(text[i]);
                        if(Connect.lastLine.Contains("G28") && text[i].Contains("G28")) { continue; } //no repeating g28
                        Connect.queue.Enqueue(text[i]);
                        Connect.lastLine = text[i];

                        msg.Add(text[i] + " sent to queue");
                        Connect.N++;
                    }
                }
            }

            //return msg;
        }


        /// <summary>
        /// Add strings to queue. Can be list<string> or string with \n
        /// </summary>
        /// <param name="_Data">String to add</param>
        /// <param name="msg">Message about this operation</param>
        public static void AddToQueue(string _Data, ref List<string> msg)
        {
            
            List<string> text = new List<string>();

            if (_Data.Contains("\n"))
            {
                string[] tt = _Data.Split('\n');
                text.AddRange(tt.ToList());
            }
            else
            {
                text.Add(_Data);
            }


            if (SerialComponentLibrary.Connect.thisPort.IsOpen)
            {
                //Add to queue......................................................
                for (int i = 0; i < text.Count; i++)
                {

                    if (text[i].Contains(";")) //remove comment
                    {
                        text[i] = text[i].Split(';')[0];
                    }
                    if (text[i].Length > 0)
                    {
                        text[i] = "N" + Connect.N.ToString() + " " + text[i];
                        //text[i] = AddCheckSum(text[i]);
                        Connect.queue.Enqueue(text[i]);

                        msg.Add(text[i] + " sent to queue");
                        Connect.N++;
                    }
                }
            }

            //return msg;
        }

        public static string AddCheckSum(string _line)
        {
            int cs = 0;
            for (int i = 0; i< _line.Length; i++)
                cs ^= _line[i];
            cs &= 0xff;

            return _line + "*" + cs.ToString();
        }
    }

}
