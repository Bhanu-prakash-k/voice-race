using System.Collections;
using UnityEngine.UI;
using UnityEngine;

public class CarController : MonoBehaviour
{
    public static CarController instance;

    [SerializeField] private float carSpeed = 0f;
    [SerializeField] private float maxCarSpeed = 20f;
    [SerializeField] private float acceleration = 10f;
    [SerializeField] private float deceleration = 5f;
    private float decelerationTime = 3f;
    private bool isDecelerating = false;
    [SerializeField] private Image indicationImage;

    private void Awake()
    {
        if (instance == null)
            instance = this;
    }

    void Update()
    {
        if (carSpeed > 0)
        {
            transform.Translate(Vector3.up * carSpeed * Time.deltaTime);
        }

        // Apply deceleration if triggered
        if (isDecelerating && carSpeed > 0)
        {
            carSpeed = Mathf.Max(0, carSpeed - deceleration * Time.deltaTime);
        }
    }

    public void IncreaseCarSpeed()
    {
        carSpeed = Mathf.Min(carSpeed + acceleration, maxCarSpeed);
        isDecelerating = false;
        indicationImage.color = Color.green;

        // Start deceleration after 3 seconds
        StopCoroutine(DecelerateAfterDelay());
        StartCoroutine(DecelerateAfterDelay());
    }

    private IEnumerator DecelerateAfterDelay()
    {
        yield return new WaitForSeconds(decelerationTime);
        isDecelerating = true;
        indicationImage.color = Color.red;
    }

    public float GetSpeed()
    {
        return carSpeed;
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("endpoint"))
        {
            DeepgramSearch.Instance.webSocket.Close();
            if (DeepgramSearch.Instance.timer > 0)
            {
                OnGameOver("You Win");
            }

        }
    }
    public void Lose()
    {
        DeepgramSearch.Instance.webSocket.Close();
        if (DeepgramSearch.Instance.timer <= 0)
        {
            OnGameOver("You Lose");
        }
    }
    public void OnGameOver(string winOrLose)
    {

        GameManager.instance.winOrLoseText.text = winOrLose;
        carSpeed = 0;
        GameManager.instance.OnGameOver();
    }
}
