using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// TextureAtlasGenerator - Helper để tạo texture atlas cho FlyingNumbersVFX
/// Cách dùng:
/// 1. Attach script vào GameObject bất kỳ
/// 2. Điều chỉnh settings
/// 3. Bấn "Generate Atlas" button trong Inspector
/// 4. Texture sẽ được save tại Assets/Resources/Textures/NumbersAtlas.png
/// </summary>
public class TextureAtlasGenerator : MonoBehaviour
{
    [Header("=== GENERATION SETTINGS ===")]
    [Range(256, 2048)]
    public int textureSize = 512;

    public int gridColumns = 5;
    public int gridRows = 4;

    [Header("=== FONT SETTINGS ===")]
    [Range(20, 200)]
    public int fontSize = 80;

    public Font textFont;
    public Color textColor = Color.white;

    [Header("=== CHARACTERS ===")]
    [TextArea(3, 5)]
    public string charactersToGenerate = "0123456789+-*=/().,";

    [Header("=== OUTPUT ===")]
    public string outputPath = "Assets/Resources/Textures/NumbersAtlas.png";

#if UNITY_EDITOR
    [ContextMenu("Generate Number Atlas")]
    public void GenerateAtlas()
    {
        if (charactersToGenerate.Length == 0)
        {
            Debug.LogError("[TextureAtlasGenerator] Characters to generate không được trống!");
            return;
        }

        if (charactersToGenerate.Length > gridColumns * gridRows)
        {
            Debug.LogError($"[TextureAtlasGenerator] Quá nhiều ký tự ({charactersToGenerate.Length}) cho grid {gridColumns}x{gridRows}");
            return;
        }

        // Tạo texture
        Texture2D atlas = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        
        // Clear to transparent
        Color[] pixels = new Color[textureSize * textureSize];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = new Color(0, 0, 0, 0);
        }
        atlas.SetPixels(pixels);

        int cellWidth = textureSize / gridColumns;
        int cellHeight = textureSize / gridRows;

        // Draw each character
        for (int i = 0; i < charactersToGenerate.Length; i++)
        {
            int col = i % gridColumns;
            int row = i / gridColumns;

            string character = charactersToGenerate[i].ToString();

            // Render text to texture
            // Note: Đây là simplified version, thực tế cần dùng Canvas render hoặc TextMesh
            DrawCharacterToAtlas(atlas, character, col, row, cellWidth, cellHeight);
        }

        // Apply changes
        atlas.Apply(false, false);

        // Save to file
        byte[] pngData = atlas.EncodeToPNG();
        
        // Ensure directory exists
        string directory = System.IO.Path.GetDirectoryName(outputPath);
        if (!System.IO.Directory.Exists(directory))
        {
            System.IO.Directory.CreateDirectory(directory);
        }

        System.IO.File.WriteAllBytes(outputPath, pngData);
        Object.DestroyImmediate(atlas);

        Debug.Log($"[TextureAtlasGenerator] Atlas generated tại: {outputPath}");
        
        // Refresh asset database
        #if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
        #endif
    }

    /// <summary>
    /// Draw character to atlas (simple implementation)
    /// Note: Phương pháp này là basic. Để đẹp hơn, dùng Canvas render
    /// </summary>
    void DrawCharacterToAtlas(Texture2D atlas, string character, int col, int row, int cellWidth, int cellHeight)
    {
        // Simple pixel-based drawing (placeholders)
        // Trong production, nên dùng Canvas Renderer hoặc third-party text rendering

        Color bgColor = new Color(0.1f, 0.1f, 0.1f, 1f);
        Color borderColor = new Color(0.3f, 0.3f, 0.3f, 1f);

        int startX = col * cellWidth;
        int startY = row * cellHeight;

        // Fill cell with background
        for (int x = startX; x < startX + cellWidth; x++)
        {
            for (int y = startY; y < startY + cellHeight; y++)
            {
                atlas.SetPixel(x, y, bgColor);
            }
        }

        // Draw border
        for (int x = startX; x < startX + cellWidth; x++)
        {
            atlas.SetPixel(x, startY, borderColor);
            atlas.SetPixel(x, startY + cellHeight - 1, borderColor);
        }
        for (int y = startY; y < startY + cellHeight; y++)
        {
            atlas.SetPixel(startX, y, borderColor);
            atlas.SetPixel(startX + cellWidth - 1, y, borderColor);
        }

        // Add text label (simple center dot + character label)
        int centerX = startX + cellWidth / 2;
        int centerY = startY + cellHeight / 2;

        // Center marker
        for (int dx = -2; dx <= 2; dx++)
        {
            for (int dy = -2; dy <= 2; dy++)
            {
                if (centerX + dx >= 0 && centerX + dx < atlas.width &&
                    centerY + dy >= 0 && centerY + dy < atlas.height)
                {
                    atlas.SetPixel(centerX + dx, centerY + dy, textColor);
                }
            }
        }

        Debug.Log($"[TextureAtlas] Drew '{character}' at grid ({col}, {row})");
    }

    /// <summary>
    /// Setup default characters (0-9)
    /// </summary>
    [ContextMenu("Set Numbers Only")]
    public void SetNumbersOnly()
    {
        charactersToGenerate = "0123456789";
        gridColumns = 5;
        gridRows = 2;
    }

    /// <summary>
    /// Setup math operators (0-9 + operators)
    /// </summary>
    [ContextMenu("Set Numbers + Math")]
    public void SetNumbersAndMath()
    {
        charactersToGenerate = "0123456789+-*=/().,";
        gridColumns = 5;
        gridRows = 4;
    }

    /// <summary>
    /// Setup extended set
    /// </summary>
    [ContextMenu("Set Extended")]
    public void SetExtended()
    {
        charactersToGenerate = "0123456789+-*=/().,!?#$%&^~@";
        gridColumns = 6;
        gridRows = 5;
    }
#endif
}


