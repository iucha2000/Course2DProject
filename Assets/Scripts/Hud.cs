using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The permanent on-screen readout: hearts, coins and shuriken ammo.
///
/// <para><b>It reads, it is never told.</b> Every value it shows is already public and read-only on
/// the object that owns it (<see cref="PlayerHealth.Hearts"/>, <see cref="PlayerCombat.Ammo"/>,
/// <see cref="Player.CoinCount"/>), so the HUD polls them rather than having the player call
/// into it after every pickup and every hit. Polling cannot get out of sync, and it means neither
/// the player nor the enemies need to know a HUD exists at all.</para>
///
/// <para>The values are compared against what is currently drawn before anything is written, so
/// TextMeshPro only rebuilds its mesh when a number has actually changed rather than every frame.</para>
/// </summary>
public class Hud : MonoBehaviour
{
    [Tooltip("One heart icon. Instantiated MaxHearts times at startup and then shown or hidden.")]
    [SerializeField] private GameObject heartPrefab;

    [Header("Icon sources")]
    [Tooltip("The pickup prefabs the two counters describe. The HUD takes its icons from them " +
             "at runtime, so the coin art is defined in exactly one place.")]
    [SerializeField] private Item coinItem;
    [SerializeField] private Item ammoItem;

    private PlayerHealth health;
    private PlayerCombat combat;
    private Player player;

    private Transform heartsContainer;
    private Image coinIcon;
    private Image ammoIcon;
    private TextMeshProUGUI coinText;
    private TextMeshProUGUI ammoText;

    private readonly List<Image> hearts = new List<Image>();

    // What is currently on screen. -1 means "nothing drawn yet", which forces the first refresh.
    private int shownHearts = -1;
    private int shownCoins = -1;
    private int shownAmmo = -1;

    private void Awake()
    {
        heartsContainer = transform.Find("Hearts");
        coinIcon = FindChild<Image>("CoinIcon");
        ammoIcon = FindChild<Image>("AmmoIcon");
        coinText = FindChild<TextMeshProUGUI>("CoinCounter");
        ammoText = FindChild<TextMeshProUGUI>("AmmoCounter");

        // Image.sprite assigned at runtime from the pickup itself. The alternative - dragging the
        // same sprite onto the HUD as well - means the coin art is recorded in two places and
        // they can silently disagree the day one of them is changed.
        if (coinItem != null)
        {
            coinIcon.sprite = coinItem.itemSprite;
        }

        if (ammoItem != null)
        {
            ammoIcon.sprite = ammoItem.itemSprite;
        }
    }

    private void Start()
    {
        // Start, not Awake: these live on a different object, and Awake order between two
        // objects is undefined, so the player might not have run its own Awake yet.
        player = FindFirstObjectByType<Player>();

        if (player == null)
        {
            // A level with no player is a broken level, but the HUD should say so rather than
            // throw a NullReferenceException every frame.
            Debug.LogWarning("Hud found no Player in the scene.", this);
            enabled = false;
            return;
        }

        health = player.GetComponent<PlayerHealth>();
        combat = player.GetComponent<PlayerCombat>();

        BuildHearts();
    }

    private void Update()
    {
        RefreshHearts();

        if (shownCoins != player.CoinCount)
        {
            shownCoins = player.CoinCount;
            coinText.text = $"x {shownCoins}";
        }

        if (shownAmmo != combat.Ammo)
        {
            shownAmmo = combat.Ammo;
            ammoText.text = $"x {shownAmmo}";
        }
    }

    /// <summary>
    /// Creates one icon per heart the player can hold. They are made once and then only ever
    /// shown or hidden, which is far cheaper than destroying and recreating them on every hit.
    /// </summary>
    private void BuildHearts()
    {
        for (int i = 0; i < health.MaxHearts; i++)
        {
            GameObject heart = Instantiate(heartPrefab);

            // SetParent with worldPositionStays: false is the important part for UI. The default
            // overload keeps the object's world position, which for a RectTransform means it
            // holds whatever position the prefab happened to have and lands off-screen. Passing
            // false tells it to keep its local position instead and let the layout place it.
            heart.transform.SetParent(heartsContainer, false);
            heart.name = $"Heart{i}";

            hearts.Add(heart.GetComponent<Image>());
        }
    }

    private void RefreshHearts()
    {
        if (shownHearts == health.Hearts)
        {
            return;
        }

        shownHearts = health.Hearts;

        for (int i = 0; i < hearts.Count; i++)
        {
            // Graphic.enabled rather than SetActive, and the difference matters here. Turning the
            // GameObject off would take it out of the Horizontal Layout Group as well, so the
            // remaining hearts would slide across every time one was lost. Disabling just the
            // Image stops it drawing while the slot keeps its place, so the row never moves.
            hearts[i].enabled = i < shownHearts;
        }
    }

    private T FindChild<T>(string childName) where T : Component
    {
        Transform child = transform.Find(childName);

        if (child == null)
        {
            Debug.LogError($"Hud expects a child called '{childName}'.", this);
            return null;
        }

        return child.GetComponent<T>();
    }
}
