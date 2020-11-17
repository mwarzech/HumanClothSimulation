using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClothCollisions : MonoBehaviour
{
    public SkinnedMeshRenderer collisionMesh;
    public float bucketSize = 1f;
    public float collisionRadius;

    private Dictionary<Vector3Int, List<Vector3>> dictionary;

    public Vector3Int GetKeyForPosition(Vector3 pos)
    {
        return new Vector3Int((int)Mathf.Floor(pos.x / bucketSize),
            (int)Mathf.Floor(pos.y / bucketSize),
            (int)Mathf.Floor(pos.z / bucketSize));
    }

    public void AddPositionToDictionary(Vector3 pos)
    {
        Vector3Int key = GetKeyForPosition(pos);
        if (!dictionary.ContainsKey(key))
        {
            dictionary.Add(key, new List<Vector3>());
        }
        dictionary[key].Add(pos);
    }

    public List<Vector3> GetNearestPoints(Vector3 pos)
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
        List<Vector3> points = new List<Vector3>();
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
        dictionary = new Dictionary<Vector3Int, List<Vector3>>();
    }

    private void AddCollisionMeshToDict()
    {
        Vector3[] vertices = collisionMesh.sharedMesh.vertices;
        Vector3[] normals = collisionMesh.sharedMesh.normals;
        for(int i = 0; i < vertices.Length; ++i)
        {
            AddPositionToDictionary(vertices[i] - normals[i] * collisionRadius);
        }
    }


    void Update()
    {
        ResetDict();
        AddCollisionMeshToDict();
    }
}
