using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Drawing;

namespace BOTW_Multiplayer_Freecam_Injector
{
    public partial class Form1 : Form
    {
        private ListBox processListBox;
        private Button refreshButton;
        private Button injectButton;
        private Button browseButton;
        private TextBox dllPathTextBox;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        private static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] buffer, uint size, out uint lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, out uint lpThreadId);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string moduleName);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll")]
        private static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

        private const int PROCESS_ALL_ACCESS = 0x1F0FFF;
        private const uint MEM_COMMIT = 0x1000;
        private const uint PAGE_READWRITE = 0x04;

        public Form1()
        {
            // Initialize components
            SetupForm();
        }

        private void SetupForm()
        {
            this.Text = "BOTW Multiplayer Freecam Injector";
            this.Width = 600;
            this.Height = 400;

            Label lblProcessList = new Label()
            {
                Text = "Processes:",
                Location = new System.Drawing.Point(20, 20),
                AutoSize = true
            };
            this.Controls.Add(lblProcessList);

            processListBox = new ListBox()
            {
                Location = new System.Drawing.Point(20, 50),
                Width = 400,
                Height = 200
            };
            this.Controls.Add(processListBox);

            refreshButton = new Button()
            {
                Text = "Refresh Processes",
                Location = new System.Drawing.Point(440, 50),
                Width = 120
            };
            refreshButton.Click += RefreshButton_Click;
            this.Controls.Add(refreshButton);

            Label lblDllPath = new Label()
            {
                Text = "DLL Path:",
                Location = new System.Drawing.Point(20, 270),
                AutoSize = true
            };
            this.Controls.Add(lblDllPath);

            dllPathTextBox = new TextBox()
            {
                Location = new System.Drawing.Point(20, 300),
                Width = 400
            };
            this.Controls.Add(dllPathTextBox);

            browseButton = new Button()
            {
                Text = "Browse",
                Location = new System.Drawing.Point(440, 300),
                Width = 120
            };
            browseButton.Click += BrowseButton_Click;
            this.Controls.Add(browseButton);

            injectButton = new Button()
            {
                Text = "Inject DLL",
                Location = new System.Drawing.Point(440, 330),
                Width = 120
            };
            injectButton.Click += InjectButton_Click;
            this.Controls.Add(injectButton);
        }

        private void BrowseButton_Click(object sender, EventArgs e)
        {
            // Open file dialog to select DLL
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "DLL Files|*.dll";
            openFileDialog.Title = "Select DLL File";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                // Set the selected file path in the text box
                dllPathTextBox.Text = openFileDialog.FileName;
            }
        }

        private void RefreshButton_Click(object sender, EventArgs e)
        {
            processListBox.Items.Clear();
            var processes = Process.GetProcesses()
    .Where(p => p.ProcessName.IndexOf("Cemu", StringComparison.OrdinalIgnoreCase) >= 0);
            foreach (var process in processes)
            {
                processListBox.Items.Add($"{process.Id} - {process.ProcessName}");
            }
        }

        private void InjectButton_Click(object sender, EventArgs e)
        {
            if (processListBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a process first.");
                return;
            }

            string selectedProcess = processListBox.SelectedItem.ToString();
            int processId = int.Parse(selectedProcess.Split('-')[0].Trim());

            string dllPath = dllPathTextBox.Text;
            if (string.IsNullOrEmpty(dllPath))
            {
                MessageBox.Show("Please specify a valid DLL path.");
                return;
            }

            InjectDll(processId, dllPath);
        }

        private void InjectDll(int processId, string dllPath)
        {
            IntPtr hProcess = OpenProcess(PROCESS_ALL_ACCESS, false, processId);
            if (hProcess == IntPtr.Zero)
            {
                MessageBox.Show("Failed to open process.");
                return;
            }

            IntPtr allocMemAddress = VirtualAllocEx(hProcess, IntPtr.Zero, (uint)dllPath.Length + 1, MEM_COMMIT, PAGE_READWRITE);
            if (allocMemAddress == IntPtr.Zero)
            {
                MessageBox.Show("Failed to allocate memory.");
                return;
            }

            bool isWritten = WriteProcessMemory(hProcess, allocMemAddress, System.Text.Encoding.Default.GetBytes(dllPath), (uint)dllPath.Length + 1, out _);
            if (!isWritten)
            {
                MessageBox.Show("Failed to write process memory.");
                return;
            }

            IntPtr kernel32Handle = GetModuleHandle("kernel32.dll");
            IntPtr loadLibraryAddress = GetProcAddress(kernel32Handle, "LoadLibraryA");

            IntPtr remoteThreadHandle = CreateRemoteThread(hProcess, IntPtr.Zero, 0, loadLibraryAddress, allocMemAddress, 0, out _);
            if (remoteThreadHandle == IntPtr.Zero)
            {
                MessageBox.Show("Failed to create remote thread.");
                return;
            }

            WaitForSingleObject(remoteThreadHandle, 0xFFFFFFFF);
            MessageBox.Show("DLL Injected successfully!");
        }

        [STAThread]
        public static void Form()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
