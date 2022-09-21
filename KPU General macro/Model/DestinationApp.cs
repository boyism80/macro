using IronPython.Runtime;
using KPUGeneralMacro.Extension;
using OpenCvSharp;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Linq;

namespace KPUGeneralMacro.ViewModel
{
    public enum OperationType
    {
        Software, Hardware
    }

    public sealed class DestinationApp : BaseViewModel
    {
        public static DestinationApp Instance { get; private set; } = new DestinationApp();

        #region Windows enumerator
        public enum TernaryRasterOperations : uint
        {
            /// <summary>dest = source</summary>
            SRCCOPY = 0x00CC0020,
            /// <summary>dest = source OR dest</summary>
            SRCPAINT = 0x00EE0086,
            /// <summary>dest = source AND dest</summary>
            SRCAND = 0x008800C6,
            /// <summary>dest = source XOR dest</summary>
            SRCINVERT = 0x00660046,
            /// <summary>dest = source AND (NOT dest)</summary>
            SRCERASE = 0x00440328,
            /// <summary>dest = (NOT source)</summary>
            NOTSRCCOPY = 0x00330008,
            /// <summary>dest = (NOT src) AND (NOT dest)</summary>
            NOTSRCERASE = 0x001100A6,
            /// <summary>dest = (source AND pattern)</summary>
            MERGECOPY = 0x00C000CA,
            /// <summary>dest = (NOT source) OR dest</summary>
            MERGEPAINT = 0x00BB0226,
            /// <summary>dest = pattern</summary>
            PATCOPY = 0x00F00021,
            /// <summary>dest = DPSnoo</summary>
            PATPAINT = 0x00FB0A09,
            /// <summary>dest = pattern XOR dest</summary>
            PATINVERT = 0x005A0049,
            /// <summary>dest = (NOT dest)</summary>
            DSTINVERT = 0x00550009,
            /// <summary>dest = BLACK</summary>
            BLACKNESS = 0x00000042,
            /// <summary>dest = WHITE</summary>
            WHITENESS = 0x00FF0062,
            /// <summary>
            /// Capture window as seen on screen.  This includes layered windows 
            /// such as WPF windows with AllowsTransparency="true"
            /// </summary>
            CAPTUREBLT = 0x40000000
        }

        public enum BitmapCompressionMode : uint
        {
            BI_RGB = 0,
            BI_RLE8 = 1,
            BI_RLE4 = 2,
            BI_BITFIELDS = 3,
            BI_JPEG = 4,
            BI_PNG = 5
        }

        public enum DIB_Color_Mode : uint
        {
            DIB_RGB_COLORS = 0,
            DIB_PAL_COLORS = 1
        }



        #endregion

        #region Windows structures
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left, Top, Right, Bottom;

            public RECT(int left, int top, int right, int bottom)
            {
                Left = left;
                Top = top;
                Right = right;
                Bottom = bottom;
            }

            public RECT(System.Drawing.Rectangle r) : this(r.Left, r.Top, r.Right, r.Bottom) { }

            public int X
            {
                get { return this.Left; }
                set { this.Right -= (this.Left - value); this.Left = value; }
            }

            public int Y
            {
                get { return this.Top; }
                set { this.Bottom -= (this.Top - value); this.Top = value; }
            }

            public int Height
            {
                get { return this.Bottom - this.Top; }
                set { this.Bottom = value + this.Top; }
            }

            public int Width
            {
                get { return this.Right - this.Left; }
                set { this.Right = value + this.Left; }
            }

            public System.Drawing.Point Location
            {
                get { return new System.Drawing.Point(this.Left, this.Top); }
                set { this.X = value.X; this.Y = value.Y; }
            }

            public System.Drawing.Size Size
            {
                get { return new System.Drawing.Size(Width, Height); }
                set { this.Width = value.Width; this.Height = value.Height; }
            }

            public static implicit operator System.Drawing.Rectangle(RECT r)
            {
                return new System.Drawing.Rectangle(r.Left, r.Top, r.Width, r.Height);
            }

            public static implicit operator RECT(System.Drawing.Rectangle r)
            {
                return new RECT(r);
            }

            public static bool operator ==(RECT r1, RECT r2)
            {
                return r1.Equals(r2);
            }

            public static bool operator !=(RECT r1, RECT r2)
            {
                return !r1.Equals(r2);
            }

