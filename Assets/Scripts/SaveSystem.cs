using UnityEngine;

/// <summary>
/// Everything the game remembers, both between levels and between launches.
///
/// <para>This is a <c>static class</c> rather than a MonoBehaviour because there is only ever one
/// save and it has no presence in a scene. PlayerPrefs is already a global store, so putting it on
/// a GameObject would add an object that owns nothing and can be forgotten in a scene.</para>
///
/// <para>The keys are private consts. PlayerPrefs looks a key up by string and quietly returns the
/// default when it has never seen that key, so a typo does not throw - it just silently loses the
/// save. Writing each key exactly once is what stops that happening.</para>
///
/// <para><b>The checkpoint is the save.</b> There is no separate "saved game": touching a
/// checkpoint records where you were and what you were carrying, and that same record is what a
/// death restores, what <c>Continue</c> resumes from, and what carries into the next level. One
/// piece of state, so the three cannot disagree with each other.</para>
/// </summary>
public static class SaveSystem
{
    private const string UnlockedLevelKey = "UnlockedLevel";
    private const string MusicVolumeKey = "MusicVolume";
    private const string SfxVolumeKey = "SfxVolume";

    private const string CheckpointLevelKey = "CheckpointLevel";
    private const string CheckpointXKey = "CheckpointX";
    private const string CheckpointYKey = "CheckpointY";
    private const string HasCheckpointPosKey = "HasCheckpointPos";
    private const string AmmoKey = "CarriedAmmo";
    private const string CoinsKey = "CarriedCoins";

    // Build indices, matching Build Profiles: 0 = IntroScene, 1..3 = Level1..Level3.
    public const int MenuBuildIndex = 0;
    public const int FirstLevelBuildIndex = 1;
    public const int LastLevelBuildIndex = 3;

    /// <summary>Highest level the player has reached. Level 1 is always available.</summary>
    public static int UnlockedLevel
    {
        get => PlayerPrefs.GetInt(UnlockedLevelKey, FirstLevelBuildIndex);
        private set
        {
            PlayerPrefs.SetInt(UnlockedLevelKey, value);
            Save();
        }
    }

    public static float MusicVolume
    {
        get => PlayerPrefs.GetFloat(MusicVolumeKey, 0.5f);
        set
        {
            PlayerPrefs.SetFloat(MusicVolumeKey, Mathf.Clamp01(value));
            Save();
        }
    }

    public static float SfxVolume
    {
        get => PlayerPrefs.GetFloat(SfxVolumeKey, 0.8f);
        set
        {
            PlayerPrefs.SetFloat(SfxVolumeKey, Mathf.Clamp01(value));
            Save();
        }
    }

    /// <summary>Which level the saved checkpoint is in, or <see cref="FirstLevelBuildIndex"/>.</summary>
    public static int CheckpointLevel =>
        PlayerPrefs.GetInt(CheckpointLevelKey, FirstLevelBuildIndex);

    /// <summary>
    /// False when the checkpoint is only "the start of level N" - after finishing a level, say -
    /// in which case the player spawns wherever the scene puts them instead of at a saved point.
    /// </summary>
    public static bool HasCheckpointPosition =>
        PlayerPrefs.GetInt(HasCheckpointPosKey, 0) == 1;

    public static Vector2 CheckpointPosition => new Vector2(
        PlayerPrefs.GetFloat(CheckpointXKey, 0f),
        PlayerPrefs.GetFloat(CheckpointYKey, 0f));

    /// <summary>Shuriken carried into the next life or the next level.</summary>
    public static int CarriedAmmo => PlayerPrefs.GetInt(AmmoKey, -1);

    /// <summary>Coins collected so far. A running total for the whole game, not per level.</summary>
    public static int CarriedCoins => PlayerPrefs.GetInt(CoinsKey, 0);

    /// <summary>True once the player has got somewhere worth returning to.</summary>
    public static bool HasProgress =>
        UnlockedLevel > FirstLevelBuildIndex || HasCheckpointPosition;

    /// <summary>Where <c>Continue</c> should drop the player back in.</summary>
    public static int ResumeLevel => Mathf.Clamp(CheckpointLevel, FirstLevelBuildIndex, LastLevelBuildIndex);

    /// <summary>
    /// Records a checkpoint inside a level. Hearts are deliberately not stored: touching a
    /// checkpoint heals the player, so the answer is always "full" and there is nothing to keep.
    /// </summary>
    public static void SaveCheckpoint(int buildIndex, Vector2 position, int ammo, int coins)
    {
        PlayerPrefs.SetInt(CheckpointLevelKey, buildIndex);
        PlayerPrefs.SetFloat(CheckpointXKey, position.x);
        PlayerPrefs.SetFloat(CheckpointYKey, position.y);
        PlayerPrefs.SetInt(HasCheckpointPosKey, 1);
        PlayerPrefs.SetInt(AmmoKey, ammo);
        PlayerPrefs.SetInt(CoinsKey, coins);
        Save();
    }

    /// <summary>
    /// Records arriving at the start of a level, carrying ammo and coins but no position - the
    /// next level has its own spawn point and knows nothing about where the last one ended.
    /// </summary>
    public static void SaveLevelStart(int buildIndex, int ammo, int coins)
    {
        PlayerPrefs.SetInt(CheckpointLevelKey, buildIndex);
        PlayerPrefs.SetInt(HasCheckpointPosKey, 0);
        PlayerPrefs.SetInt(AmmoKey, ammo);
        PlayerPrefs.SetInt(CoinsKey, coins);
        Save();
    }

    /// <summary>
    /// Records that a level has been reached. Only ever moves forwards, so replaying Level 1
    /// after finishing Level 3 cannot throw the save away.
    /// </summary>
    public static void UnlockLevel(int buildIndex)
    {
        buildIndex = Mathf.Clamp(buildIndex, FirstLevelBuildIndex, LastLevelBuildIndex);

        if (buildIndex > UnlockedLevel)
        {
            UnlockedLevel = buildIndex;
        }
    }

    /// <summary>
    /// "New Game". Deletes the progress keys so every getter falls back to its default, which is
    /// cleaner than writing the defaults back - the save then genuinely looks untouched.
    /// Volume is deliberately left alone: it is a setting, not progress.
    /// </summary>
    public static void ResetProgress()
    {
        foreach (string key in new[]
                 {
                     UnlockedLevelKey, CheckpointLevelKey, CheckpointXKey, CheckpointYKey,
                     HasCheckpointPosKey, AmmoKey, CoinsKey
                 })
        {
            PlayerPrefs.DeleteKey(key);
        }

        Save();
    }

    /// <summary>
    /// PlayerPrefs normally only writes to disk when the application quits cleanly, so a crash
    /// or stopping play in the Editor would lose the last change. Saving straight away costs
    /// nothing here - the game writes prefs a handful of times per session, not per frame.
    /// </summary>
    private static void Save() => PlayerPrefs.Save();
}
