using System;
using System.Diagnostics;
using System.IO;
using static SharpPatcher.Consts;

namespace SharpPatcher
{
    public static class AntiCheatHelper
    {
        #region anticheat

        public static bool DllsCheck()
        {
            FillDlls();
            return CleanClient();
        }

        public static bool HackingCheck()
        {
            FillCheatList();
            return IsCheating();
        }

        public static void FillDlls()
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

        public static void FillCheatList()
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

        public static bool IsCheating()
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

        public static bool CleanClient()
        {
            foreach (string file in Directory.GetFiles(Directory.GetCurrentDirectory()))
            {
                if (Path.GetExtension(file).ToLowerInvariant() == ".dll" && !AllowedDLLS.Contains(Path.GetFileName(file).ToLowerInvariant()))
                {
                    try
                    {
                        if(File.Exists(file))
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
    }
}