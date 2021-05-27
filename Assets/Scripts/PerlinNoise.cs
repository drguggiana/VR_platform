// Taken from the tutorial here:
// https://www.youtube.com/watch?v=bG0uEXV6aHQ

using UnityEngine;


public class PerlinNoise : MonoBehaviour
{
    [Range(1f, 100f)]
    public float scale = 20f;

    //[Range(0.1f, 1f)]
    //public float alpha = 1f;

    private int width = 256;
    private int height = 256;
    private float offsetX;
    private float offsetY;

    private Renderer renderer;
 

    void Start()
    {
        renderer = GetComponent<Renderer>();

        offsetX = Random.Range(0f, 100f);
        offsetY = Random.Range(0f, 100f);
        
    }

    void Update()
    {
        renderer.material.mainTexture = GenerateTexture();
    }

    Texture2D GenerateTexture()
    {
        Texture2D texture = new Texture2D(width, height);

        // Generate a Perlin noise map for the texture

        for (int x = 0; x< width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Color color = CalculateColor(x, y);
                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();

        return texture;
    }

    Color CalculateColor(int x, int y)
    {
        float xCoord = (float) x / width * scale + offsetX;
        float yCoord = (float) y / height * scale + offsetY;

        float sample = Mathf.PerlinNoise(xCoord, yCoord);
        // sample = sample * alpha;
        return new Color(sample, sample, sample);
    }

}
