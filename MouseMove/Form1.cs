using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace MouseMove
{
    internal struct LastInputInfo
    {
        public uint cbSize;
        public uint dwTime;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Input
    {
        public uint type;
        public CombinedInput data;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct CombinedInput
    {
        [FieldOffset(0)]
        public HardwareInput Hardware;
        [FieldOffset(0)]
        public KeyboardInput Keyboard;
        [FieldOffset(0)]
        public MouseInput Mouse;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct HardwareInput
    {
        public uint msg;
        public ushort paramL;
        public ushort paramH;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct KeyboardInput
    {
        public ushort vK;
        public ushort scan;
        public uint flags;
        public uint time;
        public IntPtr extraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MouseInput
    {
        public int x;
        public int y;
        public uint mouseData;
        public uint flags;
        public uint time;
        public IntPtr extraInfo;
    }

    public partial class Form1 : Form
    {
        [DllImport("User32.dll")]
        private static extern bool GetLastInputInfo(ref LastInputInfo ptrInputInfo);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint numberOfInputs, Input[] inputs, int sizeOfInputStructure);

        static uint GetLastInputTime()
        {
            uint idleTime = 0;
            LastInputInfo lastInputInfo = new LastInputInfo();
            lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);
            lastInputInfo.dwTime = 0;

            uint envTicks = (uint)Environment.TickCount;

            if (GetLastInputInfo(ref lastInputInfo))
            {
                uint lastInputTick = lastInputInfo.dwTime;

                idleTime = (envTicks - lastInputTick);
            }

            return ((idleTime > 0) ? (idleTime / 1000) : idleTime);
        }

        private readonly Random random = new Random();
        private readonly uint defaultSecs = 40;

        private SynchronizationContext synchronizationContext;
        private System.Threading.Timer timer = null;

        public Form1()
        {
            InitializeComponent();
            synchronizationContext = SynchronizationContext.Current;
        }

        private void ScheduleTask(uint secs, Action task)
        {
            if (timer == null)
            {
                timer = new System.Threading.Timer(x =>
                {
                    task.Invoke();
                }, null, secs * 1000, secs * 1000);
            }
        }

        private void SendMouseInput()
        {
            Input input = new Input
            {
                type = 0
            };

            input.data.Mouse = new MouseInput
            {
                x = 0,
                y = 0
            };

            Input[] inputs = new Input[]
            {
                input
            };

            SendInput(1, inputs, Marshal.SizeOf(typeof(Input)));
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            //if the form is minimized  
            //hide it from the task bar  
            //and show the system tray icon (represented by the NotifyIcon control)  
            if (this.WindowState == FormWindowState.Minimized)
            {
                Hide();
                notifyIcon.Visible = true;
            }
        }

        private void notifyIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Show();
            this.WindowState = FormWindowState.Normal;
            notifyIcon.Visible = false;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            uint secs = defaultSecs;
            bool start = true;

            if (button1.Text.Equals("Start"))
            {
                button1.Text = "Stop";
                start = true;
            }
            else
            {
                button1.Text = "Start";
                start = false;
            }

            try
            {
                string val = textBox.Text.Trim();
                textBox.Text = val;
                secs = Convert.ToUInt32(val);
            }
            catch
            {
                secs = defaultSecs;
                textBox.Text = defaultSecs.ToString();
            }

            if (start)
            {
                ScheduleTask(secs, (() =>
                {
                    synchronizationContext.Post((object o) =>
                    {
                        uint idleTime = GetLastInputTime();
                        idleTextBox.Text = idleTime.ToString();

                        if (idleTime < secs)
                        {
                            return;
                        }

                        this.Cursor = new Cursor(Cursor.Current.Handle);

                        Point point;
                        int opr = random.Next(100);

                        if (opr < 50)
                        {
                            point = new Point(Cursor.Position.X - random.Next(10), Cursor.Position.Y - random.Next(10));
                        }
                        else
                        {
                            point = new Point(Cursor.Position.X + random.Next(10), Cursor.Position.Y + random.Next(10));
                        }

                        Cursor.Position = point;

                        try
                        {
                            SendMouseInput();
                        }
                        catch
                        {
                            // noop
                        }

                    }, sender);

                }));
            }
            else
            {
                if (timer != null)
                {
                    timer.Dispose();
                    idleTextBox.Text = "0";
                    timer = null;
                }
            }
        }
    }
}
