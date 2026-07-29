using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameCanvasHandler : MonoBehaviour
{
    public Image itemImage;
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI collectedCountText;

    public float displayDuration = 2f;

    private Coroutine hideCoroutine;

    void Start()
    {
        itemImage.enabled = false;
        itemNameText.enabled = false;
        collectedCountText.text = "";
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

    public void UpdateCollectedCount(string itemName, int count)
    {
        collectedCountText.text = $"{Pluralize(itemName)} collected: {count}";
    }

    private static string Pluralize(string itemName)
    {
        if (string.IsNullOrEmpty(itemName))
            return itemName;

        char lastChar = itemName[itemName.Length - 1];
        bool endsInConsonantY = lastChar == 'y' && itemName.Length > 1 && "aeiouAEIOU".IndexOf(itemName[itemName.Length - 2]) < 0;

        return endsInConsonantY
            ? itemName.Substring(0, itemName.Length - 1) + "ies"
            : itemName + "s";
    }
}
