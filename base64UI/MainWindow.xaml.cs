using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace base64UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        System.Windows.Forms.NotifyIcon _notifyIcon;

        IntPtr nextClipboardViewer;
        private static string GUID = string.Empty;

        public MainWindow()
        {
            Clipboard.SetText(string.Empty); //clean clipboard
            InitializeComponent();

            progressBar.Visibility = System.Windows.Visibility.Collapsed;

            _notifyIcon = new System.Windows.Forms.NotifyIcon();
            _notifyIcon.BalloonTipTitle = "Base64 tool!";
            _notifyIcon.BalloonTipText = "Application is running";
            _notifyIcon.Visible = true;
            _notifyIcon.Icon = Properties.Resources.img64;
            _notifyIcon.Click += ClickNotify;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            // Get this window's handle
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            // Change the extended window style to not show a window icon
            int extendedStyle = Win32.GetWindowLong(hwnd, Win32.GWL_EXSTYLE);
            Win32.SetWindowLong(hwnd, Win32.GWL_EXSTYLE, extendedStyle | Win32.WS_EX_DLGMODALFRAME);
            // Update the window's non-client area to reflect the changes
            Win32.SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0, Win32.SWP_NOMOVE | Win32.SWP_NOSIZE | Win32.SWP_NOZORDER | Win32.SWP_FRAMECHANGED);

            WindowInteropHelper wih = new WindowInteropHelper(this);
            hWndSource = HwndSource.FromHwnd(wih.Handle);

            hWndSource.AddHook(this.WinProc);   // start processing window messages
            hWndNextViewer = Win32.SetClipboardViewer(hWndSource.Handle);   // set this window as a viewer
        }

        public void ClickNotify(object sender, EventArgs e)
        {
            //_notifyIcon.Visible = false;
            this.Visibility = System.Windows.Visibility.Visible;
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                new Action(delegate()
                {
                    this.WindowState = WindowState.Normal;
                    this.Activate();
                })
            );

        }

        private bool ConvertAsync(DragEventArgs e, bool isChecked)
        {
            System.Collections.Concurrent.ConcurrentBag<string> list = new System.Collections.Concurrent.ConcurrentBag<string>();
            object sync = new object();

            // Note that you can have more than one file.
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            Parallel.For(0, files.Length, i =>
            {
                var filename = files[i];

                if (filename.Contains(".64Part."))
                {
                    var tempname = filename.Substring(0, filename.LastIndexOf("."));

                    lock (sync)
                    {
                        if (list.Contains(tempname))
                        {
                            return;
                        }
                        list.Add(tempname);
                    }

                    var temppart = tempname + ".part{0}";
                    base64.Utils.JoinFiles(temppart, tempname);
                    base64.Utils.FromBase64(tempname, tempname.Replace(".to64.64Part", ""));
                    System.IO.File.Delete(tempname);
                }
                else if (filename.Contains(".to64"))
                {
                    base64.Utils.FromBase64(filename, filename.Replace(".to64", string.Empty));
                }
                else
                {
                    var filenameAux = files[i] + ".to64";
                    var number = base64.Utils.ToBase64(files[i], filenameAux);

                    if (isChecked)
                    {
                        GUID = Guid.NewGuid().ToString();

                        if (number > 1)
                        {
                            Dispatcher.BeginInvoke(new Action(delegate
                                {
                                    progressBar.Visibility = System.Windows.Visibility.Visible;
                                }));
                            
                            for (var n = 0; n < number; n++)
                            {
                                var part = filenameAux + ".64Part.part" + n;
                                var partAux = part + "aux";
                                base64.Utils.ToBase64(part, partAux, 50000000);

                                var msg = String.Format("{0}\n{1}\n{2}\n{3}", System.IO.File.ReadAllText(partAux), "base64clipboard", part.Substring(files[i].LastIndexOf("\\") + 1), GUID);

                                Dispatcher.Invoke(new Action(delegate
                                {
                                    Clipboard.SetText(msg);
                                }));
                                System.IO.File.Delete(part);
                                System.IO.File.Delete(partAux);

                                string msgTest = string.Format("Chunk {0} of {1} should be in the destination in the next 50 seconds", n + 1, number);
                                this.Dispatcher.BeginInvoke(new Action(delegate
                                {
                                    _notifyIcon.ShowBalloonTip(50, "Base64 Tool", msgTest, System.Windows.Forms.ToolTipIcon.Info);
                                    progressBar.Value = ((double)(n + 1) /(double)number )*100;
                                }));
                                System.Threading.Thread.Sleep(1000 * 50);
                            }

                            Dispatcher.BeginInvoke(new Action(delegate
                            {
                                progressBar.Visibility = System.Windows.Visibility.Collapsed;
                            }));
                        }
                        else
                        {
                            Dispatcher.Invoke(new Action(delegate
                            {
                                Clipboard.SetText(String.Format("{0}\n{1}\n{2}\n{3}", System.IO.File.ReadAllText(filenameAux), "base64clipboard", files[i].Substring(files[i].LastIndexOf("\\") + 1), GUID));
                            }));
                            System.IO.File.Delete(filenameAux);
                        }
                    }
                }
            });

            Dispatcher.Invoke(new Action(delegate
            {
                _notifyIcon.ShowBalloonTip(50, "Base64 Tool", "Operation Completed!", System.Windows.Forms.ToolTipIcon.Info);

            }));

            return true;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var isChecked = this.checkBox2Clip.IsChecked == true;

                Task.Factory.StartNew(() => ConvertAsync(e, isChecked));
            }
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == System.Windows.WindowState.Minimized)
            {
                Visibility = System.Windows.Visibility.Hidden;
                //_notifyIcon.Visible = true;
                _notifyIcon.ShowBalloonTip(50, "Base64 Tool", "Base64 tool is still running!", System.Windows.Forms.ToolTipIcon.Info);
            }
        }

        private void FromClipboardText()
        {
            //return;
            string text = null;

            bool repeat = false;
            do
            {
                repeat = false;
                try
                {
                    text = Clipboard.GetText();
                }
                catch (Exception e)
                {
                    if (e.Message == "OpenClipboard Failed (Exception from HRESULT: 0x800401D0 (CLIPBRD_E_CANT_OPEN))")
                    {
                        repeat = true;
                        System.Threading.Thread.Sleep(1000);
                    }
                }
            }
            while (repeat);

            if (text == null)
                return;

            var textArray = text.Split('\n');

            string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string name = string.Empty;

            if (textArray.Length > 3 && textArray[1] == "base64clipboard")
            {
                name = textArray[2];
                var msg = string.Format("A base64file was received!\nFile received: {0}", name);
                this.Dispatcher.BeginInvoke(new Action(delegate
                {
                    _notifyIcon.ShowBalloonTip(50, "Base64 Tool", msg, System.Windows.Forms.ToolTipIcon.Info);

                }));

                if (GUID.Equals(textArray[3]))
                    return;


                // After retrive Data Clear Clipboard
                Clipboard.SetText(string.Empty);
            }
            else
                return;

            try
            {
                Task.Factory.StartNew(new Action(() =>
                {
                    base64.Utils.FromTextBase64(textArray[0], System.IO.Path.Combine(path, name));
                }));
            }
            catch { }
        }

        protected override void OnClosed(EventArgs e)
        {
            this.CloseCBViewer();
            base.OnClosed(e);
        }

        #region Private fields

        /// <summary>
        /// Next clipboard viewer window 
        /// </summary>
        private IntPtr hWndNextViewer;

        /// <summary>
        /// The <see cref="HwndSource"/> for this window.
        /// </summary>
        private HwndSource hWndSource;

        private bool isViewing;

        #endregion

        #region Clipboard viewer related methods

        private void InitCBViewer()
        {
            WindowInteropHelper wih = new WindowInteropHelper(this);
            hWndSource = HwndSource.FromHwnd(wih.Handle);

            hWndSource.AddHook(this.WinProc);   // start processing window messages
            hWndNextViewer = Win32.SetClipboardViewer(hWndSource.Handle);   // set this window as a viewer
            isViewing = true;
        }

        private void CloseCBViewer()
        {
            // remove this window from the clipboard viewer chain
            Win32.ChangeClipboardChain(hWndSource.Handle, hWndNextViewer);
        }

        private IntPtr WinProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case Win32.WM_CHANGECBCHAIN:
                    if (wParam == hWndNextViewer)
                    {
                        // clipboard viewer chain changed, need to fix it.
                        hWndNextViewer = lParam;
                    }
                    else if (hWndNextViewer != IntPtr.Zero)
                    {
                        // pass the message to the next viewer.
                        Win32.SendMessage(hWndNextViewer, msg, wParam, lParam);
                    }
                    break;

                case Win32.WM_DRAWCLIPBOARD:
                    // clipboard content changed
                    ClipboardChanged();
                    // pass the message to the next viewer.
                    Win32.SendMessage(hWndNextViewer, msg, wParam, lParam);
                    break;
            }

            return IntPtr.Zero;
        }

        private void ClipboardChanged()
        {
            if (Clipboard.ContainsText())
            {
                FromClipboardText();
            }
        }

        #endregion

    }
}
