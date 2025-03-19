using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.IO;

public class DeepgramVoiceRecognition : MonoBehaviour
{
    private AudioClip audioClip;
    private int sampleRate = 16000;  // Set to 44100 if needed
    private string deepgramApiKey = "adaeb2577a03357cb0bb59302380ec2339607bdc";  // Replace with your API key
    private string deepgramUrl = "https://api.deepgram.com/v1/listen";  // Streaming endpoint

    void Start()
    {
        StartCoroutine(StartRecording());
    }

    IEnumerator StartRecording()
    {
        Debug.Log("🎤 Starting microphone recording...");

        // Start recording audio from the default microphone
        audioClip = Microphone.Start(null, false, 5, sampleRate); // 5 seconds of audio

        yield return new WaitForSeconds(5);  // Wait for recording to complete
        Microphone.End(null);  // Stop recording

        Debug.Log("✅ Recording finished, preparing to send to Deepgram...");

        byte[] audioData = ConvertAudioClipToWav(audioClip);
        SaveAudioFile(audioData);  // Save for debugging

        StartCoroutine(SendAudioToDeepgram(audioData));
    }

    IEnumerator SendAudioToDeepgram(byte[] audioData)
    {
        Debug.Log("🚀 Sending audio to Deepgram...");

        UnityWebRequest request = new UnityWebRequest(deepgramUrl, "POST");
        request.uploadHandler = new UploadHandlerRaw(audioData);
        request.downloadHandler = new DownloadHandlerBuffer();

        // Set request headers
        request.SetRequestHeader("Authorization", "Token " + deepgramApiKey);
        request.SetRequestHeader("Content-Type", "audio/wav");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("❌ Deepgram Error: " + request.error);
        }
        else
        {
            Debug.Log("✅ Deepgram Response: " + request.downloadHandler.text);
        }
    }

    byte[] ConvertAudioClipToWav(AudioClip clip)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            int sampleCount = clip.samples * clip.channels;
            float[] samples = new float[sampleCount];
            clip.GetData(samples, 0);

            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                // Write WAV Header
                writer.Write(new char[4] { 'R', 'I', 'F', 'F' });
                writer.Write(36 + sampleCount * 2);
                writer.Write(new char[4] { 'W', 'A', 'V', 'E' });
                writer.Write(new char[4] { 'f', 'm', 't', ' ' });
                writer.Write(16);
                writer.Write((short)1); // PCM format
                writer.Write((short)clip.channels);
                writer.Write(sampleRate);
                writer.Write(sampleRate * clip.channels * 2);
                writer.Write((short)(clip.channels * 2));
                writer.Write((short)16);
                writer.Write(new char[4] { 'd', 'a', 't', 'a' });
                writer.Write(sampleCount * 2);

                // Write Audio Data
                foreach (var sample in samples)
                {
                    writer.Write((short)(sample * 32767));  // Convert float (-1 to 1) to PCM 16-bit
                }
            }
            return stream.ToArray();
        }
    }

    void SaveAudioFile(byte[] audioData)
    {
        string path = Application.persistentDataPath + "/testAudio.wav";
        File.WriteAllBytes(path, audioData);
        Debug.Log("💾 Audio saved at: " + path);
    }
}
