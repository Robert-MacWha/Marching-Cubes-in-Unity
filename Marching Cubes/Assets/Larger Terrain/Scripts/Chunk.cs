using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk : MonoBehaviour
{
    private float[,,] densityMap;

    private int pointsPerAxis;
    private int numThreads;
    private float surface;

    private ComputeBuffer triangleBuffer, triCountBuffer, mapBuffer;

    private struct Tri
    {
        public Vector3 vertexA;
        public Vector3 vertexB;
        public Vector3 vertexC;
    };

    public void Init(float[,,] map, ComputeShader shader, int ppa, float surface, bool smoothMesh, int numThreads)
    {
        this.densityMap = map;
        this.pointsPerAxis = ppa;
        this.numThreads = numThreads;
        this.surface = surface;

        this.gameObject.AddComponent<MeshFilter>();
        this.gameObject.AddComponent<MeshRenderer>();

        // Seems to help run a little faster, not entirely sure (def isn't slower)
        if (!HasGeometry()) { return; } else

        this.gameObject.GetComponent<MeshFilter>().mesh = CreateMesh(shader, smoothMesh);
    }

    private bool HasGeometry()
    {
        for (int x = 0; x < pointsPerAxis; x++)
        {
            for (int y = 0; y < pointsPerAxis; y++)
            {
                for (int z = 0; z < pointsPerAxis; z++)
                {
                    if (densityMap[x, y, z] > surface)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private Mesh CreateMesh (ComputeShader shader, bool smoothMesh)
    {
        Mesh m = new Mesh();

        // Setup the compute shader
        int threadsPerAxis = Mathf.CeilToInt(pointsPerAxis / (float)numThreads);

        CreateBufferes();

        int kernel = shader.FindKernel("Marcher_Ex");
        shader.SetBuffer(kernel, "triangles", triangleBuffer);

        // Setup shader inputs
        shader.SetInt("numThreads", numThreads);
        shader.SetInt("pointsPerAxis", pointsPerAxis);
        shader.SetFloat("surfaceLevel", surface);
        shader.SetBool("smoothMesh", smoothMesh);
        shader.SetBuffer(kernel, "densityMap", mapBuffer);

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

        DisposeBufferes();

        // Build the mesh
        m = ConstructMesh(triangles);

        return m;
    }

    private void CreateBufferes ()
    {
        int maxTriCount = (pointsPerAxis) * (pointsPerAxis) * (pointsPerAxis) * 5;
        triangleBuffer = new ComputeBuffer(maxTriCount, sizeof(float) * 3 * 3, ComputeBufferType.Append);
        triCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
        triangleBuffer.SetCounterValue(0);

        mapBuffer = new ComputeBuffer(densityMap.Length, sizeof(float));
        mapBuffer.SetData(densityMap);
    }

    private void DisposeBufferes ()
    {
        triangleBuffer.Dispose();
        triCountBuffer.Dispose();
        mapBuffer.Dispose();
    }

    private Mesh ConstructMesh(Tri[] triangles)
    {
        Mesh m = new Mesh();

        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();

        foreach (Tri t in triangles)
        {
            verts.Add(t.vertexA);
            tris.Add(verts.Count - 1);

            verts.Add(t.vertexB);
            tris.Add(verts.Count - 1);

            verts.Add(t.vertexC);
            tris.Add(verts.Count - 1);
        }

        m.vertices = verts.ToArray();
        m.triangles = tris.ToArray();
        m.RecalculateNormals();

        return m;
    }

    /*  Used for debugging point placement (all should be inside mesh)
    public void OnDrawGizmos()
    {
        for (int x = 0; x < pointsPerAxis; x++)
        {
            for (int y = 0; y < pointsPerAxis; y++)
            {
                for (int z = 0; z < pointsPerAxis; z++)
                {
                    if (densityMap[x, y, z] < surface)
                    {
                        Gizmos.DrawSphere(new Vector3(x, y, z) + transform.position, 0.1f);
                    }
                }
            }
        }
    }
    */
}
