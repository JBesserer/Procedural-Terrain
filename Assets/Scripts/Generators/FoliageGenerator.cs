using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FoliageGenerator
{
    public static FoliageInfo GenerateFoliage(Vector3[] chunkVertices, Matrix4x4 matrix, HeightMapSettings heightMapSettings, TextureData textureSettings)
    {
        List<Vector3> verticesFoliageLevel = new List<Vector3>();
        //TODO: The layers shouldnt be written like this.
        float heightAtGrassLevel = heightMapSettings.maxHeight*(textureSettings.layers[2].startHeight + 0.05f);
        float heightAtNextLevel = heightMapSettings.maxHeight*(textureSettings.layers[3].startHeight - 0.05f);
        for (int i = 0; i < chunkVertices.Length; i++)
        {
            Vector3 meshWorldPosition = matrix.MultiplyPoint3x4(chunkVertices[i]);
            if(meshWorldPosition.y >= heightAtGrassLevel && meshWorldPosition.y < heightAtNextLevel)
            {
                verticesFoliageLevel.Add(meshWorldPosition);
            }
        }
        //Returns the percentage of vertices at the foliage level and the untrimmed array of vertices 
        //Untrimmed because(Random.Range can only be called on the Main Thread and this method is used in multithreading situations)
        float percentFoliageLevel = ((float)verticesFoliageLevel.Count / (float)chunkVertices.Length);
        int numberOfTreesToSpawnUntrimmed = Mathf.CeilToInt(verticesFoliageLevel.Count * percentFoliageLevel);
        return new FoliageInfo(verticesFoliageLevel, numberOfTreesToSpawnUntrimmed);
    }
}
