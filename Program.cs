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
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

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

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool AddClipboardFormatListener(IntPtr hwnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

        private const int HOTKEY_ID = 1;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint MOD_ALT = 0x0001;
        
        private uint currentModifiers = 0x0002;
        private uint currentKey = 0x53;
        private string currentKeyName = "Ctrl+S";
        
        private bool isChineseUI = true;
        private bool autoMode = false;
        private string autoSaveDir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        private NotifyIcon trayIcon;
        private HotkeyMessageWindow messageWindow;

        public ClipboardImageSaverApp()
        {
            messageWindow = new HotkeyMessageWindow(this);
            LoadSettings();
            if (!RegisterHotKey(messageWindow.Handle, HOTKEY_ID, currentModifiers, currentKey))
            {
                 // No message box on startup failure to keep it silent
            }
            InitializeTrayIcon();
            if (autoMode)
            {
                AddClipboardFormatListener(messageWindow.Handle);
            }
        }

        private void InitializeTrayIcon()
        {
            trayIcon = new NotifyIcon();
            try
            {
                trayIcon.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch {}
            UpdateTrayText();
            UpdateContextMenu();
            trayIcon.Visible = true;
        }

        public void OnHotKeyPressed()
        {
            SaveClipboardImage();
        }

        public void OnClipboardChanged()
        {
            if (autoMode)
            {
                try
                {
                    if (Clipboard.ContainsImage())
                    {
                        Image image = Clipboard.GetImage();
                        if (image == null) return; // If GetImage returns null, nothing to save.
                        Directory.CreateDirectory(autoSaveDir);
                        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                        string filename = Path.Combine(autoSaveDir, "image_" + timestamp + ".png");
                        image.Save(filename, ImageFormat.Png);
                    }
                }
                catch (Exception ex)
                {
                    string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log.txt");
                    File.AppendAllText(logPath, DateTime.Now.ToString() + " [OnClipboardChanged]\n" + ex.ToString() + "\n\n");
                }
            }
        }

        private string GetCurrentExplorerPath()
        {
            try
            {
                IntPtr hwnd = GetForegroundWindow();
                uint processId;
                GetWindowThreadProcessId(hwnd, out processId);
                var process = System.Diagnostics.Process.GetProcessById((int)processId);
                if (process.ProcessName.ToLower() == "explorer")
                {
                    Type shellWindowsType = Type.GetTypeFromProgID("Shell.Application");
                    if (shellWindowsType != null)
                    {
                        dynamic shellWindows = Activator.CreateInstance(shellWindowsType);
                        dynamic windows = shellWindows.Windows();
                        for (int i = 0; i < windows.Count; i++)
                        {
                            dynamic window = windows.Item(i);
                            if (window != null && window.HWND == (int)hwnd)
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
            catch (Exception ex)
            {
                string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log.txt");
                File.AppendAllText(logPath, DateTime.Now.ToString() + " [GetCurrentExplorerPath]\n" + ex.ToString() + "\n\n");
            }
            return Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        }

        private void SaveClipboardImage()
        {
            try
            {
                if (Clipboard.ContainsImage())
                {
                    Image image = Clipboard.GetImage();
                    string targetPath = GetCurrentExplorerPath();
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    string filename = Path.Combine(targetPath, "image_" + timestamp + ".png");
                    image.Save(filename, ImageFormat.Png);
                }
            }
            catch (Exception ex)
            {
                string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log.txt");
                File.AppendAllText(logPath, DateTime.Now.ToString() + " [SaveClipboardImage]\n" + ex.ToString() + "\n\n");
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
                contextMenu.Items.Add("设置自动保存路径...", null, (s, e) => ChooseAutoSaveDir());
                contextMenu.Items.Add(autoMode ? "切换为手动模式" : "切换为自动模式", null, (s, e) => ToggleMode());
                contextMenu.Items.Add("关于", null, (s, e) => ShowAbout());
                contextMenu.Items.Add(new ToolStripSeparator());
                contextMenu.Items.Add("切换到英文", null, (s, e) => ToggleLanguage());
                contextMenu.Items.Add(new ToolStripSeparator());
                contextMenu.Items.Add("退出", null, (s, e) => Exit());
            }
            else
            {
                contextMenu.Items.Add("Change Hotkey...", null, (s, e) => ChangeHotkey());
                contextMenu.Items.Add("Set Auto Save Folder...", null, (s, e) => ChooseAutoSaveDir());
                contextMenu.Items.Add(autoMode ? "Switch to Manual Mode" : "Switch to Auto Mode", null, (s, e) => ToggleMode());
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
                string configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ClipboardImageSaver", "config.txt");
                if (File.Exists(configPath))
                {
                    string[] lines = File.ReadAllLines(configPath);
                    if (lines.Length >= 3)
                    {
                        currentModifiers = uint.Parse(lines[0]);
                        currentKey = uint.Parse(lines[1]);
                        currentKeyName = lines[2];
                    }
                    if (lines.Length >= 4) { isChineseUI = lines[3] == "zh"; }
                    if (lines.Length >= 5) { autoMode = lines[4] == "auto"; }
                    if (lines.Length >= 6) { autoSaveDir = lines[5]; }
                }
            }
            catch (Exception ex)
            {
                string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log.txt");
                File.AppendAllText(logPath, DateTime.Now.ToString() + " [LoadSettings]\n" + ex.ToString() + "\n\n");
            }
        }

        private void SaveSettings()
        {
            try
            {
                string configDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ClipboardImageSaver");
                Directory.CreateDirectory(configDir);
                string configPath = Path.Combine(configDir, "config.txt");
                File.WriteAllLines(configPath, new string[] { currentModifiers.ToString(), currentKey.ToString(), currentKeyName, isChineseUI ? "zh" : "en", autoMode ? "auto" : "manual", autoSaveDir });
            }
            catch (Exception ex)
            {
                string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log.txt");
                File.AppendAllText(logPath, DateTime.Now.ToString() + " [SaveSettings]\n" + ex.ToString() + "\n\n");
            }
        }

        private void ChangeHotkey()
        {
            Form hotkeyForm = new Form();
            hotkeyForm.Text = isChineseUI ? "更改快捷键" : "Change Hotkey";
            hotkeyForm.Width = 350; hotkeyForm.Height = 200;
            hotkeyForm.StartPosition = FormStartPosition.CenterScreen;
            hotkeyForm.FormBorderStyle = FormBorderStyle.FixedDialog;
            hotkeyForm.MaximizeBox = false; hotkeyForm.MinimizeBox = false;
            Label label = new Label();
            label.Text = isChineseUI ? "当前快捷键: " + currentKeyName + "\n\n按下新的快捷键组合:" : "Current: " + currentKeyName + "\n\nPress new hotkey combination:";
            label.Location = new System.Drawing.Point(20, 20); label.AutoSize = true;
            hotkeyForm.Controls.Add(label);
            TextBox textBox = new TextBox();
            textBox.Location = new System.Drawing.Point(20, 70); textBox.Width = 290; textBox.ReadOnly = true;
            hotkeyForm.Controls.Add(textBox);
            uint newModifiers = 0; uint newKey = 0; string newKeyName = "";
            textBox.KeyDown += (s, e) => {
                newModifiers = 0;
                if (e.Control) newModifiers |= MOD_CONTROL;
                if (e.Shift) newModifiers |= MOD_SHIFT;
                if (e.Alt) newModifiers |= MOD_ALT;
                if (e.KeyCode != Keys.ControlKey && e.KeyCode != Keys.ShiftKey && e.KeyCode != Keys.Menu && e.KeyCode != Keys.LWin && e.KeyCode != Keys.RWin)
                {
                    newKey = (uint)e.KeyCode;
                    string modStr = "";
                    if (e.Control) modStr += "Ctrl+";
                    if (e.Shift) modStr += "Shift+";
                    if (e.Alt) modStr += "Alt+";
                    newKeyName = modStr + e.KeyCode.ToString();
                    textBox.Text = newKeyName;
                }
                e.Handled = true; e.SuppressKeyPress = true;
            };
            Button okButton = new Button();
            okButton.Text = isChineseUI ? "确定" : "OK";
            okButton.Location = new System.Drawing.Point(120, 110);
            okButton.Click += (s, e) => {
                if (newKey != 0)
                {
                    UnregisterHotKey(messageWindow.Handle, HOTKEY_ID);
                    if (RegisterHotKey(messageWindow.Handle, HOTKEY_ID, newModifiers, newKey))
                    {
                        currentModifiers = newModifiers; currentKey = newKey; currentKeyName = newKeyName;
                        SaveSettings(); UpdateTrayText();
                        MessageBox.Show(isChineseUI ? "快捷键已更改为: " + currentKeyName : "Hotkey changed to: " + currentKeyName, isChineseUI ? "成功" : "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        hotkeyForm.Close();
                    }
                    else
                    {
                        MessageBox.Show(isChineseUI ? "注册快捷键失败: " + newKeyName + "\n可能已被占用" : "Failed to register " + newKeyName + "\nIt may be in use.", isChineseUI ? "错误" : "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            Form aboutForm = new Form();
            aboutForm.Text = isChineseUI ? "关于" : "About";
            aboutForm.Width = 450; aboutForm.Height = 380;
            aboutForm.StartPosition = FormStartPosition.CenterScreen;
            aboutForm.FormBorderStyle = FormBorderStyle.FixedDialog;
            aboutForm.MaximizeBox = false; aboutForm.MinimizeBox = false;
            RichTextBox richTextBox = new RichTextBox();
            richTextBox.Location = new System.Drawing.Point(10, 10); richTextBox.Width = 410; richTextBox.Height = 280;
            richTextBox.ReadOnly = true; richTextBox.BorderStyle = BorderStyle.None; richTextBox.DetectUrls = true;
            aboutForm.Controls.Add(richTextBox);
            string version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.1.0";
            if (isChineseUI)
            {
                richTextBox.Text = "剪贴板图片保存工具 v" + version + "\n\n" + "当前快捷键: " + currentKeyName + "\n\n" + "在任意文件夹窗口按快捷键保存剪贴板图片\n\n" + "图片将保存到当前打开的文件夹\n" + "如果没有打开文件夹，则保存到桌面\n\n" + "右键托盘图标可更改快捷键\n\n" + "网站: https://sevencn.com\n" + "上传到: https://github.com/75110/openstar-ClipboardImageSaver";
            }
            else
            {
                richTextBox.Text = "Clipboard Image Saver v" + version + "\n\n" + "Current hotkey: " + currentKeyName + "\n\n" + "Press the hotkey in any folder to save clipboard image.\n\n" + "Images will be saved to the current active folder.\n" + "If no folder is active, saves to Desktop.\n\n" + "Right-click tray icon to change hotkey.\n\n" + "Website: https://sevencn.com\n" + "GitHub: https://github.com/75110/openstar-ClipboardImageSaver";
            }
            Button closeButton = new Button();
            closeButton.Text = isChineseUI ? "关闭" : "Close";
            closeButton.Location = new System.Drawing.Point(190, 300);
            closeButton.Click += (s, e) => aboutForm.Close();
            aboutForm.Controls.Add(closeButton);
            richTextBox.LinkClicked += (s, e) => {
                try { System.Diagnostics.Process.Start(e.LinkText); }
                catch (Exception ex)
                {
                    string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log.txt");
                    File.AppendAllText(logPath, DateTime.Now.ToString() + " [ShowAbout LinkClicked]\n" + ex.ToString() + "\n\n");
                }
            };
            aboutForm.ShowDialog();
        }

        private void Exit()
        {
            UnregisterHotKey(messageWindow.Handle, HOTKEY_ID);
            RemoveClipboardFormatListener(messageWindow.Handle);
            trayIcon.Visible = false;
            messageWindow.DestroyHandle();
            Application.Exit();
        }

        private class HotkeyMessageWindow : NativeWindow
        {
            private const int WM_HOTKEY = 0x0312;
            private const int WM_CLIPBOARDUPDATE = 0x031D;
            private ClipboardImageSaverApp app;
            public HotkeyMessageWindow(ClipboardImageSaverApp app) { this.app = app; CreateHandle(new CreateParams()); }
            protected override void WndProc(ref Message m)
            {
                if (m.Msg == WM_HOTKEY) { app.OnHotKeyPressed(); }
                else if (m.Msg == WM_CLIPBOARDUPDATE) { app.OnClipboardChanged(); }
                base.WndProc(ref m);
            }
        }

        private void ToggleMode()
        {
            autoMode = !autoMode;
            if (autoMode) { AddClipboardFormatListener(messageWindow.Handle); }
            else { RemoveClipboardFormatListener(messageWindow.Handle); }
            SaveSettings();
            UpdateContextMenu();
            UpdateTrayText();
        }

        private void ChooseAutoSaveDir()
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = isChineseUI ? "选择自动保存路径" : "Choose auto save folder";
                dialog.SelectedPath = autoSaveDir;
                if (dialog.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
                {
                    autoSaveDir = dialog.SelectedPath;
                    SaveSettings();
                    UpdateContextMenu();
                }
            }
        }
    }
}
