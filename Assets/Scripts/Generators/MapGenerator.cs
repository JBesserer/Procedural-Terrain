using System.Collections;
using System.Collections.Generic;
using System;
using System.Threading;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public enum DrawMode {NoiseMap, ColourMap, Mesh, FalloffMap};
    public DrawMode drawMode;
    public Noise.NormalizeMode normalizeMode;
    public bool useFlatShading;
    [Range(0,6)]
    public int editorPreviewLOD;
    public float noiseScale;
    [Tooltip("Number of noise maps to overlap for finer grain detail")]
    public int octaves;
    [Range(0,1)]
    [Tooltip("Controls decrease in amplitude(y-axis) of octaves")]
    public float persistance;
    [Tooltip("Controls increase in frequency(x-axis) of octaves")]
    public float lacunarity;
    public int seed;
    public Vector2 offset;
    public bool useFalloff;
    public float meshHeightMultiplier;
    public AnimationCurve meshHeightCurve;
    public bool autoUpdate;
    public TerrainType[] regions;
    private float[,] falloffMap;
    private static MapGenerator instance;

    private Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
    private Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

    private void Awake() 
    {
        falloffMap = FalloffGenerator.GenerateFalloffMap(mapChunkSize);
    }

    public static int mapChunkSize{
        get {
            if(instance == null)
            {
                instance = FindObjectOfType<MapGenerator>();
            }
            if(instance.useFlatShading)
            {
                return 95;
            } else {
                return 239;
            }
        }
    }
    public void DrawMapInEditor()
    {
        MapData mapData = GenerateMapData(Vector2.zero);
        MapView view = FindObjectOfType<MapView>();
        if(drawMode == DrawMode.NoiseMap)
        {
            view.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.heightMap));
        }  
        else if (drawMode == DrawMode.ColourMap)
        {
            view.DrawTexture(TextureGenerator.TextureFromColourMap(mapData.colourMap, mapChunkSize, mapChunkSize));
        }
        else if (drawMode == DrawMode.Mesh)
        {
            view.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, editorPreviewLOD, useFlatShading),TextureGenerator.TextureFromColourMap(mapData.colourMap, mapChunkSize, mapChunkSize));
        }
        else if (drawMode == DrawMode.FalloffMap)
        {
            view.DrawTexture(TextureGenerator.TextureFromHeightMap(FalloffGenerator.GenerateFalloffMap(mapChunkSize)));
        }
    }

    public void RequestMapData(Vector2 center, Action<MapData> callback)
    {
        ThreadStart threadStart = delegate {
            MapDataThread(center, callback);
        };

        new Thread(threadStart).Start();
    }

    private void MapDataThread(Vector2 center, Action<MapData> callback)
    {
        MapData mapData = GenerateMapData(center);
        //Locks the thread when the method reaches that point so that no other thread can execute it as well, will have to wait it's turn
        lock (mapDataThreadInfoQueue)
        {
            mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback,mapData));
        }
    }

    public void RequestMeshData(MapData mapData, int lod, Action<MeshData> callback)
    {
        ThreadStart threadStart = delegate {
            MeshDataThread(mapData, lod, callback);
        };

        new Thread(threadStart).Start();
    }

    private void MeshDataThread(MapData mapData, int lod, Action<MeshData> callback)
    {
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap,meshHeightMultiplier, meshHeightCurve, lod, useFlatShading);
        //Locks the thread when the method reaches that point so that no other thread can execute it as well, will have to wait it's turn
        lock (meshDataThreadInfoQueue)
        {
            meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
        }
    }

    private void Update() {
        if(mapDataThreadInfoQueue.Count > 0)
        {
            for (int i = 0; i < mapDataThreadInfoQueue.Count; i++)
            {
                MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }

        if(meshDataThreadInfoQueue.Count > 0)
        {
            for (int i = 0; i < meshDataThreadInfoQueue.Count; i++)
            {
                MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
    }
    private MapData GenerateMapData(Vector2 center)
    {
        float[,] heightMap = Noise.GenerateNoiseMap(mapChunkSize + 2, mapChunkSize + 2, seed, noiseScale, octaves, persistance, lacunarity, center + offset, normalizeMode);
        Color[] colourMap = GenerateColourMap(heightMap);
        return new MapData(heightMap, colourMap);
    }
    
    //Generate the Colour map from a height map + the regions based on heights
    private Color[] GenerateColourMap(float[,] heightMap)
    {
        Color[] colourMap = new Color[mapChunkSize*mapChunkSize];
        for (int y = 0; y < mapChunkSize; y++)
        {
            for (int x = 0; x < mapChunkSize; x++)
            {
                if(useFalloff)
                {
                    heightMap[x,y] = Mathf.Clamp01(heightMap[x,y] - falloffMap[x,y]);
                }
                float currentHeight = heightMap[x,y];
                for (int i = 0; i < regions.Length; i++)
                {
                    if(currentHeight >= regions[i].height)
                    {
                        colourMap[y*mapChunkSize + x] = regions[i].colour;
                    } else {
                        break;
                    }
                }
            }
        }
        return colourMap;
    }
    private void OnValidate() 
    {
        if(lacunarity < 1){ lacunarity = 1;}
        if(octaves < 1){ octaves = 1;}
        falloffMap = FalloffGenerator.GenerateFalloffMap(mapChunkSize);
    }

    private struct MapThreadInfo<T>
    {
        public readonly Action<T> callback;
        public readonly T parameter;

        public MapThreadInfo(Action<T> callback, T parameter)
        {
            this.callback = callback;
            this.parameter = parameter;
        }
    }
}

[System.Serializable]
public struct TerrainType
{
    public string name;
    public float height;
    public Color colour;

}

public struct MapData
{
    public readonly float[,] heightMap;
    public readonly Color[] colourMap;

    public MapData(float[,] heightMap, Color[] colourMap)
    {
        this.heightMap = heightMap;
        this.colourMap = colourMap;
    }
}
