using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using NativeWebSocket;
using TMPro;

public class DeepgramSpeechToText : MonoBehaviour
{
    [Header("Deepgram Settings")]
    [SerializeField] private string deepgramApiKey = "8574d72e6ff9f6de1b6c61e31dd5cbfebf61d9ef";
    [SerializeField] private string deepgramEndpoint = "wss://api.deepgram.com/v1/listen?encoding=linear16&sample_rate=16000&channels=1&interim_results=true";

    [Header("UI References")]
    [SerializeField] private TMP_Text subtitleText;
    [SerializeField] private int maxSubtitleChars = 100;
    [SerializeField] private string subtitleFilePath = "Assets/Subtitles/transcript.txt";

    private WebSocket webSocket;
    private AudioClip microphoneClip;
    private bool isRecording = false;
    private bool isConnected = false;
    private string deviceName;
    private int sampleRate = 16000;
    private float[] sampleBuffer;
    private int bufferSize = 4096;

    [Serializable]
    private class DeepgramResponse
    {
        [Serializable]
        public class Alternative
        {
            public string transcript;
        }

        [Serializable]
        public class Channel
        {
            public Alternative[] alternatives;
        }

        [Serializable]
        public class Result
        {
            public bool is_final;
            public Channel channel;
        }

        public Result result;
    }

    void Start()
    {
        InitializeSubtitleFile();
        sampleBuffer = new float[bufferSize];

        // Check available microphone devices
        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("No microphone device found!");
            return;
        }

