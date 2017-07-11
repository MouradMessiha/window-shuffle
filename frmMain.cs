using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Runtime.InteropServices;

namespace WindowShuffle
{
    public partial class frmMain : Form
    {
        private Thread mobjShuffleThread;
        private bool mblnFormClosed = false;
        private IntPtr mobjdeskDC;

        [DllImport("user32.dll", EntryPoint = "GetDC")]
        public static extern IntPtr GetDC(IntPtr ptr);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        [DllImport("user32")]
        public static extern int ReleaseDC(IntPtr hwnd, IntPtr hDC);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool RedrawWindow(IntPtr hWnd, ref RECT lprcUpdate, IntPtr hrgnUpdate, RedrawWindowFlags flags);

        [DllImport("gdi32.dll", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
        public static extern int BitBlt(IntPtr hDC, int x, int y, int nWidth, int nHeight, IntPtr hSrcDC, int xSrc, int ySrc, int dwRop);  

        public enum RedrawWindowFlags
        {
            RDW_INVALIDATE = 0x0001,
            RDW_NOERASE = 0x0020,
            RDW_ERASE = 0x0004,
            RDW_UPDATENOW = 0x0100
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
            public RECT(int left, int top, int right, int bottom)
            {
                this.left = left;
                this.top = top;
                this.right = right;
                this.bottom = bottom;
            }
        }

        public frmMain()
        {
            InitializeComponent();
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void frmMain_FormClosed(object sender, FormClosedEventArgs e)
        {
            mblnFormClosed = true;
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            mobjdeskDC = IntPtr.Zero;
            IntPtr hProgMan = FindWindow("ProgMan", null);
            if (!hProgMan.Equals(IntPtr.Zero))
            {
                IntPtr hShellDefView = FindWindowEx(hProgMan, IntPtr.Zero, "SHELLDLL_DefView", null);
                if (!hShellDefView.Equals(IntPtr.Zero))
                    mobjdeskDC = FindWindowEx(hShellDefView, IntPtr.Zero, "SysListView32", null);
            }

            mobjShuffleThread = new Thread(StartShuffleThread);
            mobjShuffleThread.Start();
        }

        private void StartShuffleThread()
        {
            IntPtr objDesktopDC;
            Random objRandom = new Random(DateTime.Now.Second);
            RECT recScreen;
            Rectangle objRectangle1 = new Rectangle();
            Rectangle objRectangle2 = new Rectangle();
            int intRectangle1;
            int intRectangle2;
            int intRectangleWidth;
            int intRectangleHeight;
            Bitmap objTempBitmap;
            Graphics objTempBitmapGraphics;
            IntPtr objTempBitmapGraphicsDC;
            int intReturnValue;
            
            recScreen.left = 0;
            recScreen.top = 0;
            recScreen.right = Screen.AllScreens[0].Bounds.Width - 1;
            recScreen.bottom = Screen.AllScreens[0].Bounds.Height - 1;

            intRectangleWidth = Screen.AllScreens[0].Bounds.Width / 6;          // divide screen into 6 columns
            intRectangleHeight = Screen.AllScreens[0].Bounds.Height / 6;        // and 6 rows

            objRectangle1.Width = intRectangleWidth;
            objRectangle1.Height = intRectangleHeight;

            objRectangle2.Width = intRectangleWidth;
            objRectangle2.Height = intRectangleHeight;

            objTempBitmap = new Bitmap(intRectangleWidth, intRectangleHeight);   // create a temporary bitmap for switching the 2 rectangles
            objTempBitmapGraphics = Graphics.FromImage(objTempBitmap);

            while (!mblnFormClosed)
            {
                objDesktopDC = GetDC(mobjdeskDC);
                objTempBitmapGraphicsDC = objTempBitmapGraphics.GetHdc();

                intRectangle1 = objRandom.Next(36);    // divide the screen into 36 rectangles and pick 1 randomly 
                intRectangle2 = (intRectangle1 + 1 + objRandom.Next(35)) % 36;    // pick another rectangle different from first one

                // get the coordinates of the rectangles from their index numbers
                objRectangle1.Location = new Point((intRectangle1 % 6) * intRectangleWidth, (int)Math.Floor((double)intRectangle1 / 6) * intRectangleHeight);
                objRectangle2.Location = new Point((intRectangle2 % 6) * intRectangleWidth, (int)Math.Floor((double)intRectangle2 / 6) * intRectangleHeight);

                // save the contents of rectangle 1 on the desktop to the temporary bitmap
                intReturnValue = BitBlt(objTempBitmapGraphicsDC, 0, 0, intRectangleWidth, intRectangleHeight, objDesktopDC, objRectangle1.Location.X, objRectangle1.Location.Y, (int)CopyPixelOperation.SourceCopy);
                
                // move the contents of rectangle 2 on the desktop to rectangle 1
                intReturnValue = BitBlt(objDesktopDC, objRectangle1.Location.X, objRectangle1.Location.Y, intRectangleWidth, intRectangleHeight, objDesktopDC, objRectangle2.Location.X, objRectangle2.Location.Y, (int)CopyPixelOperation.SourceCopy);

                // move the contents of the temporary bitmap to rectangle 2
                intReturnValue = BitBlt(objDesktopDC, objRectangle2.Location.X, objRectangle2.Location.Y, intRectangleWidth, intRectangleHeight, objTempBitmapGraphicsDC, 0, 0, (int)CopyPixelOperation.SourceCopy);

                objTempBitmapGraphics.ReleaseHdc();
                ReleaseDC(mobjdeskDC, objDesktopDC);
                Thread.Sleep(500);
            }

            RedrawWindow(mobjdeskDC, ref recScreen, IntPtr.Zero, RedrawWindowFlags.RDW_INVALIDATE | RedrawWindowFlags.RDW_ERASE | RedrawWindowFlags.RDW_UPDATENOW);
        }
    }
}
