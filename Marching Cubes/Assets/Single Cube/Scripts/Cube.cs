using System.Collections;
using System.Collections.Generic;
using TMPro.EditorUtilities;
using UnityEditor.Animations;
using UnityEngine;

public class Cube : MonoBehaviour
{
    [Header("Selectors for cube's vertices")]
    public GameObject[] Selectors = new GameObject[8];
    private Selector[] states = new Selector[8];

    private MeshFilter meshFilter;

    struct Tri
    {
        public Vector3 vertexA;
        public Vector3 vertexB;
        public Vector3 vertexC;
    }

    private void Start()
    {
        // Get the object's mesh filter
        meshFilter = transform.GetComponent<MeshFilter>();

        // Get the selector objects from each of the selectors
        int i = 0;
        foreach(GameObject g in Selectors)
        {
            states[i] = g.GetComponent<Selector>();
            i++;
        }
    }

    private void Update()
    {
        // If any of the selectors have been changed, update the mesh
        bool hasChanged = false;
        foreach (Selector s in states)
        {
            if (s.hasUpdated()) { hasChanged = true; }
        }

        // Update the cube's mesh
        if (hasChanged)
        {
            List<Tri> triangles = new List<Tri>();

            // Get the cube's new index for lookup tables
            int cubeIndex = 0;
            if (states[0].state) { cubeIndex |= 1; }
            if (states[1].state) { cubeIndex |= 2; }
            if (states[2].state) { cubeIndex |= 4; }
            if (states[3].state) { cubeIndex |= 8; }
            if (states[4].state) { cubeIndex |= 16; }
            if (states[5].state) { cubeIndex |= 32; }
            if (states[6].state) { cubeIndex |= 64; }
            if (states[7].state) { cubeIndex |= 128; }

            // Create triangles for current cube
            for (int i = 0; MarchTables.triangulation[cubeIndex, i] != -1; i += 3)
            {
                // Get indices of corner points A and B for each of the three edges
                // of the cube that need to be joined to form the triangle.
                int a0 = MarchTables.cornerIndexAFromEdge[MarchTables.triangulation[cubeIndex, i]];
                int b0 = MarchTables.cornerIndexBFromEdge[MarchTables.triangulation[cubeIndex, i]];

                int a1 = MarchTables.cornerIndexAFromEdge[MarchTables.triangulation[cubeIndex, i + 1]];
                int b1 = MarchTables.cornerIndexBFromEdge[MarchTables.triangulation[cubeIndex, i + 1]];

                int a2 = MarchTables.cornerIndexAFromEdge[MarchTables.triangulation[cubeIndex, i + 2]];
                int b2 = MarchTables.cornerIndexBFromEdge[MarchTables.triangulation[cubeIndex, i + 2]];

                Tri tri;
                tri.vertexA = interpolateVerts(states[a0].transform.position, states[b0].transform.position);
                tri.vertexB = interpolateVerts(states[a1].transform.position, states[b1].transform.position);
                tri.vertexC = interpolateVerts(states[a2].transform.position, states[b2].transform.position);
                triangles.Add(tri);
            }

            // Create the new mesh
            Vector3[] vertices = new Vector3[triangles.Count * 3];
            int[] tris = new int[triangles.Count * 3];

            int j = 0;
            foreach(Tri t in triangles)
            {
                vertices[j + 0] = t.vertexA;
                vertices[j + 1] = t.vertexB;
                vertices[j + 2] = t.vertexC;

                tris[j + 0] = j + 0;
                tris[j + 1] = j + 1;
                tris[j + 2] = j + 2;

                j += 3;
            }

            Mesh m = new Mesh();
            m.vertices = vertices;
            m.triangles = tris;
            m.RecalculateNormals();

            // Apply the new mesh
            meshFilter.mesh = m;
        }
    }

    private Vector3 interpolateVerts (Vector3 v1, Vector3 v2)
    {
        return ((v1 + v2) / 2);
    }
}
