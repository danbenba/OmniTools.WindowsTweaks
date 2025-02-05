using System;
using System.Net;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace TelechargementEtExecutionPS1
{
    /// <summary>
    /// Classe utilitaire pour afficher des messages en console avec des couleurs et un préfixe,
    /// et pour écrire dans un fichier log.
    /// </summary>
    public static class Logger
    {
        // Chemin du fichier log (placé dans le même dossier que l'exécutable)
        private static string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log.txt");

        public static void StartupMessage(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=");
            Console.WriteLine("                Windows Tweaker            ");
            Console.WriteLine(" ");
            Console.WriteLine("Core Version: Omni0.7");
            Console.WriteLine("Created by danbenba");
            Console.WriteLine("=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=");
            Console.WriteLine();
            Console.ResetColor();

            WriteLog("Startup: " + message);
        }

        public static void LogInfo(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[+] " + message);
            Console.ResetColor();
            WriteLog("INFO: " + message);
        }

        public static void LogWarning(string message)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("[-] " + message);
            Console.ResetColor();
            WriteLog("WARNING: " + message);
        }

        public static void LogError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[x] " + message);
            Console.ResetColor();
            WriteLog("ERROR: " + message);
        }

        private static void WriteLog(string message)
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(logFilePath, true))
                {
                    sw.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}");
                }
            }
            catch (Exception)
            {
                // En cas d'erreur lors de l'écriture du fichier log, on passe outre.
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Logger.StartupMessage("Démarrage de l'application.");

            // URL du fichier .ps1 à télécharger (à modifier selon vos besoins)
            string urlScript = "https://github.com/ChrisTitusTech/winutil/releases/download/25.01.11/winutil.ps1";

            // Chemin local où le script sera sauvegardé (ici dans le dossier temporaire)
            string cheminLocal = Path.Combine(Path.GetTempPath(), "WindowsTweak.OmniTools.ps1");

            try
            {
                using (WebClient client = new WebClient())
                {
                    // Gestionnaire pour afficher la barre de progression
                    client.DownloadProgressChanged += (s, e) =>
                    {
                        DrawTextProgressBar(e.ProgressPercentage, 100);
                    };

                    // Gestionnaire pour consigner l’issue du téléchargement
                    client.DownloadFileCompleted += (s, e) =>
                    {
                        if (e.Error != null)
                        {
                            Logger.LogError("Erreur lors du téléchargement : " + e.Error.Message);
                        }
                        else if (e.Cancelled)
                        {
                            Logger.LogWarning("Téléchargement annulé.");
                        }
                        else
                        {
                            Logger.LogInfo("Téléchargement terminé avec succès.");
                        }
                    };

                    Logger.LogInfo("Téléchargement du script depuis : " + urlScript);
                    client.DownloadFileAsync(new Uri(urlScript), cheminLocal);

                    // Attendre la fin du téléchargement
                    while (client.IsBusy)
                    {
                        Thread.Sleep(100);
                    }
                }
                Logger.LogInfo("Fichier sauvegardé ici : " + cheminLocal);

                // Préparation de l'exécution du script avec PowerShell
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    // On contourne la politique d'exécution avec "-ExecutionPolicy Bypass"
                    Arguments = $"-ExecutionPolicy Bypass -File \"{cheminLocal}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                // Exécution du script PowerShell
                using (Process process = Process.Start(startInfo))
                {
                    // Lecture de la sortie et des erreurs
                    string sortie = process.StandardOutput.ReadToEnd();
                    string erreur = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    Logger.LogInfo("Sortie du script :");
                    Logger.LogInfo(sortie);

                    if (!string.IsNullOrWhiteSpace(erreur))
                    {
                        Logger.LogError("Erreurs lors de l'exécution :");
                        Logger.LogError(erreur);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Une erreur est survenue : " + ex.Message);
            }
        }

        /// <summary>
        /// Affiche une barre de progression dans la console.
        /// </summary>
        /// <param name="progress">Pourcentage de progression actuel.</param>
        /// <param name="total">Valeur totale de progression (généralement 100).</param>
        private static void DrawTextProgressBar(int progress, int total)
        {
            Console.CursorVisible = false;
            int totalBlocks = 50;
            int filledBlocks = (int)(progress * totalBlocks / total);
            string progressBar = "[" + new string('#', filledBlocks) + new string('-', totalBlocks - filledBlocks) + $"] {progress}%";
            Console.Write("\r" + progressBar);
            if (progress == total)
            {
                Console.WriteLine();
                Console.CursorVisible = true;
            }
        }
    }
}
