using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ReaderTest : MonoBehaviour 
{
    private TextMoment _enquedMoment;
    private TextMeshPro _textMesh; 
    public Transform Camera;
    private FileSystemWatcher _watcher;
    private string _logOutputFolder;
    private bool _newMoment;
    private bool _improvedMoment;
    private List<Subtitle> _subtitles;
    private Subtitle _latestSubtitle;
 
    void Start () 
    {
        _textMesh = GetComponent<TextMeshPro>();
        _subtitles = new List<Subtitle>();
        _logOutputFolder = Directory.GetParent(Application.dataPath) + @"\SpeechLog\";
        _watcher = new FileSystemWatcher(_logOutputFolder);
        _watcher.Changed += Watcher_Changed;
        _watcher.EnableRaisingEvents = true;
	}

    private void Watcher_Changed(object sender, FileSystemEventArgs e)
    {
        if (e.ChangeType == WatcherChangeTypes.Changed || e.ChangeType == WatcherChangeTypes.Created)
        {
            string log = GetLog(e.FullPath);
            string latestLine = log.Split('\n').First();
            string[] components = latestLine.Split(':');
            
            long timeCode = Convert.ToInt64(components[0]);
            string text = components[1];
            _newMoment = e.ChangeType == WatcherChangeTypes.Created;
            _improvedMoment = e.ChangeType == WatcherChangeTypes.Changed;
            _enquedMoment = new TextMoment(timeCode, text);
        }
    }

    private void OnDestroy()
    {
        _watcher.EnableRaisingEvents = false;
        _watcher.Dispose();
    }

    void Update () 
    {
        if(_newMoment)
        {
            TextMoment moment = _enquedMoment;
            _newMoment = false;
            ImproveMoment(moment);
        }
        if(_improvedMoment)
        {
            TextMoment moment = _enquedMoment;
            _improvedMoment = false;
            CreateNewMoment(moment);
        }
        if (_latestSubtitle != null)
        {
            _textMesh.text = _latestSubtitle.LatestMoment.Text;
        }
        LookAtCamera();
	}

    private void LookAtCamera()
    {
        Vector3 target = new Vector3(Camera.position.x, transform.position.y, Camera.position.z);
        transform.LookAt(target, Vector3.up);
        transform.Rotate(0, 180, 0);
    }

    private void CreateNewMoment(TextMoment newMoment)
    {
        Subtitle subtitle = new Subtitle(newMoment);
        _subtitles.Add(subtitle);
        _latestSubtitle = subtitle;
    }

    private void ImproveMoment(TextMoment latestMoment)
    {
        _latestSubtitle.AddTextMoment(latestMoment);
    }

    private string GetLog(string path)
    {
        using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
        {
            using (StreamReader reader = new StreamReader(fs))
            {
                while (true)
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }
}
public class Subtitle
{
    private TextMoment _latestMoment;
    public TextMoment LatestMoment { get{ return _latestMoment; } }

    private readonly List<TextMoment> _textHistory;
    public List<TextMoment> TextHistory { get{ return _textHistory; } }
    public long EarliestTime { get { return _textHistory[0].Timecode; } }
    public long LatestTime { get { return _latestMoment.Timecode; } }

    public Subtitle(TextMoment firstMoment)
    {
        _textHistory = new List<TextMoment>() { firstMoment };
        _latestMoment = firstMoment;
    }

    public void AddTextMoment(TextMoment moment)
    {
        _textHistory.Add(moment);
        _latestMoment = moment;
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