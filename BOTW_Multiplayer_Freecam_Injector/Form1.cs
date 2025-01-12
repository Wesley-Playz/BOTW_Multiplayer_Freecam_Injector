using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace BOTW_Multiplayer_Freecam_Injector
{
    public partial class Form1 : Form
    {
        private ListBox processListBox;
        private ListBox logBox;
        private Button refreshButton;
        private Button clearlogsButton;
        private Button injectButton;

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

        private const string DllUrl = "https://gitea.30-seven.cc/Wesley/botw-freecam/releases/download/v0.2.6/botw_freecam.dll";
        private const string DllFileName = "botw_freecam.dll";

        public Form1()
        {
            // Initialize components
            SetupForm();
        }

        private void SetupForm()
        {
            this.Text = "DLL Injector";
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
                Height = 130
            };
            this.Controls.Add(processListBox);

            logBox = new ListBox()
            {
                Location = new System.Drawing.Point(20, 210),
                Width = 400,
                Height = 130
            };
            this.Controls.Add(logBox);

            refreshButton = new Button()
            {
                Text = "Refresh Processes",
                Location = new System.Drawing.Point(440, 50),
                Width = 120
            };
            refreshButton.Click += RefreshButton_Click;
            this.Controls.Add(refreshButton);

            clearlogsButton = new Button()
            {
                Text = "Clear Logs",
                Location = new System.Drawing.Point(440, 245),
                Width = 120
            };
            clearlogsButton.Click += ClearlogsButton_Click;
            this.Controls.Add(clearlogsButton);

            Label log = new Label()
            {
                Text = "Logs:",
                Location = new System.Drawing.Point(20, 185),
                AutoSize = true
            };
            this.Controls.Add(log);

            injectButton = new Button()
            {
                Text = "Inject DLL",
                Location = new System.Drawing.Point(440, 210),
                Width = 120
            };
            injectButton.Click += InjectButton_Click;
            this.Controls.Add(injectButton);
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

        private void ClearlogsButton_Click(object sender, EventArgs e)
        {
            logBox.Items.Clear();
        }

        private void InjectButton_Click(object sender, EventArgs e)
        {
            if (processListBox.SelectedItem == null)
            {
                logBox.Items.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Please select a process first.");
                logBox.TopIndex = logBox.Items.Count - 1;
                return;
            }

            string dllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DllFileName);

            if (!File.Exists(dllPath))
            {
                try
                {
                    using (var client = new WebClient())
                    {
                        logBox.Items.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Downloading DLL...");
                        client.DownloadFile(DllUrl, dllPath);
                        logBox.Items.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] DLL downloaded successfully.");
                    }
                }
                catch (Exception ex)
                {
                    logBox.Items.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Error downloading DLL: {ex.Message}");
                    logBox.TopIndex = logBox.Items.Count - 1;
                    return;
                }
            }
            else
            {
                logBox.Items.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] DLL already exists. Using the existing file.");
            }

            string selectedProcess = processListBox.SelectedItem.ToString();
            int processId = int.Parse(selectedProcess.Split('-')[0].Trim());

            InjectDll(processId, dllPath);
        }

        private void InjectDll(int processId, string dllPath)
        {
            IntPtr hProcess = OpenProcess(PROCESS_ALL_ACCESS, false, processId);
            if (hProcess == IntPtr.Zero)
            {
                logBox.Items.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Failed to open process.");
                logBox.TopIndex = logBox.Items.Count - 1;
                return;
            }

            IntPtr allocMemAddress = VirtualAllocEx(hProcess, IntPtr.Zero, (uint)dllPath.Length + 1, MEM_COMMIT, PAGE_READWRITE);
            if (allocMemAddress == IntPtr.Zero)
            {
                logBox.Items.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Failed to allocate memory.");
                logBox.TopIndex = logBox.Items.Count - 1;
                return;
            }

            bool isWritten = WriteProcessMemory(hProcess, allocMemAddress, System.Text.Encoding.Default.GetBytes(dllPath), (uint)dllPath.Length + 1, out _);
            if (!isWritten)
            {
                logBox.Items.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Failed to write process memory.");
                logBox.TopIndex = logBox.Items.Count - 1;
                return;
            }

            IntPtr kernel32Handle = GetModuleHandle("kernel32.dll");
            IntPtr loadLibraryAddress = GetProcAddress(kernel32Handle, "LoadLibraryA");

            IntPtr remoteThreadHandle = CreateRemoteThread(hProcess, IntPtr.Zero, 0, loadLibraryAddress, allocMemAddress, 0, out _);
            if (remoteThreadHandle == IntPtr.Zero)
            {
                logBox.Items.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Failed to create remote thread.");
                logBox.TopIndex = logBox.Items.Count - 1;
                return;
            }

            WaitForSingleObject(remoteThreadHandle, 0xFFFFFFFF);
            logBox.Items.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] DLL injected successfully!");
            logBox.TopIndex = logBox.Items.Count - 1;
        }
    }
}