using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class ReaderTest : MonoBehaviour 
{
    private const string logOutputPath = @"C:\Users\Lisa\Documents\SubtitlesForReality\log.txt";
    public Text TextObject;
 
    // Use this for initialization
    void Start () 
    {
		
	}
	
	// Update is called once per frame
	void Update () 
    {
        TextObject.text = GetLog();
	}

    private string GetLog()
    {
        using (FileStream fs = new FileStream(logOutputPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
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