            public bool Equals(RECT r)
            {
                return r.Left == Left && r.Top == Top && r.Right == Right && r.Bottom == Bottom;
            }

            public override bool Equals(object obj)
            {
                if (obj is RECT)
                    return Equals((RECT)obj);
                else if (obj is System.Drawing.Rectangle)
                    return Equals(new RECT((System.Drawing.Rectangle)obj));
                return false;
            }

            public override int GetHashCode()
            {
                return ((System.Drawing.Rectangle)this).GetHashCode();
            }

            public override string ToString()
            {
                return string.Format(System.Globalization.CultureInfo.CurrentCulture, "{{Left={0},Top={1},Right={2},Bottom={3}}}", Left, Top, Right, Bottom);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct BITMAPINFO
        {
            public Int32 biSize;
            public Int32 biWidth;
            public Int32 biHeight;
            public Int16 biPlanes;
            public Int16 biBitCount;
            public Int32 biCompression;
            public Int32 biSizeImage;
            public Int32 biXPelsPerMeter;
            public Int32 biYPelsPerMeter;
            public Int32 biClrUsed;
            public Int32 biClrImportant;
        }
        #endregion

        #region Windows APIs
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string className, string windowTitle);

        [DllImport("gdi32.dll")]
        static extern IntPtr CreateBitmap(int nWidth, int nHeight, uint cPlanes, uint cBitsPerPel, IntPtr lpvBits);

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern bool PostMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern bool ClientToScreen(IntPtr hWnd, ref System.Drawing.Point lpPoint);

        [DllImport("user32.dll")]
        internal static extern uint SendInput(uint nInputs, [MarshalAs(UnmanagedType.LPArray), In] INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll", EntryPoint = "CreateCompatibleDC", SetLastError = true)]
        static extern IntPtr CreateCompatibleDC([In] IntPtr hdc);

        [DllImport("gdi32.dll", EntryPoint = "CreateCompatibleBitmap")]
        static extern IntPtr CreateCompatibleBitmap([In] IntPtr hdc, int nWidth, int nHeight);

        [DllImport("gdi32.dll", EntryPoint = "SelectObject")]
        public static extern IntPtr SelectObject([In] IntPtr hdc, [In] IntPtr hgdiobj);

        [DllImport("gdi32.dll", EntryPoint = "BitBlt", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool BitBlt([In] IntPtr hdc, int nXDest, int nYDest, int nWidth, int nHeight, [In] IntPtr hdcSrc, int nXSrc, int nYSrc, TernaryRasterOperations dwRop);

        [DllImport("gdi32.dll", EntryPoint = "DeleteDC")]
        public static extern bool DeleteDC([In] IntPtr hdc);

        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteObject([In] IntPtr hObject);

        [DllImport("user32.dll")]
        static extern bool GetWindowRect(IntPtr hwnd, out RECT rectRef);

        [DllImport("user32.dll")]
        static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);


        [DllImport("user32.dll")]
        public static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, int flags);

        [DllImport("gdi32.dll")]
        static extern uint GetPixel(IntPtr hdc, int x, int y);

        [DllImport("user32.dll", SetLastError = true)]
        static extern Boolean AdjustWindowRect(ref RECT rect, UInt32 style, bool menu);

        [DllImport("user32.dll")]
        static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("gdi32.dll")]
        static extern IntPtr CreateDIBSection(IntPtr hdc, [In] ref BITMAPINFO pbmi, uint pila, out IntPtr ppvBits, IntPtr hSection, uint dwOffset);

