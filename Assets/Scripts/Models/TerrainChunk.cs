using UnityEngine;
using System.Collections.Generic;
public class TerrainChunk
{
    public event System.Action<TerrainChunk, bool> onVisibilityChanged;
    public Vector2 coordinates;
    public MeshCollider meshCollider { get; private set;}
    public bool hasSetCollider{get; private set;}
    public bool hasSetFoliage = false;
    public int colliderLODIndex { get; private set;}
    public  int previousLODIndex { get; private set;}
    public GameObject meshObject { get; private set;}
    private const float colliderGenerationDistanceThreshold = 5f;
    private Vector2 sampleCenter;
    private Bounds bounds;
    private HeightMap heightMap;
    private bool hasReceivedHeightMap;
    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;
    private LODInfo[] detailLevels;
    private LODMesh[] lodMeshes;
    private float maxViewDistance;
    private HeightMapSettings heightMapSettings;
    private MeshSettings meshSettings;
    private Transform viewer;

    public TerrainChunk(Vector2 coord, HeightMapSettings heighMapSettings, MeshSettings meshSettings, LODInfo[] detailLevels, int colliderLODIndex, Transform parent, Transform viewer, Material material)
    {
        this.coordinates = coord;
        this.detailLevels = detailLevels;
        this.colliderLODIndex = colliderLODIndex;
        this.heightMapSettings = heighMapSettings;
        this.meshSettings = meshSettings;
        this.viewer = viewer;
        this.previousLODIndex = -1;


        sampleCenter = coord * meshSettings.meshWorldSize / meshSettings.meshScale;
        Vector2 position = coord * meshSettings.meshWorldSize;
        bounds = new Bounds(position, Vector2.one * meshSettings.meshWorldSize);
        

        meshObject = new GameObject("Terrain Chunk");
        meshRenderer = meshObject.AddComponent<MeshRenderer>();
        meshRenderer.material = material;
        meshFilter = meshObject.AddComponent<MeshFilter>();
        meshCollider = meshObject.AddComponent<MeshCollider>();

        meshObject.transform.position = new Vector3 (position.x, 0, position.y);
        meshObject.transform.parent = parent;
        SetVisible(false); //Default state of the Mesh is not visible

        lodMeshes = new LODMesh[detailLevels.Length];
        for (int i = 0; i < detailLevels.Length; i++)
        {
            lodMeshes[i] = new LODMesh(detailLevels[i].lod);
            lodMeshes[i].updateCallback += UpdateTerrainChunk;
            if(i == colliderLODIndex)
            {
                lodMeshes[i].updateCallback += UpdateCollisionMesh;
            }
        }

        maxViewDistance = detailLevels[detailLevels.Length-1].visibleDistanceThreshold;
    }

    public void Load()
    {
        ThreadedDataRequester.RequestData(() => HeightMapGenerator.GenerateHeightMap(meshSettings.numVertsPerLine, meshSettings.numVertsPerLine, heightMapSettings, sampleCenter), OnHeightMapReceived);
    }

    private void OnHeightMapReceived(object heightMapObject)
    {
        this.heightMap = (HeightMap)heightMapObject;
        hasReceivedHeightMap = true;

        UpdateTerrainChunk();
    }

    Vector2 viewerPosition
    {
        get {
            return new Vector2(viewer.position.x, viewer.position.z);
        }
    }

    public void UpdateTerrainChunk() 
    {
        if(hasReceivedHeightMap)
        {
            float viewerDistanceFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
            bool wasVisible = IsVisible();
            bool visible = viewerDistanceFromNearestEdge <= maxViewDistance;

            if(visible)
            {
                int lodIndex = 0;

                for (int i = 0; i < detailLevels.Length-1; i++)
                {
                    if(viewerDistanceFromNearestEdge > detailLevels[i].visibleDistanceThreshold)
                    {
                        lodIndex = i + 1;
                    } else {
                        break;
                    }
                }

                if(lodIndex != previousLODIndex)
                {
                    LODMesh lodMesh = lodMeshes[lodIndex];
                    if(lodMesh.hasMesh)
                    {
                        previousLODIndex = lodIndex;
                        meshFilter.mesh = lodMesh.mesh;
                    } else if (!lodMesh.hasRequestedMesh) {
                        lodMesh.RequestMesh(heightMap, meshSettings);
                    }
                }
            }

            if(wasVisible != visible)
            {
                SetVisible(visible);
                if(onVisibilityChanged != null)
                {
                    onVisibilityChanged(this, visible);
                }
            } 
        }
    }

    public void UpdateCollisionMesh()
    {
        if(!hasSetCollider)
        {   
            float sqrDistanceFromViewerToEdge = bounds.SqrDistance(viewerPosition);
            if(sqrDistanceFromViewerToEdge < detailLevels[colliderLODIndex].sqrVisibleDistanceThreshold)
            {
                if(!lodMeshes[colliderLODIndex].hasRequestedMesh)
                {
                    lodMeshes[colliderLODIndex].RequestMesh(heightMap, meshSettings);
                }
            }

            if(sqrDistanceFromViewerToEdge < colliderGenerationDistanceThreshold * colliderGenerationDistanceThreshold)
            {
                if(lodMeshes[colliderLODIndex].hasMesh)
                {
                    meshCollider.sharedMesh = lodMeshes[colliderLODIndex].mesh;
                    hasSetCollider = true;

                }
            }
        }  
    }

    public void SetVisible(bool visible)
    {
        meshObject.SetActive(visible);
    }

    public bool IsVisible()
    {
        return meshObject.activeSelf;
    }

    private class LODMesh
    {
        public Mesh mesh;
        public bool hasRequestedMesh;
        public bool hasMesh;
        private int lod;
        public event System.Action updateCallback;

        public LODMesh(int lod)
        {
            this.lod = lod;
        }

        public void RequestMesh(HeightMap heightMap, MeshSettings meshSettings)
        {
            hasRequestedMesh = true;
            ThreadedDataRequester.RequestData(() => MeshGenerator.GenerateTerrainMesh(heightMap.values, meshSettings, lod), OnMeshDataReceived);
        }

        private void OnMeshDataReceived(object meshDataObject)
        {
            mesh = ((MeshData)meshDataObject).CreateMesh();
            hasMesh = true;
            updateCallback();
        }
    }
}



[System.Serializable]
public struct LODInfo 
{
    [Range(0,MeshSettings.numSupportedLODs-1)]
    public int lod;
    public float visibleDistanceThreshold;

    public float sqrVisibleDistanceThreshold{
        get {
            return visibleDistanceThreshold * visibleDistanceThreshold;
        }
    }
}