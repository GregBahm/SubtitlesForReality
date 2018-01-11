using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CognitiveServices.SpeechRecognition
{
    class SpeechLogger
    {
        private readonly StringBuilder _logBuilder;
        private readonly string _logOutputDirectory;
        private string _latestEntryName;

        public SpeechLogger()
        {
            _logBuilder = new StringBuilder();
            _logOutputDirectory = Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.Parent.FullName + @"\SpeechLog\";
            CleanOutputDirectory();
        }

        private void CleanOutputDirectory()
        {
            if(!Directory.Exists(_logOutputDirectory))
            {
                Directory.CreateDirectory(_logOutputDirectory);
            }
            foreach (string item in Directory.GetFiles(_logOutputDirectory))
            {
                File.Delete(item);
            }
        }

        public void UpdateLog(string responseText)
        {
            if(string.IsNullOrEmpty(_latestEntryName))
            {
                throw new InvalidOperationException("Called WriteLog() before ever calling StartNewEntry()");
            }
            string entry = DateTime.Now.Ticks + ": " + responseText;
            _logBuilder.Insert(0, entry + "\n");
            File.WriteAllText(_logOutputDirectory + _latestEntryName, _logBuilder.ToString());
        }

        public void StartNewEntry()
        {
            _logBuilder.Clear();
            _latestEntryName = DateTime.Now.Ticks + ".txt";
        }
    }
}
