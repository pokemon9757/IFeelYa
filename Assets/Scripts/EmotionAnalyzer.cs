using Unity.Barracuda;
using UnityEngine;

public class EmotionAnalyzer : MonoBehaviour
{
    public NNModel modelAsset;
    // Match Python's offset values exactly
   public float offset_v = 0.25f;
    public float offset_a = 0.05f; private Model runtimeModel;
    private IWorker worker;
    private const int IMAGE_SIZE = 224;

    // Match Python's normalization values exactly
    private static readonly float[] NORMALIZATION_MEAN = new float[] { 0.485f, 0.456f, 0.406f };
    private static readonly float[] NORMALIZATION_STD = new float[] { 0.229f, 0.224f, 0.225f };

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



        float valence = outputTensor[0, 0] + offset_v;
        float arousal = outputTensor[0, 1] + offset_a;

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

                // Match Python's normalization exactly
                tensorData[offset] = (pixel.r - NORMALIZATION_MEAN[0]) / NORMALIZATION_STD[0];
                tensorData[offset + 1] = (pixel.g - NORMALIZATION_MEAN[1]) / NORMALIZATION_STD[1];
                tensorData[offset + 2] = (pixel.b - NORMALIZATION_MEAN[2]) / NORMALIZATION_STD[2];
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