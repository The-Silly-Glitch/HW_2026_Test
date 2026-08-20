using UnityEngine;

/// <summary>
/// Plain data container matching the fields in doofus_diary.json.
/// Field names MUST match the JSON keys exactly (JsonUtility uses
/// reflection on field names, it does not care about property names).
/// </summary>
[System.Serializable]
public class DoofusDiaryData
{
    public float doofusSpeed;         // units/second Doofus moves at
    public float minPulpitLifetime;   // y - minimum seconds a Pulpit survives
    public float maxPulpitLifetime;   // z - maximum seconds a Pulpit survives
}

/// <summary>
/// Loads and validates the Doofus Diary JSON. Put doofus_diary.json
/// inside Assets/Resources/ (NOT StreamingAssets) so Resources.Load
/// can find it as a TextAsset on every platform without extra path code.
/// </summary>
public static class DoofusDiaryLoader
{
    private const string ResourceName = "doofus_diary"; // no extension, no folder

    // Sensible fallback values, used only if the JSON is missing/broken,
    // so the game never hard-crashes just because a config file is absent.
    private static readonly DoofusDiaryData Fallback = new DoofusDiaryData
    {
        doofusSpeed = 5f,
        minPulpitLifetime = 3f,
        maxPulpitLifetime = 6f
    };

    public static DoofusDiaryData Load()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>(ResourceName);

        if (jsonFile == null)
        {
            Debug.LogError($"[DoofusDiaryLoader] Could not find Resources/{ResourceName}.json. " +
                            "Using fallback values instead.");
            return Fallback;
        }

        DoofusDiaryData data;
        try
        {
            data = JsonUtility.FromJson<DoofusDiaryData>(jsonFile.text);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[DoofusDiaryLoader] Failed to parse JSON: {e.Message}. Using fallback values.");
            return Fallback;
        }

        if (data == null)
        {
            Debug.LogError("[DoofusDiaryLoader] JSON parsed to null. Using fallback values.");
            return Fallback;
        }

        // --- Edge case handling: sanitize bad/negative/zero data from the file ---
        if (data.doofusSpeed <= 0f)
        {
            Debug.LogWarning("[DoofusDiaryLoader] doofusSpeed <= 0 in JSON, clamping to fallback.");
            data.doofusSpeed = Fallback.doofusSpeed;
        }

        if (data.minPulpitLifetime <= 0f)
        {
            Debug.LogWarning("[DoofusDiaryLoader] minPulpitLifetime <= 0 in JSON, clamping to fallback.");
            data.minPulpitLifetime = Fallback.minPulpitLifetime;
        }

        if (data.maxPulpitLifetime < data.minPulpitLifetime)
        {
            Debug.LogWarning("[DoofusDiaryLoader] maxPulpitLifetime < minPulpitLifetime in JSON, swapping/fixing.");
            data.maxPulpitLifetime = data.minPulpitLifetime + 1f;
        }

        return data;
    }
}
