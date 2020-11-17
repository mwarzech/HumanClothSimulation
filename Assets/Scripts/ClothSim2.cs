#define CLOTH_DEBUG

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class ClothSim2 : MonoBehaviour
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
    int[] triangles;
    float[] distances;
    Vector3[] normals;
    
    public ClothCollisions collisionHandler;

    List<int>[] clothToVerts;
    int[] vertToCloth;
    Dictionary<Vector3, List<int>> vertexDict;

#if CLOTH_DEBUG
    int debugInd = 1 * 3;
#endif

    private int PosToVertIndex(int posIndex)
    {
        return clothToVerts[posIndex][0];
    }

    private int VertToPosIndex(int vertIndex)
    {
        return vertToCloth[vertIndex];
    }

    private void InitializeClothVertices(Vector3[] vertices)
    {
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

    private void InitializePositions(List<int>[] clothToVerts)
    {
        positions = new Vector3[clothToVerts.Length];
        oldPositions = new Vector3[clothToVerts.Length];
        for (int i = 0; i < positions.Length; i++)
        {
            positions[i] = transform.TransformPoint(vertices[PosToVertIndex(i)]);
            oldPositions[i] = positions[i];
        }
    }

    public void ChangeTolerance(float newValue)
    {
        tolerance = newValue;
    }
    public void ChangeDamping(float newValue)
    {
        damping = newValue;
    }

    void InitializeDistances(int[] triangles, Vector3[] positions)
    {
        distances = new float[triangles.Length];
        for (int i = 0; i < triangles.Length; i += 3)
        {
            distances[i] = (positions[VertToPosIndex(triangles[i])] - positions[VertToPosIndex(triangles[i + 1])]).magnitude;
            distances[i + 1] = (positions[VertToPosIndex(triangles[i + 1])] - positions[VertToPosIndex(triangles[i + 2])]).magnitude;
            distances[i + 2] = (positions[VertToPosIndex(triangles[i + 2])] - positions[VertToPosIndex(triangles[i])]).magnitude;
        }
    }

    // Use this for initialization
    void Start()
    {
        // Get cloth mesh vertex positions
        acceleration = new Vector3(0.0f, -9.81f, 0.0f);

        //mesh = (Mesh)Instantiate(GetComponent<SkinnedMeshRenderer>().sharedMesh);
        //GetComponent<SkinnedMeshRenderer>().sharedMesh = mesh;
        mesh = GetComponent<MeshFilter>().mesh;

        vertices = mesh.vertices;
        triangles = mesh.triangles;
        normals = new Vector3[mesh.normals.Length];
        InitializeClothVertices(vertices);
        InitializePositions(clothToVerts);
        InitializeDistances(triangles, positions);

#if CLOTH_DEBUG
        Vector3 pos1 = positions[VertToPosIndex(triangles[debugInd])];
        Vector3 pos2 = positions[VertToPosIndex(triangles[debugInd + 1])];
        Vector3 pos3 = positions[VertToPosIndex(triangles[debugInd + 2])];
        Debug.DrawLine(pos1, pos2, Color.green, 10000);
        Debug.DrawLine(pos2, pos3, Color.green, 10000);
        Debug.DrawLine(pos3, pos1, Color.green, 10000);
        Debug.Log(distances[debugInd]);
        Debug.Log(distances[debugInd + 1]);
        Debug.Log(distances[debugInd + 2]);
#endif

        //MakeMeshDoubleFaced();
    }

    // Update is called once per frame
    void Update()
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

        // Satisfy constraints
        for (int s = 0; s < physicsIterationsPerFrame; s++)
        {
            for (int i = 0; i < triangles.Length; i += 3)
            {
                SatisfyEnvironmentConstraints(i);
                SatisfyClothConstraints(i);
                if(s == physicsIterationsPerFrame - 1)
                {
                    CalculateNormals(i);
                }
            }
            SolveLockedConstraints();
        }
    }

    void CalculateNormals(int i)
    {
        Vector3 t1 = positions[VertToPosIndex(triangles[i])];
        Vector3 t2 = positions[VertToPosIndex(triangles[i + 1])];
        Vector3 t3 = positions[VertToPosIndex(triangles[i + 2])];
        Vector3 n = -Vector3.Cross(t2 - t1, t1 - t3).normalized;
        normals[triangles[i]] = n;
        normals[triangles[i + 1]] = n;
        normals[triangles[i + 2]] = n;
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
        positions[posIndex] = transform.TransformPoint(vertices[PosToVertIndex(posIndex)]);

        // Perform verlet integration
        Vector3 temp = positions[posIndex];

        Vector3 velocity = positions[posIndex] - oldPositions[posIndex];

        positions[posIndex] += velocity * damping + acceleration * Time.deltaTime * Time.deltaTime;

        oldPositions[posIndex] = temp;
    }

    void SatisfyClothConstraints(int index)
    {
        SatisfyDistanceConstraint(triangles[index], triangles[index + 1], distances[index]);
        SatisfyDistanceConstraint(triangles[index + 1], triangles[index + 2], distances[index + 1]);
        SatisfyDistanceConstraint(triangles[index + 2], triangles[index], distances[index + 2]);
    }

    void SatisfyDistanceConstraint(int indexA, int indexB, float distance)
    {
        Vector3 diffVec = positions[VertToPosIndex(indexA)] - positions[VertToPosIndex(indexB)];
        float dist = diffVec.magnitude;
        float difference = distance - dist;

        // normalize
        diffVec /= dist;

        // Change positions of A and B to satisfy distance constraint
        if (Mathf.Abs(difference) > tolerance)// tolerance is set close to zero
        {
            Vector3 correction = diffVec * (difference * 0.5f);
            positions[VertToPosIndex(indexA)] += correction;
            positions[VertToPosIndex(indexB)] -= correction;
        }
    }

    void SatisfyEnvironmentConstraints(int index)
    {
        // Platform constraint
        HandleGroundCollision(index, groundLevel);

        // Sphere collision constraints
        Vector3 pos = positions[VertToPosIndex(triangles[index])]
            + positions[VertToPosIndex(triangles[index + 1])]
            + positions[VertToPosIndex(triangles[index + 2])];
        pos /= 3f;
        List<Vector3> spheres = collisionHandler.GetNearestPoints(pos);

#if CLOTH_DEBUG
        if (index == debugInd)
        {
            foreach(Vector3 sphere in spheres)
            {
                Debug.Log(sphere);
            }
        }
#endif

        for (int i = 0; i < spheres.Count; i++)
        {
            HandleSphereCollision(index, spheres[i]);
        }
    }

    void HandleGroundCollision(int index, float groundLevel)
    {
        for(int i = 0; i < 3; ++i)
        {
            if (positions[VertToPosIndex(triangles[index + i])].y < groundLevel + EPSILON)
                positions[VertToPosIndex(triangles[index + i])].y = groundLevel + EPSILON;
        }
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

    void HandleSphereCollision(int index, Vector3 sphere)
    {
        Vector3 vecToTriangle = FindPoinToTriangleVec(sphere,
                                                    positions[VertToPosIndex(triangles[index])], 
                                                    positions[VertToPosIndex(triangles[index + 1])], 
                                                    positions[VertToPosIndex(triangles[index + 2])]);

        float dist = vecToTriangle.magnitude;

        if (dist < collisionHandler.collisionRadius + EPSILON)
        {
            positions[VertToPosIndex(triangles[index])] += vecToTriangle.normalized * (collisionHandler.collisionRadius - dist + EPSILON);
            positions[VertToPosIndex(triangles[index + 1])] += vecToTriangle.normalized * (collisionHandler.collisionRadius - dist + EPSILON);
            positions[VertToPosIndex(triangles[index + 2])] += vecToTriangle.normalized * (collisionHandler.collisionRadius - dist + EPSILON);
        }
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
