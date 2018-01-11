namespace Microsoft.CognitiveServices.SpeechRecognition
{
    using System;
    using System.ComponentModel;
    using System.Configuration;
    using System.Diagnostics;
    using System.IO;
    using System.IO.IsolatedStorage;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Windows;
    using System.Windows.Threading;

    class ViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private SpeechLogger _logger;
        private MicrophoneRecognitionClient _micClient;

        private bool _autoRestart;
        public bool AutoRestart
        {
            get{ return _autoRestart; }
            set
            {
                _autoRestart = value;
                OnPropertyChanged(nameof(AutoRestart));
            }
        }
        
        private string DefaultLocale
        {
            get { return "en-US"; }
        }

        private string AuthenticationUri
        {
            get
            {
                return ConfigurationManager.AppSettings["AuthenticationUri"];
            }
        }

        private string _subscriptionKey;
        public string SubscriptionKey
        {
            get
            {
                return this._subscriptionKey;
            }

            set
            {
                this._subscriptionKey = value;
                this.OnPropertyChanged(nameof(SubscriptionKey));
            }
        }

        private readonly StringBuilder _displayText;
        public string DisplayText
        {
            get{ return _displayText.ToString(); }
        }

        private bool _recording;
        public bool Recording
        {
            get { return _recording; }
            set
            {
                _recording = value;
                OnPropertyChanged(nameof(Recording));
                OnPropertyChanged(nameof(StartButtonEnabled));
            }
        }
        public bool StartButtonEnabled { get{ return !Recording; } }

        private Dispatcher Dispatcher { get; }

        public ViewModel(Dispatcher dispatcher)
        {
            Dispatcher = dispatcher;
            _displayText = new StringBuilder();
            _logger = new SpeechLogger();
            AutoRestart = true;
        }

        public void StartRecordingSession()
        {
            Recording = true;

            LogRecognitionStart();

            if (_micClient == null)
            {
                CreateMicrophoneRecoClient();
            }

            _micClient.StartMicAndRecognition();
        }

        private void LogRecognitionStart()
        {
            _logger.StartNewEntry();
            _displayText.Clear();
            WriteLine("* Starting speech recognition *");
        }

        private void WriteLine()
        {
            WriteLine(string.Empty);
        }

        private void WriteLine(string format, params object[] args)
        {
            string formattedStr = string.Format(format, args);
            Trace.WriteLine(formattedStr);
            Dispatcher.Invoke(() =>
            {
                _displayText.Insert(0, formattedStr + "\n");
                OnPropertyChanged(nameof(DisplayText));
            });
        }

        private void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        private void CreateMicrophoneRecoClient()
        {
            _micClient = SpeechRecognitionServiceFactory.CreateMicrophoneClient(
                 SpeechRecognitionMode.ShortPhrase,
                DefaultLocale,
                SubscriptionKey);
            _micClient.AuthenticationUri = this.AuthenticationUri;

            _micClient.OnMicrophoneStatus += this.OnMicrophoneStatus;
            _micClient.OnPartialResponseReceived += this.OnPartialResponseReceivedHandler;
            _micClient.OnResponseReceived += this.OnMicShortPhraseResponseReceivedHandler;

            _micClient.OnConversationError += this.OnConversationErrorHandler;
        }

        private void OnMicShortPhraseResponseReceivedHandler(object sender, SpeechResponseEventArgs e)
        {
            Dispatcher.Invoke((Action)(() =>
            {
                WriteLine("--- OnMicShortPhraseResponseReceivedHandler ---");

                // we got the final result, so it we can end the mic reco.  No need to do this
                // for dataReco, since we already called endAudio() on it as soon as we were done
                // sending all the data.
                _micClient.EndMicAndRecognition();

                WriteResponseResult(e);

                Recording = false;
                if (e.PhraseResponse.Results.Any())
                {
                    _logger.UpdateLog(e.PhraseResponse.Results[0].DisplayText);
                }

                if(AutoRestart)
                {
                    StartRecordingSession();
                }
            }));
        }
        private void OnConversationErrorHandler(object sender, SpeechErrorEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                StartRecordingSession();
            });

            this.WriteLine("--- Error received by OnConversationErrorHandler() ---");
            this.WriteLine("Error code: {0}", e.SpeechErrorCode.ToString());
            this.WriteLine("Error text: {0}", e.SpeechErrorText);
            this.WriteLine();
        }

        private void OnMicrophoneStatus(object sender, MicrophoneEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                WriteLine("--- Microphone status change received by OnMicrophoneStatus() ---");
                WriteLine("********* Microphone status: {0} *********", e.Recording);
                if (e.Recording)
                {
                    WriteLine("Please start speaking.");
                }

                WriteLine();
            });
        }

        private void WriteResponseResult(SpeechResponseEventArgs e)
        {
            if (e.PhraseResponse.Results.Length == 0)
            {
                WriteLine("No phrase response is available.");
            }
            else
            {
                WriteLine("********* Final n-BEST Results *********");
                for (int i = 0; i < e.PhraseResponse.Results.Length; i++)
                {
                    WriteLine(
                        "[{0}] Confidence={1}, Text=\"{2}\"",
                        i,
                        e.PhraseResponse.Results[i].Confidence,
                        e.PhraseResponse.Results[i].DisplayText);
                }

                WriteLine();
                _logger.StartNewEntry();
            }
        }

        private void OnPartialResponseReceivedHandler(object sender, PartialSpeechResponseEventArgs e)
        {
            WriteLine("--- Partial result received by OnPartialResponseReceivedHandler() ---");
            WriteLine("{0}", e.PartialResult);
            WriteLine();

            _logger.UpdateLog(e.PartialResult);
        }

        public void OnClosed()
        {
            if (null != _micClient)
            {
                _micClient.Dispose();
            }
        }
    }
}
