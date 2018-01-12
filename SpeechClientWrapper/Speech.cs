using Microsoft.CognitiveServices.SpeechRecognition;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;

namespace SpeechClientWrapper
{
    public class Speech
    {
        public event EventHandler<SpeechUpdateEventArgs> Update;

        private object _locker;
        private SubtitleSession _activeSubtitle;
        public readonly List<SubtitleSession> Subtitles;

        private readonly MicrophoneRecognitionClient _micClient;

        private readonly string DefaultLocale = "en-US";
        private readonly string _subscriptionKey;

        public Speech(string subscriptionKey)
        {
            _subscriptionKey = subscriptionKey;
            _activeSubtitle = new SubtitleSession();
            Subtitles = new List<SubtitleSession>() { _activeSubtitle };

            _micClient = SpeechRecognitionServiceFactory.CreateMicrophoneClient(
                 SpeechRecognitionMode.ShortPhrase,
                DefaultLocale,
                _subscriptionKey);
            _micClient.OnPartialResponseReceived += this.OnPartialResponseReceivedHandler;
            _micClient.OnResponseReceived += this.OnMicShortPhraseResponseReceivedHandler;

        }

        private void CompleteSession(string text)
        {
            ContinueSession(text);
            _activeSubtitle = new SubtitleSession();
            Subtitles.Add(_activeSubtitle);
        }

        private void ContinueSession(string text)
        {
            TextMoment moment = new TextMoment(DateTime.Now.Ticks, text);
            _activeSubtitle.AddTextMoment(moment);
            Update?.Invoke(this, new SpeechUpdateEventArgs(moment));
        }

        public void StartRecordingSession()
        {
            _micClient.StartMicAndRecognition();
        }

        private void OnMicShortPhraseResponseReceivedHandler(object sender, SpeechResponseEventArgs e)
        {
            StartRecordingSession();

            if (e.PhraseResponse.Results.Any())
            {
                CompleteSession(e.PhraseResponse.Results[0].DisplayText);
            }
        }

        private void OnPartialResponseReceivedHandler(object sender, PartialSpeechResponseEventArgs e)
        {
            ContinueSession(e.PartialResult);
        }

        public void OnClosed()
        {
            if (null != _micClient)
            {
                _micClient.Dispose();
            }
        }
    }
    public class SpeechUpdateEventArgs : EventArgs
    {
        public TextMoment Moment { get; }
        public SpeechUpdateEventArgs(TextMoment moment)
        {
            Moment = moment;
        }
    }

    public class SubtitleSession
    {
        public List<TextMoment> TextHistory;
        public long EarliestTime;
        public long LatestTime;

        public void AddTextMoment(TextMoment moment)
        {
            TextHistory.Add(moment);
            LatestTime = moment.Timecode;
        }
    }

    public struct TextMoment
    {
        public readonly long Timecode;
        public readonly string Text;

        public TextMoment(long timeCode, string text)
        {
            Timecode = timeCode;
            Text = text;
        }
    }
}