﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct VertWithNorm
{
    public Vector3 pos;
    public Vector3 norm;
}

public class ClothNormalCollisions : MonoBehaviour
{
    public SkinnedMeshRenderer[] collisionMeshes;
    public float bucketSize = 1f;
    public float collisionRadius;

    private Dictionary<Vector3Int, List<VertWithNorm>> dictionary;

    public Vector3Int GetKeyForPosition(Vector3 pos)
    {
        return new Vector3Int((int)Mathf.Floor(pos.x / bucketSize),
            (int)Mathf.Floor(pos.y / bucketSize),
            (int)Mathf.Floor(pos.z / bucketSize));
    }

    public void AddPositionToDictionary(Vector3 pos, Vector3 norm)
    {
        Vector3Int key = GetKeyForPosition(pos);
        if (!dictionary.ContainsKey(key))
        {
            dictionary.Add(key, new List<VertWithNorm>());
        }
        dictionary[key].Add(new VertWithNorm() { pos = pos, norm = norm });
    }

    public List<VertWithNorm> GetNearestPoints(Vector3 pos)
    {
        Vector3Int key = GetKeyForPosition(pos);
        int xNeighbour = ((key.x - pos.x) > (bucketSize * 0.5f)) ? 1 : -1;
        int yNeighbour = ((key.y - pos.y) > (bucketSize * 0.5f)) ? 1 : -1;
        int zNeighbour = ((key.z - pos.z) > (bucketSize * 0.5f)) ? 1 : -1;
        Vector3Int[] keys = new Vector3Int[8];
        keys[0] = key;
        keys[1] = new Vector3Int(key.x + xNeighbour, key.y, key.z);
        keys[2] = new Vector3Int(key.x, key.y + yNeighbour, key.z);
        keys[3] = new Vector3Int(key.x, key.y, key.z + zNeighbour);
        keys[4] = new Vector3Int(key.x + xNeighbour, key.y + yNeighbour, key.z);
        keys[5] = new Vector3Int(key.x, key.y + yNeighbour, key.z + zNeighbour);
        keys[6] = new Vector3Int(key.x + xNeighbour, key.y, key.z + zNeighbour);
        keys[7] = new Vector3Int(key.x + xNeighbour, key.y + yNeighbour, key.z + zNeighbour);
        List<VertWithNorm> points = new List<VertWithNorm>();
        for(int i = 0; i < keys.Length; ++i)
        {
            if (dictionary.ContainsKey(keys[i]))
            {
                points.AddRange(dictionary[keys[i]]);
            }
        }

        return points;
    }

    private void ResetDict()
    {
        dictionary = new Dictionary<Vector3Int, List<VertWithNorm>>();
    }

    private void AddCollisionMeshToDict(SkinnedMeshRenderer skinnedMesh)
    {
        Mesh mesh = new Mesh();
        skinnedMesh.BakeMesh(mesh);
        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;
        for(int i = 0; i < vertices.Length; ++i)
        {
            //Vector3 pos = collisionMesh.transform.TransformPoint(vertices[i] - normals[i].normalized * collisionRadius * 0.5f);
            Vector3 pos = skinnedMesh.transform.TransformPoint(vertices[i]);
            //Debug.DrawLine(pos, pos + normals[i].normalized * collisionRadius);
            AddPositionToDictionary(pos, normals[i].normalized);
        }
    }

    private void Start()
    {
        ResetDict();
        for(int i = 0; i < collisionMeshes.Length; ++i)
        {
            AddCollisionMeshToDict(collisionMeshes[i]);
        }
    }


    void Update()
    {
        ResetDict();
        for (int i = 0; i < collisionMeshes.Length; ++i)
        {
            AddCollisionMeshToDict(collisionMeshes[i]);
        }
    }
}
