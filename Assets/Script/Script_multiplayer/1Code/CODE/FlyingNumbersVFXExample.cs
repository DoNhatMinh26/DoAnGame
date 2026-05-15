using UnityEngine;

/// <summary>
/// FlyingNumbersVFXExample - Ví dụ sử dụng FlyingNumbersVFX
/// Attach vào GameObject có FlyingNumbersVFX component
/// </summary>
public class FlyingNumbersVFXExample : MonoBehaviour
{
    private FlyingNumbersVFX flyingNumbers;

    void Start()
    {
        flyingNumbers = GetComponent<FlyingNumbersVFX>();

        if (flyingNumbers == null)
        {
            Debug.LogError("[FlyingNumbersVFXExample] FlyingNumbersVFX component không tìm thấy!");
            return;
        }

        // Setup default
        SetupDefaultMode();
    }

    void Update()
    {
        // Bấn phím để thay đổi mode
        if (Input.GetKeyDown(KeyCode.M))
            SetupMathMode();

        if (Input.GetKeyDown(KeyCode.A))
            SetupAlgorithmMode();

        if (Input.GetKeyDown(KeyCode.R))
            SetupRandomMode();

        if (Input.GetKeyDown(KeyCode.P))
            TogglePause();

        if (Input.GetKeyDown(KeyCode.Space))
            flyingNumbers.ResetToDefault();
    }

    /// <summary>
    /// Default mode - white numbers, normal speed
    /// </summary>
    void SetupDefaultMode()
    {
        Debug.Log("[FlyingNumbersVFX] Default Mode");
        flyingNumbers.SetEffectColor(Color.white, 1f);
        flyingNumbers.SetUpwardForce(2f);
        flyingNumbers.SetEmissionRate(50f);
        flyingNumbers.SetAnimationSpeed(1f);
        
        // Nếu bạn có 15 hình riêng lẻ, kéo chúng vào FlyingNumbersVFX.symbolSprites trong Inspector
    }

    /// <summary>
    /// Math mode - green numbers, fast
    /// </summary>
    void SetupMathMode()
    {
        Debug.Log("[FlyingNumbersVFX] Math Mode - Press M");
        flyingNumbers.SetEffectColor(Color.green, 1.2f);
        flyingNumbers.SetUpwardForce(2.5f);
        flyingNumbers.SetEmissionRate(80f);
        flyingNumbers.SetAnimationSpeed(1.2f);
    }

    /// <summary>
    /// Algorithm mode - cyan numbers, slow
    /// </summary>
    void SetupAlgorithmMode()
    {
        Debug.Log("[FlyingNumbersVFX] Algorithm Mode - Press A");
        flyingNumbers.SetEffectColor(Color.cyan, 1.5f);
        flyingNumbers.SetUpwardForce(1.5f);
        flyingNumbers.SetEmissionRate(100f);
        flyingNumbers.SetAnimationSpeed(0.8f);
    }

    /// <summary>
    /// Random mode - random colors, mixed speed
    /// </summary>
    void SetupRandomMode()
    {
        Debug.Log("[FlyingNumbersVFX] Random Mode - Press R");
        Color randomColor = new Color(
            Random.value,
            Random.value,
            Random.value,
            1f
        );
        flyingNumbers.SetEffectColor(randomColor, Random.Range(0.8f, 1.5f));
        flyingNumbers.SetUpwardForce(Random.Range(1f, 3f));
        flyingNumbers.SetEmissionRate(Random.Range(30f, 120f));
    }

    /// <summary>
    /// Toggle pause
    /// </summary>
    void TogglePause()
    {
        // Note: Cần lưu trạng thái pause nếu muốn track
        Debug.Log("[FlyingNumbersVFX] Toggle Pause - Press P");
        
        // Get pause state từng script hoặc tạo boolean
        flyingNumbers.PauseVFX(!IsPaused());
    }

    /// <summary>
    /// Check nếu đang pause (helper)
    /// </summary>
    bool IsPaused()
    {
        // Simplistic check
        return flyingNumbers.vfxParticleSystem.isPaused;
    }

    /// <summary>
    /// Trigger effect khi có event (ví dụ: boss spawn)
    /// </summary>
    public void TriggerBossSpawn()
    {
        Debug.Log("[FlyingNumbersVFX] Boss Spawn Triggered!");
        
        // Explosion effect - red, fast
        flyingNumbers.SetEffectColor(Color.red, 2f);
        flyingNumbers.SetUpwardForce(4f);
        flyingNumbers.SetEmissionRate(200f);
        flyingNumbers.SetAnimationSpeed(2f);

        // Dial back sau 2 giây
        Invoke(nameof(SetupDefaultMode), 2f);
    }

    /// <summary>
    /// Debug info
    /// </summary>
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label("FlyingNumbersVFX Controls", new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold });
        
        GUILayout.Label("M - Math Mode (Green)");
        GUILayout.Label("A - Algorithm Mode (Cyan)");
        GUILayout.Label("R - Random Mode");
        GUILayout.Label("P - Pause/Resume");
        GUILayout.Label("Space - Reset to Default");
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Trigger Boss Effect", GUILayout.Height(30)))
        {
            TriggerBossSpawn();
        }

        GUILayout.EndArea();
    }
}
