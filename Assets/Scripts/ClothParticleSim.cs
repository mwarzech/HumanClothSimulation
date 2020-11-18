#define CLOTH_DEBUG

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class ClothParticleSim : MonoBehaviour
{
    public float EPSILON = 0.01f;

    [Range(0f,1f)]
    public float damping = 0.99f;
    [Range(0f, 1f)]
    public float tolerance = 0.1f;// the lower, the stiffer the cloth
    public float groundLevel;
    public int physicsIterationsPerFrame = 50;

    Mesh mesh;
    Vector3 acceleration;
    Vector3[] vertices;
    Vector3[] positions;
    Vector3[] oldPositions;
    int[] clothTriangles;
    Vector3[] normals;
    
    public ClothNormalCollisions collisionHandler;

    List<float>[] distances;
    List<int>[] neighbours;
    List<int>[] clothToVerts;
    int[] vertToCloth;

#if CLOTH_DEBUG
    int debugInd = 1 * 3;
#endif

    public void ChangeTolerance(float newValue)
    {
        tolerance = newValue;
    }
    public void ChangeDamping(float newValue)
    {
        damping = newValue;
    }

    private int ClothToVertIndex(int posIndex)
    {
        return clothToVerts[posIndex][0];
    }

    private void InitializeClothVertices(Vector3[] vertices)
    {
        Dictionary<Vector3, List<int>> vertexDict;
        vertexDict = new Dictionary<Vector3, List<int>>();
        vertToCloth = new int[vertices.Length];
        for (int i = 0; i < vertices.Length; ++i)
        {
            if(!vertexDict.ContainsKey(vertices[i]))
            {
                vertexDict[vertices[i]] = new List<int>();
            }
            vertexDict[vertices[i]].Add(i);
        }
        List<List<int>> vertList = new List<List<int>>();
        int clothIndex = 0;
        foreach(var vert in vertexDict)
        {
            vertList.Add(vert.Value);
            foreach(int vertIndex in vert.Value)
            {
                vertToCloth[vertIndex] = clothIndex;
            }
            ++clothIndex;
        }
        clothToVerts = vertList.ToArray();

    }

    private void InitializeClothTriangles(int[] triangles, int[] vertToCloth)
    {
        clothTriangles = new int[triangles.Length];

        for (int i = 0; i < triangles.Length; ++i)
        {
            clothTriangles[i] = vertToCloth[triangles[i]];
        }
    }

    private void InitializePositions(List<int>[] clothToVerts)
    {
        positions = new Vector3[clothToVerts.Length];
        oldPositions = new Vector3[clothToVerts.Length];
        for (int i = 0; i < positions.Length; i++)
        {
            positions[i] = transform.TransformPoint(vertices[ClothToVertIndex(i)]);
            oldPositions[i] = positions[i];
        }
    }

    private void InitializeNeighbours(int[] clothTriangles)
    {
        neighbours = new List<int>[positions.Length];
        for(int i = 0; i < clothTriangles.Length; ++i)
        {
            if (null == neighbours[clothTriangles[i]]) neighbours[clothTriangles[i]] = new List<int>();
            neighbours[clothTriangles[i]].Add(clothTriangles[i - (i % 3) + ((i + 1) % 3)]);
            neighbours[clothTriangles[i]].Add(clothTriangles[i - (i % 3) + ((i + 2) % 3)]);
        }
    }

    private void InitializeDistances(Vector3[] positions, List<int>[] neighbours)
    {
        distances = new List<float>[neighbours.Length];
        for (int i = 0; i < neighbours.Length; ++i)
        {
            distances[i] = new List<float>();
            foreach(int neighbour in neighbours[i])
            {
                distances[i].Add((positions[i] - positions[neighbour]).magnitude);
            }
        }
    }

    // Use this for initialization
    private void Start()
    {
        // Get cloth mesh vertex positions
        acceleration = new Vector3(0.0f, -9.81f, 0.0f);

        //mesh = (Mesh)Instantiate(GetComponent<SkinnedMeshRenderer>().sharedMesh);
        //GetComponent<SkinnedMeshRenderer>().sharedMesh = mesh;
        mesh = GetComponent<MeshFilter>().mesh;

        vertices = mesh.vertices;
        normals = new Vector3[mesh.normals.Length];
        InitializeClothVertices(vertices);
        InitializeClothTriangles(mesh.triangles, vertToCloth);
        InitializePositions(clothToVerts);
        InitializeNeighbours(clothTriangles);
        InitializeDistances(positions, neighbours);

#if CLOTH_DEBUG
        for(int i = 0; i < neighbours[debugInd].Count; ++i)
        {
            Debug.DrawLine(positions[debugInd], positions[neighbours[debugInd][i]], Color.red, 10000);
            Debug.Log(distances[debugInd][i]);
        }
#endif

        //MakeMeshDoubleFaced();
    }

    // Update is called once per frame
    private void Update()
    {
        CalculatePhysics();
        UpdateMesh();
    }

    void CalculatePhysics()
    {
        for (int i = 0; i < positions.Length; i++)
        {
            PerformVerletIntegration(i);
        }

        bool hasColided = false;
        // Satisfy constraints
        for (int s = 0; s < physicsIterationsPerFrame; s++)
        {
            for (int i = 0; i < positions.Length; ++i)
            {
                hasColided |= SatisfyEnvironmentConstraints(i);
                SatisfyClothConstraints(i);
                if(s == physicsIterationsPerFrame - 1)
                {
                    /*if (!hasColided)
                    {
                        PerformVerletIntegration(i);
                    }*/
                    CalculateNormals(i);
                }
            }
            SolveLockedConstraints();
        }
    }

    void CalculateNormals(int i)
    {
        Vector3 t1 = positions[i];
        Vector3 t2 = positions[neighbours[i][0]];
        Vector3 t3 = positions[neighbours[i][1]];
        Vector3 n = -Vector3.Cross(t2 - t1, t1 - t3).normalized;
        normals[i] = n;
    }

    void UpdateMesh()
    {
        vertices = mesh.vertices;

        // Transform new global vertex positions to local positions
        for (int i = 0; i < positions.Length; i++)
        {
            foreach(int vertIndex in clothToVerts[i])
            {
                vertices[vertIndex] = transform.InverseTransformPoint(positions[i]);
            }
            //vertices[i + positions.Length] = vertices[i];
            //normals[i + positions.Length] = -normals[i];
        }

        mesh.normals = normals;
        mesh.vertices = vertices;
        mesh.RecalculateBounds();
    }

    void PerformVerletIntegration(int posIndex)
    {
        // Transform new local vertex positions to global positions
        positions[posIndex] = transform.TransformPoint(vertices[ClothToVertIndex(posIndex)]);

        // Perform verlet integration
        Vector3 temp = positions[posIndex];

        Vector3 velocity = positions[posIndex] - oldPositions[posIndex];

        positions[posIndex] += velocity * damping + acceleration * Time.deltaTime * Time.deltaTime;

        oldPositions[posIndex] = temp;
    }

    void SatisfyClothConstraints(int index)
    {
        for(int i = 0; i < neighbours[index].Count; ++i)
        {
            SatisfyDistanceConstraint(index, neighbours[index][i], distances[index][i]);
        }
    }

    void SatisfyDistanceConstraint(int indexA, int indexB, float distance)
    {
        Vector3 diffVec = positions[indexA] - positions[indexB];
        float dist = diffVec.magnitude;
        float difference = distance - dist;

        // normalize
        diffVec /= dist;

        // Change positions of A and B to satisfy distance constraint
        if (Mathf.Abs(difference) > tolerance)// tolerance is set close to zero
        {
            Vector3 correction = diffVec * (difference * 0.5f);
            positions[indexA] += correction;
            positions[indexB] -= correction;
        }
    }

    bool HandleCLothCollision(int index)
    {
        // Sphere collision constraints
        List<VertWithNorm> spheres = collisionHandler.GetNearestPoints(positions[index]);

        Vector3 disp = Vector3.zero;
        int count = 0;
        for (int i = 0; i < spheres.Count; i++)
        {
#if CLOTH_DEBUG
            if (index == debugInd)
            {
                Debug.DrawLine(spheres[i].pos, spheres[i].pos + spheres[i].norm * collisionHandler.collisionRadius, Color.green, 1000);
            }
#endif
            Vector3 sphereDisp = HandleSphereCollision(index, spheres[i]);
            if (sphereDisp.magnitude > 0.001f)
            {
                /*
#if CLOTH_DEBUG
                if (index == debugInd)
                {
                    Debug.Log(positions[index] + " , " + spheres[i].pos);
                }
#endif
*/
                disp += sphereDisp;
                ++count;
            }
        }
        if (count > 0) disp /= count;
        positions[index] += disp;
        if (disp.magnitude > 0.001f)
        {
            return true;
        }
        return false;
    }

    bool SatisfyEnvironmentConstraints(int index)
    {
        bool hasColided = false;

        // Platform constraint
        hasColided |= HandleGroundCollision(index, groundLevel);

        hasColided |= HandleCLothCollision(index);

        return hasColided;

    }

    bool HandleGroundCollision(int index, float groundLevel)
    {
        if (positions[index].y < groundLevel + EPSILON)
        {
            positions[index].y = groundLevel;
            return true;
        }
        return false;
    }

    Vector3 FindPoinToTriangleVec(Vector3 p, Vector3 v1, Vector3 v2, Vector3 v3)
    {
        Vector3 v21 = v2 - v1;
        Vector3 v32 = v3 - v2;
        Vector3 v13 = v1 - v3;
        Vector3 p1 = p - v1;
        Vector3 p2 = p - v2;
        Vector3 p3 = p - v3;
        Vector3 n = Vector3.Cross(v21, v13).normalized;

        bool isInside = (Mathf.Sign(Vector3.Dot(Vector3.Cross(v21, n), p1)) +
                         Mathf.Sign(Vector3.Dot(Vector3.Cross(v32, n), p2)) +
                         Mathf.Sign(Vector3.Dot(Vector3.Cross(v13, n), p3)) > 2.0);
        
        if (isInside)
        {
            float dot = Vector3.Dot(p1, n);
            return -(n * dot);
        }
        else
        {
            Vector3 d1 = v21 * Mathf.Clamp(Vector3.Dot(v21, p1) / v21.magnitude, 0.0f, 1.0f) - p1;
            Vector3 d2 = v32 * Mathf.Clamp(Vector3.Dot(v32, p1) / v32.magnitude, 0.0f, 1.0f) - p2;
            Vector3 d3 = v13 * Mathf.Clamp(Vector3.Dot(v13, p1) / v13.magnitude, 0.0f, 1.0f) - p3;

            Vector3 result = d1;
            if (d2.magnitude < result.magnitude) result = d2;
            if (d3.magnitude < result.magnitude) result = d3;
            return result;
        }
    }

    Vector3 HandleSphereCollision(int index, VertWithNorm sphere)
    {
        Vector3 diff = (positions[index] - sphere.pos);
        Vector3 disp = sphere.norm * (collisionHandler.collisionRadius - Vector3.Dot(diff, sphere.norm));

        float dist = diff.magnitude;

        if (dist < collisionHandler.collisionRadius + EPSILON)
        {
            return disp;
        }
        return Vector3.zero;
    }

    void SolveLockedConstraints()
    {
        // Locked to object constraints
    }

    // source: https://answers.unity.com/questions/280741/how-make-visible-the-back-face-of-a-mesh.html
    void MakeMeshDoubleFaced()
    {
        vertices = mesh.vertices;
        var normals = mesh.normals;
        var szV = vertices.Length;
        var newVerts = new Vector3[szV * 2];
        var newNorms = new Vector3[szV * 2];
        for (var j = 0; j < szV; j++)
        {
            // duplicate vertices and uvs:
            newVerts[j] = newVerts[j + szV] = vertices[j];
            // copy the original normals...
            newNorms[j] = normals[j];
            // and revert the new ones
            newNorms[j + szV] = -normals[j];
        }
        var triangles = mesh.triangles;
        var szT = triangles.Length;
        var newTris = new int[szT * 2]; // double the triangles
        for (var i = 0; i < szT; i += 3)
        {
            // copy the original triangle
            newTris[i] = triangles[i];
            newTris[i + 1] = triangles[i + 1];
            newTris[i + 2] = triangles[i + 2];
            // save the new reversed triangle
            var j = i + szT;
            newTris[j] = triangles[i] + szV;
            newTris[j + 2] = triangles[i + 1] + szV;
            newTris[j + 1] = triangles[i + 2] + szV;
        }
        mesh.vertices = newVerts;
        mesh.normals = newNorms;
        mesh.triangles = newTris; // assign triangles last!
    }
}
