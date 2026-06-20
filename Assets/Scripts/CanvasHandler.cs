using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CanvasHandler : MonoBehaviour
{
    public Button startButton;

    void Start()
    {
        startButton.onClick.AddListener(OnButtonClicked);
    }

    void Update()
    {

    }

    private void OnButtonClicked()
    {
        SceneManager.LoadScene("SampleScene");
    }
}
