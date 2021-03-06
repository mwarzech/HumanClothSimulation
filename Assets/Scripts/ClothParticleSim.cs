﻿//#define CLOTH_DEBUG

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class ClothParticleSim : MonoBehaviour
{
    public enum CollisionType
    {
        PointToPoint,
        PointToSegment,
        SegmentToSegment
    }

    private float EPSILON = 0.001f;

    public float deltaTime = 0.01f;
    public CollisionType collisionType;
    [Range(0f,1f)]
    public float damping = 0.99f;
    [Range(0f, 1f)]
    public float tolerance = 0f;// the lower, the stiffer the cloth
    public float groundLevel;
    public int physicsIterationsPerFrame = 50;

    public Transform hookTransform;

    Mesh mesh;
    public Vector3 acceleration = new Vector3(0.0f, -9.81f, 0.0f);
    Vector3[] vertices;
    Vector3[] positions;
    Color[] vertColors;
    Vector3[] oldPositions;
    int[] clothTriangles;
    Vector3[] normals;
    Vector3[] meshNormals;
    
    public ClothNormalCollisions collisionHandler;

    List<float>[] distances;
    List<int>[] neighbours;
    List<int>[] clothToVerts;
    int[] vertToCloth;

    Vector3[] start_positions;

    Dictionary<int, Vector3> lockedPositions;

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
        start_positions = new Vector3[clothToVerts.Length];
        oldPositions = new Vector3[clothToVerts.Length];
        lockedPositions = new Dictionary<int, Vector3>();
        for (int i = 0; i < positions.Length; i++)
        {
            positions[i] = transform.TransformPoint(vertices[ClothToVertIndex(i)]);
            if (IsLockedVertex(ClothToVertIndex(i)))
            {
                lockedPositions[i] = hookTransform.InverseTransformPoint(positions[i]);
            }
            start_positions[i] = positions[i];
            oldPositions[i] = positions[i];
        }
    }

    private void InitializeNormals(List<int>[] clothToVerts)
    {
        normals = new Vector3[clothToVerts.Length];
        for (int i = 0; i < normals.Length; i++)
        {
            normals[i] = mesh.normals[ClothToVertIndex(i)];
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

    private bool IsLockedVertex(int vertIndex)
    {
        return (vertColors.Length > vertIndex && vertColors[vertIndex].r > 0.5f && null != hookTransform);
    }

    // Initialization
    private void Start()
    {
        mesh = GetComponent<MeshFilter>().mesh;

        vertices = mesh.vertices;
        meshNormals =  mesh.normals;
        vertColors = mesh.colors;
        InitializeClothVertices(vertices);
        InitializeClothTriangles(mesh.triangles, vertToCloth);
        InitializePositions(clothToVerts);
        InitializeNormals(clothToVerts);
        InitializeNeighbours(clothTriangles);
        InitializeDistances(positions, neighbours);

#if CLOTH_DEBUG
        for(int i = 0; i < neighbours[debugInd].Count; ++i)
        {
            Debug.DrawLine(positions[debugInd], positions[neighbours[debugInd][i]], Color.red, 10000);
            Debug.Log(distances[debugInd][i]);
        }
#endif
    }

    // Update is called once per frame
    private void Update()
    {

        CalculatePhysics();
        UpdateMesh();
    }

    public void ResetMesh()
    {
        for (int i = 0; i < positions.Length; i++)
        {
            oldPositions[i] = start_positions[i];
            positions[i] = start_positions[i];
        }
        UpdateMesh();
    }
    
    void CalculatePhysics()
    {
        // Perform Verlet integration
        for (int i = 0; i < positions.Length; i++)
        {
            if (IsLockedVertex(ClothToVertIndex(i)))
            {
                positions[i] = transform.TransformPoint(vertices[ClothToVertIndex(i)]);
                continue;
            }
            PerformVerletIntegration(i);
        }
        
        // Satisfy constraints
        for (int s = 0; s < physicsIterationsPerFrame; s++)
        {
            for (int i = 0; i < positions.Length; ++i)
            {
                SatisfyClothConstraints(i);
                if (IsLockedVertex(ClothToVertIndex(i))) continue;
                SatisfyEnvironmentConstraints(i);
                if (s == physicsIterationsPerFrame - 1)
                {
                    CalculateNormals(i);
                }
            }
        }
    }

    void CalculateNormals(int i)
    {
        Vector3 t1 = positions[i];
        Vector3 t2 = positions[neighbours[i][0]];
        Vector3 t3 = positions[neighbours[i][1]];
        Vector3 n = Vector3.Cross(t2 - t1, t1 - t3).normalized;
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
                if (IsLockedVertex(ClothToVertIndex(i)))
                {
                    Vector3 pos = hookTransform.TransformPoint(lockedPositions[i]);
                    vertices[vertIndex] = transform.InverseTransformPoint(pos);
                }
                else
                {
                    vertices[vertIndex] = transform.InverseTransformPoint(positions[i]);
                }
                meshNormals[vertIndex] = -transform.InverseTransformDirection(normals[i]);
            }
        }

        mesh.normals = meshNormals;
        mesh.vertices = vertices;
        mesh.RecalculateBounds();
    }

    void PerformVerletIntegration(int posIndex, bool hasColided = false)
    {
        // Transform new local vertex positions to global positions
        positions[posIndex] = transform.TransformPoint(vertices[ClothToVertIndex(posIndex)]);

        // Perform verlet integration
        Vector3 temp = positions[posIndex];

        Vector3 velocity = positions[posIndex] - oldPositions[posIndex];

        Vector3 acc = hasColided ? Vector3.zero : acceleration;

        positions[posIndex] += velocity * damping + acc * deltaTime * deltaTime;

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
            if (IsLockedVertex(ClothToVertIndex(indexA)) && IsLockedVertex(ClothToVertIndex(indexB))) return;
            if (IsLockedVertex(ClothToVertIndex(indexA)))
            {
                positions[indexB] -= 2 * correction;
                return;
            }
            if (IsLockedVertex(ClothToVertIndex(indexB)))
            {
                positions[indexA] += 2 * correction;
                return;
            }
            positions[indexA] += correction;
            positions[indexB] -= correction;
        }
    }

    bool HandleBodyCollision(int index)
    {
        // Sphere collision constraints
        List<VertWithNorm> spheres = collisionHandler.GetNearestPoints(positions[index]);

        Vector3 disp = Vector3.zero;
        int count = 0;
        for (int i = 0; i < spheres.Count; i++)
        {
            Vector3 sphereDisp = Vector3.zero;

            switch (collisionType)
            {
                case CollisionType.PointToPoint:
                    sphereDisp = HandlePointToPointCollision(index, spheres[i]);
                    break;
                case CollisionType.PointToSegment:
                    sphereDisp = HandleLineToPointCollision(index, spheres[i]);
                    break;
                case CollisionType.SegmentToSegment:
                    sphereDisp = HandleSphereCollision(index, spheres[i]);
                    break;
            }
            if (sphereDisp.magnitude > EPSILON)
            {
                disp += sphereDisp;
                ++count;
#if CLOTH_DEBUG
                if (index == debugInd)
                {
                    Debug.DrawLine(spheres[i].pos, spheres[i].pos + spheres[i].norm * collisionHandler.collisionRadius, Color.green, 1000);
                    Debug.DrawLine(positions[index], positions[index] + sphereDisp, Color.cyan, 1000);
                }
#endif
            }
        }
        if (count > 0) disp /= count;
        positions[index] += disp;
        if (disp.magnitude > EPSILON)
        {
            return true;
        }
        return false;
    }

    bool SatisfyEnvironmentConstraints(int index)
    {
        bool hasColided = false;
        
        hasColided |= HandleGroundCollision(index, groundLevel);
        hasColided |= HandleBodyCollision(index);

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

    Vector3 HandlePointToPointCollision(int index, VertWithNorm sphere)
    {
        if (Vector3.Dot(positions[index] - sphere.pos, sphere.norm.normalized) >= collisionHandler.collisionRadius)
        {
            return Vector3.zero;
        }
        Vector3 diff = (positions[index] - sphere.pos);
        Vector3 disp = sphere.norm * (collisionHandler.collisionRadius - Vector3.Dot(diff, sphere.norm));
        float dist = diff.magnitude;

        if (dist < collisionHandler.collisionRadius + EPSILON)
        {
            return disp;
        }
        return Vector3.zero;
    }

    Vector3 HandleLineToPointCollision(int index, VertWithNorm sphere)
    {
        if (Vector3.Dot(positions[index] - sphere.pos, sphere.norm.normalized) >= collisionHandler.collisionRadius)
        {
            return Vector3.zero;
        }
        if (oldPositions[index] == positions[index])
        {
            return HandlePointToPointCollision(index, sphere);
        }
        Vector3 movVec = positions[index] - oldPositions[index];
        Vector3 toSphere = oldPositions[index] - sphere.pos;
        float posOnLine = Vector3.Dot(toSphere, movVec) / movVec.magnitude;
        posOnLine = Mathf.Clamp(posOnLine, 0, movVec.magnitude);
        Vector3 closestPoint = oldPositions[index] + movVec.normalized * posOnLine;

        Vector3 diff = closestPoint - sphere.pos;

        float dist = diff.magnitude;


        if (dist < collisionHandler.collisionRadius + EPSILON)
        {
            Vector3 disp = sphere.norm * (collisionHandler.collisionRadius - Vector3.Dot(diff, sphere.norm));
            Vector3 posAfterDisp = closestPoint + disp;
            Vector3 resultDisp = posAfterDisp - positions[index];

            return resultDisp;
        }
        return Vector3.zero;
    }

    Vector3 HandleSphereCollision(int index, VertWithNorm sphere)
    {
        if (Vector3.Dot(positions[index] - sphere.pos, sphere.norm.normalized) >= collisionHandler.collisionRadius)
        {
            return Vector3.zero;
        }

        Line clothLine = new Line() { startPos = oldPositions[index], endPos = positions[index] };
        Line sphereLine = new Line() { startPos = sphere.prevPos, endPos = sphere.pos };

        MathHelper.CollisionResult closestLine = MathHelper.DistBetweenSegments(sphereLine, clothLine);

        if (null == closestLine.coords)
        {
            return Vector3.zero;
        }
        Vector3 diff = closestLine.coords[1] - closestLine.coords[0];
        Vector3 closestPoint = sphere.pos + diff;

        float dist = diff.magnitude;

        if (dist < collisionHandler.collisionRadius + EPSILON)
        {
            Vector3 disp = sphere.norm * (collisionHandler.collisionRadius - Vector3.Dot(diff, sphere.norm));
            Vector3 posAfterDisp = closestPoint + disp;
            Vector3 resultDisp = posAfterDisp - positions[index];

            return resultDisp;
        }
        return Vector3.zero;
    }
}
