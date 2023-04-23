using Keystroke.API;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace GenshinAutoSkip
{
    sealed class Win32
    {
        [DllImport("user32.dll")]
        static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("user32.dll")]
        static extern Int32 ReleaseDC(IntPtr hwnd, IntPtr hdc);

        [DllImport("gdi32.dll")]
        static extern uint GetPixel(IntPtr hdc, int nXPos, int nYPos);

        static public RgbaColor GetPixelColor(int x, int y)
        {
            IntPtr hdc = GetDC(IntPtr.Zero);
            uint pixel = GetPixel(hdc, x, y);
            ReleaseDC(IntPtr.Zero, hdc);
            Color color = Color.FromArgb((int)(pixel & 0x000000FF),
                         (int)(pixel & 0x0000FF00) >> 8,
                         (int)(pixel & 0x00FF0000) >> 16);
            return RgbaColor.From(color);
        }
        static public RgbaColor GetPixelColor(Point p)
        {

            return GetPixelColor(p.X, p.Y);
        }
    }



    struct RgbaColor
    {
        public byte R;
        public byte G;
        public byte B;
        public byte A;
        // overload the equality operator
        public static bool operator ==(RgbaColor c1, RgbaColor c2)
        {
            return c1.R == c2.R && c1.G == c2.G && c1.B == c2.B;
        }
        public static bool operator !=(RgbaColor c1, RgbaColor c2)
        {
            return c1.R != c2.R || c1.G != c2.G || c1.B != c2.B;
        }
        public override bool Equals(object obj)
        {
            if (obj is RgbaColor)
            {
                return this == (RgbaColor)obj;
            }
            return false;
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        public static RgbaColor From(Color color)
        {
            return new RgbaColor() { R = color.R, G = color.G, B = color.B, A = color.A };
        }
        public override string ToString()
        {
            return string.Format("(R:{0} G:{1} B:{2} A:{3})", R, G, B, A);
        }
    }

    internal class Program
    {

        // private static readonly int MOUSEEVENTF_MOVE = 0x0001;
        private static readonly int MOUSEEVENTF_LEFTDOWN = 0x0002;
        private static readonly int MOUSEEVENTF_LEFTUP = 0x0004;
        // private static readonly int MOUSEEVENTF_RIGHTDOWN = 0x0008;
        // private static readonly int MOUSEEVENTF_RIGHTUP = 0x0010;
        // private static readonly int MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        // private static readonly int MOUSEEVENTF_MIDDLEUP = 0x0040;
        // private static readonly int MOUSEEVENTF_ABSOLUTE = 0x8000;
        static readonly Point MAX = new Point() { X = 1775, Y = 825 };
        static readonly Point MIN = new Point() { X = 1282, Y = 774 };
        static readonly Point PLAYING_ICON = new Point() { X = 111, Y = 46 };
        static readonly Point DIALOGUE_ICON = new Point() { X = 1301, Y = 808 };
        static readonly Point LOADING_SCREEN = new Point() { X = 1200, Y = 700 };
        static readonly Point AUTO_TEXT = new Point() { X = 142, Y = 50 };//168,194,194
        static float SCREEN_SCALE = 1.25f;

        static Point RandomCursorPosition()
        {
            Random rnd = new Random();
            int x = rnd.Next(MIN.X, MAX.X + 1);
            int y = rnd.Next(MIN.Y, MAX.Y + 1);
            int newX = (int)(x / SCREEN_SCALE);
            int newY = (int)(y / SCREEN_SCALE);
            Point position = new Point() { X = newX, Y = newY };
            return position;
        }
        static void WorkerThread()
        {
            while (true)
            {
                if (Win32.GetPixelColor(AUTO_TEXT) == new RgbaColor() { R = 168, G = 194, B = 194 } || Win32.GetPixelColor(PLAYING_ICON) == new RgbaColor() { R = 236, G = 229, B = 216 } ||
                    Win32.GetPixelColor(DIALOGUE_ICON) == new RgbaColor() { R = 255, G = 255, B = 255 } &&
                    Win32.GetPixelColor(LOADING_SCREEN) != new RgbaColor() { R = 255, G = 255, B = 255 })
                {
                    ClickSomePoint(RandomCursorPosition());
                    Thread.Sleep(500);
                }
            }
        }
        static Thread workerThread;
        public static void ClickSomePoint(Point point)
        {
            Point screenPos = Cursor.Position;
            Cursor.Position = point;
            ClickPoint(point);
            Console.WriteLine("Cursor moved to: " + point);
            Console.WriteLine(screenPos);
        }
        [DllImport("user32.dll")]
        static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);
        static void ClickPoint(Point p)
        {
            mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, p.X, p.Y, 0, 0);
        }
        public static bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        public static void StartAsAdmin(string fileName)
        {
            var proc = new Process
            {
                StartInfo =
        {
            FileName = fileName,
            UseShellExecute = true,
            Verb = "runas"
        }
            };

            proc.Start();
        }
        static void Main(string[] args)
        {

            if (!IsAdministrator())
            {
                Console.WriteLine("restarting as admin");
                StartAsAdmin(Assembly.GetExecutingAssembly().Location);
                return;
            }
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine("-----------------------------------");
            Console.WriteLine("Nhấn phím mũi tên lên để bắt đầu");
            Console.WriteLine("Nhấn phím mũi tên xuống để tạm dừng");
            Console.WriteLine("Nhấn phím mũi tên trái để thoát");
            Console.WriteLine("-----------------------------------");

            var api = new KeystrokeAPI();
                api.CreateKeyboardHook((character) =>
                {
                    if (character.KeyCode == KeyCode.Up)
                    {
                        Console.WriteLine("Starting...");
                        aif(workerThread != null)
                        {
                            workerThread.Abort();
                            workerThread = null;
                        }
                        workerThread = new Thread(WorkerThread);
                        workerThread.Start();
                    }
                    if (character.KeyCode == KeyCode.Down)
                    {
                        Console.WriteLine("Pause ...");

                        if (workerThread != null)
                        {

                            workerThread.Abort();
                            workerThread = null;

                        }

                    }
                    if (character.KeyCode == KeyCode.Left)
                    {
                        Console.WriteLine("Exit ...");

                        if (workerThread != null)
                        {
                            workerThread.Abort();
                            workerThread = null;
                        }
                        Application.Exit();
                        Environment.Exit(0);
                        return;
                    }

                });

                Application.Run();
        }
    }
}
