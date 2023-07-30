using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Windows.Forms;
using Seastar;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Eto.Forms;


namespace SerialComponentLibrary
{
    
    public class Connect : GH_Component
    {
        #region control constant variables..............................................
        /// <summary> The current active port seastar is using. </summary>
        public static readonly SerialPort thisPort = new SerialPort();
        /// <summary> The current line count. Add one after wrote to queue. </summary>
        public static int N = 0; 
        public static int lastNsent = 0; //keep track of lastN sent, check with read ok
        public static string lastLine = "";
        public static int ok = 0; //last ok signal
        public static int? resend = null;
        public static bool busy = false;
        //public static List<string> logQueue = new List<string>();
        public static Queue<string> logQueue = new Queue<string>();
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
            "M155 S1 ; auto send temp\n" +
            "G28;Home\n";
        public static string firmwareInfo = "";

        #endregion............................................................

        public Connect() : base("Connect to printer", "Connect", 
            "Connect to printer through Serial Port.", 
            "Seastar", "05 | Connect")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Port", "P", "Port Name", GH_ParamAccess.item, "COM6");
            pManager.AddIntegerParameter("Baud", "B", "Baud Rate", GH_ParamAccess.item, 250000);
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
            var dd1 = Menu_AppendItem(menu, "include initiation code", Menu_initCode, true, includeInitCode);
            //dd1.Click += (sender, e) => this.ExpireSolution(true);
        }

        private void Menu_initCode(object Sender, EventArgs e)
        {
            includeInitCode = !includeInitCode;
        }

        public override void AddedToDocument(GH_Document document)
        {
            base.AddedToDocument(document);
            int count = this.OnPingDocument().ActiveObjects().FindAll(X => X.Name == "Register GSuite Access").Count;

            if (count > 1) //An registry component already on canvas, do not allow duplicate...
            {
                Eto.Forms.MessageBox.Show("Connect Component already exists on canvas. Only one component allowed per canvas.", null, Eto.Forms.MessageBoxButtons.OK, Eto.Forms.MessageBoxType.Error);
                document.RemoveObject(this, false);
                return;
            }
        }

        public override void RemovedFromDocument(GH_Document document)
        {
            ClosePort();
            base.RemovedFromDocument(document);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string port = null;
            int baud = 0;
            bool open = false;
            string message = "";
            List<string> msgs = new List<string>();

            DA.GetData(0, ref port);
            DA.GetData(1, ref baud);
            DA.GetData(2, ref open);

            //before open and getting ready...........................................
            if (open == false && !thisPort.IsOpen) 
            {
                message = "Ready to connect\nConnect toggle switch to input O\nAvailable port: " + GetPortNameMessage();
            }

            //port already open.......................................................
            else if (open == true && thisPort.IsOpen) 
            {
                message = "Machine connected";
            }

            //open port now...........................................................
            else if (open == true && !thisPort.IsOpen) 
            {
                try //Port could be occupied. Let's try
                {
                    thisPort.PortName = port;
                    thisPort.BaudRate = baud;
                    thisPort.Parity = Parity.None;
                    thisPort.StopBits = StopBits.One;
                    thisPort.DataBits = 8;
                    thisPort.RtsEnable = true;
                    thisPort.DtrEnable = true;
                    thisPort.ReadTimeout = 1000;
                    thisPort.Handshake = System.IO.Ports.Handshake.XOnXOff;
                    thisPort.ReadBufferSize = 32768;

                    logQueue.Clear();
                    thisPort.Open();
                    thisPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
                    
                    //initialised printer.................
                    thisPort.WriteLine(Gcode.LineSyntax("M110", ref N)); //set line number
                    thisPort.WriteLine(Gcode.LineSyntax("M115", ref N)); //get firmware info
                    thisPort.WriteLine(Gcode.LineSyntax("M105", ref N)); //report temp
                    thisPort.WriteLine(Gcode.LineSyntax("M114", ref N)); //get current pos
                    thisPort.WriteLine(Gcode.LineSyntax("M111 S7", ref N)); //debug level
                    lastNsent = N-1;

                    if(firmwareInfo.Length == 0)
                        this.OnPingDocument().ScheduleSolution(500, CallBack); //wait for firmware info report back...

                    if (includeInitCode)
                        SerialComponentLibrary.Add2Queue.AddToQueue(initCode, ref msgs); //send init code...

                    message = "Machine connected.";
                    Connect.printerReady = true;
                }
                catch
                {
                    message = "Error while opening Serial Port";
                    this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, message);
                    DA.SetData(0, message);
                    return;
                }
            }

            //close port..............................................................
            else if (open == false && thisPort.IsOpen)  
            {
                ClosePort();
                message = "Printer disconnected. Ready to connect again.\nAvailable port: " + GetPortNameMessage();
                Seastar.Extension.expireOthers("WriteQueue", this);
                Debug.WriteLine("Port closed");
            }


            if (firmwareInfo.Length > 0)
                message += firmwareInfo;

