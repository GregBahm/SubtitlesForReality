using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ReaderTest : MonoBehaviour 
{
    private FileSystemWatcher watcher;
    private string logOutputFolder;
    private bool newMoment;
    private bool improvedMoment;
    private List<Subtitle> subtitles;
    private TextMoment latestMoment;
 
    void Start () 
    {
        subtitles = new List<Subtitle>();
        logOutputFolder = Directory.GetParent(Application.dataPath) + @"\SpeechLog\";
        watcher = new FileSystemWatcher(logOutputFolder);
        watcher.Changed += Watcher_Changed;
        watcher.EnableRaisingEvents = true;
	}

    private void Watcher_Changed(object sender, FileSystemEventArgs e)
    {
        if (e.ChangeType == WatcherChangeTypes.Changed || e.ChangeType == WatcherChangeTypes.Created)
        {
            string log = GetLog(e.FullPath);
            string latestLine = log.Split('\n').First();
            string[] components = latestLine.Split(':');

            Debug.Log(components);
            Debug.Log(components[0]);
            Debug.Log(components[1]);
            long timeCode = Convert.ToInt64(components[0]);
            string text = components[1];
            newMoment = e.ChangeType == WatcherChangeTypes.Created;
            improvedMoment = e.ChangeType == WatcherChangeTypes.Changed;
            latestMoment = new TextMoment(timeCode, text);
        }
    }

    private void OnDestroy()
    {
        watcher.EnableRaisingEvents = false;
        watcher.Dispose();
    }

    void Update () 
    {
        if(newMoment)
        {
            TextMoment moment = latestMoment;
            newMoment = false;
            ImproveMoment(moment);
        }
        if(improvedMoment)
        {
            TextMoment moment = latestMoment;
            improvedMoment = false;
            CreateNewMoment(latestMoment);
        }
	}

    private void CreateNewMoment(TextMoment newMoment)
    {
        if(subtitles.Any())
        {
            subtitles.Last().gameObject.SetActive(false);
        }
        GameObject newSubtitleObject = new GameObject("Subtitle " + newMoment.Timecode);
        TextMesh textMesh = newSubtitleObject.AddComponent<TextMesh>();
        textMesh.fontSize = 72;
        Subtitle subtitleBehavior = newSubtitleObject.AddComponent<Subtitle>();
        subtitleBehavior.TextHistory = new List<TextMoment>();
        subtitleBehavior.EarliestTime = newMoment.Timecode;
        subtitleBehavior.TextObject = textMesh;
        subtitleBehavior.AddTextMoment(newMoment);
        subtitles.Add(subtitleBehavior);
    }

    private void ImproveMoment(TextMoment latestMoment)
    {
        subtitles.Last().AddTextMoment(latestMoment);
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
public class Subtitle : MonoBehaviour
{
    public TextMesh TextObject;
    public List<TextMoment> TextHistory;
    public long EarliestTime;
    public long LatestTime;

    public void AddTextMoment(TextMoment moment)
    {
        TextHistory.Add(moment);
        LatestTime = moment.Timecode;
        TextObject.text = moment.Text;
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