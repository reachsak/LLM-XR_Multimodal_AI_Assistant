using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using UnityEngine.Networking;
using System.Threading.Tasks;

public class chataiopen2 : MonoBehaviour
{
    private string sttServerUrl = "https://mutw1r7bdb0b.share.zrok.io/transcribe";
    private string ttsServerUrl = "https://slkuovy3zopr.share.zrok.io/generate_wav";
    private string llmServerUrl = "https://de6cqsttz51l.share.zrok.io/completion";

    private string recordedText = "";
    private AudioSource audioSource;
    private bool isRecording = false;
    private string microphoneName;

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        microphoneName = Microphone.devices[0]; // Use the first available microphone
    }

    public void RecordAndSendToAI()
    {
        StartCoroutine(RecordAndSendCoroutine());
    }

    private IEnumerator RecordAndSendCoroutine()
    {
        Debug.Log("Recording...");

        yield return StartCoroutine(RecordAudio(5)); // Record for 5 seconds

        // Send recorded audio to speech-to-text server
        yield return StartCoroutine(TranscribeAudio());

        if (string.IsNullOrEmpty(recordedText))
        {
            Debug.LogWarning("Speech recognition failed.");
            yield break;
        }

        Debug.Log("Recorded text: " + recordedText);

        // Send recorded text to AI
        JObject data = new JObject();
        data["prompt"] = recordedText;
        data["n_predict"] = 64;
        data["stream"] = true;
        string jsonString = data.ToString();

        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(llmServerUrl);
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

    private IEnumerator RecordAudio(int durationInSeconds)
    {
        isRecording = true;
        AudioClip recordedClip = Microphone.Start(microphoneName, false, durationInSeconds, 44100);
        yield return new WaitForSeconds(durationInSeconds);
        Microphone.End(microphoneName);
        isRecording = false;

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



    public void StartSpeechSynthesis(string prompt)
    {
        StartCoroutine(StartSpeechSynthesisCoroutine(prompt));
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
}