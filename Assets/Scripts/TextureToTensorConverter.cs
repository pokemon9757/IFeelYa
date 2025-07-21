using UnityEngine;
using Unity.Barracuda;

public class TextureToTensorConverter : MonoBehaviour
{
    /// <summary>
    /// Converts a Texture2D to a RGB Tensor with specified dimensions
    /// </summary>
    /// <param name="inputTexture">The input texture to convert</param>
    /// <param name="targetWidth">Target width for the output tensor</param>
    /// <param name="targetHeight">Target height for the output tensor</param>
    /// <param name="normalizePixels">Whether to normalize pixel values to 0-1 range</param>
    /// <returns>A Tensor with shape (1, targetHeight, targetWidth, 3) for RGB</returns>
    public static Tensor ConvertTextureToTensor(Texture2D inputTexture, int targetWidth, int targetHeight, bool normalizePixels = true)
    {
        if (inputTexture == null)
        {
            Debug.LogError("Input texture is null!");
            return null;
        }

        // Create a resized version of the texture at target dimensions
        Texture2D resizedTexture = ResizeTexture(inputTexture, targetWidth, targetHeight);
        
        // Get pixel data
        Color[] pixels = resizedTexture.GetPixels();
        
        // Create tensor data array for RGB (3 channels)
        float[] tensorData = new float[targetWidth * targetHeight * 3];
        
        // Fill tensor data with RGB values
        for (int i = 0; i < pixels.Length; i++)
        {
            Color pixel = pixels[i];
            int baseIndex = i * 3;
            
            tensorData[baseIndex] = normalizePixels ? pixel.r : pixel.r * 255f;     // R
            tensorData[baseIndex + 1] = normalizePixels ? pixel.g : pixel.g * 255f; // G
            tensorData[baseIndex + 2] = normalizePixels ? pixel.b : pixel.b * 255f; // B
        }
        
        // Create tensor with shape (batch=1, height=targetHeight, width=targetWidth, channels=3)
        TensorShape shape = new TensorShape(1, targetHeight, targetWidth, 3);
        Tensor tensor = new Tensor(shape, tensorData);
        
        // Clean up temporary texture if it was created
        if (resizedTexture != inputTexture)
        {
            DestroyImmediate(resizedTexture);
        }
        
        return tensor;
    }
    
    /// <summary>
    /// Resizes a Texture2D to the specified dimensions
    /// </summary>
    /// <param name="source">Source texture</param>
    /// <param name="targetWidth">Target width</param>
    /// <param name="targetHeight">Target height</param>
    /// <returns>Resized texture</returns>
    private static Texture2D ResizeTexture(Texture2D source, int targetWidth, int targetHeight)
    {
        // If already the correct size, return original
        if (source.width == targetWidth && source.height == targetHeight)
        {
            return source;
        }
        
        // Create render texture for resizing
        RenderTexture rt = new RenderTexture(targetWidth, targetHeight, 24);
        RenderTexture.active = rt;
        
        // Render the source texture to the render texture
        Graphics.Blit(source, rt);
        
        // Create new texture and read pixels from render texture
        Texture2D result = new Texture2D(targetWidth, targetHeight, TextureFormat.RGB24, false);
        result.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
        result.Apply();
        
        // Clean up
        RenderTexture.active = null;
        rt.Release();
        
        return result;
    }
    
    /// <summary>
    /// Overload for backward compatibility - defaults to 256x256
    /// </summary>
    public static Tensor ConvertTextureToTensor(Texture2D inputTexture, bool normalizePixels = true)
    {
        return ConvertTextureToTensor(inputTexture, 256, 256, normalizePixels);
    }
}