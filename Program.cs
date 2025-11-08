using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Text;
using System.Configuration;

namespace ClipboardImageSaver
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new ClipboardImageSaverApp());
        }
    }

    class ClipboardImageSaverApp : ApplicationContext
    {
        // Win32 API for global hotkey
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        // Win32 API for getting foreground window and path
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("shell32.dll")]
        private static extern IntPtr ILCreateFromPath(string path);

        [DllImport("shell32.dll")]
        private static extern void ILFree(IntPtr pidl);

        private const int HOTKEY_ID = 1;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint MOD_ALT = 0x0001;
        
        // Current hotkey settings
        private uint currentModifiers = 0x0002;  // Default: Ctrl
        private uint currentKey = 0x53;  // Default: S
        private string currentKeyName = "Ctrl+S";
        
        // Language setting
        private bool isChineseUI = true;

        private NotifyIcon trayIcon;
        private HotkeyMessageWindow messageWindow;

        public ClipboardImageSaverApp()
        {
            // Create message window for hotkey
            messageWindow = new HotkeyMessageWindow(this);

            // Load saved settings
            LoadSettings();

            // Register hotkey
            if (!RegisterHotKey(messageWindow.Handle, HOTKEY_ID, currentModifiers, currentKey))
            {
                MessageBox.Show(
                    "Failed to register hotkey " + currentKeyName + ".\nIt may be already in use by another application.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }

            // Create system tray icon
            InitializeTrayIcon();
        }

        private void InitializeTrayIcon()
        {
            trayIcon = new NotifyIcon();
            
            // Use application's embedded icon
            trayIcon.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            
            UpdateTrayText();
            UpdateContextMenu();
            trayIcon.Visible = true;
        }

        public void OnHotKeyPressed()
        {
            SaveClipboardImage();
        }

        private string GetCurrentExplorerPath()
        {
            try
            {
                IntPtr hwnd = GetForegroundWindow();
                StringBuilder windowTitle = new StringBuilder(256);
                GetWindowText(hwnd, windowTitle, 256);
                string title = windowTitle.ToString();

                uint processId;
                GetWindowThreadProcessId(hwnd, out processId);

                // Check if it's explorer window
                var process = System.Diagnostics.Process.GetProcessById((int)processId);
                if (process.ProcessName.ToLower() == "explorer")
                {
                    // Try to use SHDocVw
                    Type shellWindowsType = Type.GetTypeFromProgID("Shell.Application");
                    if (shellWindowsType != null)
                    {
                        dynamic shellWindows = Activator.CreateInstance(shellWindowsType);
                        dynamic windows = shellWindows.Windows();
                        
                        for (int i = 0; i < windows.Count; i++)
                        {
                            dynamic window = windows.Item(i);
                            if (window.HWND == (int)hwnd)
                            {
                                string path = window.LocationURL;
                                if (!string.IsNullOrEmpty(path) && path.StartsWith("file:///"))
                                {
                                    path = path.Substring(8).Replace('/', '\\');
                                    path = Uri.UnescapeDataString(path);
                                    return path;
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            // Fallback to desktop
            return Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        }

        private void SaveClipboardImage()
        {
            try
            {
                // Only process if clipboard contains an image
                if (Clipboard.ContainsImage())
                {
                    Image image = Clipboard.GetImage();
                    string targetPath = GetCurrentExplorerPath();
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    string filename = Path.Combine(targetPath, "image_" + timestamp + ".png");

                    image.Save(filename, ImageFormat.Png);

                    // Show notification
                    trayIcon.ShowBalloonTip(
                        2000,
                        isChineseUI ? "图片已保存" : "Image Saved",
                        (isChineseUI ? "保存到:\n" : "Saved to:\n") + targetPath + "\n" + Path.GetFileName(filename),
                        ToolTipIcon.Info
                    );
                }
                // If no image, do nothing (let system handle normal paste)
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    (isChineseUI ? "保存失败:\n" : "Failed to save image:\n") + ex.Message,
                    isChineseUI ? "错误" : "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private void UpdateTrayText()
        {
            if (isChineseUI)
            {
                trayIcon.Text = "剪贴板图片保存工具\n" + currentKeyName + " 保存图片";
            }
            else
            {
                trayIcon.Text = "Clipboard Image Saver\n" + currentKeyName + " to save image";
            }
        }

        private void UpdateContextMenu()
        {
            var contextMenu = new ContextMenuStrip();
            
            if (isChineseUI)
            {
                contextMenu.Items.Add("更改快捷键...", null, (s, e) => ChangeHotkey());
                contextMenu.Items.Add("关于", null, (s, e) => ShowAbout());
                contextMenu.Items.Add(new ToolStripSeparator());
                contextMenu.Items.Add("切换到英文", null, (s, e) => ToggleLanguage());
                contextMenu.Items.Add(new ToolStripSeparator());
                contextMenu.Items.Add("退出", null, (s, e) => Exit());
            }
            else
            {
                contextMenu.Items.Add("Change Hotkey...", null, (s, e) => ChangeHotkey());
                contextMenu.Items.Add("About", null, (s, e) => ShowAbout());
                contextMenu.Items.Add(new ToolStripSeparator());
                contextMenu.Items.Add("Switch to Chinese", null, (s, e) => ToggleLanguage());
                contextMenu.Items.Add(new ToolStripSeparator());
                contextMenu.Items.Add("Exit", null, (s, e) => Exit());
            }

            trayIcon.ContextMenuStrip = contextMenu;
        }

        private void ToggleLanguage()
        {
            isChineseUI = !isChineseUI;
            SaveSettings();
            UpdateContextMenu();
            UpdateTrayText();
        }

        private void LoadSettings()
        {
            try
            {
                string configPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "ClipboardImageSaver",
                    "config.txt"
                );
                
                if (File.Exists(configPath))
                {
                    string[] lines = File.ReadAllLines(configPath);
                    if (lines.Length >= 3)
                    {
                        currentModifiers = uint.Parse(lines[0]);
                        currentKey = uint.Parse(lines[1]);
                        currentKeyName = lines[2];
                    }
                    if (lines.Length >= 4)
                    {
                        isChineseUI = lines[3] == "zh";
                    }
                }
            }
            catch { }
        }

        private void SaveSettings()
        {
            try
            {
                string configDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "ClipboardImageSaver"
                );
                Directory.CreateDirectory(configDir);
                
                string configPath = Path.Combine(configDir, "config.txt");
                File.WriteAllLines(configPath, new string[] {
                    currentModifiers.ToString(),
                    currentKey.ToString(),
                    currentKeyName,
                    isChineseUI ? "zh" : "en"
                });
            }
            catch { }
        }

        private void ChangeHotkey()
        {
            Form hotkeyForm = new Form();
            hotkeyForm.Text = isChineseUI ? "更改快捷键" : "Change Hotkey";
            hotkeyForm.Width = 350;
            hotkeyForm.Height = 200;
            hotkeyForm.StartPosition = FormStartPosition.CenterScreen;
            hotkeyForm.FormBorderStyle = FormBorderStyle.FixedDialog;
            hotkeyForm.MaximizeBox = false;
            hotkeyForm.MinimizeBox = false;

            Label label = new Label();
            label.Text = isChineseUI 
                ? "当前快捷键: " + currentKeyName + "\n\n按下新的快捷键组合:"
                : "Current: " + currentKeyName + "\n\nPress new hotkey combination:";
            label.Location = new System.Drawing.Point(20, 20);
            label.AutoSize = true;
            hotkeyForm.Controls.Add(label);

            TextBox textBox = new TextBox();
            textBox.Location = new System.Drawing.Point(20, 70);
            textBox.Width = 290;
            textBox.ReadOnly = true;
            hotkeyForm.Controls.Add(textBox);

            uint newModifiers = 0;
            uint newKey = 0;
            string newKeyName = "";

            textBox.KeyDown += (s, e) => {
                newModifiers = 0;
                if (e.Control) newModifiers |= MOD_CONTROL;
                if (e.Shift) newModifiers |= MOD_SHIFT;
                if (e.Alt) newModifiers |= MOD_ALT;

                if (e.KeyCode != Keys.ControlKey && e.KeyCode != Keys.ShiftKey && 
                    e.KeyCode != Keys.Menu && e.KeyCode != Keys.LWin && e.KeyCode != Keys.RWin)
                {
                    newKey = (uint)e.KeyCode;
                    
                    string modStr = "";
                    if (e.Control) modStr += "Ctrl+";
                    if (e.Shift) modStr += "Shift+";
                    if (e.Alt) modStr += "Alt+";
                    
                    newKeyName = modStr + e.KeyCode.ToString();
                    textBox.Text = newKeyName;
                }
                
                e.Handled = true;
                e.SuppressKeyPress = true;
            };

            Button okButton = new Button();
            okButton.Text = isChineseUI ? "确定" : "OK";
            okButton.Location = new System.Drawing.Point(120, 110);
            okButton.Click += (s, e) => {
                if (newKey != 0)
                {
                    // Unregister old hotkey
                    UnregisterHotKey(messageWindow.Handle, HOTKEY_ID);
                    
                    // Try to register new hotkey
                    if (RegisterHotKey(messageWindow.Handle, HOTKEY_ID, newModifiers, newKey))
                    {
                        currentModifiers = newModifiers;
                        currentKey = newKey;
                        currentKeyName = newKeyName;
                        SaveSettings();
                        UpdateTrayText();
                        
                        MessageBox.Show(
                            isChineseUI ? "快捷键已更改为: " + currentKeyName : "Hotkey changed to: " + currentKeyName,
                            isChineseUI ? "成功" : "Success",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        hotkeyForm.Close();
                    }
                    else
                    {
                        MessageBox.Show(
                            isChineseUI ? "注册快捷键失败: " + newKeyName + "\n可能已被占用" : "Failed to register " + newKeyName + "\nIt may be in use.",
                            isChineseUI ? "错误" : "Error", 
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        
                        // Re-register old hotkey
                        RegisterHotKey(messageWindow.Handle, HOTKEY_ID, currentModifiers, currentKey);
                    }
                }
            };
            hotkeyForm.Controls.Add(okButton);

            Button cancelButton = new Button();
            cancelButton.Text = isChineseUI ? "取消" : "Cancel";
            cancelButton.Location = new System.Drawing.Point(210, 110);
            cancelButton.Click += (s, e) => hotkeyForm.Close();
            hotkeyForm.Controls.Add(cancelButton);

            hotkeyForm.ShowDialog();
        }

        private void ShowAbout()
        {
            string message, title;
            
            if (isChineseUI)
            {
                message = "剪贴板图片保存工具 {{VERSION}}\\n\\n" +
                    "当前快捷键: " + currentKeyName + "\\n\\n" +
                    "在任意文件夹窗口按快捷键保存剪贴板图片\\n\\n" +
                    "图片将保存到当前打开的文件夹\\n" +
                    "如果没有打开文件夹，则保存到桌面\\n\\n" +
                    "右键托盘图标可更改快捷键\\n\\n" +
                    "开发者: sevencn.com";
                title = "关于";
            }
            else
            {
                message = "Clipboard Image Saver {{VERSION}}\\n\\n" +
                    "Current hotkey: " + currentKeyName + "\\n\\n" +
                    "Press the hotkey in any folder to save clipboard image.\\n\\n" +
                    "Images will be saved to the current active folder.\\n" +
                    "If no folder is active, saves to Desktop.\\n\\n" +
                    "Right-click tray icon to change hotkey.\\n\\n" +
                    "Developer: sevencn.com";
                title = "About";
            }
            
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void Exit()
        {
            UnregisterHotKey(messageWindow.Handle, HOTKEY_ID);
            trayIcon.Visible = false;
            messageWindow.DestroyHandle();
            Application.Exit();
        }

        // Message window to receive hotkey events
        private class HotkeyMessageWindow : NativeWindow
        {
            private const int WM_HOTKEY = 0x0312;
            private ClipboardImageSaverApp app;

            public HotkeyMessageWindow(ClipboardImageSaverApp app)
            {
                this.app = app;
                CreateHandle(new CreateParams());
            }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == WM_HOTKEY)
                {
                    app.OnHotKeyPressed();
                }
                base.WndProc(ref m);
            }
        }
    }
}
