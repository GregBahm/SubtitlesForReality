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
    public Transform Camera;
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
        LookAtCamera();
	}

    private void LookAtCamera()
    {
        Vector3 target = new Vector3(Camera.position.x, transform.position.y, Camera.position.z);
        transform.LookAt(target, Vector3.up);
    }

    private void CreateNewMoment(TextMoment newMoment)
    {
        if(subtitles.Any())
        {
            subtitles.Last().gameObject.SetActive(false);
        }
        GameObject newSubtitleObject = new GameObject("Subtitle " + newMoment.Timecode);
        newSubtitleObject.transform.rotation = Quaternion.Euler(0, 180, 0);
        newSubtitleObject.transform.SetParent(transform, false);
        TextMeshPro textMesh = newSubtitleObject.AddComponent<TextMeshPro>();
        textMesh.rectTransform.sizeDelta = new Vector2(1, .24f);
        textMesh.fontSize = .5f;
        textMesh.alignment = TextAlignmentOptions.BottomLeft;
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
    public TextMeshPro TextObject;
    public List<TextMoment> TextHistory;
    public long EarliestTime;
    public long LatestTime;

    public const float DefaultOpacity = 3;
    public const float OpacityFade = 0.01f;
    public float Opacity;

    private void Start()
    {
        Opacity = DefaultOpacity;
    }

    public void AddTextMoment(TextMoment moment)
    {
        TextHistory.Add(moment);
        LatestTime = moment.Timecode;
        TextObject.text = moment.Text;
        Opacity = DefaultOpacity;
    }

    private void Update()
    {
        Opacity -= OpacityFade;
        TextObject.color = new Color(1, 1, 1, Mathf.Min(Opacity, 1));
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