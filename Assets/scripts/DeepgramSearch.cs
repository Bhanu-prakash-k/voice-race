using UnityEngine;
using System.Collections;
using TMPro;
using NativeWebSocket;
using System.Collections.Generic;
using System;
using UnityEngine.Audio;

[Serializable]
public class DeepgramResponse
{
    public bool is_final;
    public Channel channel;
}

[Serializable]
public class Channel
{
    public Alternative[] alternatives;
}

[Serializable]
public class Alternative
{
    public string transcript;
}

public class DeepgramSearch : MonoBehaviour
{
    public static DeepgramSearch Instance;
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI transcriptionText;
    [SerializeField] private TextMeshProUGUI debugText;

    [Header("Configuration")]
    [SerializeField] private string deepgramApiKey = "YOUR_DEEPGRAM_API_KEY"; // Replace with your actual API key
    // [SerializeField] private float carSpeed = 0f;
    // [SerializeField] private float maxCarSpeed = 20f;
    // [SerializeField] private float acceleration = 10f;
    // [SerializeField] private float deceleration = 5f;
    [SerializeField] private float timerDuration = 60f;

    public bool isConnected = false;

    // Components
    private AudioSource audioSource;
    public WebSocket webSocket;

    // Audio processing variables
    private int lastPosition = 0;
    private int currentPosition = 0;

    // Transcription buffer
    private string currentTranscription = "";

