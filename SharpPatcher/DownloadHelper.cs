using GRF.Core;
using SharpCompress.Archives;
using SharpCompress.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using static SharpPatcher.AnimationHelper;
using static SharpPatcher.Consts;
using static SharpPatcher.Config;

namespace SharpPatcher
{
    public static class DownloadHelper
    {
       
        public static void AddToProcessQueue(string arquive, HostCMD type)
        {
            DownloadedArquive NewArchive = new DownloadedArquive
            {
                Arquive = arquive,
                Type = type
            };
            DownloadedList.Add(NewArchive);
        }

        public static async Task ProcessesPatchs()
        {
            try
            {
                int i = 0;
                foreach (var arquive in DownloadedList)
                {
                    await ProcessDownloadedFile(arquive.Arquive, arquive.Type);
                    i++;
                    UpdateArchiveText($"{i}/{DownloadedList.Count()}");
                }
                DownloadedList.Clear();
            }
            catch (Exception ex)
            {
                ShowMsgError($"Fail processing downloaded arquive: {ex.Message}");
            }
        }

        public static void LoadDownloadedIndex()
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

        #region DownloadFuncs

        public static void SaveDownloadedIndex()
        {
            try
            {
                string[] indicesArray = downloadedIndices.Select(index => index.ToString()).ToArray();

                File.WriteAllLines(indicesFilePath, indicesArray);
            }
            catch (Exception ex)
            {
                ShowMsgError($"Error saving downloaded indices: {ex.Message}");
            }
        }

        public static HostCMD ParsePatchCMD(string cmd)
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

        public static async void DownloadPatchNotes()
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
                    WorkingWindow.Login.Visibility = Visibility.Collapsed;

                    if (patchcount < patchNotesLines.Length - patchcount)
                        WorkingWindow.ProgressBarGrid.Visibility = Visibility.Visible;

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
                                UpdateArchiveText($"{fileIndex - patchcount}/{patchNotesLines.Length - patchcount}");

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
                                ShowMsgError($"Invalid index in line: {line}");
                                break;
                        }
                    }
                }
                //await ProcessesPatchs();
                SaveDownloadedIndex();
            }
            catch (Exception ex)
            {
                ShowMsgError($"Error downloading patch notes: {ex.Message}");
            }
            finally
            {
                // Reset the progress bar value after completion
                UpdateUserInfo("Game is ready to Start!");
                UpdateProgressBar(100);
                WorkingWindow?.Dispatcher.Invoke(()=>
                { 
                    WorkingWindow.Login.Visibility = Visibility.Visible;
                    WorkingWindow.Login.IsEnabled = true;
                    InitializeLoginAnimation();
                });
            }
        }

        public static async Task DownloadFileWithRetry(string fileUrl, string fileName, HostCMD type, short maxRetries = 3)
        {
            short retries = 0;

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
                    ShowMsgError($"Error downloading file '{fileName}': '{ex.Message}'.");
                    await Task.Delay(3000); // Wait for a short period before retrying
                }
            }

            // If all retries fail, handle the error accordingly
            ShowMsgError($"Error downloading file '{fileName}': Maximum retries exceeded.");
        }

        public static async Task DownloadFile(string fileUrl, string fileName, HostCMD type)
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
                        UpdateProgressBar(e.ProgressPercentage);
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
                    ShowMsgError($"Error processing host command to file '{fileName}': {ex.Message} : {exd.Message}");
                }
            }
        }

        #endregion DownloadFuncs

        #region GRF_DLL

        public static void GRF_Clean_Temp() => GRF.System.TemporaryFilesManager.ClearTemporaryFiles();

        public static void GRF_Set_Temp() => GRF.System.Settings.TempPath = $"{programDirectory}/tmp";

        #endregion GRF_DLL

        #region ProcessDownloadedPatches

        public static async Task ProcessDownloadedFile(string filePath, HostCMD type)
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
                ShowMsgError($"Error processing downloaded file '{filePath}': {ex.Message}");
            }
        }

        public static async Task ExtractRarFileAsync(string FilePath, HostCMD type) => await Task.Run(() => ExtractRarFile(FilePath, type));

        public static async Task ExtractGpfFileAsync(string FilePath, HostCMD type) => await Task.Run(() => ExtractGpfFile(FilePath, type));

        public static async Task DeleteThorFileAsync(string FilePath, HostCMD type) => await Task.Run(() => DeleteThorFile(FilePath, type));

        public static void ExtractRarFile(string rarFilePath, HostCMD type)
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
                            // Report progress (adjust this according to your UI update mechanism)
                            int progressPercentage = (++currentEntry * 100) / totalEntries;
                            UpdateUserInfo($"Extracting {entry.Key} - {progressPercentage}%");
                            UpdateProgressBar(progressPercentage);

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
               ShowMsgError($"Error extracting .rar file '{rarFilePath}': {ex.Message}");
            }
        }

        public static void DeleteThorFile(string FilePath, HostCMD type)
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
                                        UpdateProgressBar(progressPercentage);
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
                                    UpdateProgressBar(progressPercentage);
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
                ShowMsgError($"Error Deleting .thor files '{FilePath}': {ex.Message}");
            }
        }

        public static void ExtractGpfFile(string FilePath, HostCMD type)
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
                ShowMsgError($"Error extracting .gpf file '{FilePath}': {ex.Message}");
            }
        }

        #endregion ProcessDownloadedPatches

        #region ProgressRelated

        public static void UpdateUserInfo(string text)
        {
            WorkingWindow?.Dispatcher.Invoke(() =>
            {
                WorkingWindow.progressTextBlock.Text = text;
            });
        }

        public static void UpdateProgressBar(int percent)
        {
            WorkingWindow?.Dispatcher.Invoke(() =>
            {
                WorkingWindow.downloadProgressBar.Value = percent;
            });
        }
        public static void UpdateArchiveText(string text)
        {
            WorkingWindow?.Dispatcher.Invoke(() =>
            {
                WorkingWindow.progressArchives.Text = text;
            });
        }

        #endregion ProgressRelated
    }
}