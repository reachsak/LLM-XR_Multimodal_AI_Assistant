using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Net;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Newtonsoft.Json.Linq;

public class Aiagent : MonoBehaviour
{
    private string speechKey = "8daf8bda21e54a438ceb9be7f055577a";
    private string speechRegion = "eastus";
    private string speechRecognitionLanguage = "en-US";

    private SpeechRecognizer speechRecognizer;
    private SpeechConfig speechConfig;
    private AudioConfig audioConfig;

    private string recordedText = "";

    public void Start()
    {
        InitializeSpeechRecognition();
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
            yield break;
        }

        // Send recorded text to AI
        string apiUrl = "https://ai-reachsak.pagekite.me/process_input";
        JObject data = new JObject();
        data["prompt"] = recordedText;
        data["n_predict"] = 512;
        data["stream"] = true;
        string jsonString = data.ToString();

        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(apiUrl);
        request.Method = "POST";
        request.ContentType = "application/json";

        using (StreamWriter streamWriter = new StreamWriter(request.GetRequestStream()))
        {
            streamWriter.Write(jsonString);
            streamWriter.Flush();
            streamWriter.Close();
        }

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
        responseText = responseText.Replace("\n", "").Replace("\r", "");

        // Set AI response as prompt for speech synthesis
        StartSpeechSynthesis(responseText);
    }

    public void StartSpeechSynthesis(string prompt)
    {
        StartCoroutine(StartSpeechSynthesisCoroutine(prompt));
    }

    private IEnumerator StartSpeechSynthesisCoroutine(string prompt)
    {
        var speechConfig = SpeechConfig.FromSubscription(speechKey, speechRegion);
        speechConfig.SpeechSynthesisVoiceName = "en-US-AvaMultilingualNeural";

        using (var speechSynthesizer = new SpeechSynthesizer(speechConfig))
        {
            var speechSynthesisResult = speechSynthesizer.SpeakTextAsync(prompt);
            yield return new WaitUntil(() => speechSynthesisResult.IsCompleted);

            OutputSpeechSynthesisResult(speechSynthesisResult.Result, prompt);
        }
    }

    private void OutputSpeechSynthesisResult(SpeechSynthesisResult speechSynthesisResult, string text)
    {
        switch (speechSynthesisResult.Reason)
        {
            case ResultReason.SynthesizingAudioCompleted:
                Debug.Log($"Speech synthesized for text: [{text}]");
                // Do something with the synthesized audio if needed
                break;
            case ResultReason.Canceled:
                var cancellation = SpeechSynthesisCancellationDetails.FromResult(speechSynthesisResult);
                Debug.LogWarning($"CANCELED: Reason={cancellation.Reason}");

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

    public void OnDestroy()
    {
        // Release the resources
        speechRecognizer.Dispose();
        audioConfig.Dispose();
    }
}