    // Timer
    public float timer;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    void Start()
    {
        DebugLog("Starting Deepgram Manager");

        // Check UI references


        // Set up audio source
        //SetupAudioSource();

        // Connect to Deepgram
        StartCoroutine(ConnectToDeepgram());

        // Initialize timer
        timer = timerDuration;

        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            if (debugText != null)
            {
                debugText.text = "Time Remaining : " + timer.ToString("00");
            }
        });
    }

    public void SetupAudioSource()
    {
        // Add AudioSource component if it doesn't exist
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Check for microphone
        if (Microphone.devices.Length == 0)
        {
            DebugLog("No microphones found!");
            return;
        }

        // Log available microphones
        DebugLog("Available microphones:");
        foreach (string device in Microphone.devices)
        {
            DebugLog("- " + device);
        }

        // Start microphone with system sample rate
        string microphoneName = Microphone.devices[0];
        DebugLog("Using microphone: " + microphoneName);
        DebugLog("System sample rate: " + AudioSettings.outputSampleRate);

        audioSource.clip = Microphone.Start(microphoneName, true, 10, AudioSettings.outputSampleRate);

        if (audioSource.clip == null)
        {
            DebugLog("Failed to start microphone!");
            return;
        }

        // We're not playing through speakers, just capturing
        audioSource.volume = 0;
        audioSource.Play();

        DebugLog("Microphone recording started");
    }

    IEnumerator ConnectToDeepgram()
    {
        DebugLog("Connecting to Deepgram...");

        // Create WebSocket with correct URL and headers
        string deepgramURL = "wss://api.deepgram.com/v1/listen?encoding=linear16&sample_rate=" +
                     AudioSettings.outputSampleRate.ToString() + "&channels=1&model=nova-3" +
                     "&language=en-US&version=latest&noise_reduction=true";

        // Create headers with API key
        Dictionary<string, string> headers = new Dictionary<string, string>
        {
            { "Authorization", "Token " + deepgramApiKey }
        };

        // Create and configure WebSocket
        webSocket = new WebSocket(deepgramURL, headers);

        // Set up event handlers
        webSocket.OnOpen += () =>
        {
            DebugLog("Connected to Deepgram successfully!");
            isConnected = true;
        };

        webSocket.OnError += (errorMsg) =>
        {
            Debug.LogError("WebSocket Error: " + errorMsg);
        };

        webSocket.OnClose += (closeCode) =>
        {
            Debug.LogError("WebSocket closed with code: " + closeCode);
        };

        webSocket.OnMessage += (bytes) =>
        {
            string message = System.Text.Encoding.UTF8.GetString(bytes);

            try
            {
                // Parse response
                DeepgramResponse response = new DeepgramResponse();
                JsonUtility.FromJsonOverwrite(message, response);

                // Process transcript
                if (response.channel != null &&
                    response.channel.alternatives != null &&
                    response.channel.alternatives.Length > 0)
                {
                    string transcript = response.channel.alternatives[0].transcript.ToLower();

                    if (!string.IsNullOrEmpty(transcript))
                    {
                        // If final, update the current transcription
                        if (response.is_final)
                        {
                            currentTranscription += transcript + " ";

                            // Update UI on main thread
                            UnityMainThreadDispatcher.Instance().Enqueue(() =>
                            {
                                if (transcriptionText != null)
                                {
                                    //transcriptionText.text = currentTranscription;
                                }
                                DebugLog("Final: " + transcript);

                                if (transcript.Contains("go"))
                                {
                                    CarController.instance.IncreaseCarSpeed();
                                    //carSpeed = Mathf.Min(carSpeed + acceleration, maxCarSpeed);
                                    //DebugLog("Go detected! Car speed: " + carSpeed);
                                }
                            });
                        }
                        else
                        {
                            // Update UI with interim result
                            UnityMainThreadDispatcher.Instance().Enqueue(() =>
                            {
                                if (transcriptionText != null)
                                {
                                    //transcriptionText.text = currentTranscription + "(" + transcript + ")";
                                }
                            });
                        }
                    }
                }
            }
            catch (Exception e)
            {
                DebugLog("Error parsing response: " + e.Message);
            }
        };

        // Connect to WebSocket
        yield return webSocket.Connect();

        if (webSocket.State != WebSocketState.Open)
        {
            DebugLog("Failed to connect to Deepgram. Status: " + webSocket.State);
        }
    }

    void Update()
    {
        // Process WebSocket messages
        if (webSocket != null)
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            webSocket.DispatchMessageQueue();
#endif
        }

        // Process microphone data
        ProcessMicrophoneData();

        // Car movement


        // Timer
        if (timer > 0)
        {
            if (GameManager.instance.isGameOver)
                return;
            timer -= Time.deltaTime;
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                if (debugText != null)
                {
                    debugText.text = "Time Remaining : " + timer.ToString("00");
                }
            });
        }
        else
        {
            CarController.instance.Lose();
            DebugLog("Timer finished!");
            enabled = false;
        }
    }

    void ProcessMicrophoneData()
    {
        if (audioSource == null || audioSource.clip == null || webSocket == null ||
            webSocket.State != WebSocketState.Open)
        {
            return;
        }

        // Get current position in the microphone buffer
        currentPosition = Microphone.GetPosition(null);

        if (currentPosition <= 0)
        {
            return;
        }

        // Handle wrap-around
        if (lastPosition > currentPosition)
        {
            lastPosition = 0;
        }

        // If we have new data
        if (currentPosition - lastPosition > 0)
        {
            // Get audio samples
            float[] samples = new float[(currentPosition - lastPosition) * audioSource.clip.channels];
            audioSource.clip.GetData(samples, lastPosition);

            // Convert to shorts (16-bit PCM)
            short[] samplesAsShorts = new short[samples.Length];
            for (int i = 0; i < samples.Length; i++)
            {
                samplesAsShorts[i] = ConvertF32ToI16(samples[i]);
            }

            // Convert to bytes
            byte[] audioBytes = new byte[samplesAsShorts.Length * 2];
            System.Buffer.BlockCopy(samplesAsShorts, 0, audioBytes, 0, audioBytes.Length);

            // Send to Deepgram
            SendAudioData(audioBytes);

            // Update position for next frame
            lastPosition = currentPosition;
        }
    }

    short ConvertF32ToI16(float sample)
    {
        sample = sample * 32768f;
        if (sample > 32767f) return 32767;
        if (sample < -32768f) return -32768;
        return (short)sample;
    }

    async void SendAudioData(byte[] audioData)
    {
        if (webSocket != null && webSocket.State == WebSocketState.Open)
        {
            try
            {
                await webSocket.Send(audioData);
            }
            catch (Exception e)
            {
                DebugLog("Error sending audio: " + e.Message);
            }
        }
    }

    void DebugLog(string message)
    {
        //Debug.Log("[Deepgram] " + message);

        // Update UI on main thread
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            if (debugText != null)
            {
                debugText.text = message;
            }
        });
    }

    async void OnDisable()
    {
        if (webSocket != null)
        {
            await webSocket.Close();
        }

        if (Microphone.IsRecording(null))
        {
            Microphone.End(null);
        }
    }

    async void OnApplicationQuit()
    {
        if (webSocket != null)
        {
            await webSocket.Close();
        }

        if (Microphone.IsRecording(null))
        {
            Microphone.End(null);
        }
    }

}