using System;
using System.IO;

namespace Updater
{
    class Config
    {
        public const string GameDirectoryName = "MyGame";
        public static string GetGameDirectory()
        {
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), GameDirectoryName);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            return path;
        }
        public const string updateURL = "http://mysite.com/update/";
        public readonly string path = GetGameDirectory();
        public const string binName = "MyGame.exe";
        public const string serverName = "MyGameServer.exe";
        public static bool isServer = false;
        public static string GetBinary()
        {
            if (isServer)
                return serverName;
            else
                return binName;
        }
    }
}
