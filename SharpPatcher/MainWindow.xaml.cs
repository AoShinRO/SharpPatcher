using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using static SharpPatcher.AnimationHelper;
using static SharpPatcher.Config;
using static SharpPatcher.Consts;
using static SharpPatcher.DownloadHelper;
using static SharpPatcher.ProcessHelper;

namespace SharpPatcher
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            StartingProcess();
        }

        public void StartingProcess()
        {
            GRF_Set_Temp();
            GetWindowPosition();

            StartNavigator();
            CollapseWindowOnStart();
            InitializeNotifyIcon();

            LoadDownloadedIndex();
            DownloadPatchNotes();
        }

        public void StartNavigator()
        {
            try
            {
                if (LoadWebpages() == true)
                {
                    if (Urls.FirstOrDefault() != string.Empty)
                        webBrowser1.Navigate(Urls.FirstOrDefault());
                    else
                        MessageBox.Show($"Fail to load URLs");
                }
                else
                    MessageBox.Show($"Fail to load URLs");
            }
            catch
            {
                MessageBox.Show($"Fail to load URLs");
            }
        }

        public void CollapseWindowOnStart()
        {
            Login.IsEnabled = false;
            Login.Visibility = Visibility.Collapsed;
            background.Visibility = Visibility.Collapsed;
            play.Visibility = Visibility.Collapsed;
        }

        public void GetWindowPosition()
        {
            SideBackgroundMargin = SideBackground.Margin;
            playMargin = play.Margin;
            pause1Margin = pause1.Margin;
            NoticesMargin = Notices.Margin;
            PatchNotesMargin = PatchNotes.Margin;
            optionsMargin = options.Margin;
            closebtnMargin = closebtn.Margin;
            SideMenuBtnSlideMargin = SideMenuBtnSlide.Margin;
            confGridMargin = confGrid.Margin;
            LoginMargin = Login.Margin;
            WindowedBtnMargin = WindowedBtn.Margin;
        }

        private void Setup_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Process.Start(Properties.Settings.Default.config);
            }
            catch
            {
                MessageBox.Show("The configurator file '" + Properties.Settings.Default.config + "' was not found in your Ragnarok folder, check if the antivirus has deleted it.");
            }
            finally
            {
                ConfigBtn_MouseDown(sender, e);
            }
        }

        private void ReDownload_MouseDown(object sender, MouseButtonEventArgs e)
        {
            switch (btn1.Visibility)
            {
                case Visibility.Visible:
                    ConfigBtn_MouseDown(sender, e);
                    if (File.Exists(DownloadedPatches))
                        File.Delete(DownloadedPatches);
                    patchcount = 0;
                    downloadedIndices.Clear();
                    Login.IsEnabled = false;
                    Login.Visibility = Visibility.Collapsed;
                    DownloadPatchNotes();
                    break;

                default: MessageBox.Show("Please, wait finish download."); break;
            }
        }

        #region ProcessStart

        private void BT_Click(object sender, MouseButtonEventArgs e) => Do_StartClient();

        private void OnKeyDownHandler(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && Login.IsEnabled)
            {
                Do_StartClient();
            }
        }

        #endregion ProcessStart

        #region EndProgram

        private void Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Properties.Settings.Default.lembrarid = chkid.IsChecked.Value;
            Properties.Settings.Default.lembrarpass = chkpass.IsChecked.Value;
            Properties.Settings.Default.usuario = Properties.Settings.Default.lembrarid ? txtUserId.Text : string.Empty;
            Properties.Settings.Default.senha = Properties.Settings.Default.lembrarpass ? txtUser_Pass.Password : string.Empty;
            Properties.Settings.Default.Save();
            Close();
        }

        #endregion EndProgram

        #region DragWindow

        private void WindowHeader_MouseUp(object sender, MouseButtonEventArgs e) => isDragging = false;

        private void WindowHeader_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                isDragging = true;
                startDragPoint = e.GetPosition(this);
            }
        }

        private void WindowHeader_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging && e.LeftButton == MouseButtonState.Pressed)
            {
                Point currentMousePoint = e.GetPosition(this);
                Left += currentMousePoint.X - startDragPoint.X;
                Top += currentMousePoint.Y - startDragPoint.Y;
            }
            else
            {
                isDragging = false;
            }
        }

        #endregion DragWindow

        #region WebCarroucel

        private void Notices_MouseDown(object sender, MouseEventArgs e)
        {
            if (web_Counter < Urls.Count() - 1)
                web_Counter++;
            else
                web_Counter = 0;

            webBrowser1?.Navigate(Urls?.ElementAtOrDefault(web_Counter));
        }

        private void PatchNotes_MouseDown(object sender, MouseEventArgs e)
        {
            if (web_Counter > 0)
                web_Counter--;
            else
                web_Counter = Urls.Count() - 1;

            webBrowser1?.Navigate(Urls?.ElementAtOrDefault(web_Counter));
        }

        #endregion WebCarroucel

        #region SystemTray

        private void Windowed_MouseDown(object sender, MouseButtonEventArgs e) => Do_SysTray();

        #endregion SystemTray

        #region Background

        private void MediaElement_Loaded_1(object sender, RoutedEventArgs e) =>
            background.Source = BG_IMG;

        #region Video

        private void Pause1_MouseDown(object sender, MouseButtonEventArgs e)
        {
            videoBG.Stop();
            videoBG.Visibility = Visibility.Collapsed;
            background.Visibility = Visibility.Visible;

            pause1.Visibility = Visibility.Collapsed;
            play.Visibility = Visibility.Visible;
        }

        private void Play_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Video_Play();
            background.Visibility = Visibility.Collapsed;

            play.Visibility = Visibility.Collapsed;
            pause1.Visibility = Visibility.Visible;
        }

        private void CheckBox_Checked_1(object sender, RoutedEventArgs e)
        {
            videoBG.Stop();
            videoBG.Visibility = Visibility.Collapsed;
            background.Visibility = Visibility.Visible;
        }

        private void CheckBox_Unchecked_1(object sender, RoutedEventArgs e)
        {
            videoBG.Play();
            videoBG.Visibility = Visibility.Visible;
            background.Visibility = Visibility.Collapsed;
        }

        private void MediaElement_Loaded(object sender, RoutedEventArgs e)
        {
            videoBG.Source = BG_VIDEO;
            videoBG.LoadedBehavior = MediaState.Manual;

            Video_Play();
        }

        private void VideoBG_MediaEnded(object sender, RoutedEventArgs e) => Video_Play();

        private void Video_Play()
        {
            videoBG.Volume = 0.5;
            videoBG.Position = TimeSpan.FromSeconds(0.1);
            videoBG.Play();
            videoBG.Visibility = Visibility.Visible;
        }

        #endregion Video

        #endregion Background

        #region Blank

        private void discord_enter(object sender, MouseEventArgs e)
        {
        }

        private void discord_leave(object sender, MouseEventArgs e)
        {
        }

        //facebook enter mouse
        private void facebook_enter(object sender, MouseEventArgs e)
        {
        }

        //facebook leave mouse
        private void facebook_leave(object sender, MouseEventArgs e)
        {
        }

        //news enter
        private void news_enter(object sender, MouseEventArgs e)
        {
        }

        private void news_leave(object sender, MouseEventArgs e)
        {
        }

        private void register_enter(object sender, MouseEventArgs e)
        {
        }

        private void register_leave(object sender, MouseEventArgs e)
        {
        }

        #endregion Blank

        #region Animation

        private void ConfigBtn_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                switch (confGrid.Visibility)
                {
                    case Visibility.Visible:
                        ThicknessAnimation marginAnimation = F_Thickness_Animate(
                            new Thickness(confGridMargin.Left, confGridMargin.Top - 100, confGridMargin.Right, confGridMargin.Bottom), confGridMargin, 0.5);
                        marginAnimation.Completed += (s, _) =>
                        {
                            if (confGrid.Visibility == Visibility.Visible)
                                confGrid.Visibility = Visibility.Collapsed;
                        };
                        confGrid.BeginAnimation(MarginProperty, marginAnimation);
                        break;

                    default:
                        confGrid.Visibility = Visibility.Visible;
                        confGrid.BeginAnimation(MarginProperty,
                            F_Thickness_Animate(confGridMargin,
                            new Thickness(confGridMargin.Left, confGridMargin.Top - 100, confGridMargin.Right, confGridMargin.Bottom), 0.5));

                        break;
                }
            }
            catch (Exception)
            {
            }
        }

        #region Links

        private void discord_btn_(object sender, MouseButtonEventArgs e) => Process.Start(DiscordLink);

        private void facebook_link(object sender, MouseButtonEventArgs e) => Process.Start(FacebookLink);

        private void news_link(object sender, MouseButtonEventArgs e) => Process.Start(NewsLink);

        private void register_link(object sender, MouseButtonEventArgs e) => Process.Start(RegisterLink);

        #endregion Links

        private void SideMenu_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                switch (SideMenuBtnSlide.Visibility)
                {
                    case Visibility.Visible:
                        ThicknessAnimation marginAnimation = F_Thickness_Animate(new Thickness(0, 0, SideMenuBtnSlideMargin.Right - 744, 0), SideMenuBtnSlideMargin, 0.5);
                        marginAnimation.Completed += (s, _) =>
                        {
                            if (SideMenuBtnSlide.Visibility == Visibility.Visible)
                                SideMenuBtnSlide.Visibility = Visibility.Collapsed;
                        };
                        SideMenuBtnSlide.BeginAnimation(MarginProperty, marginAnimation);
                        break;

                    default:
                        SideMenuBtnSlide.Visibility = Visibility.Visible;
                        SideMenuBtnSlide.BeginAnimation(MarginProperty, F_Thickness_Animate(SideMenuBtnSlideMargin, new Thickness(0, 0, SideMenuBtnSlideMargin.Right - 744, 0), 0.5));

                        break;
                }
            }
            catch (Exception)
            {
            }
        }

        private void Label_MouseEnter(object sender, MouseEventArgs e) => AnimateButton(Login, LoginMargin, true);

        private void Label_MouseLeave(object sender, MouseEventArgs e) => AnimateButton(Login, LoginMargin);

        private void Play_MouseEnter(object sender, MouseEventArgs e) => AnimateButton(play, playMargin, true);

        private void Play_MouseLeave(object sender, MouseEventArgs e) => AnimateButton(play, playMargin);

        private void Pause_MouseEnter(object sender, MouseEventArgs e) => AnimateButton(pause1, pause1Margin, true);

        private void Pause_MouseLeave(object sender, MouseEventArgs e) => AnimateButton(pause1, pause1Margin);

        private void Notices_MouseEnter(object sender, MouseEventArgs e) => AnimateButton(Notices, NoticesMargin, true);

        private void Notices_MouseLeave(object sender, MouseEventArgs e) => AnimateButton(Notices, NoticesMargin);

        private void PatchNotes_MouseEnter(object sender, MouseEventArgs e) => AnimateButton(PatchNotes, PatchNotesMargin, true);

        private void PatchNotes_MouseLeave(object sender, MouseEventArgs e) => AnimateButton(PatchNotes, PatchNotesMargin);

        private void Options_MouseEnter(object sender, MouseEventArgs e) => AnimateButton(options, optionsMargin, true);

        private void Options_MouseLeave(object sender, MouseEventArgs e) => AnimateButton(options, optionsMargin);

        private void Closebtn_MouseEnter(object sender, MouseEventArgs e) => AnimateButton(closebtn, closebtnMargin, true);

        private void Closebtn_MouseLeave(object sender, MouseEventArgs e) => AnimateButton(closebtn, closebtnMargin);

        private void SideBackground_MouseEnter(object sender, MouseEventArgs e) => AnimateButton(SideBackground, SideBackgroundMargin, true);

        private void SideBackground_MouseLeave(object sender, MouseEventArgs e) => AnimateButton(SideBackground, SideBackgroundMargin);

        private void Windowed_MouseEnter(object sender, MouseEventArgs e) => AnimateButton(WindowedBtn, WindowedBtnMargin, true);

        private void Windowed_MouseLeave(object sender, MouseEventArgs e) => AnimateButton(WindowedBtn, WindowedBtnMargin);

        #endregion Animation
    }
}