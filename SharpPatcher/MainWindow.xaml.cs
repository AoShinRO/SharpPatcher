using GRF.Core;
using SharpCompress.Archives;
using SharpCompress.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using ContextMenu = System.Windows.Forms.ContextMenu;
using NotifyIcon = System.Windows.Forms.NotifyIcon;

namespace SharpPatcher
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Application.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            GRF_Set_Temp();
            GetWindowPosition();
            LoadWebpages();
            CollapseWindowOnStart();
            InitializeNotifyIcon();

            LoadDownloadedIndices();
            DownloadPatchNotes();
        }

        #region Confs

        public string patchUrl = "http://127.0.0.1/kpatcher";
        public string patchArchive = "patch_notes.txt"; //srv side archive
        public string mainFilePath = "server.grf";
        public string indicesFilePath = "patch.ini"; //client side archive
        public string weburlPath = "links.ini"; //client side archive

        #endregion Confs

        #region structures&vars

        public enum HostCMD
        {
            NONE,
            GRF,
            PATH
        };

        private struct DownloadedArquive
        {
            public string Arquive { get; set; }
            public HostCMD Type { get; set; }
        }

        private NotifyIcon _notifyIcon;
        private readonly ContextMenu trayMenu = new ContextMenu();

        public Thickness SideBackgroundMargin;
        public Thickness playMargin;
        public Thickness pause1Margin;
        public Thickness NoticesMargin;
        public Thickness PatchNotesMargin;
        public Thickness optionsMargin;
        public Thickness closebtnMargin;
        public Thickness SideMenuBtnSlideMargin;
        public Thickness confGridMargin;
        public Thickness LoginMargin;
        public Thickness WindowedBtnMargin;

        private readonly HashSet<int> downloadedIndices = new HashSet<int>();
        private readonly HashSet<string> Urls = new HashSet<string>();
        private readonly HashSet<string> AllowedDLLS = new HashSet<string>();
        private readonly HashSet<string> UnAllowedProcess = new HashSet<string>();

        public int web_Counter;
        public int patchcount;

        private bool isDragging = false;
        private Point startDragPoint;

        public string programDirectory = Directory.GetCurrentDirectory();//Environment.CurrentDirectory;//;//Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);//Assembly.GetExecutingAssembly().GetName().Name.Substring(Assembly.GetExecutingAssembly().GetName().Name.Length -  );//AppDomain.CurrentDomain.BaseDirectory;

        #endregion structures&vars

        #region AssemblyResolve

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            string assemblyName = new AssemblyName(args.Name).Name;
            string dllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "References", $"{assemblyName}.dll");
            return Assembly.LoadFile(dllPath);
        }

        private void Current_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(e.Exception.ToString());
            Application.Current.Shutdown();
            e.Handled = true;
        }

        #endregion AssemblyResolve

        #region LoadingConfs

        private void LoadWebpages()
        {
            try
            {
                if (!File.Exists(weburlPath))
                    return;

                string[] urls = File.ReadAllLines(weburlPath);

                foreach (string url in urls)
                {
                    Urls.Add(url);
                }

                // Navigate to the specified website
                webBrowser1.Navigate(Urls?.FirstOrDefault());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fail to add URL: {ex.Message}");
            }
        }

        private void LoadDownloadedIndices()
        {
            try
            {
                patchcount = 0;
                string[] indices = File.ReadAllLines(indicesFilePath);

                foreach (string index in indices)
                {
                    if (!int.TryParse(index, out int downloadedIndex))
                        continue;
                    downloadedIndices.Add(downloadedIndex);
                    patchcount++;
                }
            }
            catch (Exception)
            {
            }
        }

        private void GetWindowPosition()
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

        private void CollapseWindowOnStart()
        {
            Login.IsEnabled = false;
            Login.Visibility = Visibility.Collapsed;
            background.Visibility = Visibility.Collapsed;
            play.Visibility = Visibility.Collapsed;
        }

        #endregion LoadingConfs

        private List<DownloadedArquive> DownloadedList = new List<DownloadedArquive>();

        public void AddToProcessQueue(string arquive, HostCMD type)
        {
            DownloadedArquive NewArchive = new DownloadedArquive
            {
                Arquive = arquive,
                Type = type
            };
            DownloadedList.Add(NewArchive);
        }

        private async Task ProcessesPatchs()
        {
            try
            {
                int i = 0;
                progressArchives.Text = $"{i}/{DownloadedList.Count()}";
                foreach (var arquive in DownloadedList)
                {
                    await ProcessDownloadedFile(arquive.Arquive, arquive.Type);
                    i++;
                    progressArchives.Text = $"{i}/{DownloadedList.Count()}";
                }
                DownloadedList.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fail processing downloaded arquive: {ex.Message}");
            }
        }

        #region DownloadFuncs

        private void SaveDownloadedIndex()
        {
            try
            {
                string[] indicesArray = downloadedIndices.Select(index => index.ToString()).ToArray();

                File.WriteAllLines(indicesFilePath, indicesArray);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving downloaded indices: {ex.Message}");
            }
        }

        private HostCMD ParsePatchCMD(string cmd)
        {
            int cmdtype;
            if (int.TryParse(cmd, out cmdtype))
            {
                switch (cmdtype)
                {
                    case 1: return HostCMD.GRF;
                    case 2: return HostCMD.PATH;
                    default: return HostCMD.NONE;
                }
            }
            else
            {
                switch (cmd.Trim().ToLowerInvariant())
                {
                    case "grf": return HostCMD.GRF;
                    case "path": return HostCMD.PATH;
                    default: return HostCMD.NONE;
                }
            }
        }

        private async void DownloadPatchNotes()
        {
            try
            {
                // Replace "patchUrl" with the actual URL where your patch notes are hosted
                string Patch = $"{patchUrl}/{patchArchive}";

                using (WebClient client = new WebClient())
                {
                    client.Encoding = Encoding.UTF8;
                    string patchNotesContent = await client.DownloadStringTaskAsync(Patch);

                    string[] patchNotesLines = patchNotesContent.Split('\n');
                    Login.Visibility = Visibility.Collapsed;

                    if (patchcount < patchNotesLines.Length - patchcount)
                        ProgressBarGrid.Visibility = Visibility.Visible;

                    foreach (string line in patchNotesLines)
                    {
                        // Skip empty lines
                        if (string.IsNullOrWhiteSpace(line))
                            continue;

                        // Assuming each line contains index and file name separated by space
                        string[] fileInfo = line.Split(' ');

                        // fileInfo[0] contains the index, fileInfo[1] contains the file name
                        int fileIndex;
                        switch (int.TryParse(fileInfo[0], out fileIndex))
                        {
                            case true:
                                if (downloadedIndices.Contains(fileIndex))
                                {
                                    // Skip already downloaded files
                                    continue;
                                }
                                progressArchives.Text = $"{fileIndex - patchcount}/{patchNotesLines.Length - patchcount}";

                                string fileName = fileInfo[1];
                                HostCMD type = HostCMD.NONE;
                                if (fileInfo.Length >= 3)
                                    type = ParsePatchCMD(fileInfo[2]);

                                // Construct the download URL based on the provided information
                                string fileUrl = $"{patchUrl}/data/{fileName}";

                                // Create a method to download the file based on the constructed URL
                                await DownloadFileWithRetry(fileUrl, fileName, type);

                                downloadedIndices.Add(fileIndex);
                                break;

                            default:
                                // Handle the case where the index is not a valid integer
                                MessageBox.Show($"Invalid index in line: {line}");
                                break;
                        }
                    }
                }
                //await ProcessesPatchs();
                SaveDownloadedIndex();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error downloading patch notes: {ex.Message}");
            }
            finally
            {
                // Reset the progress bar value after completion
                UpdateUserInfo("Game is ready to Start!");
                downloadProgressBar.Value = 100;
                Login.Visibility = Visibility.Visible;
                Login.IsEnabled = true;
                InitializeLoginAnimation();
            }
        }

        private async Task DownloadFileWithRetry(string fileUrl, string fileName, HostCMD type, int maxRetries = 3)
        {
            int retries = 0;

            while (retries < maxRetries)
            {
                try
                {
                    await DownloadFile(fileUrl, fileName, type);
                    return; // Break out of the loop if download is successful
                }
                catch (Exception ex)
                {
                    // Log or handle the exception as needed
                    retries++;
                    MessageBox.Show($"Error downloading file '{fileName}': '{ex.Message}'.");
                    await Task.Delay(3000); // Wait for a short period before retrying
                }
            }

            // If all retries fail, handle the error accordingly
            MessageBox.Show($"Error downloading file '{fileName}': Maximum retries exceeded.");
        }

        private async Task DownloadFile(string fileUrl, string fileName, HostCMD type)
        {
            try
            {
                using (WebClient client = new WebClient())
                {
                    // Specify the local path where you want to save the downloaded file
                    string localFilePath = $"./{fileName.Trim()}";

                    // Report progress using the ProgressChanged event
                    client.DownloadProgressChanged += (sender, e) =>
                    {
                        // Update the progress text or any other UI element
                        downloadProgressBar.Value = e.ProgressPercentage;
                        UpdateUserInfo($"Downloading: {fileName.Trim()} {e.ProgressPercentage}%");
                    };
                    // Download the file asynchronously
                    await client.DownloadFileTaskAsync(fileUrl, localFilePath);

                    // Process the downloaded file as needed (e.g., move to the application directory)
                    // MoveFileToApplicationDirectory(localFilePath);
                    // Process the downloaded file based on its extension
                    await ProcessDownloadedFile(localFilePath, type);
                    //AddToProcessQueue(localFilePath, type);
                }
            }
            catch (Exception exd)
            { //Exception ex
                //if cant find on host, try delete it from path
                try
                {
                    switch (type)
                    {
                        case HostCMD.GRF:
                            string FullFilePath = Path.Combine(programDirectory, fileName);
                            string relativePath = FullFilePath.Substring(programDirectory.Length);
                            UpdateUserInfo($"Deleting {relativePath}");
                            using (GrfHolder grf = new GrfHolder(@mainFilePath))
                            {
                                grf.Commands.RemoveFile(@relativePath);
                                grf.QuickSave();
                                grf.Close();
                            }
                            break;

                        case HostCMD.PATH:
                            UpdateUserInfo($"Deleting {fileName}");
                            File.Delete(fileName);
                            break;
                    }
                    GRF_Clean_Temp();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error processing host command to file '{fileName}': {ex.Message} : {exd.Message}");
                }
            }
        }

        #endregion DownloadFuncs

        #region GRF_DLL

        public static void GRF_Clean_Temp() => GRF.System.TemporaryFilesManager.ClearTemporaryFiles();

        public void GRF_Set_Temp() => GRF.System.Settings.TempPath = $"{programDirectory}/tmp";

        #endregion GRF_DLL

        #region ProcessDownloadedPatches

        private async Task ProcessDownloadedFile(string filePath, HostCMD type)
        {
            try
            {
                UpdateUserInfo($"Processing {filePath.Substring(2)}");
                string fileExtension = Path.GetExtension(filePath);
                switch (fileExtension.ToLowerInvariant())
                {
                    case ".rar":
                    case ".zip":
                        await ExtractRarFileAsync(filePath, type);
                        UpdateUserInfo($"Deleting {filePath.Substring(2)}");
                        File.Delete(filePath);
                        break;

                    case ".gpf":
                    case ".grf":
                        await ExtractGpfFileAsync(filePath, type);
                        // Lida com outros tipos de arquivo ou executa processamento adicional, se necessário
                        if (type != HostCMD.NONE)
                        {
                            UpdateUserInfo($"Deleting {filePath.Substring(2)}");
                            File.Delete(filePath);
                        }
                        break;

                    case ".thor":
                    case ".rgz":
                        await DeleteThorFileAsync(filePath, type);
                        UpdateUserInfo($"Deleting {filePath.Substring(2)}");
                        File.Delete(filePath);
                        break;

                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error processing downloaded file '{filePath}': {ex.Message}");
            }
        }

        private async Task ExtractRarFileAsync(string FilePath, HostCMD type) => await Task.Run(() => ExtractRarFile(FilePath, type));

        private async Task ExtractGpfFileAsync(string FilePath, HostCMD type) => await Task.Run(() => ExtractGpfFile(FilePath, type));

        private async Task DeleteThorFileAsync(string FilePath, HostCMD type) => await Task.Run(() => DeleteThorFile(FilePath, type));

        private void ExtractRarFile(string rarFilePath, HostCMD type)
        {
            try
            {
                // Specify the directory where you want to extract the contents
                string extractionDirectory = programDirectory;
                GrfHolder grf = null;
                if (type == HostCMD.GRF)
                    grf = new GrfHolder(@mainFilePath);

                HashSet<string> arquives = new HashSet<string>();
                HashSet<string> grfarquives = new HashSet<string>();

                using (var archive = ArchiveFactory.Open(rarFilePath))
                {
                    int totalEntries = archive.Entries.Count();
                    int currentEntry = 0;

                    foreach (var entry in archive.Entries)
                    {
                        if (!entry.IsDirectory)
                        {
                            UpdateUserInfo($"Extracting {entry.Key}");
                            // Extract each entry in the .rar file
                            entry.WriteToDirectory(extractionDirectory, new ExtractionOptions { ExtractFullPath = true, Overwrite = true });

                            if (type == HostCMD.GRF)
                            {
                                string extractedFilePath = Path.Combine(extractionDirectory, entry.Key);

                                // Obtenha o caminho relativo em relação à raiz
                                string relativePath = extractedFilePath.Substring(extractionDirectory.Length);
                                grf.Commands.AddFilesInDirectory(@relativePath, @extractedFilePath);
                                grfarquives.Add(@relativePath);
                                arquives.Add(extractedFilePath);
                            }

                            // Report progress (adjust this according to your UI update mechanism)
                            int progressPercentage = (++currentEntry * 100) / totalEntries;
                            Dispatcher.Invoke(() =>
                            {
                                // Update progress bar, labels, or any other UI elements
                                downloadProgressBar.Value = progressPercentage;
                                progressTextBlock.Text = $"Extracting: {progressPercentage}%";
                            });
                        }
                    }

                    if (type == HostCMD.GRF)
                    {
                        UpdateUserInfo($"Saving {mainFilePath}");
                        grf.QuickSave();
                        grf.Close();
                        for (int i = 0; i < arquives.Count; i++)
                        {
                            UpdateUserInfo($"Deleting {arquives.ElementAtOrDefault(i)}");
                            File.Delete(arquives.ElementAtOrDefault(i));
                        }
                    }
                    grfarquives.Clear();
                    arquives.Clear();
                    GRF_Clean_Temp();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error extracting .rar file '{rarFilePath}': {ex.Message}");
            }
        }

        private void DeleteThorFile(string FilePath, HostCMD type)
        {
            try
            {
                using (GrfHolder thor = new GrfHolder(@FilePath))
                {
                    switch (type)
                    {
                        case HostCMD.GRF:
                            {
                                using (GrfHolder grf = new GrfHolder(@mainFilePath))
                                {
                                    int count = 0;
                                    foreach (var item in thor.FileTable.FastAccessEntries)
                                    {
                                        string Value = item.Value.ToString();
                                        string modifiedValue = Value.Replace("root\\", "");
                                        UpdateUserInfo($"Deleting {modifiedValue}");
                                        grf.Commands.RemoveFile(@modifiedValue);
                                        int progressPercentage = (++count * 100) / thor.FileTable.Count;
                                        Dispatcher.Invoke(() =>
                                        {
                                            // Update progress bar, labels, or any other UI elements
                                            downloadProgressBar.Value = progressPercentage;
                                        });
                                    }
                                    UpdateUserInfo($"Saving {mainFilePath}");
                                    grf.QuickSave();
                                    grf.Close();
                                }
                            }
                            break;

                        case HostCMD.PATH:
                            {
                                int count = 0;
                                foreach (var item in thor.FileTable.FastAccessEntries)
                                {
                                    string Value = item.Value.ToString();
                                    string modifiedValue = Value.Replace("root\\", "");
                                    UpdateUserInfo($"Deleting {modifiedValue}");
                                    int progressPercentage = (++count * 100) / thor.FileTable.Count;
                                    Dispatcher.Invoke(() =>
                                    {
                                        // Update progress bar, labels, or any other UI elements
                                        downloadProgressBar.Value = progressPercentage;
                                    });
                                    if (File.Exists(modifiedValue))
                                        File.Delete(modifiedValue);
                                }
                            }
                            break;

                        default: break;
                    }
                    thor.Close();
                }
                GRF_Clean_Temp();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error Deleting .thor files '{FilePath}': {ex.Message}");
            }
        }

        private void ExtractGpfFile(string FilePath, HostCMD type)
        {
            try
            {
                using (GrfHolder patch = new GrfHolder(@FilePath))
                {
                    switch (type)
                    {
                        case HostCMD.GRF:
                            {
                                UpdateUserInfo($"Merging {mainFilePath}");
                                using (GrfHolder grf = new GrfHolder(@mainFilePath))
                                {
                                    Task.WaitAll(Task.Run(() => grf.QuickMerge(patch)));
                                    grf.Close();
                                }
                            }
                            break;

                        case HostCMD.PATH:
                            {
                                UpdateUserInfo($"Extracting {FilePath.Substring(2)}");
                                Task.WaitAll(Task.Run(() => patch.Extract(@programDirectory, patch.FileTable.ToList(), @FilePath, ExtractOptions.UseAppDataPathToExtract, SyncMode.Synchronous)));
                            }
                            break;
                    }
                    patch.Close();
                }
                GRF_Clean_Temp();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error extracting .gpf file '{FilePath}': {ex.Message}");
            }
        }

        #endregion ProcessDownloadedPatches

        #region ProgressRelated

        private void UpdateUserInfo(string text)
        {
            progressTextBlock.Dispatcher.Invoke(() =>
            {
                progressTextBlock.Text = text;
            });
        }

        #endregion ProgressRelated

        #region UserOptions

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
            try
            {
                switch (btn1.Visibility)
                {
                    case Visibility.Visible:
                        ConfigBtn_MouseDown(sender, e);
                        File.Delete(indicesFilePath);
                        patchcount = 0;
                        downloadedIndices.Clear();
                        Login.IsEnabled = false;
                        Login.Visibility = Visibility.Collapsed;
                        DownloadPatchNotes();
                        break;

                    default: MessageBox.Show("Please, wait finish download."); break;
                }
            }
            catch { }
        }

        #endregion UserOptions

        #region ProcessStart

        private void BT_Click(object sender, MouseButtonEventArgs e)
        {
            Do_StartClient();
        }

        private void OnKeyDownHandler(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && Login.IsEnabled)
            {
                Do_StartClient();
            }
        }

        public async void Do_StartClient()
        {
            var hexed = Properties.Settings.Default.hexedFile;

            try
            {
                switch (File.Exists(hexed))
                {
                    case true:
                        string User = txtUserId.Text,
                           Pass = txtUser_Pass.Password;

                        // Grava as configurações no arquivo de aplicação e inicializa o cliente do jogo com as configurações.

                        Properties.Settings.Default.lembrarid = chkid.IsChecked.Value;
                        Properties.Settings.Default.lembrarpass = chkpass.IsChecked.Value;
                        Properties.Settings.Default.usuario = Properties.Settings.Default.lembrarid ? txtUserId.Text : string.Empty;
                        Properties.Settings.Default.senha = Properties.Settings.Default.lembrarpass ? txtUser_Pass.Password : string.Empty;
                        Properties.Settings.Default.Save();

                        Hide();

                        if (!chk_anime.IsChecked.Value)
                            videoBG.Pause();

                        // Inicializa o hexed do usuário com as informações de usuário e senha.
                        Process prc = new Process();
                        prc.EnableRaisingEvents = true;
                        prc.Exited += prc_Exited;
                        prc.StartInfo.FileName = hexed;
                        prc.StartInfo.Arguments = String.Format("-t:{0} {1} 1sak1", Pass, User);
                        AntiCheater wnd = new AntiCheater();
                        wnd.Show();
                        if (!DllsCheck() || HackingCheck())
                            return;
                        await Task.Delay(3000);
                        prc.Start();
                        break;

                    default:
                        MessageBox.Show("The game launcher '" + hexed + "' was not found, check if your Anti-Virus deleted it or contact our support team.");
                        break;
                }
            }
            catch
            {
                Show();
                MessageBox.Show("The game launcher '" + hexed + "' was not found, check if your Anti-Virus deleted it or contact our support team.");
            }
            finally
            {
                Do_SysTray();
            }
        }

        #endregion ProcessStart

        #region anticheat

        private bool DllsCheck()
        {
            FillDlls();
            return CleanClient();
        }

        private bool HackingCheck()
        {
            FillCheatList();
            return IsCheating();
        }

        private void FillDlls()
        {
            AllowedDLLS.Add("aossdk.dll");
            AllowedDLLS.Add("binkw32.dll");
            AllowedDLLS.Add("bz32ex.dll");
            AllowedDLLS.Add("cdclient.dll");
            AllowedDLLS.Add("cps.dll");
            AllowedDLLS.Add("concrt140.dll");
            AllowedDLLS.Add("d3dx9_42.dll");
            AllowedDLLS.Add("d3dx9_43.dll");
            AllowedDLLS.Add("dbghelp.dll");
            AllowedDLLS.Add("dinput.dll");
            AllowedDLLS.Add("granny2.dll");
            AllowedDLLS.Add("ijl15.dll");
            AllowedDLLS.Add("libcurl.dll");
            AllowedDLLS.Add("mfc90.dll");
            AllowedDLLS.Add("mfc90u.dll");
            AllowedDLLS.Add("mfc100.dll");
            AllowedDLLS.Add("mfc100u.dll");
            AllowedDLLS.Add("mfc110.dll");
            AllowedDLLS.Add("mfc110u.dll");
            AllowedDLLS.Add("mfc140.dll");
            AllowedDLLS.Add("mfc140u.dll");
            AllowedDLLS.Add("mfcm90.dll");
            AllowedDLLS.Add("mfcm90u.dll");
            AllowedDLLS.Add("mfcm100.dll");
            AllowedDLLS.Add("mfcm100u.dll");
            AllowedDLLS.Add("mfcm140.dll");
            AllowedDLLS.Add("mfcm140u.dll");
            AllowedDLLS.Add("mse.dll");
            AllowedDLLS.Add("mss32.dll");
            AllowedDLLS.Add("msvcp60.dll");
            AllowedDLLS.Add("msvcp90.dll");
            AllowedDLLS.Add("msvcp100.dll");
            AllowedDLLS.Add("msvcp110.dll");
            AllowedDLLS.Add("msvcp140.dll");
            AllowedDLLS.Add("msvcp140_1.dll");
            AllowedDLLS.Add("msvcp140_2.dll");
            AllowedDLLS.Add("msvcr90.dll");
            AllowedDLLS.Add("msvcr100.dll");
            AllowedDLLS.Add("msvcr110.dll");
            AllowedDLLS.Add("msvcr190.dll");
            AllowedDLLS.Add("npchk.dll");
            AllowedDLLS.Add("npcipher.dll");
            AllowedDLLS.Add("npkcrypt.dll");
            AllowedDLLS.Add("npkeysdk.dll");
            AllowedDLLS.Add("npkpdb.dll");
            AllowedDLLS.Add("nppsk.dll");
            AllowedDLLS.Add("npupdate.dll");
            AllowedDLLS.Add("npupdate0.dll");
            AllowedDLLS.Add("npx.dll");
            AllowedDLLS.Add("v3hunt.dll");
            AllowedDLLS.Add("vccorlib140.dll");
            AllowedDLLS.Add("vcruntime140.dll");
        }

        private void FillCheatList()
        {
            UnAllowedProcess.Add("open-kore");
            UnAllowedProcess.Add("openkore");
            UnAllowedProcess.Add("open kore");
            UnAllowedProcess.Add("gKore");
            UnAllowedProcess.Add("4RTools");
            UnAllowedProcess.Add("Erfolgreiches");
            UnAllowedProcess.Add("Ragnabot");
            UnAllowedProcess.Add("xkore0");
            UnAllowedProcess.Add("GGH BOT");
            UnAllowedProcess.Add("AutoHotkey");
            UnAllowedProcess.Add("Auto-Hotkey");
            UnAllowedProcess.Add("Auto-HK");
            UnAllowedProcess.Add("RO AHK");
            UnAllowedProcess.Add("Autopot");
            UnAllowedProcess.Add("Auto-pot");
            UnAllowedProcess.Add("Auto Buffer");
        }

        public bool IsCheating()
        {
            try
            {
                Process[] processes = Process.GetProcesses();
                foreach (Process process in processes)
                {
                    foreach (string name in UnAllowedProcess)
                    {
                        if (process.ProcessName.ToLowerInvariant().Contains(name.ToLowerInvariant()))
                        {
                            try
                            {
                                process.Kill();
                            }
                            catch (Exception)
                            {
                                return true;
                            }
                        }
                    }
                }
                return false;
            }
            catch
            {
                return true;
            }
        }

        private bool CleanClient()
        {
            foreach (string file in Directory.GetFiles(Directory.GetCurrentDirectory()))
            {
                if (Path.GetExtension(file).ToLowerInvariant() == ".dll" && !AllowedDLLS.Contains(Path.GetFileName(file).ToLowerInvariant()))
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (IOException)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        #endregion anticheat

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

        private void prc_Exited(object sender, EventArgs e)
        {
            Application.Current.Dispatcher?.Invoke(() =>
            {
                Show();
                _notifyIcon.Visible = false;
            });
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

            webBrowser1.Navigate(Urls.ElementAtOrDefault(web_Counter));
        }

        private void PatchNotes_MouseDown(object sender, MouseEventArgs e)
        {
            if (web_Counter > 0)
                web_Counter--;
            else
                web_Counter = Urls.Count() - 1;

            webBrowser1.Navigate(Urls.ElementAtOrDefault(web_Counter));
        }

        #endregion WebCarroucel

        #region SystemTray

        private void InitializeNotifyIcon()
        {
            _notifyIcon = new NotifyIcon
            {
                Icon = Properties.Resources.icon, // Substitua "SeuIcone" pelo nome do seu ícone nos recursos
                Visible = true,
                Text = "SharpPatcher"
            };
            _notifyIcon.MouseDoubleClick += (sender, e) =>
            {
                Show();
                _notifyIcon.Visible = false;
                WindowState = WindowState.Normal;
            };

            trayMenu.MenuItems.Add("Restore", (sender, e) =>
            {
                Show();
                _notifyIcon.Visible = false;
                WindowState = WindowState.Normal;
            });
            trayMenu.MenuItems.Add("Close", (sender, e) =>
            {
                Close();
            });
            _notifyIcon.Visible = false;
            _notifyIcon.ContextMenu = trayMenu;
        }

        public void Do_SysTray()
        {
            Hide();
            _notifyIcon.Visible = true;
        }

        private void Windowed_MouseDown(object sender, MouseButtonEventArgs e) => Do_SysTray();

        #endregion SystemTray

        #region Background

        private void MediaElement_Loaded_1(object sender, RoutedEventArgs e)
        {
            Uri ImgUrl = new Uri(programDirectory + "/Resources/Background/background.png");
            background.Source = ImgUrl;
        }

        #region Video

        private void Pause1_MouseDown(object sender, MouseButtonEventArgs e)
        {
            videoBG.Stop();
            videoBG.Visibility = Visibility.Collapsed;
            background.Visibility = Visibility.Visible;

            //pause1
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

        /// ========== video:
        ///
        private void MediaElement_Loaded(object sender, RoutedEventArgs e)
        {
            videoBG.Source = new Uri(programDirectory + "/Resources/Background/bg.mp4");
            videoBG.LoadedBehavior = MediaState.Manual;

            Video_Play();
        }

        private void VideoBG_MediaEnded_1(object sender, RoutedEventArgs e) => Video_Play();

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

        private void Label_Enter(object sender, MouseEventArgs e)
        {
        }

        private void Label_Leave(object sender, MouseEventArgs e)
        {
        }

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

        #region Links

        private void Label_MouseDown_3(object sender, MouseButtonEventArgs e) => Process.Start("http://seurag.com");

        private void discord_btn_(object sender, MouseButtonEventArgs e) => Process.Start("https://discord.com");

        private void facebook_link(object sender, MouseButtonEventArgs e) => Process.Start("https://facebook.com");

        private void news_link(object sender, MouseButtonEventArgs e) => Process.Start("https://google.com");

        private void register_link(object sender, MouseButtonEventArgs e) => Process.Start("https://seuro.com/register");

        #endregion Links

        #region Animation

        private void InitializeLoginAnimation() => LoginScroll.RenderTransform.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(-575, TimeSpan.FromSeconds(1)));

        #region AnimationCore

        private ThicknessAnimation F_Thickness_Animate(Thickness From, Thickness To, double Duration = 0.1)
        {
            ThicknessAnimation marginAnimation = new ThicknessAnimation();
            marginAnimation.GetAnimationBaseValue(MarginProperty);
            marginAnimation.From = From; // Valor inicial da margem
            marginAnimation.To = To; // Valor final da margem
            marginAnimation.Duration = TimeSpan.FromSeconds(Duration);
            return marginAnimation;
        }

        private Thickness F_Pressed(Thickness margin) => new Thickness(margin.Left + 1, margin.Top + 1, margin.Right, margin.Bottom);

        private void F_GridAnimate(Grid grid, Thickness margin, bool enter = false)
        {
            if (enter)
            {
                grid.BeginAnimation(MarginProperty, F_Thickness_Animate(margin, F_Pressed(margin)));
                return;
            }
            grid.BeginAnimation(MarginProperty, F_Thickness_Animate(F_Pressed(margin), margin));
        }

        private void F_LabelAnimate(Label label, Thickness margin, bool enter = false)
        {
            if (enter)
            {
                label.BeginAnimation(MarginProperty, F_Thickness_Animate(margin, F_Pressed(margin)));
                return;
            }
            label.BeginAnimation(MarginProperty, F_Thickness_Animate(F_Pressed(margin), margin));
        }

        #endregion AnimationCore

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

        private void VideoBG_MediaEnded(object sender, RoutedEventArgs e) => Video_Play();

        private void Label_MouseEnter(object sender, MouseEventArgs e) => F_GridAnimate(Login, LoginMargin, true);

        private void Label_MouseLeave(object sender, MouseEventArgs e) => F_GridAnimate(Login, LoginMargin);

        private void Play_MouseEnter(object sender, MouseEventArgs e) => F_LabelAnimate(play, playMargin, true);

        private void Play_MouseLeave(object sender, MouseEventArgs e) => F_LabelAnimate(play, playMargin);

        private void Pause_MouseEnter(object sender, MouseEventArgs e) => F_LabelAnimate(pause1, pause1Margin, true);

        private void Pause_MouseLeave(object sender, MouseEventArgs e) => F_LabelAnimate(pause1, pause1Margin);

        private void Notices_MouseEnter(object sender, MouseEventArgs e) => F_LabelAnimate(Notices, NoticesMargin, true);

        private void Notices_MouseLeave(object sender, MouseEventArgs e) => F_LabelAnimate(Notices, NoticesMargin);

        private void PatchNotes_MouseEnter(object sender, MouseEventArgs e) => F_LabelAnimate(PatchNotes, PatchNotesMargin, true);

        private void PatchNotes_MouseLeave(object sender, MouseEventArgs e) => F_LabelAnimate(PatchNotes, PatchNotesMargin);

        private void Options_MouseEnter(object sender, MouseEventArgs e) => F_LabelAnimate(options, optionsMargin, true);

        private void Options_MouseLeave(object sender, MouseEventArgs e) => F_LabelAnimate(options, optionsMargin);

        private void Closebtn_MouseEnter(object sender, MouseEventArgs e) => F_LabelAnimate(closebtn, closebtnMargin, true);

        private void Closebtn_MouseLeave(object sender, MouseEventArgs e) => F_LabelAnimate(closebtn, closebtnMargin);

        private void SideBackground_MouseEnter(object sender, MouseEventArgs e) => F_LabelAnimate(SideBackground, SideBackgroundMargin, true);

        private void SideBackground_MouseLeave(object sender, MouseEventArgs e) => F_LabelAnimate(SideBackground, SideBackgroundMargin);

        private void Windowed_MouseEnter(object sender, MouseEventArgs e) => F_LabelAnimate(WindowedBtn, WindowedBtnMargin, true);

        private void Windowed_MouseLeave(object sender, MouseEventArgs e) => F_LabelAnimate(WindowedBtn, WindowedBtnMargin);

        #endregion Animation
    }
}