using Unity.Barracuda;
using UnityEngine;

public class EmotionAnalyzer : MonoBehaviour
{
    public NNModel modelAsset;

    [Header("Emotion Recognition Parameters")]
    [Tooltip("Offset added to valence output. Decrease if valence is too high.")]
    public float offset_v = 0.0f;  // Removed default offset
    [Tooltip("Offset added to arousal output. Adjust to shift arousal range.")]
    public float offset_a = 0.0f;  // Removed default offset

    [Header("Normalization Parameters")]
    [Tooltip("RGB mean values for normalization. Adjust if emotions aren't recognized correctly.")]
    public Vector3 normalizationMean = new Vector3(0.285f, 0.256f, 0.206f);  // Much lower for increased sensitivity
    [Tooltip("RGB standard deviation values for normalization. Adjust if emotions aren't recognized correctly.")]
    public Vector3 normalizationStd = new Vector3(0.4f, 0.4f, 0.4f);  // Significantly increased for more pronounced differences

    private Model runtimeModel;
    private IWorker worker;
    private const int IMAGE_SIZE = 224;
    float timeCount = 0;
    // Match Python's emotion categories
    private readonly string[] emotionList = new string[]
    {
        "angry", "sad", "happy", "pleased", "neutral"
    };

    void Start()
    {
        runtimeModel = ModelLoader.Load(modelAsset);
        worker = WorkerFactory.CreateWorker(WorkerFactory.Type.ComputePrecompiled, runtimeModel);
    }

    public Vector2 GetValenceArousal(Texture2D faceImage)
    {
        Tensor inputTensor = PreprocessImage(faceImage);
        worker.Execute(inputTensor);
        Tensor outputTensor = worker.PeekOutput();



        float rawValence = outputTensor[0, 0];
        float rawArousal = outputTensor[0, 1];

        float valence = rawValence + offset_v;
        float arousal = rawArousal + offset_a;

        // Log raw and adjusted values for tuning
        if (Time.time - timeCount > 3f)
        {
            Debug.Log($"Emotion Values - Raw: (V:{rawValence:F3}, A:{rawArousal:F3}), " +
                     $"Adjusted: (V:{valence:F3}, A:{arousal:F3})");
            timeCount = Time.time;  
        }

        inputTensor.Dispose();
        return new Vector2(valence, arousal);
    }

    public string GetDiscreteEmotion(Vector2 emotionValues)
    {
        float valence = emotionValues.x;
        float arousal = emotionValues.y;

        // Match Python's emotion classification logic exactly
        if (Mathf.Abs(valence) < 0.1f && Mathf.Abs(arousal) < 0.1f)
            return emotionList[4];  // neutral
        else if (valence > 0.1f && arousal > 0.2f)
            return emotionList[2];  // happy
        else if (valence < -0.1f && arousal > 0.1f)
            return emotionList[0];  // angry
        else if (valence < -0.1f && arousal < -0.1f)
            return emotionList[1];  // sad
        else if (valence > 0.1f && arousal < -0.1f)
            return emotionList[3];  // pleased

        return emotionList[4];  // default to neutral
    }

    private Tensor PreprocessImage(Texture2D image)
    {
        Texture2D resized = ResizeImage(image, IMAGE_SIZE, IMAGE_SIZE);
        float[] tensorData = new float[1 * 3 * IMAGE_SIZE * IMAGE_SIZE];

        for (int y = 0; y < IMAGE_SIZE; y++)
        {
            for (int x = 0; x < IMAGE_SIZE; x++)
            {
                Color pixel = resized.GetPixel(x, y);
                int offset = (y * IMAGE_SIZE + x) * 3;

                // Apply normalization with adjustable parameters
                tensorData[offset] = (pixel.r - normalizationMean.x) / normalizationStd.x;
                tensorData[offset + 1] = (pixel.g - normalizationMean.y) / normalizationStd.y;
                tensorData[offset + 2] = (pixel.b - normalizationMean.z) / normalizationStd.z;
            }
        }

        return new Tensor(1, IMAGE_SIZE, IMAGE_SIZE, 3, tensorData);
    }

    private Texture2D ResizeImage(Texture2D source, int targetWidth, int targetHeight)
    {
        RenderTexture rt = RenderTexture.GetTemporary(targetWidth, targetHeight);
        Graphics.Blit(source, rt);

        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D result = new Texture2D(targetWidth, targetHeight);
        result.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
        result.Apply();

        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);

        return result;
    }

    void OnDestroy()
    {
        worker?.Dispose();
    }
}