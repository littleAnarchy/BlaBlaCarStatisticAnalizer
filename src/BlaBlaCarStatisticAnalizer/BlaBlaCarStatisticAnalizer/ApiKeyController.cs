using System;
using System.Collections.Generic;
using System.IO;

namespace BlaBlaCarStatisticAnalizer
{
    public static class ApiKeyController
    {
        public static List<string> Keys { get; } = new List<string>();
        public static string CurrentKey { get; private set; }
        public static event EventHandler OnChangeCurrentKey;

        private static int _currentIntex;
        private static readonly string Path = Directory.GetCurrentDirectory() + @"\Keys.txt";
        private static readonly object Locker = new object();

        public static void LoadKeys()
        {
            CheckFileExists(Path);

            using (var sr = new StreamReader(Path))
            {
                while (true)
                {
                    var str = sr.ReadLine();
                    if (str != null) Keys.Add(str);
                    else
                        break;
                }
            }

            CurrentKey = Keys[0];
            OnChangeCurrentKey?.Invoke(CurrentKey, null);
        }

        public static void SkipKey()
        {
            lock (Locker)
            {
                if (_currentIntex != Keys.Count-1) _currentIntex++;
                else
                    _currentIntex = 0;
                CurrentKey = Keys[_currentIntex];
                OnChangeCurrentKey?.Invoke(CurrentKey, null);
            }
        }

        private static void CheckFileExists(string path)
        {
            if (File.Exists(path)) return;
            using (File.Create(path))
            {
            }
        }
    }
}
