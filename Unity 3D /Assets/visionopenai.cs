using UnityEngine;
using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine.UI;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

public class visionopenai : MonoBehaviour
{
    private string sttServerUrl = "https://mutw1r7bdb0b.share.zrok.io/transcribe";
    private string ttsServerUrl = "https://slkuovy3zopr.share.zrok.io/generate_wav";
    private string llmServerUrl = "https://695lysg67pfr.share.zrok.io/completion";

    private string recordedText = "";
    private AudioSource audioSource;

    UnityEngine.Windows.WebCam.PhotoCapture photoCaptureObject;
    Texture2D targetTexture;
    public Button recordButton;
    public Button askAIButton;

    public void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        StartCapture();

        // Add click listeners to the buttons
        recordButton.onClick.AddListener(RecordAndAskAI);
        askAIButton.onClick.AddListener(CaptureAndProcessImage);
    }

    public void RecordAndSendToAI()
    {
        StartCoroutine(RecordAndSendCoroutine());
    }

    private IEnumerator RecordAndSendCoroutine()
    {
        Debug.Log("Recording...");
        yield return StartCoroutine(RecordAudio(5)); // Record for 5 seconds
        yield return StartCoroutine(TranscribeAudio());

        if (string.IsNullOrEmpty(recordedText))
        {
            Debug.LogWarning("Speech recognition failed.");
            yield break;
        }

        Debug.Log("Recorded text: " + recordedText);
    }

    private IEnumerator RecordAudio(int durationInSeconds)
    {
        string microphoneName = Microphone.devices[0];
        AudioClip recordedClip = Microphone.Start(microphoneName, false, durationInSeconds, 44100);
        yield return new WaitForSeconds(durationInSeconds);
        Microphone.End(microphoneName);

        // Convert AudioClip to WAV byte array
        byte[] wavData = ConvertAudioClipToWav(recordedClip);

        // Save WAV data for transcription
        File.WriteAllBytes(Application.persistentDataPath + "/recordedAudio.wav", wavData);
    }

    private byte[] ConvertAudioClipToWav(AudioClip clip)
    {
        float[] samples = new float[clip.samples];
        clip.GetData(samples, 0);

        using (MemoryStream stream = new MemoryStream())
        {
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                // WAV file header
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(36 + samples.Length * 2);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
                writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
                writer.Write(16);
                writer.Write((ushort)1);
                writer.Write((ushort)1);
                writer.Write(44100);
                writer.Write(44100 * 2);
                writer.Write((ushort)2);
                writer.Write((ushort)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                writer.Write(samples.Length * 2);

                // Convert float samples to 16-bit PCM
                foreach (float sample in samples)
                {
                    writer.Write((short)(sample * 32767));
                }
            }
            return stream.ToArray();
        }
    }

    private IEnumerator TranscribeAudio()
    {
        string filePath = Application.persistentDataPath + "/recordedAudio.wav";
        byte[] audioData = File.ReadAllBytes(filePath);

        WWWForm form = new WWWForm();
        form.AddBinaryData("file", audioData, "audio.wav", "audio/wav");

        using (UnityWebRequest www = UnityWebRequest.Post(sttServerUrl, form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error: " + www.error);
            }
            else
            {
                string responseText = www.downloadHandler.text;
                JObject result = JObject.Parse(responseText);
                recordedText = result["text"].ToString();
            }
        }
    }

    public async void CaptureAndProcessImage()
    {
        if (photoCaptureObject != null)
        {
            // Capture image from photoCaptureObject
            await CaptureImage();
        }
        else
        {
            Debug.LogError("Photo capture object is null.");
        }
    }

    async Task CaptureImage()
    {
        await Task.Delay(TimeSpan.FromSeconds(0.5f)); // Delay to ensure proper capture

        // Convert SupportedResolutions into an array and sort it
        Resolution[] resolutions = UnityEngine.Windows.WebCam.PhotoCapture.SupportedResolutions.ToArray();
        Resolution cameraResolution = resolutions.OrderByDescending((res) => res.width * res.height).First();

        // Create camera parameters
        UnityEngine.Windows.WebCam.CameraParameters cameraParameters = new UnityEngine.Windows.WebCam.CameraParameters();
        cameraParameters.hologramOpacity = 0.0f;
        cameraParameters.cameraResolutionWidth = cameraResolution.width;
        cameraParameters.cameraResolutionHeight = cameraResolution.height;
        cameraParameters.pixelFormat = UnityEngine.Windows.WebCam.CapturePixelFormat.BGRA32;

        // Start photo mode
        photoCaptureObject.StartPhotoModeAsync(cameraParameters, delegate (UnityEngine.Windows.WebCam.PhotoCapture.PhotoCaptureResult result) {
            if (result.success)
            {
                Debug.Log("Photo capture mode started.");
            }
            else
            {
                Debug.LogError("Unable to start photo capture mode.");
            }
        });

        // Take photo (no need to await this method)
        photoCaptureObject.TakePhotoAsync(OnCapturedPhotoToMemory);
    }

    void StartCapture()
    {
        // Convert SupportedResolutions into a list and sort it
        List<Resolution> resolutionsList = UnityEngine.Windows.WebCam.PhotoCapture.SupportedResolutions.ToList();
        Resolution cameraResolution = resolutionsList.OrderByDescending((res) => res.width * res.height).First();
        targetTexture = new Texture2D(cameraResolution.width, cameraResolution.height);

        // Create a PhotoCapture object
        UnityEngine.Windows.WebCam.PhotoCapture.CreateAsync(false, OnPhotoCaptureCreated);
    }

    void OnPhotoCaptureCreated(UnityEngine.Windows.WebCam.PhotoCapture captureObject)
    {
        photoCaptureObject = captureObject;
        UnityEngine.Windows.WebCam.CameraParameters cameraParameters = new UnityEngine.Windows.WebCam.CameraParameters();
        cameraParameters.hologramOpacity = 0.0f;
        cameraParameters.cameraResolutionWidth = targetTexture.width;
        cameraParameters.cameraResolutionHeight = targetTexture.height;
        cameraParameters.pixelFormat = UnityEngine.Windows.WebCam.CapturePixelFormat.BGRA32;

        // Activate the camera
        photoCaptureObject.StartPhotoModeAsync(cameraParameters, delegate (UnityEngine.Windows.WebCam.PhotoCapture.PhotoCaptureResult result) {
            if (result.success)
            {
                Debug.Log("Photo capture mode started.");
            }
            else
            {
                Debug.LogError("Unable to start photo capture mode.");
            }
        });
    }

    void OnCapturedPhotoToMemory(UnityEngine.Windows.WebCam.PhotoCapture.PhotoCaptureResult result, UnityEngine.Windows.WebCam.PhotoCaptureFrame photoCaptureFrame)
    {
        // Copy the raw image data into the target texture
        photoCaptureFrame.UploadImageDataToTexture(targetTexture);

        // Encode texture to PNG format
        byte[] imageData = targetTexture.EncodeToJPG(30);

        // Convert image data to Base64 string
        string base64String = Convert.ToBase64String(imageData);

        // Send request and get response text
        string responseText = SendRequestAndGetResponse(base64String).Result;

        // Output response text as speech
        StartCoroutine(StartSpeechSynthesisCoroutine(responseText));

        // Deactivate the camera
        photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);
    }

    async Task<string> SendRequestAndGetResponse(string base64String)
    {
        // Create JObject for image data
        JObject imageDataJson = new JObject();
        imageDataJson["data"] = base64String;
        imageDataJson["id"] = 12;

        // Create JArray and add image data JObject
        JArray imageDataArray = new JArray();
        imageDataArray.Add(imageDataJson);

        // Create JObject for request data
        JObject requestData = new JObject();
        requestData["prompt"] = recordedText;
        requestData["n_predict"] = 64;
        requestData["image_data"] = imageDataArray;
        requestData["stream"] = true;

        // Convert JObject to JSON string
        string jsonString = requestData.ToString();

        // Set up HTTP request
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(llmServerUrl);
        request.Method = "POST";
        request.ContentType = "application/json";

        // Write JSON string to request stream
        using (StreamWriter streamWriter = new StreamWriter(request.GetRequestStream()))
        {
            streamWriter.Write(jsonString);
            streamWriter.Flush();
            streamWriter.Close();
        }

        // Send request and get response
        string responseText = "";
        using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
        {
            Debug.Log("Sent request to the server...\n");
            using (StreamReader streamReader = new StreamReader(response.GetResponseStream()))
            {
                string line;
                while ((line = streamReader.ReadLine()) != null)
                {
                    string content = line.Trim().Split(new[] { "\n\n" }, StringSplitOptions.None)[0];
                    try
                    {
                        string[] contentSplit = content.Split(new[] { "data: " }, StringSplitOptions.None);
                        if (contentSplit.Length > 1)
                        {
                            JObject contentJson = JObject.Parse(contentSplit[1]);
                            responseText += contentJson["content"].ToString();
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("Error: " + e.Message);
                        break;
                    }
                }
            }
        }

        // Clean up and process the response text
        responseText = responseText.Replace("\n", "").Replace("\r", "");
        responseText = responseText.TrimStart();

        return responseText;
    }

    private IEnumerator StartSpeechSynthesisCoroutine(string prompt)
    {
        WWWForm form = new WWWForm();
        form.AddField("text", prompt);

        using (UnityWebRequest www = UnityWebRequest.Post(ttsServerUrl, form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error: " + www.error);
            }
            else
            {
                byte[] wavFileBytes = www.downloadHandler.data;
                PlayAudioData(wavFileBytes);
                Debug.Log($"Speech synthesized for text: [{prompt}]");
            }
        }
    }

    private async void PlayAudioData(byte[] wavFileBytes)
    {
        using (MemoryStream stream = new MemoryStream(wavFileBytes))
        {
            AudioClip audioClip = await LoadAudioClipFromStream(stream);
            if (audioClip != null)
            {
                audioSource.clip = audioClip;
                audioSource.Play();

                // Wait for playback to finish
                while (audioSource.isPlaying)
                {
                    await Task.Delay(100);
                }

                Debug.Log("WAV file played.");
            }
            else
            {
                Debug.LogError("Failed to load audio clip from memory stream.");
            }
        }
    }

    private async Task<AudioClip> LoadAudioClipFromStream(MemoryStream stream)
    {
        AudioClip audioClip = null;

        // Create a temporary file to store the WAV data
        string tempPath = Path.Combine(Application.temporaryCachePath, "temp.wav");
        File.WriteAllBytes(tempPath, stream.ToArray());

        // Use UnityWebRequest to load the audio clip
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + tempPath, AudioType.WAV))
        {
            var operation = www.SendWebRequest();

            while (!operation.isDone)
            {
                await Task.Yield();
            }

            if (www.result == UnityWebRequest.Result.Success)
            {
                audioClip = DownloadHandlerAudioClip.GetContent(www);
            }
            else
            {
                Debug.LogError($"Failed to load audio clip: {www.error}");
            }
        }

        // Clean up the temporary file
        File.Delete(tempPath);

        return audioClip;
    }

    void OnStoppedPhotoMode(UnityEngine.Windows.WebCam.PhotoCapture.PhotoCaptureResult result)
    {
        photoCaptureObject.Dispose();
        photoCaptureObject = null;
    }

    public void RecordAndAskAI()
    {
        StartCoroutine(RecordAndAskAICoroutine());
    }

    private IEnumerator RecordAndAskAICoroutine()
    {
        // Record voice
        yield return RecordAndSendCoroutine();

        // Capture and process image
        CaptureAndProcessImage();
    }

    public void OnDestroy()
    {
        // Release the resources
        if (photoCaptureObject != null)
        {
            photoCaptureObject.Dispose();
            photoCaptureObject = null;
        }
    }
}