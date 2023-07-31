﻿using ShadowDemo;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Seastar
{
    public partial class JoggerForm : Form
    {

        private bool Drag;
        private int MouseX;
        private int MouseY;

        private const int WM_NCHITTEST = 0x84;
        private const int HTCLIENT = 0x1;
        private const int HTCAPTION = 0x2;

        private bool m_aeroEnabled;

        private const int CS_DROPSHADOW = 0x00020000;
        private const int WM_NCPAINT = 0x0085;
        private const int WM_ACTIVATEAPP = 0x001C;

        [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
        public static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMarInset);
        [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
        [System.Runtime.InteropServices.DllImport("dwmapi.dll")]

        public static extern int DwmIsCompositionEnabled(ref int pfEnabled);
        [System.Runtime.InteropServices.DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(
            int nLeftRect,
            int nTopRect,
            int nRightRect,
            int nBottomRect,
            int nWidthEllipse,
            int nHeightEllipse
            );

        public struct MARGINS
        {
            public int leftWidth;
            public int rightWidth;
            public int topHeight;
            public int bottomHeight;
        }
        //protected override CreateParams CreateParams
        //{
        //    get
        //    {
        //        m_aeroEnabled = CheckAeroEnabled();
        //        CreateParams cp = base.CreateParams;
                
        //        if (!m_aeroEnabled)
        //            cp.ClassStyle |= CS_DROPSHADOW; return cp;

        //    }
        //}
        private bool CheckAeroEnabled()
        {
            if (Environment.OSVersion.Version.Major >= 6)
            {
                int enabled = 0; DwmIsCompositionEnabled(ref enabled);
                return (enabled == 1) ? true : false;
            }
            return false;
        }
        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_NCPAINT:
                    if (m_aeroEnabled)
                    {
                        var v = 2;
                        DwmSetWindowAttribute(this.Handle, 2, ref v, 4);
                        MARGINS margins = new MARGINS()
                        {
                            bottomHeight = 1,
                            leftWidth = 0,
                            rightWidth = 0,
                            topHeight = 0
                        }; DwmExtendFrameIntoClientArea(this.Handle, ref margins);
                    }
                    break;
                default: break;
            }
            base.WndProc(ref m);
            if (m.Msg == WM_NCHITTEST && (int)m.Result == HTCLIENT) m.Result = (IntPtr)HTCAPTION;
        }
        private void PanelMove_MouseDown(object sender, MouseEventArgs e)
        {
            Drag = true;
            MouseX = Cursor.Position.X - this.Left;
            MouseY = Cursor.Position.Y - this.Top;
        }
        private void PanelMove_MouseMove(object sender, MouseEventArgs e)
        {
            if (Drag)
            {
                this.Top = Cursor.Position.Y - MouseY;
                this.Left = Cursor.Position.X - MouseX;
            }
        }
        private void PanelMove_MouseUp(object sender, MouseEventArgs e) { Drag = false; }

        private void DemoForm_Load(object sender, EventArgs e)
        {
            Width = 300;
            Height = 300;
            if (!DesignMode)
            {
                shadow = new Dropshadow(this)
                {
                    ShadowBlur = 40,
                    ShadowSpread = 10,
                    ShadowColor = Color.DimGray

                };
                shadow.RefreshShadow();
                
            }
        }
        private Dropshadow shadow;

        public JoggerForm()
        {
            InitializeComponent();
            m_aeroEnabled = false;
           // Region = System.Drawing.Region.FromHrgn(CreateRoundRectRgn(0, 0, Width, Height, 50, 50));
            
            //Region = new Region(RoundedRectangles.RoundedRectangle.Create(new Rectangle(0, 0, Size.Width, Size.Height), 18, RoundedRectangles.RoundedRectangle.RectangleCorners.All));
            //TopLeftPath = RoundedRectangles.RoundedRectangle.Create(new Rectangle(0, 0, Size.Width, Size.Height), 8, RoundedRectangles.RoundedRectangle.RectangleCorners.TopRight | RoundedRectangles.RoundedRectangle.RectangleCorners.TopLeft, RoundedRectangles.RoundedRectangle.WhichHalf.TopLeft);
            //BottomRightPath = RoundedRectangles.RoundedRectangle.Create(new Rectangle(0, 0, Size.Width - 1, Size.Height - 1), 8, RoundedRectangles.RoundedRectangle.RectangleCorners.TopRight | RoundedRectangles.RoundedRectangle.RectangleCorners.TopLeft, RoundedRectangles.RoundedRectangle.WhichHalf.BottomRight);
        }

        private void Button1_Click(object sender, EventArgs e)
        {

        }

        private void SimForm_Load(object sender, EventArgs e)
        {

        }

        private void Panel1_Paint(object sender, PaintEventArgs e)
        {

        }
    }
}
