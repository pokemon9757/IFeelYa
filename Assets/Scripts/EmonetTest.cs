using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Barracuda;
using UnityEngine;
using UnityEngine.AI;

public class EmonetTest : MonoBehaviour
{
    public Texture2D texture;
    public NNModel modelAsset;
    private Model _runtimeModel;
    private IWorker _worker;

    // Start is called before the first frame update
    void Start()
    {
        // Set up the runtime model and worker.
        _runtimeModel = ModelLoader.Load(modelAsset);
        _worker = WorkerFactory.CreateWorker(_runtimeModel, WorkerFactory.Device.GPU);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // Create a tensor for input from the texture.
            var inputX = ConvertTextureToTensor(normalizePixels: true);

            // Peek at the output tensor without copying it.
            _worker.Execute(inputX);

            // Get results
            var expressionOutput = _worker.PeekOutput("expression");
            var valenceOutput = _worker.PeekOutput("valence");
            var arousalOutput = _worker.PeekOutput("arousal");

            // Find the emotion with highest score
            float[] scores = expressionOutput.ToReadOnlyArray();

            Debug.Log($"Expression length {scores.Length} scores: {string.Join(", ", scores)}");

            int bestEmotion = 0;
            for (int i = 1; i < scores.Length; i++)
            {
                if (scores[i] > scores[bestEmotion]) bestEmotion = i;
            }

            // Format result
            float valence = valenceOutput[0];
            float arousal = arousalOutput[0];
            Debug.Log($"Best emotion: {bestEmotion}, Valence: {valence}, Arousal: {arousal}");

            // Dispose of the input tensor manually (not garbage-collected).
            inputX.Dispose();
            valenceOutput.Dispose();
            arousalOutput.Dispose();
            expressionOutput.Dispose();
        }
    }
  
    public Tensor ConvertTextureToTensor(bool normalizePixels = true)
    {
        // Get pixel data
        Color[] pixels = texture.GetPixels();
     
        // Create tensor data array for RGB (3 channels)
        float[] tensorData = new float[256 * 256 * 3];

        // Fill tensor data with RGB values
        for (int i = 0; i < pixels.Length; i++)
        {
            Color pixel = pixels[i];
            int baseIndex = i * 3;

            tensorData[baseIndex] = normalizePixels ? pixel.r : pixel.r * 255f;     // R
            tensorData[baseIndex + 1] = normalizePixels ? pixel.g : pixel.g * 255f; // G
            tensorData[baseIndex + 2] = normalizePixels ? pixel.b : pixel.b * 255f; // B
        }

        // Create tensor with shape (batch=1, height=256, width=256, channels=3)
        return new Tensor(new TensorShape(1, 256, 256, 3), tensorData);
    }

    private void OnDestroy()
    {
        // Dispose of the engine manually (not garbage-collected).
        _worker?.Dispose();
    }
}