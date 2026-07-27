using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class IntroCanvasHandler : MonoBehaviour
{
    public Button startButton;

    void Start()
    {
        startButton.onClick.AddListener(OnButtonClicked);
    }

    private void OnButtonClicked()
    {
        SceneManager.LoadScene("Level1");
    }
}
