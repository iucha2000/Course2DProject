using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameCanvasHandler : MonoBehaviour
{
    public Image itemImage;
    public TextMeshProUGUI itemNameText;

    public float displayDuration = 2f;

    private Coroutine hideCoroutine;

    void Start()
    {
        itemImage.enabled = false;
        itemNameText.enabled = false;
    }

    void Update()
    {
        
    }

    public void DisplayItemInfo(string itemName, Sprite itemSprite)
    {
        if (hideCoroutine != null)
            StopCoroutine(hideCoroutine);

        itemImage.sprite = itemSprite;
        itemNameText.text = itemName + " collected!";
        itemImage.enabled = true;
        itemNameText.enabled = true;
        hideCoroutine = StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(displayDuration);
        itemImage.enabled = false;
        itemNameText.enabled = false;
        hideCoroutine = null;
    }
}
