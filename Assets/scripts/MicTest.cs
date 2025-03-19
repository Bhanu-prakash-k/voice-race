using UnityEngine;

public class MicTest : MonoBehaviour
{
    private AudioClip microphoneClip;
    private string microphoneName;

    void Start()
    {
        Debug.Log("Available microphones: " + string.Join(", ", Microphone.devices));

        if (Microphone.devices.Length > 0)
        {
            microphoneName = Microphone.devices[0];
            Debug.Log("Using microphone: " + microphoneName);

            // Start recording - this should trigger the microphone permission prompt
            microphoneClip = Microphone.Start(microphoneName, true, 10, 44100);
            Debug.Log("Microphone started: " + (microphoneClip != null));
        }
        else
        {
            Debug.LogError("No microphones found!");
        }
    }

    void OnDestroy()
    {
        if (Microphone.IsRecording(microphoneName))
        {
            Microphone.End(microphoneName);
            Debug.Log("Microphone stopped");
        }
    }
}