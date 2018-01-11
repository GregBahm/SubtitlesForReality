using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Runtime.Serialization;
using System.Web;
using System.Threading;
using UnityEngine;
using System.Collections.Specialized;

public class TestScript : MonoBehaviour
{
    public bool Do;
    public string ResponseString;
    public TextAsset testText;

    void Start()
    {
        AudioSource audio = GetComponent<AudioSource>();
        audio.clip = Microphone.Start(null, true, 15, 16000);

        ServicePointManager.ServerCertificateValidationCallback = CertificateValidationCallBack; // This voodoo is necessary so that Unity can do certifications
        SpeechServerAuthenticator authenticator = new SpeechServerAuthenticator("8acd5da4fcc84791a4be9159f9296895");
        communicator = new SpeechServerCommunicator(authenticator);
    }

    private void Update()
    {
        if(Do)
        {
            Do = false;
            communicator.DoTheThing();
        }
        ResponseString = communicator.ResponseString;
    }

    private static bool CertificateValidationCallBack(object sender,
        System.Security.Cryptography.X509Certificates.X509Certificate certificate,
        System.Security.Cryptography.X509Certificates.X509Chain chain,
        System.Net.Security.SslPolicyErrors sslPolicyErrors)
    {
        return true; // Ignore bad certs
    }

    SpeechServerCommunicator communicator;
    
    private void OnDestroy()
    {
        communicator.DestroyThread();
    }
}

public class SpeechServerCommunicator
{
    private readonly string requestUri;
    private readonly string contentType;
    private readonly SpeechServerAuthenticator authenticator;

    private Thread thread;

    public string ResponseString;

    public SpeechServerCommunicator(SpeechServerAuthenticator authenticator)
    {
        this.authenticator = authenticator;

        requestUri = @"https://speech.platform.bing.com/speech/recognition/conversation/cognitiveservices/v1?language=en-US&format=detailed";

        contentType = @"audio/wav; codec=""audio/pcm""; samplerate=16000";
    }

    public void DoTheThing()
    {
        thread = new Thread(() => TranslateAudio());
        thread.IsBackground = true;
        thread.Start();
    }

    private void TranslateAudio()
    {
        HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(requestUri);
        request.SendChunked = true;
        request.Accept = @"application/json;text/xml";
        request.Method = "POST";
        request.ProtocolVersion = HttpVersion.Version11;

        string token = authenticator.GetAccessToken();
        request.ContentType = contentType;
        request.Headers["Authorization"] = "Bearer " + token;

        string audioFile = @"D:\MixedWorldMultitaskingVideo\simplerSample.wav";

        Debug.Log("Getting this party started.");
        using (FileStream fs = new FileStream(audioFile, FileMode.Open, FileAccess.Read))
        {
            UploadData(fs, request);
            Debug.Log("Uploaded");

            using (WebResponse webResponse = request.GetResponse())
            {
                Debug.Log("Response Status:" + ((HttpWebResponse)webResponse).StatusCode);

                using (StreamReader sr = new StreamReader(webResponse.GetResponseStream()))
                {
                    string responseString = sr.ReadToEnd();
                    Debug.Log(responseString);
                    SpeechResponse response = SpeechResponse.CreateFromJson(responseString);
                    ResponseString = response.NBest[0].Display;
                }
            }
        }
    }

    private void UploadData(FileStream fs, HttpWebRequest request)
    {
        byte[] buffer = null;
        int bytesRead = 0;
        using (Stream requestStream = request.GetRequestStream())
        {
            buffer = new Byte[checked((uint)Math.Min(1024, (int)fs.Length))];
            while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) != 0)
            {
                requestStream.Write(buffer, 0, bytesRead);
            }

            // Flush
            requestStream.Flush();
        }
    }

    internal void DestroyThread()
    {
        if(thread != null)
        {
            thread.Abort();
        }
    }
}


[System.Serializable]
public class SpeechResponse
{
    public string RecognitionStatus;
    public int Offset;
    public int Duration;
    public BestResponse[] NBest;

    public static SpeechResponse CreateFromJson(string rawResponseJson)
    {
        return JsonUtility.FromJson<SpeechResponse>(rawResponseJson);
    }
}

[System.Serializable]
public class BestResponse
{
    public float Confidence;
    public string Lexical;
    public string ITN;
    public string MaskedITN;
    public string Display;
}

public class SpeechServerAuthenticator
{
    public static readonly string FetchTokenUri = "https://api.cognitive.microsoft.com/sts/v1.0";
    private string subscriptionKey;
    private string token;
    private Timer accessTokenRenewer;

    //Access token expires every 10 minutes. Renew it every 9 minutes only.
    private const int RefreshTokenDuration = 9;

    public SpeechServerAuthenticator(string subscriptionKey)
    {
        this.subscriptionKey = subscriptionKey;

        this.token = FetchToken(FetchTokenUri, subscriptionKey);
        // renew the token every specfied minutes
        accessTokenRenewer = new Timer(new TimerCallback(OnTokenExpiredCallback),
                                       this,
                                       TimeSpan.FromMinutes(RefreshTokenDuration),
                                       TimeSpan.FromMilliseconds(-1));
    }

    public string GetAccessToken()
    {
        return this.token;
    }

    private void RenewAccessToken()
    {
        this.token = FetchToken(FetchTokenUri, this.subscriptionKey);
        Console.WriteLine("Renewed token.");
    }

    private void OnTokenExpiredCallback(object stateInfo)
    {
        try
        {
            RenewAccessToken();
        }
        catch (Exception ex)
        {
            Console.WriteLine(string.Format("Failed renewing access token. Details: {0}", ex.Message));
        }
        finally
        {
            try
            {
                accessTokenRenewer.Change(TimeSpan.FromMinutes(RefreshTokenDuration), TimeSpan.FromMilliseconds(-1));
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("Failed to reschedule the timer to renew access token. Details: {0}", ex.Message));
            }
        }
    }

    private string FetchToken(string fetchUri, string subscriptionKey)
    {
        using (var client = new WebClient())
        {
            NameValueCollection values = new NameValueCollection();
            values["Ocp-Apim-Subscription-Key"] = subscriptionKey;
            client.Headers.Add(values);

            UriBuilder uriBuilder = new UriBuilder(fetchUri);
            uriBuilder.Path += "/issueToken"; 
            byte[] response = client.UploadData(uriBuilder.Uri.AbsoluteUri, "POST", new byte[0]);
            return Encoding.Default.GetString(response);
        }
    }
}