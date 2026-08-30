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
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool AddClipboardFormatListener(IntPtr hwnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

        private bool isChineseUI = true;
        private bool autoMode = false;
        private string autoSaveDir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        private NotifyIcon trayIcon;
        private HotkeyMessageWindow messageWindow;

        public ClipboardImageSaverApp()
        {
            messageWindow = new HotkeyMessageWindow(this);
            LoadSettings();
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

        public void OnClipboardChanged()
        {
            if (!autoMode) return;

            Image image = null;
            byte[] pngData = null;
            // 重试机制：剪贴板数据可能尚未完全就绪，需要稍等再读取
            for (int i = 0; i < 8; i++)
            {
                try
                {
                    pngData = TryGetClipboardPng();
                    if (pngData != null)
                    {
                        break;
                    }

                    if (Clipboard.ContainsImage())
                    {
                        image = Clipboard.GetImage();
                        if (image != null) break;
                    }
                }
                catch (Exception ex)
                {
                    string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log.txt");
                    File.AppendAllText(logPath, $"{DateTime.Now} [OnClipboardChanged retry {i}]\n{ex}\n\n");
                }
                if (i < 7)
                    System.Threading.Thread.Sleep(250);
            }

            if (pngData == null && image == null) return;

            try
            {
                Directory.CreateDirectory(autoSaveDir);
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string filename = Path.Combine(autoSaveDir, "image_" + timestamp + ".png");

                // 快速连续复制时防止文件名冲突
                int counter = 1;
                while (File.Exists(filename))
                {
                    filename = Path.Combine(autoSaveDir, "image_" + timestamp + "_" + counter + ".png");
                    counter++;
                }

                if (pngData != null)
                {
                    File.WriteAllBytes(filename, pngData);
                }
                else
                {
                    image.Save(filename, ImageFormat.Png);
                    image.Dispose();
                }
            }
            catch (Exception ex)
            {
                string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log.txt");
                File.AppendAllText(logPath, $"{DateTime.Now} [OnClipboardChanged save]\n{ex}\n\n");
            }
        }

        private static byte[] TryGetClipboardPng()
        {
            var dataObject = Clipboard.GetDataObject();
            if (dataObject == null || !dataObject.GetDataPresent("PNG", false)) return null;

            object data = dataObject.GetData("PNG", false);
            if (data is byte[] bytes && bytes.Length > 0)
            {
                return bytes;
            }

            var stream = data as Stream;
            if (stream == null) return null;

            using (stream)
            using (var memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);
                return memoryStream.Length > 0 ? memoryStream.ToArray() : null;
            }
        }

        private void UpdateTrayText()
        {
            if (isChineseUI)
            {
                trayIcon.Text = "剪贴板图片保存工具";
            }
            else
            {
                trayIcon.Text = "Clipboard Image Saver";
            }
        }

        private void UpdateContextMenu()
        {
            var contextMenu = new ContextMenuStrip();
            if (isChineseUI)
            {
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
                    if (lines.Length >= 2) { isChineseUI = lines[1] == "zh"; }
                    if (lines.Length >= 3) { autoMode = lines[2] == "auto"; }
                    if (lines.Length >= 4) { autoSaveDir = lines[3]; }
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
                File.WriteAllLines(configPath, new string[] { "v2", isChineseUI ? "zh" : "en", autoMode ? "auto" : "manual", autoSaveDir });
            }
            catch (Exception ex)
            {
                string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log.txt");
                File.AppendAllText(logPath, DateTime.Now.ToString() + " [SaveSettings]\n" + ex.ToString() + "\n\n");
            }
        }

        private void ShowAbout()
        {
            Form aboutForm = new Form();
            aboutForm.Text = isChineseUI ? "关于" : "About";
            aboutForm.Width = 450; aboutForm.Height = 320;
            aboutForm.StartPosition = FormStartPosition.CenterScreen;
            aboutForm.FormBorderStyle = FormBorderStyle.FixedDialog;
            aboutForm.MaximizeBox = false; aboutForm.MinimizeBox = false;
            RichTextBox richTextBox = new RichTextBox();
            richTextBox.Location = new System.Drawing.Point(10, 10); richTextBox.Width = 410; richTextBox.Height = 220;
            richTextBox.ReadOnly = true; richTextBox.BorderStyle = BorderStyle.None; richTextBox.DetectUrls = true;
            aboutForm.Controls.Add(richTextBox);
            string version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.2.0";
            if (isChineseUI)
            {
                richTextBox.Text = "剪贴板图片保存工具 v" + version + "\n\n" + "自动检测剪贴板中的图片并保存\n\n" + "图片将保存到设置的自动保存目录\n\n" + "右键托盘图标可设置保存路径\n\n" + "网站: https://sevencn.com\n" + "GitHub: https://github.com/sevencnup/wotty-ClipboardImageSaver";
            }
            else
            {
                richTextBox.Text = "Clipboard Image Saver v" + version + "\n\n" + "Auto-detect and save clipboard images.\n\n" + "Images will be saved to the configured auto-save folder.\n\n" + "Right-click tray icon to configure save folder.\n\n" + "Website: https://sevencn.com\n" + "GitHub: https://github.com/sevencnup/wotty-ClipboardImageSaver";
            }
            Button closeButton = new Button();
            closeButton.Text = isChineseUI ? "关闭" : "Close";
            closeButton.Location = new System.Drawing.Point(190, 240);
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
            RemoveClipboardFormatListener(messageWindow.Handle);
            trayIcon.Visible = false;
            messageWindow.DestroyHandle();
            Application.Exit();
        }

        private class HotkeyMessageWindow : NativeWindow
        {
            private const int WM_CLIPBOARDUPDATE = 0x031D;
            private ClipboardImageSaverApp app;
            public HotkeyMessageWindow(ClipboardImageSaverApp app) { this.app = app; CreateHandle(new CreateParams()); }
            protected override void WndProc(ref Message m)
            {
                if (m.Msg == WM_CLIPBOARDUPDATE) { app.OnClipboardChanged(); }
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
