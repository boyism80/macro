using System;
using System.Runtime.InteropServices;
using System.Windows;


namespace KPUGeneralMacro
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Windows
        public enum HookType : int
        {
            /// <summary>
            /// Installs a hook procedure that monitors messages generated as a result of an input event in a dialog box,
            /// message box, menu, or scroll bar. For more information, see the MessageProc hook procedure.
            /// </summary>
            WH_MSGFILTER = -1,
            /// <summary>
            /// Installs a hook procedure that records input messages posted to the system message queue. This hook is
            /// useful for recording macros. For more information, see the JournalRecordProc hook procedure.
            /// </summary>
            WH_JOURNALRECORD = 0,
            /// <summary>
            /// Installs a hook procedure that posts messages previously recorded by a WH_JOURNALRECORD hook procedure.
            /// For more information, see the JournalPlaybackProc hook procedure.
            /// </summary>
            WH_JOURNALPLAYBACK = 1,
            /// <summary>
            /// Installs a hook procedure that monitors keystroke messages. For more information, see the KeyboardProc
            /// hook procedure.
            /// </summary>
            WH_KEYBOARD = 2,
            /// <summary>
            /// Installs a hook procedure that monitors messages posted to a message queue. For more information, see the
            /// GetMsgProc hook procedure.
            /// </summary>
            WH_GETMESSAGE = 3,
            /// <summary>
            /// Installs a hook procedure that monitors messages before the system sends them to the destination window
            /// procedure. For more information, see the CallWndProc hook procedure.
            /// </summary>
            WH_CALLWNDPROC = 4,
            /// <summary>
            /// Installs a hook procedure that receives notifications useful to a CBT application. For more information,
            /// see the CBTProc hook procedure.
            /// </summary>
            WH_CBT = 5,
            /// <summary>
            /// Installs a hook procedure that monitors messages generated as a result of an input event in a dialog box,
            /// message box, menu, or scroll bar. The hook procedure monitors these messages for all applications in the
            /// same desktop as the calling thread. For more information, see the SysMsgProc hook procedure.
            /// </summary>
            WH_SYSMSGFILTER = 6,
            /// <summary>
            /// Installs a hook procedure that monitors mouse messages. For more information, see the MouseProc hook
            /// procedure.
            /// </summary>
            WH_MOUSE = 7,
            /// <summary>
            ///
            /// </summary>
            WH_HARDWARE = 8,
            /// <summary>
            /// Installs a hook procedure useful for debugging other hook procedures. For more information, see the
            /// DebugProc hook procedure.
            /// </summary>
            WH_DEBUG = 9,
            /// <summary>
            /// Installs a hook procedure that receives notifications useful to shell applications. For more information,
            /// see the ShellProc hook procedure.
            /// </summary>
            WH_SHELL = 10,
            /// <summary>
            /// Installs a hook procedure that will be called when the application's foreground thread is about to become
            /// idle. This hook is useful for performing low priority tasks during idle time. For more information, see the
            /// ForegroundIdleProc hook procedure.
            /// </summary>
            WH_FOREGROUNDIDLE = 11,
            /// <summary>
            /// Installs a hook procedure that monitors messages after they have been processed by the destination window
            /// procedure. For more information, see the CallWndRetProc hook procedure.
            /// </summary>
            WH_CALLWNDPROCRET = 12,
            /// <summary>
            /// Installs a hook procedure that monitors low-level keyboard input events. For more information, see the
            /// LowLevelKeyboardProc hook procedure.
            /// </summary>
            WH_KEYBOARD_LL = 13,
            /// <summary>
            /// Installs a hook procedure that monitors low-level mouse input events. For more information, see the
            /// LowLevelMouseProc hook procedure.
            /// </summary>
            WH_MOUSE_LL = 14
        }

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr SetWindowsHookEx(HookType hookType, HookProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Ansi)]
        static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPStr)] string lpFileName);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam,
           IntPtr lParam);

        // overload for use with LowLevelKeyboardProc
        [DllImport("user32.dll")]
        static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, int wParam, int lParam);


        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }


        [StructLayout(LayoutKind.Sequential)]
        public struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public int mouseData; // be careful, this must be ints, not uints (was wrong before I changed it...). regards, cmew.
            public int flags;
            public int time;
            public UIntPtr dwExtraInfo;
        }


        delegate IntPtr HookProc(int code, IntPtr wParam, IntPtr lParam);
        #endregion

        private IntPtr _user32dll;
        private IntPtr _hook;
        private HookProc _hookProc;

        public MainWindowViewModel MainWindowViewModel { get; private set; }

        private static int LOWORD(int value)
        {
            return BitConverter.ToInt16(BitConverter.GetBytes(value), 0);
        }

        private static int HIWORD(int value)
        {
            return BitConverter.ToInt16(BitConverter.GetBytes(value), 2);
        }

        public MainWindow()
        {
            InitializeComponent();

            this.MainWindowViewModel = new MainWindowViewModel(this);
            try
            {
                this.MainWindowViewModel.Load();
            }
            catch
            { }

            this.MainWindowViewModel.Started += MainWindowViewModel_Started;
            this.MainWindowViewModel.Stopped += MainWindowViewModel_Stopped;

            this.DataContext = this.MainWindowViewModel;
        }

        private void MainWindowViewModel_Stopped(object sender, EventArgs e)
        {
            
        }

        private void MainWindowViewModel_Started(object sender, EventArgs e)
        {
            if (_hook != IntPtr.Zero)
                UnhookWindowsHookEx(_hook);

            _hook = SetWindowsHookEx(HookType.WH_MOUSE_LL, _hookProc, _user32dll, 0);
        }

        private IntPtr OnWindowsHook(int code, IntPtr wParam, IntPtr lParam)
        {
            var input = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
            switch ((uint)wParam)
            {
                case 0x20B:
                    var xbutton = HIWORD(input.mouseData);
                    if (xbutton == 1)
                        this.MainWindowViewModel.OnXButton1Down();
                    else if (xbutton == 2)
                        this.MainWindowViewModel.OnXButton2Down();

                    break;

                case 520:
                    this.MainWindowViewModel.OnWheelClick();
                    break;

                case 512:
                    break;
            }

            return CallNextHookEx(_hook, code, wParam, lParam);
        }

        private void Window_Closed(object sender, System.EventArgs e)
        {
            this.MainWindowViewModel.Dispose();
            this.MainWindowViewModel.Stop();
            this.MainWindowViewModel.Save();

            if (_hook != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hook);
                _hook = IntPtr.Zero;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _hookProc = new HookProc(this.OnWindowsHook);
            _user32dll = LoadLibrary("User32");
            _hook = SetWindowsHookEx(HookType.WH_MOUSE_LL, _hookProc, _user32dll, 0);
        }

        private void MainScreen_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //this.MainWindowViewModel.BitmapSize = new OpenCvSharp.Size { Width = (int)MainScreen.ActualWidth, Height = (int)MainScreen.ActualHeight };
        }
    }
}
