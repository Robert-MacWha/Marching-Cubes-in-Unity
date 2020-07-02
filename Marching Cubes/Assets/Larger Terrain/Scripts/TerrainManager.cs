using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class TerrainManager : MonoBehaviour
{
    public bool updateAutomaticly;
    [Header("Compute Shaders")]
    public ComputeShader simplexNoiseShader;
    public ComputeShader hydraulicErosionShader;
    public ComputeShader marchingCubesShader;
    [Tooltip("Very important to be accurate")]
    public int numThreads;

    [Header("World Generation Settings")]
    [Range(1, 30)]
    public int chunkSize;
    [Range(1, 10)]
    public int worldSize;

    [Header("Noise Settings")]
    [Range(1, 10)]
    [Tooltip("Number of layers usded")]
    public int octaves;
    [Range(0.0f, 1.0f)]
    [Tooltip("Affect each following layer has")]
    public float persistance;
    [Range(0.001f, 0.07f)]
    [Tooltip("Size each following layer has")]
    public float frequency;
    [Tooltip("How tall the mesh gets")]
    public float steepness;

    [Header("Erosion settings")]
    [Tooltip("In thousands")]
    public int dropCount;
    [Tooltip("How large the drop starts as")]
    public float dropStartSize;
    [Tooltip("How quickly the drop shrinks")]
    public float evaporationRate;
    [Tooltip("How much material the drop can pick up each iteration")]
    public float groundHardness;

    [Header("Chunk settings")]
    public Material chunkMaterial;
    [Tooltip("Averages vertice positions in a more accurate manner, no impact on preformace")]
    public bool smoothMesh;

    private float[,,] densityMap;
    private Dictionary<Vector3, GameObject> chunks;

    private ComputeBuffer densityMapBuffer, pointBuffer;

    private void Start()
    {
        // Update the terrain
        if (Application.isPlaying)
        {
            DeleteChildren();
            GenerateScene();
        }
    }

    private void Update()
    {
        // Update the terrain
        if (Application.isEditor && updateAutomaticly)
        {
            DeleteChildren();
            GenerateScene();
        }
    }

    public void DeleteChildren ()
    {
        if (transform.childCount != 0)
        {
            int i = 0;
            GameObject[] allChildren = new GameObject[transform.childCount];

            //Find all child obj and store to that array
            foreach (Transform child in transform)
            {
                allChildren[i] = child.gameObject;
                i += 1;
            }

            //Now destroy them
            foreach (GameObject child in allChildren)
            {
                if (Application.isEditor)
                    DestroyImmediate(child.gameObject);
                else
                    Destroy(child.gameObject);
            }
        }
    }

    public void GenerateScene ()
    {
        GenerateDensityMap();
        GenerateChunks();
    }

    public void GenerateSceneWithErosion ()
    {
        GenerateDensityMap();
        ApplyHydraulicErosion();
        GenerateChunks();
    }

    private void GenerateDensityMap ()
    {
        // Initialize output map
        int samplesPerAxis = (chunkSize * worldSize) + 1;
        densityMap = new float[samplesPerAxis, samplesPerAxis, samplesPerAxis];

        // Setup compute shader
        densityMapBuffer = new ComputeBuffer(densityMap.Length, sizeof(float));
        densityMapBuffer.SetData(densityMap);

        int kernel = simplexNoiseShader.FindKernel("SimplexNoise");

        simplexNoiseShader.SetInt("numThreads", numThreads);
        simplexNoiseShader.SetInt("samplesPerAxis", samplesPerAxis);
        simplexNoiseShader.SetFloat("steepness", steepness);
        simplexNoiseShader.SetInt("octaves", octaves);
        simplexNoiseShader.SetFloat("persistance", persistance);
        simplexNoiseShader.SetFloat("frequency", frequency);
        simplexNoiseShader.SetBuffer(kernel, "densities", densityMapBuffer);

        int threads = Mathf.Max(Mathf.CeilToInt(samplesPerAxis / (float)numThreads), 1);
        simplexNoiseShader.Dispatch(kernel, threads, threads, threads);

        // Extract density map
        densityMapBuffer.GetData(densityMap);
        densityMapBuffer.Release();
    }

    private void ApplyHydraulicErosion ()
    {
        // Multiply by 1k
        int realDC = dropCount * 1;
        int samplesPerAxis = (chunkSize * worldSize) + 1;

        // Generate drop positions
        Vector3Int[] randomPositions = new Vector3Int[realDC];
        for (int i = 0; i < realDC; i ++)
        {
            int rx = UnityEngine.Random.Range(0, samplesPerAxis);
            int ry = UnityEngine.Random.Range(0, samplesPerAxis);
            int rz = UnityEngine.Random.Range(0, samplesPerAxis);

            randomPositions[i] = new Vector3Int(rx, ry, rz);
        }

        // Setup the compute shader
        densityMapBuffer = new ComputeBuffer(densityMap.Length, sizeof(float));
        pointBuffer = new ComputeBuffer(realDC, sizeof(int) * 3);

        densityMapBuffer.SetData(densityMap);
        pointBuffer.SetData(randomPositions);

        int kernel = hydraulicErosionShader.FindKernel("HydraulicErosion");
        hydraulicErosionShader.SetBuffer(kernel, "densityMap", densityMapBuffer);
        hydraulicErosionShader.SetBuffer(kernel, "positions", pointBuffer);

        hydraulicErosionShader.SetInt("dropCount", realDC);
        hydraulicErosionShader.SetFloat("dropStartSize", dropStartSize);
        hydraulicErosionShader.SetFloat("evaporationRate", evaporationRate);
        hydraulicErosionShader.SetFloat("groundHardness", groundHardness);

        int threads = Mathf.Max(Mathf.CeilToInt(realDC / (float)(numThreads * numThreads * numThreads)), 1);
        simplexNoiseShader.Dispatch(kernel, threads * threads * threads, 1, 1);

        // Extract density map
        densityMapBuffer.GetData(densityMap);
        densityMapBuffer.Release();
        pointBuffer.Release();

        Debug.Log(densityMap[0, 0, 0]);
    }

    private void GenerateChunks ()
    {
        int pointsPerAxis = chunkSize + 1;

        chunks = new Dictionary<Vector3, GameObject>();

        for (int x = 0; x < worldSize; x ++)
        {
            for(int y = 0; y < worldSize; y ++)
            {
                for(int z = 0; z < worldSize; z ++)
                {
                    float[,,] chunkMap = new float[pointsPerAxis, pointsPerAxis, pointsPerAxis];

                    for (int a = 0; a < pointsPerAxis; a++)
                    {
                        for (int b = 0; b < pointsPerAxis; b++)
                        {
                            for (int c = 0; c < pointsPerAxis; c++)
                            {
                                chunkMap[a, b, c] = densityMap[c + (z * chunkSize), b + (y * chunkSize), a + (x * chunkSize)];
                            }
                        }
                    }

                    // Create the chunk and its object
                    GameObject obj = new GameObject();

                    chunks[new Vector3(x, y, z)] = obj;
                    Chunk chunk = obj.AddComponent<Chunk>();
                    chunk.Init(chunkMap, marchingCubesShader, pointsPerAxis, 1, smoothMesh, numThreads);

                    // Set the object's identification/settings
                    obj.name = x + ", " + y + ", " + z;
                    obj.transform.position = new Vector3(x, y, z) * chunkSize;
                    obj.transform.parent = transform;
                    obj.GetComponent<MeshRenderer>().material = chunkMaterial;
                }
            }
        }
    }
}
