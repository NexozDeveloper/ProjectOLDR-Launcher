using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace ProjectOLDR
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string targetFolder = Path.Combine(appData, "Nexoz");
            string targetExe = Path.Combine(targetFolder, "ProjectOLDR.exe");
            string currentExe = Process.GetCurrentProcess().MainModule.FileName;

            if (!string.Equals(currentExe, targetExe, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    Directory.CreateDirectory(targetFolder);

                    File.Copy(currentExe, targetExe, true);

                    CreateShortcut(targetExe);

                    Process.Start(new ProcessStartInfo
                    {
                        FileName = targetExe,
                        UseShellExecute = true
                    });

                    return;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error during automatic installation : " + ex.Message,
                                    "Project OLDR",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Error);
                }
            }

            Application.Run(new MainForm());
        }

        private static void CreateShortcut(string targetPath)
        {
            try
            {
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                string shortcutPath = Path.Combine(desktop, "Project OLDR.lnk");

                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType != null)
                {
                    dynamic shell = Activator.CreateInstance(shellType);
                    var shortcut = shell.CreateShortcut(shortcutPath);
                    shortcut.TargetPath = targetPath;
                    shortcut.WorkingDirectory = Path.GetDirectoryName(targetPath);
                    shortcut.Description = "Project OLDR Launcher";
                    shortcut.Save();
                }
            }
            catch
            {
            }
        }
    }
}