        [DllImport("gdi32.dll", EntryPoint = "GetDIBits")]
        static extern int GetDIBits([In] IntPtr hdc, [In] IntPtr hbmp, uint uStartScan, uint cScanLines, [Out] byte[] lpvBits, ref BITMAPINFO lpbi, DIB_Color_Mode uUsage);


#pragma warning disable 649
        internal struct INPUT
        {
            public UInt32 Type;
            public MOUSEKEYBDHARDWAREINPUT Data;
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct MOUSEKEYBDHARDWAREINPUT
        {
            [FieldOffset(0)]
            public HARDWAREINPUT Hardware;
            [FieldOffset(0)]
            public KEYBDINPUT Keyboard;
            [FieldOffset(0)]
            public MOUSEINPUT Mouse;

        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct HARDWAREINPUT
        {
            public uint Msg;
            public ushort ParamL;
            public ushort ParamH;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct KEYBDINPUT
        {
            public ushort Vk;
            public ushort Scan;
            public uint Flags;
            public uint Time;
            public IntPtr ExtraInfo;
        }


        internal struct MOUSEINPUT
        {
            public Int32 X;
            public Int32 Y;
            public UInt32 MouseData;
            public UInt32 Flags;
            public UInt32 Time;
            public IntPtr ExtraInfo;
        }

#pragma warning restore 649
        #endregion

        #region Delegates
        public delegate void FrameEventHandler(Mat frame);
        #endregion

        private Thread _streamingThread;
        private System.Drawing.Point _prevCursorPosition;
        private string _className;

        public IntPtr Hwnd { get; private set; }

        public OperationType OperationType { get; set; } = OperationType.Software;

        public Rectangle Area { get; private set; }

        public PythonTuple PyArea { get; private set; }

        private IntPtr _foregroundActiveHandle;
        public IntPtr ForegroundActiveHandle
        {
            get
            {
                return this._foregroundActiveHandle;
            }
            private set
            {
                if (value == this.Hwnd)
                    return;

                this._foregroundActiveHandle = value;
            }
        }

        public bool IsRunning { get; private set; }

        public string ClassName
        {
            get
            {
                return this.Hwnd == IntPtr.Zero ? "Unknown" : this._className;
            }
            set
            {
                this._className = value;
            }
        }

        public event FrameEventHandler Frame;

        private DestinationApp()
        {

        }

        private System.Drawing.Rectangle GetArea()
        {
            var appPlayerRect = new RECT();
            GetClientRect(this.Hwnd, out appPlayerRect);

            var appWindowRect = new RECT();
            GetWindowRect(this.Hwnd, out appWindowRect);

            var difference = new System.Drawing.Size(appWindowRect.Width - appPlayerRect.Width, appWindowRect.Height - appPlayerRect.Height);
            var adjustedLocation = new System.Drawing.Point(appWindowRect.X + difference.Width / 2, appWindowRect.Y + difference.Height);

            return new System.Drawing.Rectangle(adjustedLocation, appPlayerRect.Size);
        }

        private Mat Capture()
        {
            try
            {
                var appClientRect = this.GetArea();
                var appClientDC = GetDC(this.Hwnd);
                var compatibleDC = CreateCompatibleDC(appClientDC);
                var compatibleBitmap = CreateCompatibleBitmap(appClientDC, appClientRect.Width, appClientRect.Height);
                var oldBitmap = SelectObject(compatibleDC, compatibleBitmap);
                BitBlt(compatibleDC, 0, 0, appClientRect.Width, appClientRect.Height, appClientDC, 0, 0, TernaryRasterOperations.SRCCOPY | TernaryRasterOperations.CAPTUREBLT);

                var bitmap = Bitmap.FromHbitmap(compatibleBitmap);
                var frame = OpenCvSharp.Extensions.BitmapConverter.ToMat(bitmap);
                bitmap.Dispose();

                SelectObject(compatibleDC, oldBitmap);
                DeleteObject(compatibleBitmap);
                DeleteDC(compatibleDC);
                ReleaseDC(this.Hwnd, appClientDC);
                bitmap.Dispose();

                return frame;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public bool Start()
        {
            if (this.IsRunning)
                return false;

            if (this._streamingThread != null)
                return false;

            this.IsRunning = true;
            this._streamingThread = new Thread(new ThreadStart(this.StreamingThreadRoutine));
            this._streamingThread.Start();
            return true;
        }

        public void Stop()
        {
            this.IsRunning = false;
            this._streamingThread = null;
        }

        private void StreamingThreadRoutine()
        {
            while (this.IsRunning)
            {
                try
                {
                    var frame = this.Capture();
                    this.Area = this.GetArea();
                    this.PyArea = Area.ToTuple();
                    this.Frame?.Invoke(frame);

                    //frame.Dispose();
                }
                catch (Exception)
                { }
            }
        }

        private IntPtr FindAppHandle(string className)
        {
            var found = Process.GetProcesses().FirstOrDefault(x => x.ProcessName == className);
            return found?.MainWindowHandle ?? IntPtr.Zero;
        }

        public void BindToApp(string className)
        {
            this.Hwnd = this.FindAppHandle(className);
            if (this.Hwnd == IntPtr.Zero)
            {
                this.Hwnd = IntPtr.Zero;
                throw new Exception($"Could not find the app '{className}'");
            }

            this.ClassName = className;
        }

        public void Unbind()
        {
            this.Hwnd = IntPtr.Zero;
            this.OnPropertyChanged(nameof(this.ClassName));
        }

        public void SetActive(bool active, int sleepTime = 0)
        {
            if (this.OperationType == OperationType.Hardware && sleepTime != 0)
                Thread.Sleep(sleepTime);

            if (active)
            {
                this.ForegroundActiveHandle = GetForegroundWindow();
                SetForegroundWindow(this.Hwnd);
            }
            else
            {
                SetForegroundWindow(this.ForegroundActiveHandle);
            }
        }

        private void MouseAction(MouseButtons button, bool down, System.Drawing.Point location)
        {
            if (this.OperationType == OperationType.Hardware)
            {
                var area = this.Area;
                var beforeLocation = Cursor.Position;
                var flag = (uint)0x0002; // base : left down

                if (!down)
                    flag = flag << 1;

                if (button != MouseButtons.Left)
                    flag = flag << 2;

                // set cursor on coords, and press mouse
                this.SetActive(true, 0);
                Cursor.Position = new System.Drawing.Point(area.X + location.X, area.Y + location.Y);

                var action = new INPUT();
                action.Type = 0; /// input type mouse
                action.Data.Mouse.Flags = flag;

                var inputs = new INPUT[] { action };
                SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));

                this.SetActive(false, 50);
                Cursor.Position = beforeLocation;
            }
            else
            {
                if (button == MouseButtons.Left)
                    PostMessage(this.Hwnd, down ? 0x201 : 0x202, new IntPtr(down ? 0x0001 : 0x0000), new IntPtr(location.Y * 0x10000 + location.X));
                else
                    PostMessage(this.Hwnd, down ? 0x204 : 0x205, new IntPtr(down ? 0x0002 : 0x0000), new IntPtr(location.Y * 0x10000 + location.X));
            }
        }

        public void MouseDown(MouseButtons button, System.Drawing.Point location)
        {
            this.MouseAction(button, true, location);
        }

        public void MouseDown(string button, int x, int y)
        {
            this.MouseDown(button.Equals("left") ? MouseButtons.Left : MouseButtons.Right, new System.Drawing.Point(x, y));
        }

        public void MouseUp(MouseButtons button, System.Drawing.Point location)
        {
            this.MouseAction(button, false, location);
        }

        public void MouseUp(string button, int x, int y)
        {
            this.MouseUp(button.Equals("left") ? MouseButtons.Left : MouseButtons.Right, new System.Drawing.Point(x, y));
        }

        public void Click(MouseButtons button, System.Drawing.Point location, bool doubleClick = false)
        {
            if (this.OperationType == OperationType.Hardware)
            {
                this.StoreCursorPosition(location);
                var flag = (uint)0x0002;

                if (button != MouseButtons.Left)
                    flag = flag << 2;

                this.SetActive(true, 0);
                Thread.Sleep(30);

                var down = new INPUT();
                down.Type = 0;
                down.Data.Mouse.Flags = flag;

                var up = new INPUT();
                up.Type = 0;
                up.Data.Mouse.Flags = flag << 1;

                var inputs = doubleClick ? new INPUT[] { down, up, down, up } : new INPUT[] { down, up };
                SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));

                this.SetActive(false, 50);
                this.RestoreCursorPosition();
            }
            else
            {
                this.MouseDown(button, location);
                this.MouseUp(button, location);

                if (doubleClick)
                {
                    this.MouseDown(button, location);
                    this.MouseUp(button, location);
                }
            }
        }

