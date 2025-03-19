using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using TMPro;
using System.IO;

public class VoiceCarController : MonoBehaviour
{
    public GameObject car;
    public float speed = 0f;
    public float speedIncrement = 10f;
    public float maxSpeed = 50f;
    public TextMeshProUGUI timerText;

    private float timer = 60f;
    private bool isRecording = false;
    private AudioClip audioClip;
    private string googleApiKeyPath;


    void Start()
    {
        googleApiKeyPath = Application.dataPath + "/GoogleAPIKey.json";
        StartCoroutine(StartListening());
    }

    void Update()
    {
        timer -= Time.deltaTime;
        timerText.text = "Time Left: " + Mathf.Ceil(timer).ToString();

        // Move the car
        car.transform.Translate(Vector3.right * speed * Time.deltaTime);

        if (timer <= 0)
        {
            StopAllCoroutines();
        }
    }

    IEnumerator StartListening()
    {
        while (timer > 0)
        {
            yield return new WaitForSeconds(2f); // Check every 2 seconds

            if (!isRecording)
            {
                isRecording = true;
                audioClip = Microphone.Start(null, false, 2, 44100);
                yield return new WaitForSeconds(2);
                Microphone.End(null);

                StartCoroutine(SendAudioToGoogle());
            }
        }
    }

    IEnumerator SendAudioToGoogle()
    {
        byte[] audioData = WavUtility.FromAudioClip(audioClip); // Convert to WAV format

        string apiKey = LoadGoogleApiKey();
        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogError("Google API Key not found!");
            yield break;
        }

        string url = "https://speech.googleapis.com/v1/speech:recognize?key=" + apiKey;

        string jsonPayload = "{ \"config\": { \"encoding\": \"LINEAR16\", \"sampleRateHertz\": 44100, \"languageCode\": \"en-US\" }, \"audio\": { \"content\": \"" + System.Convert.ToBase64String(audioData) + "\" } }";

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string responseText = request.downloadHandler.text;
            Debug.Log("Google Response: " + responseText);

            if (responseText.Contains("\"Go\"")) // Check if "Go" is detected
            {
                speed = Mathf.Min(speed + speedIncrement, maxSpeed);
            }
            else
            {
                speed = Mathf.Lerp(speed, 0, 0.1f); // Gradually slow down
            }
        }
        else
        {
            Debug.LogError("Google Speech API Error: " + request.error);
        }
        isRecording = false;
    }

    private string LoadGoogleApiKey()
    {
        if (File.Exists(googleApiKeyPath))
        {
            string jsonContent = File.ReadAllText(googleApiKeyPath);
            GoogleApiKey keyObj = JsonUtility.FromJson<GoogleApiKey>(jsonContent);
            return keyObj.apiKey;
        }
        return null;
    }

    [System.Serializable]
    private class GoogleApiKey
    {
        public string apiKey;
    }
}
