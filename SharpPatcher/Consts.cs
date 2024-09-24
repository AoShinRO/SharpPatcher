using GRF.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using static SharpPatcher.Config;
using Application = System.Windows.Application;
using ContextMenu = System.Windows.Forms.ContextMenu;
using NotifyIcon = System.Windows.Forms.NotifyIcon;

namespace SharpPatcher
{
    public static class Consts
    {
        public static MainWindow WorkingWindow = (MainWindow)Application.Current.MainWindow;

        public enum HostCMD
        {
            NONE,
            GRF,
            PATH
        };

        public struct PatcherQueueArquive
        {
            public string Path { get; set; }
            public HostCMD Type { get; set; }
            public bool IsRemove { get; set; }
        }

        public static NotifyIcon _notifyIcon { set; get; } = new NotifyIcon();
        public static ContextMenu trayMenu { set; get; } = new ContextMenu();

        public static Thickness SideBackgroundMargin { set; get; }
        public static Thickness playMargin { set; get; }
        public static Thickness pause1Margin { set; get; }
        public static Thickness NoticesMargin { set; get; }
        public static Thickness PatchNotesMargin { set; get; }
        public static Thickness optionsMargin { set; get; }
        public static Thickness closebtnMargin { set; get; }
        public static Thickness SideMenuBtnSlideMargin { set; get; }
        public static Thickness confGridMargin { set; get; }
        public static Thickness LoginMargin { set; get; }
        public static Thickness WindowedBtnMargin { set; get; }

        public static readonly HashSet<int> downloadedIndices = new HashSet<int>();

        public static readonly HashSet<string> Urls = new HashSet<string>();

        public static readonly HashSet<string> AllowedDLLS = new HashSet<string>();

        public static readonly HashSet<string> UnAllowedProcess = new HashSet<string>();

        public static int web_Counter { set; get; }
        public static int patchcount { set; get; }

        public static bool isDragging { set; get; } = false;
        public static Point startDragPoint { set; get; }

        public static readonly string programDirectory = Directory.GetCurrentDirectory();

        public static readonly string DownloadFolderName = "Downloads";

        public static List<PatcherQueueArquive> PatcherQueue { set; get; } = new List<PatcherQueueArquive>();

        public static Uri BG_IMG = new Uri(programDirectory + "/Resources/Background/background.png");

        public static Uri BG_VIDEO = new Uri(programDirectory + "/Resources/Background/bg.mp4");

        public static GrfHolder MainGRF;

        #region LoadingConfs

        public static bool LoadWebpages()
        {
            if (!File.Exists(weburlPath))
                return false;

            if (File.ReadLines(weburlPath).Any())
            {
                try
                {
                    string[] urls = File.ReadAllLines(weburlPath);

                    foreach (string url in urls)
                    {
                        Urls.Add(url);
                    }
                }
                catch
                {
                    return false;
                }
            }
            else
            {
                return false;
            }

            return true;
        }

        #endregion LoadingConfs

        public static void InitializeNotifyIcon()
        {
            _notifyIcon.Icon = Properties.Resources.icon;
            _notifyIcon.Visible = false;
            _notifyIcon.Text = "SharpPatcher";

            _notifyIcon.MouseDoubleClick += (sender, e) =>
            {
                Application.Current.MainWindow?.Show();
                _notifyIcon.Visible = false;
            };

            trayMenu.MenuItems.Add("Restore", (sender, e) =>
            {
                Application.Current.MainWindow?.Show();
                _notifyIcon.Visible = false;
            });

            trayMenu.MenuItems.Add("Close", (sender, e) =>
            {
                Application.Current.Dispatcher?.InvokeShutdown();
            });

            _notifyIcon.ContextMenu = trayMenu;
        }

        public static void Do_SysTray()
        {
            Application.Current.MainWindow?.Hide();
            _notifyIcon.Visible = true;
        }

        public static void ShowMsgError(string text)
        {
            WorkingWindow?.Dispatcher.Invoke(() =>
            {
                MessageBox.Show($"{text}");
            });
        }
    }
}