        public void StoreCursorPosition(System.Drawing.Point location)
        {
            var area = this.Area;
            this._prevCursorPosition = Cursor.Position;

            Cursor.Position = new System.Drawing.Point(area.X + location.X, area.Y + location.Y);
        }

        public void RestoreCursorPosition()
        {
            Cursor.Position = this._prevCursorPosition;
        }

        public void StoreCursorPosition(int x, int y)
        {
            this.StoreCursorPosition(new System.Drawing.Point(x, y));
        }

        public void KeyPress(Keys key, int sleepTime = 100)
        {
            if (this.OperationType == OperationType.Hardware)
            {
                this.SetActive(true, sleepTime);

                var down = new INPUT();
                down.Type = 1;
                down.Data.Keyboard.Vk = (ushort)key;
                down.Data.Keyboard.Scan = 0;
                down.Data.Keyboard.Flags = 0x0000;
                down.Data.Keyboard.Time = 0;
                down.Data.Keyboard.ExtraInfo = IntPtr.Zero;

                var up = new INPUT();
                up.Type = 1;
                up.Data.Keyboard.Vk = (ushort)key;
                up.Data.Keyboard.Scan = 0;
                up.Data.Keyboard.Flags = 0x0002;
                up.Data.Keyboard.Time = 0;
                up.Data.Keyboard.ExtraInfo = IntPtr.Zero;

                var inputs = new INPUT[] { down, up };
                SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));

