using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public GameObject startPanel;
    public GameObject gameOverPanel;
    public GameObject hudPanel;
    //public Button startButton;
    //public Button restartButton;
    public bool isGameOver = true;
    public TMP_Text winOrLoseText;

    private void Awake()
    {
        if (instance == null)
            instance = this;
    }
    void Start()
    {
        // Show only the start panel at the beginning
        startPanel.SetActive(true);
        hudPanel.SetActive(false);
        gameOverPanel.SetActive(false);
    }

    public void OnStartButtonClick()
    {
        if (DeepgramSearch.Instance.isConnected)
        {
            isGameOver = false;
            startPanel.SetActive(false);
            hudPanel.SetActive(true);
            DeepgramSearch.Instance.SetupAudioSource();
            //restartPanel.SetActive(true);
        }
    }

    public void OnRestartButtonClick()
    {
        //startPanel.SetActive(true);
        SceneManager.LoadScene(0);
    }
    public void OnGameOver()
    {
        gameOverPanel.SetActive(true);
        hudPanel.SetActive(false);
    }
}
