using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CanvasHandler : MonoBehaviour, IPointerClickHandler
{
    public RawImage image;
    public TextMeshProUGUI text;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
  
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (image.color == Color.white)
        {
            MakeRed();
        }
        else if (image.color == Color.red)
        {
            MakeWhite();
        }
    }

    private void MakeRed()
    {
        image.color = Color.red;
        text.text = "Red Game Icon";
        text.color = Color.red;
        print("Red Game Icon");
    }

    private void MakeWhite()
    {
        image.color = Color.white;
        text.text = "Game Icon";
        text.color = Color.white;
        print("Game Icon");
    }
}
