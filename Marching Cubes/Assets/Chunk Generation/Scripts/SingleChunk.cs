using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SocialPlatforms;

[ExecuteInEditMode]
public class SingleChunk : MonoBehaviour
{
    public ComputeShader shader;

    [Header("Noise settings")]
    [Tooltip("How large individual steps are")]
    [Range(0.01f, 0.1f)]
    public float frequency;
    [Range(200, 5000)]
    public int offset;

    [Header("Display settings")]
    [Tooltip("Merges like vertices to smooth normals (WARNING, EXPENSIVE)")]
    public bool smoothShading;
    [Tooltip("Places vertices at more accurate positions")]
    public bool smoothMesh;
    [Range(0, 1)]
    [Tooltip("Where to draw the surface on the shape")]
    public float surfaceLevel;
    [Tooltip("How many samples there are per chunk")]
    [Range(1, 30)]
    public int resolution;
    [Tooltip("Distance between points")]
    public float pointResolution;

    private MeshFilter meshFilter;
    private ComputeBuffer triangleBuffer, triCountBuffer;

    private struct Tri
    {
        public Vector3 vertexA;
        public Vector3 vertexB;
        public Vector3 vertexC;
    };

    private void Update()
    {
        if (Application.isEditor)
        {
            Generate();
        }
    }

    public void Generate ()
    {
        // Get the object's mesh filter
        meshFilter = transform.GetComponent<MeshFilter>();

        int threadsPerAxis = Mathf.CeilToInt(resolution / 8.0f);
        // Converts range 0 / 1 to -1 / 1
        float trueSurfaceLevel = surfaceLevel * 2 - 1;

        // Create compute buffers for data extraction
        CreateBuffers();

        // Setup the marchingCubes shader
        triangleBuffer.SetCounterValue(0);
        shader.SetBuffer(0, "triangles", triangleBuffer);

        shader.SetInt("offset", offset);
        shader.SetInt("pointsPerAxis", resolution);
        shader.SetFloat("pointRes", pointResolution);
        shader.SetFloat("surfaceLevel", trueSurfaceLevel);
        shader.SetFloat("frequency", frequency);
        shader.SetBool("smoothMesh", smoothMesh);

        // Run the kernel
        shader.Dispatch(0, threadsPerAxis, threadsPerAxis, threadsPerAxis);

        // Get number of triangles in the triangle buffer
        ComputeBuffer.CopyCount(triangleBuffer, triCountBuffer, 0);
        int[] triCountArray = { 0 };
        triCountBuffer.GetData(triCountArray);
        int numTris = triCountArray[0];

        // Get triangle data from shader
        Tri[] triangles = new Tri[numTris];
        triangleBuffer.GetData(triangles, 0, 0, numTris);

        // Convert the tri data into mesh data
        Mesh m = GenerateMesh(triangles);

        // Apply the mesh to the object
        meshFilter.mesh = m;

        ReleaseBuffers();
    }

    private Mesh GenerateMesh (Tri[] triangles)
    {
        Mesh m = new Mesh();

        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();

        foreach(Tri t in triangles)
        {
            // Smooth terrain requires a different function
            if (smoothShading)
            {
                // See if the verts have already been added
                if (verts.Contains(t.vertexA)) tris.Add(verts.IndexOf(t.vertexA));
                else
                {
                    verts.Add(t.vertexA);
                    tris.Add(verts.Count - 1);
                }

                if (verts.Contains(t.vertexB)) tris.Add(verts.IndexOf(t.vertexB));
                else
                {
                    verts.Add(t.vertexB);
                    tris.Add(verts.Count - 1);
                }

                if (verts.Contains(t.vertexC)) tris.Add(verts.IndexOf(t.vertexC));
                else
                {
                    verts.Add(t.vertexC);
                    tris.Add(verts.Count - 1);
                }

            } 
            else
            {
                verts.Add(t.vertexA);
                tris.Add(verts.Count - 1);

                verts.Add(t.vertexB);
                tris.Add(verts.Count - 1);

                verts.Add(t.vertexC);
                tris.Add(verts.Count - 1);

            }
        }

        m.vertices = verts.ToArray();
        m.triangles = tris.ToArray();
        m.RecalculateNormals();

        return m;
    }

    private void CreateBuffers ()
    {
        // Max number of tris given the resolution
        int maxTriCount = (resolution) * (resolution) * (resolution);

        triangleBuffer = new ComputeBuffer(maxTriCount, sizeof(float) * 3 * 3, ComputeBufferType.Append);
        triCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
    }

    private void ReleaseBuffers ()
    {
        if (triangleBuffer != null)
        {
            triangleBuffer.Release();
            triCountBuffer.Release();
        }
    }
}
