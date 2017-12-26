using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Ionic.Zip;
using System.Diagnostics;
using System.ComponentModel;

namespace Updater
{
    static class Updater
    {
        static string rootPath, binPath, versionFilename, versionURL;
        static bool installationProblem;
        static bool error = false;
        static int status;
        static string message = "Checking for update...";
        static float progress;
        static WebClient client = null;
        public static UpdaterStatus GetStatus()
        {
            return new UpdaterStatus(status, progress, message);
        }
        private static void Error(string message, Exception ex)
        {
            if (error)
                return;
            error = true;
            status = -1;
            Updater.message = message;
            if (ex != null)
            {
                try
                {
                    FileStream stream = File.Open("error.log", FileMode.Create);
                    BinaryWriter w = new BinaryWriter(stream);
                    w.Write("ERROR: " + message + "\r\n");
                    w.Write(ex.ToString() + "\r\n");
                    w.Write(ex.StackTrace + "\r\n");
                    w.Close();
                }
                catch (Exception ex2)
                {
                }
            }
            Console.WriteLine(message);
        }
        private static void Error(string message)
        {
            Error(message, null);
        }
        public static void Run()
        {
            try
            {
                rootPath = Config.GetGameDirectory();
                versionFilename = rootPath + "\\version.txt";
                binPath = rootPath + "\\bin";
                versionURL = Config.updateURL + "?version";

                bool update = false;
                installationProblem = false;
                bool networkProblem = false;
                int currentVersion = 0;
                string currentVersionStr = HttpGet(versionURL);

                try
                {
                    currentVersion = int.Parse(currentVersionStr);
                }
                catch (Exception ex) { networkProblem = true; }
                bool locked = CheckLock();
                if (!File.Exists(versionFilename))
                {
                    update = true;
                    installationProblem = true;
                }
                else if (!Directory.Exists(binPath))
                {
                    update = true;
                    installationProblem = true;
                }
                else if (!networkProblem)
                {
                    int version = 0;
                    try
                    {
                        BinaryReader r = new BinaryReader(File.Open(versionFilename, FileMode.Open));
                        version = r.ReadInt32();
                        r.Close();
                    }
                    catch (Exception ex)
                    {
                        installationProblem = true;
                    }
                    if (version != currentVersion)
                        update = true;
                }
                if (installationProblem && networkProblem)
                    Error("Could not connect to the update server.");
                else if (!locked && update && !networkProblem)
                {
                    status = 1;
                    if (Update() && !error)
                    {
                        FileStream versionStream = File.Open(versionFilename, FileMode.Create);
                        new BinaryWriter(versionStream).Write(currentVersion);
                        versionStream.Close();
                    }
                }
                else
                    status = StartGame() ? 2 : -1;
            }
            catch (Exception ex)
            {
                Error("Unexpected error occured", ex);
            }
        }
        private static bool StartGame()
        {
            try
            {
                ProcessStartInfo info = new ProcessStartInfo();
                info.FileName = binPath + "\\" + Config.GetBinary();
                info.WorkingDirectory = binPath;
                Process.Start(info);
                return true;
            }
            catch (Exception ex)
            {
                Error("Could not start the game", ex);
                return false;
            }
        }
        private static bool Update()
        {
            if (installationProblem && Directory.Exists(binPath))
                Directory.Delete(binPath, true);
            string request = "";
            if (!installationProblem)
            {
                Dictionary<FileInfo, string> hashes = new Dictionary<FileInfo, String>();
                DirectoryInfo dInfo = new DirectoryInfo(binPath);
                DirectoryInfo[] subdirs = dInfo.GetDirectories("*.*", SearchOption.AllDirectories);
                CalculateHashes(dInfo, hashes);
                foreach (DirectoryInfo d in subdirs)
                {
                    CalculateHashes(d, hashes);
                }

                foreach (FileInfo f in hashes.Keys)
                {
                    if (f.Name.EndsWith(".tmp") || f.Name.EndsWith(".PendingOverwrite"))
                        f.Delete();
                    else
                    {
                        string name = f.FullName.Replace(binPath + "\\", "");
                        request += name + "," + hashes[f] + ";";
                    }
                }
            }
            Console.WriteLine(request);

            try
            {
                client = new WebClient();
                client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(delegate (object sender, DownloadProgressChangedEventArgs e)
                    {
                        progress = (float)e.ProgressPercentage / 100;
                    });
                if (request == "")
                    request = "_";
                client.DownloadFileCompleted += new AsyncCompletedEventHandler(delegate (object sender, AsyncCompletedEventArgs e)
                    {
                        try
                        {
                            client.Dispose();
                            client = null;
                            ZipFile zip = new ZipFile(rootPath + "\\update.zip");
                            zip.ExtractAll(rootPath, ExtractExistingFileAction.OverwriteSilently);
                            zip.Dispose();
                            File.Delete(rootPath + "\\update.zip");
                        }
                        catch (Exception ex)
                        {
                            Error("Could not install update", ex);
                        }
                        status = StartGame() ? 2 : -1;
                    });
                client.DownloadFileAsync(new Uri(Config.updateURL + "?x=" + Uri.EscapeDataString(request)), rootPath + "\\update.zip");
            }
            catch (Exception ex)
            {
                Error("Could not download update", ex);
                return false;
            }
            return true;
        }
        private static void CalculateHashes(DirectoryInfo dInfo, Dictionary<FileInfo, String> map)
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            foreach (FileInfo file in dInfo.GetFiles())
            {
                FileStream stream = file.OpenRead();
                byte[] bytes = md5.ComputeHash(stream);
                stream.Close();
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    sb.Append(bytes[i].ToString("x2"));
                }
                map[file] = sb.ToString();
            }
        }
        public static string HttpGet(string path)
        {
            try
            {
                Uri uri = new Uri(path);
                HttpWebRequest r = (HttpWebRequest)WebRequest.Create(uri);
                r.Credentials = CredentialCache.DefaultCredentials;
                r.Timeout = 5000;
                HttpWebResponse response = (HttpWebResponse)r.GetResponse();
                StreamReader reader = new StreamReader(response.GetResponseStream());
                string result = reader.ReadToEnd().Trim();
                reader.Close();
                response.Close();
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                return "";
            }
        }
        public static string HttpPost(string path, string arguments)
        {
            try
            {
                Uri uri = new Uri(path);
                HttpWebRequest r = (HttpWebRequest)WebRequest.Create(uri);
                r.Method = "POST";
                r.Credentials = CredentialCache.DefaultCredentials;
                r.Timeout = 5000;
                ASCIIEncoding encoding = new ASCIIEncoding();
                byte[] bytes = encoding.GetBytes(arguments);
                Stream request = r.GetRequestStream();
                r.ContentLength = bytes.Length;
                request.Write(bytes, 0, bytes.Length);
                request.Close();
                HttpWebResponse response = (HttpWebResponse)r.GetResponse();
                StreamReader reader = new StreamReader(response.GetResponseStream());
                string result = reader.ReadToEnd().Trim();
                reader.Close();
                response.Close();
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                return "";
            }
        }
        private static bool CheckLock()
        {
            if (!File.Exists(rootPath + "\\lock"))
                return false;
            else
            {
                try
                {
                    File.Delete(rootPath + "\\lock");
                    return false;
                }
                catch (Exception ex)
                {
                    return true;
                }
            }
        }
    }
}
