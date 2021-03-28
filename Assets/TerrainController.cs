using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class TerrainController : MonoBehaviour
{
    public int xSize;
    public int zSize;
    public float scale;
    public int octaves;
    [Range(0, 1)]
    public float persistance;
    public float lacunarity;
    public int seed;
    public Vector2 offset;
    public bool autoUpdate;


    float quadSize = 8;
    Vector3[] vertices;
    Mesh mesh;

    void Start()
    {
        GenerateTerrain();
    }

    void Update()
    {
        if (autoUpdate)
        {
            GenerateTerrain();
        }
    }

    void OnValidate()
    {
        if (xSize < 1)
            xSize = 1;
        else if (xSize > 250)
            xSize = 250;

        if (zSize < 1)
            zSize = 1;
        else if (zSize > 250)
            zSize = 250;

        if (lacunarity < 1)
            lacunarity = 1;

        if (octaves < 0)
            octaves = 0;
    }

    void GenerateTerrain()
    {
        GetComponent<MeshFilter>().mesh = mesh = new Mesh();
        mesh.name = "Procedural Terrain";
        vertices = new Vector3[(xSize + 1) * (zSize + 1)];

        System.Random prng = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];
        for (int i = 0; i < octaves; i++)
        {
            float offsetX = prng.Next(-100000, 100000) + offset.x;
            float offsetY = prng.Next(-100000, 100000) + offset.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }

        if (scale <= 0)
        {
            scale = 0.0001f;
        }

        float maxNoiseHeight = float.MinValue;
        float minNoiseHeight = float.MaxValue;

        float halfX = xSize / 2f;
        float halfZ = zSize / 2f;

        for (int i = 0, z = 0; z <= zSize; z++)
        {
            for (int x = 0; x <= xSize; x++, i++)
            {
                (float xCoord, float zCoord) = GetCoordinates(x, z);

                float amplitude = 1;
                float frequency = 1;
                float noiseHeight = 0;

                for (int j = 0; j < octaves; j++)
                {
                    float sampleX = (x - halfX) / scale * frequency + octaveOffsets[j].x;
                    float sampleY = (z - halfZ) / scale * frequency + octaveOffsets[j].y;

                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistance;
                    frequency *= lacunarity;
                }

                if (noiseHeight > maxNoiseHeight)
                {
                    maxNoiseHeight = noiseHeight;
                }
                else if (noiseHeight < minNoiseHeight)
                {
                    minNoiseHeight = noiseHeight;
                }

                vertices[i] = new Vector3(xCoord, 150 * noiseHeight, zCoord);
            }
        }

        for (int i = 0, z = 0; z < zSize; z++)
        {
            for (int x = 0; x < xSize; x++, i++)
            {
                float percentage = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, vertices[i].y);
                if (percentage < 0.1f)
                {
                    vertices[i].y = 0;
                }    
            }
        }

        mesh.vertices = vertices;

        int[] triangles = new int[xSize * zSize * 6];
        for (int ti = 0, vi = 0, y = 0; y < zSize; y++, vi++)
        {
            for (int x = 0; x < xSize; x++, ti += 6, vi++)
            {
                triangles[ti] = vi;
                triangles[ti + 3] = triangles[ti + 2] = vi + 1;
                triangles[ti + 4] = triangles[ti + 1] = vi + xSize + 1;
                triangles[ti + 5] = vi + xSize + 2;
            }
        }
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        GetComponent<MeshCollider>().sharedMesh = mesh;
    }

    (float newX, float newZ) GetCoordinates(float x, float z)
    {
        float newX = quadSize * (x - xSize / 2);
        float newZ = quadSize * (z - xSize / 2);
        return (newX, newZ);
    }
}
