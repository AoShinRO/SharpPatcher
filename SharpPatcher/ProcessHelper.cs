using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using static SharpPatcher.AntiCheatHelper;
using static SharpPatcher.Consts;

namespace SharpPatcher
{
    public static class ProcessHelper
    {
        public static async void Do_StartClient()
        {
            var hexed = Properties.Settings.Default.hexedFile;

            try
            {
                switch (File.Exists(hexed))
                {
                    case true:
                        string User = WorkingWindow.txtUserId.Text,
                           Pass = WorkingWindow.txtUser_Pass.Password;

                        // Grava as configurações no arquivo de aplicação e inicializa o cliente do jogo com as configurações.

                        Properties.Settings.Default.lembrarid = WorkingWindow.chkid.IsChecked.Value;
                        Properties.Settings.Default.lembrarpass = WorkingWindow.chkpass.IsChecked.Value;
                        Properties.Settings.Default.usuario = Properties.Settings.Default.lembrarid ? WorkingWindow.txtUserId.Text : string.Empty;
                        Properties.Settings.Default.senha = Properties.Settings.Default.lembrarpass ? WorkingWindow.txtUser_Pass.Password : string.Empty;
                        Properties.Settings.Default.Save();

                        WorkingWindow.Hide();

                        if (!WorkingWindow.chk_anime.IsChecked.Value)
                            WorkingWindow.videoBG.Pause();

                        // Inicializa o hexed do usuário com as informações de usuário e senha.
                        Process prc = new Process();
                        prc.EnableRaisingEvents = true;
                        prc.Exited += prc_Exited;
                        prc.StartInfo.FileName = hexed;
                        prc.StartInfo.Arguments = string.Format("-t:{0} {1} 1sak1", Pass, User);
                        AntiCheater wnd = new AntiCheater();
                        wnd.Show();
                        if (!DllsCheck() || HackingCheck())
                            return;
                        await Task.Delay(3000);
                        prc.Start();
                        break;

                    default:
                        break;
                }
            }
            catch
            {
                WorkingWindow.Show();
                ShowMsgError("The game launcher '" + hexed + "' was not found, check if your Anti-Virus deleted it or contact our support team.");
            }
            finally
            {
                Do_SysTray();
            }
        }

        public static void prc_Exited(object sender, EventArgs e)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                WorkingWindow?.Show();
                _notifyIcon.Visible = false;
            });
        }
    }
}