using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    //Foliage Generation related variables
    public float radius = 1;
    public int rejectionSamples = 30;
    public GameObject treeObject;
    //Terrain Generation related variables
    public LODInfo[] detailLevels;
    public MeshSettings meshSettings;
    public HeightMapSettings heightMapSettings;
    public TextureData textureSettings;
    public Transform viewer;
    public Material mapMaterial;
    public int colliderLODIndex;
    private Vector2 viewerPosition;
    private Vector2 viewerPositionOld;
    private const float viewerMoveThresholdForChunkUpdate = 25f;
    private const float sqrViewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;
    private float meshWorldSize;
    private int chunksVisibleInViewDistance;
    private Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    private static List<TerrainChunk> visibleTerrainChunks = new List<TerrainChunk>();


    private void Start()
    {
        textureSettings.UpdateMeshHeights(mapMaterial, heightMapSettings.minHeight, heightMapSettings.maxHeight);

        float maxViewDistance = detailLevels[detailLevels.Length - 1].visibleDistanceThreshold;
        meshWorldSize = meshSettings.meshWorldSize;
        chunksVisibleInViewDistance = Mathf.RoundToInt(maxViewDistance / meshWorldSize);

        UpdateVisibleChunks();
    }

    private void Update()
    {
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z);
        foreach (TerrainChunk chunk in visibleTerrainChunks)
        {
            chunk.UpdateCollisionMesh();
            UpdateVisibleFoliage(chunk);
        }
        if ((viewerPositionOld - viewerPosition).sqrMagnitude > sqrViewerMoveThresholdForChunkUpdate)
        {
            viewerPositionOld = viewerPosition;
            UpdateVisibleChunks();
        }
    }

    private void UpdateVisibleFoliage(TerrainChunk chunk)
    {
        if (chunk.hasSetCollider && !chunk.hasSetFoliage)
        {
            List<Vector2> foliageSpawnPoints = FoliageGenerator.GenerateFoliage(radius, meshSettings, rejectionSamples);
            //Foliage generation
            for (int i = 0; i < foliageSpawnPoints.Count; i++)
            {
                RaycastHit hit;
                Ray ray = new Ray(new Vector3(foliageSpawnPoints[i].x, heightMapSettings.maxHeight, foliageSpawnPoints[i].y), Vector3.down);
                if (chunk.meshCollider.Raycast(ray, out hit, 2.0f * heightMapSettings.maxHeight))
                {
                    float heightAtGrassLevel = heightMapSettings.maxHeight*(textureSettings.layers[2].startHeight + 0.05f);
                    float heightAtNextLevel = heightMapSettings.maxHeight*(textureSettings.layers[3].startHeight - 0.05f);
                    if(hit.point.y >= heightAtGrassLevel && hit.point.y < heightAtNextLevel)
                    {  
                        GameObject gameObject = Instantiate(treeObject, hit.point, Quaternion.identity);
                        gameObject.name = "Tree";
                        gameObject.transform.parent = chunk.meshObject.transform;
                    }
                }
            }
            chunk.hasSetFoliage = true;
        }
    }

    private void UpdateVisibleChunks()
    {
        HashSet<Vector2> alreadyUpdatedChunkCoords = new HashSet<Vector2>();
        for (int i = visibleTerrainChunks.Count - 1; i >= 0; i--)
        {
            alreadyUpdatedChunkCoords.Add(visibleTerrainChunks[i].coordinates);
            visibleTerrainChunks[i].UpdateTerrainChunk();
        }

        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / meshWorldSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / meshWorldSize);

        for (int yOffset = -chunksVisibleInViewDistance; yOffset <= chunksVisibleInViewDistance; yOffset++)
        {
            for (int xOffset = -chunksVisibleInViewDistance; xOffset <= chunksVisibleInViewDistance; xOffset++)
            {
                Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);
                if (!alreadyUpdatedChunkCoords.Contains(viewedChunkCoord))
                {
                    if (terrainChunkDictionary.ContainsKey(viewedChunkCoord))
                    {
                        terrainChunkDictionary[viewedChunkCoord].UpdateTerrainChunk();
                    }
                    else
                    {
                        TerrainChunk newChunk = new TerrainChunk(viewedChunkCoord, heightMapSettings, meshSettings, detailLevels, colliderLODIndex, transform, viewer, mapMaterial);
                        terrainChunkDictionary.Add(viewedChunkCoord, newChunk);
                        newChunk.onVisibilityChanged += OnTerrainChunkVisibilityChanged;
                        newChunk.Load();
                    }
                }
            }
        }
    }

    private void OnTerrainChunkVisibilityChanged(TerrainChunk chunk, bool isVisible)
    {
        if (isVisible)
        {
            visibleTerrainChunks.Add(chunk);
        }
        else
        {
            visibleTerrainChunks.Remove(chunk);
        }
    }
}
