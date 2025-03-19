using UnityEngine;
using NativeWebSocket;
using System.Threading.Tasks;

public class WebSocketTest : MonoBehaviour
{
    private WebSocket ws;

    async void Start()
    {
        string deepgramUrl = "wss://api.deepgram.com/v1/listen?access_token=adaeb2577a03357cb0bb59302380ec2339607bdc";

        ws = new WebSocket(deepgramUrl);

        ws.OnOpen += () => Debug.Log("✅ WebSocket connected successfully!");
        ws.OnError += (error) => Debug.LogError("❌ WebSocket error: " + error);
        ws.OnClose += (code) => Debug.Log("🔴 WebSocket closed: " + code);

        await ws.Connect();
    }

    private async void OnApplicationQuit()
    {
        if (ws != null)
        {
            await ws.Close();
        }
    }
}