            DA.SetData(0, message);
            DA.SetDataList(1, logQueue);
        }

        protected override System.Drawing.Bitmap Icon => Seastar.Properties.Resources.cntConnect;

        public override Guid ComponentGuid
        {
            get { return new Guid("2B62BEA7-CF4B-41ef-BC7F-72C47AD37431"); }
        }

        private void CallBack(GH_Document gh)
        {
            this.ExpireSolution(true);
        }

        private string GetPortNameMessage()
        {
            string[] pNames = SerialPort.GetPortNames();
            string portNames = "";
            for (int i = 0; i < pNames.Length; i++)
            {
                portNames += pNames[i];
                if (i < pNames.Length - 1)
                    portNames += ",";
            }
            return portNames;
        }

        private static void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;

            if (sp.IsOpen && sp.BytesToRead > 0)
            {
                string inData = sp.ReadLine();
                Debug.WriteLine($"Data Received: {inData}");

                inData = $"{DateTime.Now.ToString()}: {inData}";
                //logQueue.Add(inData); //add to overall log
                logQueue.Enqueue(inData);
                if(logQueue.Count > 1E6) //Keep log size
                    logQueue.Dequeue();

                #region find status from messga ereceived.............................
                //find firmware info...................................
                if (inData.IndexOf("FIRMWARE", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    firmwareInfo = inData;
                }

                //find ok. check length??? && nn[i].Length > 3....................................
                if (inData.IndexOf("ok", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    try
                    {
                        string n = inData.Remove(0, 3); //TODO use set difference to remove ok text
                        Debug.WriteLine("Connect::OK Number:" + n);
                        ok = Convert.ToInt32(n);
                    }
                    catch
                    {
                        ok++;
                    }
                }

                //find resend....................................
                if (inData.IndexOf("resend", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    string n = inData.Remove(0, 7);
                    Debug.WriteLine("Connect::Resend Number:" + n);
                    resend = Convert.ToInt32(n);
                }
                else
                {
                    //resend = null;
                }

                //find busy....................................
                if (!Connect.busy && inData.IndexOf("busy", StringComparison.OrdinalIgnoreCase) >= 0)
                    Connect.busy = true;
                if (Connect.busy && !(inData.IndexOf("busy", StringComparison.OrdinalIgnoreCase) >= 0))
                    Connect.busy = false;
                #endregion
            }
        }

        private static void ClosePort()
        {
            if (!thisPort.IsOpen)
                return;

            thisPort.DiscardInBuffer();
            thisPort.DiscardOutBuffer();
            thisPort.Close();

            logQueue.Clear();
            queue.Clear();
            N = 0;
            lastNsent = 0;
            ok = 0;
            resend = null;
            busy = false;
            printerReady = false;
            lastLine = "";
            firmwareInfo = "";

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
        public ReadWrite() : base("ReadWrite", "RW", "Manage data flow between Grasshopper and printer\n" +
            "Reads printer log and sends command from queue to printer\n" +
            "This component update and loop auotmatically", "Seastar", "05 | Connect")
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
            pManager.AddTextParameter("Message", "msg", "Message", GH_ParamAccess.item);
        }

        public override void AddedToDocument(GH_Document document)
        {
            base.AddedToDocument(document);
            int count = this.OnPingDocument().ActiveObjects().FindAll(X => X.Name == "Register GSuite Access").Count;

            if (count > 1) //An registry component already on canvas, do not allow duplicate...
            {
                Eto.Forms.MessageBox.Show("Connect Component already exists on canvas. Only one component allowed per canvas.", null, Eto.Forms.MessageBoxButtons.OK, Eto.Forms.MessageBoxType.Error);
                document.RemoveObject(this, false);
                return;
            }
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string msg = "";
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
            if (Connect.busy)
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Machine is Busy.");
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
                        Connect.logQueue.Enqueue(package);
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
                    DA.SetDataList(0, Connect.logQueue);  //show log
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

        private void UpdateSetData(GH_Document gh)
        {
            ExpireSolution(false);
        }

        protected override System.Drawing.Bitmap Icon => Seastar.Properties.Resources.cntLog;
        public override Guid ComponentGuid
        {
            get { return new Guid("52D22D10-A34C-4865-A9D4-EDF86EACA8DA"); }
        }

        
    }

    

    public class Add2Queue : GH_Component
    {
        //SerialComponentLibrary.SerialTest01 thisPort = new SerialComponentLibrary.SerialTest01();

        public Add2Queue()
            : base("WriteToQueue", "WQ", "This compoent ONLY write command to queue\n" +
                  "Use ReadWrite component to actually send command to printer", "Seastar", "05 | Connect")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("string", "T", "Text to Send", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Send", "S", "Set true to send command(s) to queue\n" +
                "Commands in queue will be sent to machine asap. See queue in Connect component.", GH_ParamAccess.item, false);
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

            if (!SerialComponentLibrary.Connect.thisPort.IsOpen)
            {
                DA.SetData(0, "No printer connected");
                return; //Do not send
            }
            else if(!Connect.printerReady)
            {
                DA.SetData(0, "Printer not ready");
                return;  //Do not send
            }
            else if (!send)
            {
                DA.SetData(0, "Set Send input to true to start sending.");
                return;  //Do not send
            }

            //string message = null; 
            List<string> textin = new List<string>();
            List<string> text = new List<string>();
            List<string> msg = new List<string>();

            if (!DA.GetDataList(0, textin))
                return;

            if (Connect.printerReady)
                AddToQueue(textin, ref msg);


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
            
            List<string> text = new List<string>();

            //split \n to individual lines.................................................
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

                    if (text[i].Contains(";")) //remove comment...
                    {
                        text[i] = text[i].Split(';')[0];
                    }
                    if (text[i].Length > 0)
                    {
                        //text[i] = "N" + Connect.N.ToString() + " " + text[i];
                        //text[i] = AddCheckSum(text[i]);

                        text[i] = Gcode.LineSyntax(text[i], ref Connect.N);

                        if(Connect.lastLine.Contains("G28") && text[i].Contains("G28")) { continue; } //no repeating g28...
                        Connect.queue.Enqueue(text[i]);
                        Connect.lastLine = text[i];

                        msg.Add(text[i] + " sent to queue");
                        //Connect.N++;
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