                this.SetActive(false, sleepTime);
            }
            else
            {
                this.KeyDown(key);
                this.KeyUp(key);
            }
        }

        public void KeyPress(string key, int sleepTime = 100)
        {
            foreach (var c in key)
                this.KeyPress((Keys)c, sleepTime);
        }

        public void KeyDown(Keys key, int sleepTime = 100)
        {
            if (this.OperationType == OperationType.Hardware)
            {
                this.SetActive(true, sleepTime);

                var down = new INPUT();
                down.Type = 1;
                down.Data.Keyboard.Vk = (ushort)key;
                down.Data.Keyboard.Scan = 0;
                down.Data.Keyboard.Flags = 0;
                down.Data.Keyboard.Time = 0;
                down.Data.Keyboard.ExtraInfo = IntPtr.Zero;

                var inputs = new INPUT[] { down };
                SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));

                this.SetActive(false, sleepTime);
            }
            else
            {
                SendMessage(this.Hwnd, 0x0100, new IntPtr((int)key), IntPtr.Zero);
            }
        }

        public void KeyUp(Keys key, int sleepTime = 100)
        {
            if (this.OperationType == OperationType.Hardware)
            {
                this.SetActive(true, sleepTime);

                var up = new INPUT();
                up.Type = 1;
                up.Data.Keyboard.Vk = (ushort)key;
                up.Data.Keyboard.Scan = 0;
                up.Data.Keyboard.Flags = 2;
                up.Data.Keyboard.Time = 0;
                up.Data.Keyboard.ExtraInfo = IntPtr.Zero;

                var inputs = new INPUT[] { up };
                SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));

                /// return mouse 
                this.SetActive(false, sleepTime);
            }
            else
            {
                SendMessage(this.Hwnd, 0x0101, new IntPtr((int)key), IntPtr.Zero);
            }
        }

        public void Click(MouseButtons button, int x, int y, bool doubleClick = false)
        {
            this.Click(button, new System.Drawing.Point(x, y), doubleClick);
        }

        public void Click(string button, int x, int y, bool doubleClick = false)
        {
            this.Click(button.Equals("left") ? MouseButtons.Left : MouseButtons.Right, x, y, doubleClick);
        }

        public void Click(string button, System.Drawing.Point point, bool doubleClick = false)
        {
            this.Click(button.Equals("left") ? MouseButtons.Left : MouseButtons.Right, point.X, point.Y, doubleClick);
        }

        public void Click(string button, OpenCvSharp.Point point, bool doubleClick = false)
        {
            this.Click(button.Equals("left") ? MouseButtons.Left : MouseButtons.Right, point.X, point.Y, doubleClick);
        }

        public void Click(int x, int y, bool doubleClick = false)
        {
            this.Click(MouseButtons.Left, x, y, doubleClick);
        }

        public void Click(System.Drawing.Point point, bool doubleClick = false)
        {
            this.Click(point.X, point.Y, doubleClick);
        }

        public void Click(OpenCvSharp.Point point, bool doubleClick = false)
        {
            this.Click(point.X, point.Y, doubleClick);
        }

        public void Click(PythonTuple point, bool doubleClick = false)
        {
            this.Click((int)point[0], (int)point[1], doubleClick);
        }

        public void Escape()
        {
            this.KeyPress(Keys.Escape);
        }

        public void SendMessage(int message, IntPtr w, IntPtr l)
        {
            DestinationApp.SendMessage(this.Hwnd, message, w, l);
        }
    }
}
