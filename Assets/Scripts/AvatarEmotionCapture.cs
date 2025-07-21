using TMPro;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class AvatarEmotionCapture : MonoBehaviour
{
    public Camera avatarCamera;
    private EmotionAnalyzer emotionAnalyzer;
    private int frameCount = 0;
    [SerializeField] private TextMeshProUGUI debugText;

    [Header("Image Saving")]
    [SerializeField] private bool saveFramesToDisk = false;
    [SerializeField] private string savePath = "EmotionCaptures";
    private string fullSavePath;

    // Match Python's image batch functionality
    private List<Texture2D> imageBatch;
    private const int MAX_BATCH_SIZE = 10;

    void Start()
    {
        fullSavePath = Path.Combine(Application.dataPath, "..", savePath);
        Directory.CreateDirectory(fullSavePath);
        emotionAnalyzer = GetComponent<EmotionAnalyzer>();
        imageBatch = new List<Texture2D>();
    }

    void Update()
    {
        // Process every other frame (matching Python's f % 2 == 0)
        if (frameCount % 2 == 0)
        {
            // Capture frame
            Texture2D frameTexture = CaptureCamera(avatarCamera);
            if (frameTexture == null) return;

            // Manage batch size (matching Python's image_batch[:5] = [])
            if (imageBatch.Count > MAX_BATCH_SIZE)
            {
                // Remove oldest 5 images
                imageBatch.RemoveRange(0, 5);
            }
            imageBatch.Add(frameTexture);

            // Get emotion values
            Vector2 emotionValues = emotionAnalyzer.GetValenceArousal(frameTexture);
            string emotion = emotionAnalyzer.GetDiscreteEmotion(emotionValues);

            // Update debug text with all information
            debugText.text = string.Format(
                "Valence: {0:F2}\nArousal: {1:F2}\nEmotion: {2}",
                emotionValues.x,
                emotionValues.y,
                emotion
            );

            // Save frame if enabled
            if (saveFramesToDisk)
            {
                SaveTextureToFile(frameTexture, emotionValues, emotion);
                saveFramesToDisk = false; // Reset flag after saving
            }

            Destroy(frameTexture); // Clean up
        }
        frameCount++;
    }

    private Texture2D CaptureCamera(Camera camera)
    {
        RenderTexture rt = camera.targetTexture;
        if (rt == null)
        {
            Debug.LogError("No RenderTexture attached to the avatar camera!");
            return null;
        }

        camera.Render();
        RenderTexture.active = rt;
        Texture2D texture = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        texture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        texture.Apply();

        RenderTexture.active = null;
        return texture;
    }

    void OnDestroy()
    {
        // Clean up image batch
        foreach (var image in imageBatch)
        {
            if (image != null)
                Destroy(image);
        }
        imageBatch.Clear();
    }

    private void SaveTextureToFile(Texture2D texture, Vector2 emotionValues, string emotion)
    {
        // Create filename with timestamp and emotion data
        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
        string filename = $"emotion_{timestamp}_V{emotionValues.x:F2}_A{emotionValues.y:F2}_{emotion}.png";
        string filepath = Path.Combine(fullSavePath, filename);

        // Encode texture as PNG
        byte[] bytes = texture.EncodeToPNG();
        File.WriteAllBytes(filepath, bytes);

        Debug.Log($"Saved emotion capture: {filename}");
    }
}