        deviceName = Microphone.devices[0];
        Debug.Log("Using microphone: " + deviceName);
    }

    async void OnDestroy()
    {
        await StopRecordingAsync();
    }

    void Update()
    {
        if (webSocket != null)
        {
            // Keep the connection alive
            webSocket.DispatchMessageQueue();
        }
    }

    public void ToggleRecording()
    {
        if (!isRecording)
        {
            StartCoroutine(StartRecordingCoroutine());
        }
        else
        {
            StartCoroutine(StopRecordingCoroutine());
        }
    }

    private void InitializeSubtitleFile()
    {
        // Create directory if it doesn't exist
        string directory = Path.GetDirectoryName(subtitleFilePath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Create or clear the file
        File.WriteAllText(subtitleFilePath, "");
    }

    private IEnumerator StartRecordingCoroutine()
    {
        if (isRecording) yield break;

        // Connect to Deepgram WebSocket
        Task connectTask = ConnectToDeepgramAsync();

        // Wait for connection to complete
        while (!connectTask.IsCompleted)
        {
            yield return null;
        }

        if (connectTask.IsFaulted)
        {
            Debug.LogError("Failed to connect to Deepgram: " + connectTask.Exception.Message);
            yield break;
        }

        // Start microphone recording
        microphoneClip = Microphone.Start(deviceName, true, 10, sampleRate);
        isRecording = true;

        // Start sending audio data
        StartCoroutine(SendAudioData());

        Debug.Log("Recording started");
    }

    private async Task StartRecordingAsync()
    {
        if (isRecording) return;

        // Connect to Deepgram WebSocket
        await ConnectToDeepgramAsync();

        // Start microphone recording
        microphoneClip = Microphone.Start(deviceName, true, 10, sampleRate);
        isRecording = true;

        // Start sending audio data
        StartCoroutine(SendAudioData());

        Debug.Log("Recording started");
    }

    private IEnumerator StopRecordingCoroutine()
    {
        if (!isRecording) yield break;

        // Stop microphone
        if (Microphone.IsRecording(deviceName))
        {
            Microphone.End(deviceName);
        }

        // Close WebSocket connection
        if (webSocket != null)
        {
            Task closeTask = webSocket.Close();

            // Wait for connection to close
            while (!closeTask.IsCompleted)
            {
                yield return null;
            }

            isConnected = false;
        }

        isRecording = false;
        Debug.Log("Recording stopped");
    }

    private async Task StopRecordingAsync()
    {
        if (!isRecording) return;

        // Stop microphone
        if (Microphone.IsRecording(deviceName))
        {
            Microphone.End(deviceName);
        }

        // Close WebSocket connection
        if (webSocket != null)
        {
            await webSocket.Close();
            isConnected = false;
        }

        isRecording = false;
        Debug.Log("Recording stopped");
    }

    private async Task ConnectToDeepgramAsync()
    {
        // Setup headers for authentication
        Dictionary<string, string> headers = new Dictionary<string, string>
        {
            { "Authorization", "Token " + deepgramApiKey }
        };

        // Create WebSocket with headers
        webSocket = new WebSocket(deepgramEndpoint, headers);

        // Setup event handlers
        webSocket.OnOpen += () =>
        {
            Debug.Log("WebSocket connection opened");
            isConnected = true;
        };

        webSocket.OnMessage += (bytes) =>
        {
            string jsonResponse = System.Text.Encoding.UTF8.GetString(bytes);
            ProcessDeepgramResponse(jsonResponse);
        };

        webSocket.OnError += (e) =>
        {
            Debug.LogError("WebSocket error: " + e);
        };

        webSocket.OnClose += (e) =>
        {
            Debug.Log("WebSocket connection closed with code: " + e);
            isConnected = false;
        };

        // Connect to WebSocket server
        await webSocket.Connect();
    }

    private void ProcessDeepgramResponse(string jsonResponse)
    {
        try
        {
            DeepgramResponse response = JsonUtility.FromJson<DeepgramResponse>(jsonResponse);

            if (response != null && response.result != null &&
                response.result.channel != null &&
                response.result.channel.alternatives != null &&
                response.result.channel.alternatives.Length > 0)
            {
                string transcript = response.result.channel.alternatives[0].transcript;
                bool isFinal = response.result.is_final;

                // Update UI on main thread
                UpdateSubtitles(transcript, isFinal);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error parsing Deepgram response: " + e.Message);
        }
    }

    private void UpdateSubtitles(string newText, bool isFinal)
    {
        if (string.IsNullOrEmpty(newText)) return;

        // Update UI
        if (subtitleText != null)
        {
            // Truncate if too long
            if (subtitleText.text.Length + newText.Length > maxSubtitleChars)
            {
                subtitleText.text = subtitleText.text.Substring(subtitleText.text.Length / 2) + newText;
            }
            else
            {
                subtitleText.text += newText + " ";
            }
        }

        // Write to file if it's a final result
        if (isFinal)
        {
            AppendToSubtitleFile(newText);
        }
    }

    private void AppendToSubtitleFile(string text)
    {
        try
        {
            using (StreamWriter writer = new StreamWriter(subtitleFilePath, true))
            {
                writer.WriteLine(text);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error writing to subtitle file: " + e.Message);
        }
    }

    IEnumerator SendAudioData()
    {
        int prevPos = 0;

        while (isRecording && isConnected)
        {
            int currPos = Microphone.GetPosition(deviceName);
            if (currPos < prevPos)
            {
                // Handle wrap-around
                yield return null;
                prevPos = currPos;
                continue;
            }

            if (currPos == prevPos)
            {
                // No new data
                yield return null;
                continue;
            }

            // Calculate the number of new samples
            int sampleCount = currPos - prevPos;
            if (sampleCount < 0) sampleCount += microphoneClip.samples;

            if (sampleCount > 0)
            {
                // Get audio data from microphone clip
                sampleBuffer = new float[sampleCount];
                microphoneClip.GetData(sampleBuffer, prevPos);

                // Convert to bytes (16-bit PCM)
                byte[] audioBytes = ConvertFloatsToBytes(sampleBuffer);

                // Send data if connected
                if (isConnected && webSocket.State == WebSocketState.Open)
                {
                    // Using Fire and forget pattern instead of awaiting
                    SendWebSocketData(audioBytes);
                }
            }

            prevPos = currPos;
            yield return null;
        }
    }

    // Fire and forget method for sending WebSocket data from coroutines
    private async void SendWebSocketData(byte[] data)
    {
        if (webSocket != null && webSocket.State == WebSocketState.Open)
        {
            await webSocket.Send(data);
        }
    }

    private byte[] ConvertFloatsToBytes(float[] floats)
    {
        byte[] bytes = new byte[floats.Length * 2]; // 16-bit = 2 bytes per sample

        for (int i = 0; i < floats.Length; i++)
        {
            // Convert to 16-bit PCM
            short pcm = (short)(floats[i] * short.MaxValue);
            bytes[i * 2] = (byte)(pcm & 0xFF); // Low byte
            bytes[i * 2 + 1] = (byte)((pcm >> 8) & 0xFF); // High byte
        }

        return bytes;
    }
}