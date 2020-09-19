using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise
{
    public enum NormalizeMode {Local, Global};
    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, NoiseSettings settings, Vector2 sampleCenter)
    {
        float[,] noiseMap = new float[mapWidth,mapHeight];

        //Pseudo random number generation of a seed for the noise map
        System.Random prng = new System.Random(settings.seed);
        Vector2[] octaveOffsets = new Vector2[settings.octaves];

        float maxPossibleHeight = 0;
        float amplitude = 1;
        float frequency = 1;

        for (int i = 0; i < settings.octaves; i++)
        {
            float offsetX = prng.Next(-100000,100000) + settings.offset.x + sampleCenter.x;
            float offsetY = prng.Next(-100000,100000) - settings.offset.y - sampleCenter.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);

            maxPossibleHeight += amplitude;
            amplitude *= settings.persistance;
        }

        //For the editor mode, the procedural generation uses global values to ensure the same result for each thread
        float maxLocalNoiseHeight = float.MinValue;
        float minLocalNoiseHeight = float.MaxValue;

        //Used to sample noise from the center of the map instead of from the top right
        float halfWidth = mapWidth /2f;
        float halfHeight = mapHeight /2f;

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                amplitude = 1;
                frequency = 1;
                float noiseHeight = 0;

                for (int i = 0; i < settings.octaves; i++)
                {
                    float sampleX = (x-halfWidth + octaveOffsets[i].x) / settings.scale * frequency ;
                    float sampleY = (y-halfHeight + octaveOffsets[i].y) / settings.scale * frequency;

                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= settings.persistance;
                    frequency *= settings.lacunarity;
                }

                if(noiseHeight > maxLocalNoiseHeight) 
                { 
                    maxLocalNoiseHeight = noiseHeight;
                }
                if (noiseHeight < minLocalNoiseHeight) {
                    minLocalNoiseHeight = noiseHeight;
                }
                noiseMap[x,y] = noiseHeight;

                if(settings.normalizeMode == NormalizeMode.Global)
                {
                    float normalizedHeight = (noiseMap[x, y] + 1) / maxPossibleHeight; // 1.75f is there to estimate the max global value
                    noiseMap[x,y] = Mathf.Clamp(normalizedHeight, 0, int.MaxValue);
                }
            }
        }

        NormalizeNoiseMap(noiseMap, mapWidth, mapHeight, minLocalNoiseHeight, maxLocalNoiseHeight, maxPossibleHeight, settings.normalizeMode);

        return noiseMap;
    }

    private static void NormalizeNoiseMap(float[,] noiseMap, int mapWidth, int mapHeight, float minLocalNoiseHeight, float maxLocalNoiseHeight, float maxGlobalNoiseHeight, NormalizeMode normalizeMode)
    {
        if(normalizeMode == NormalizeMode.Local)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    noiseMap[x,y] = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap[x, y]);
                }
            }
        }  
    }
}

[System.Serializable]
public class NoiseSettings
{
    public Noise.NormalizeMode normalizeMode;
    public float scale = 50;
    //Number of noise maps to overlap for finer grain detail
    public int octaves = 6;
    //Controls decrease in amplitude(y-axis) of octaves
    [Range(0,1)]
    public float persistance =0.6f;
    //Controls increase in frequency(x-axis) of octaves
    public float lacunarity = 2;
    public int seed;
    public Vector2 offset;

    public void ValidateValues()
    {
        scale = Mathf.Max(scale, 0.01f);
        octaves = Mathf.Max(octaves, 1);
        lacunarity = Mathf.Max(lacunarity, 1);
        persistance = Mathf.Clamp01(persistance);
    }
}