using UnityEngine;
using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine.UI;
using Microsoft.CognitiveServices.Speech;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Microsoft.CognitiveServices.Speech.Audio;

public class VisionAI : MonoBehaviour
{
    private string speechKey = "8daf8bda21e54a438ceb9be7f055577a";
    private string speechRegion = "eastus";
    private string speechRecognitionLanguage = "en-US";

    private SpeechRecognizer speechRecognizer;
    private SpeechConfig speechConfig;
    private AudioConfig audioConfig;

    private string recordedText = "";

    UnityEngine.Windows.WebCam.PhotoCapture photoCaptureObject;
    Texture2D targetTexture;
    public Button recordButton;
    public Button askAIButton;

    public void Start()
    {
        InitializeSpeechRecognition();
        StartCapture();

        // Add click listeners to the buttons
        recordButton.onClick.AddListener(RecordAndAskAI);
        askAIButton.onClick.AddListener(CaptureAndProcessImage);
    }

    private void InitializeSpeechRecognition()
    {
        speechConfig = SpeechConfig.FromSubscription(speechKey, speechRegion);
        speechConfig.SpeechRecognitionLanguage = speechRecognitionLanguage;

        audioConfig = AudioConfig.FromDefaultMicrophoneInput();

        speechRecognizer = new SpeechRecognizer(speechConfig, audioConfig);
    }

    public void RecordAndSendToAI()
    {
        StartCoroutine(RecordAndSendCoroutine());
    }

    private IEnumerator RecordAndSendCoroutine()
    {
        Debug.Log("Recording...");
        var recognitionOperation = speechRecognizer.RecognizeOnceAsync();
        yield return recognitionOperation;
        var recognitionResult = recognitionOperation.Result;
        if (recognitionResult.Reason == ResultReason.RecognizedSpeech)
        {
            recordedText = recognitionResult.Text;
            Debug.Log("Recorded text: " + recordedText);
        }
        else
        {
            Debug.LogWarning("Speech recognition failed: " + recognitionResult.Reason);
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
        OutputTextToSpeechAsync(responseText).Wait();

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
        string imageUrl = "https://reachsak.pagekite.me/completion"; // Replace with your API endpoint
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(imageUrl);
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

    async Task OutputTextToSpeechAsync(string text)
    {
        var speechConfig = SpeechConfig.FromSubscription(speechKey, speechRegion);
        speechConfig.SpeechSynthesisVoiceName = "en-US-AvaMultilingualNeural";

        using (var speechSynthesizer = new SpeechSynthesizer(speechConfig))
        {
            var speechSynthesisResult = await speechSynthesizer.SpeakTextAsync(text);
            OutputSpeechSynthesisResult(speechSynthesisResult, text);
        }
    }

    void OutputSpeechSynthesisResult(SpeechSynthesisResult speechSynthesisResult, string text)
    {
        switch (speechSynthesisResult.Reason)
        {
            case ResultReason.SynthesizingAudioCompleted:
                Debug.Log($"Speech synthesized for text: [{text}]");
                break;
            case ResultReason.Canceled:
                var cancellation = SpeechSynthesisCancellationDetails.FromResult(speechSynthesisResult);
                Debug.LogError($"CANCELED: Reason={cancellation.Reason}");

                if (cancellation.Reason == CancellationReason.Error)
                {
                    Debug.LogError($"CANCELED: ErrorCode={cancellation.ErrorCode}");
                    Debug.LogError($"CANCELED: ErrorDetails=[{cancellation.ErrorDetails}]");
                    Debug.LogError($"CANCELED: Did you set the speech resource key and region values?");
                }
                break;
            default:
                break;
        }
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
        speechRecognizer.Dispose();
        audioConfig.Dispose();
    }
}
