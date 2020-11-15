using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GeneratingSpheres : MonoBehaviour
{
    public SkinnedMeshRenderer meshRenderer;
    public Cloth cloth;
    public float sphereRadius = 0.1f;
    public int skipVertex = 0;

    private GameObject[] spheres;
    private Mesh mesh;

    // Start is called before the first frame update
    void Start()
    {
        mesh = meshRenderer.sharedMesh;
        CreateSpheres();
    }

    private void CreateSpheres()
    {
        spheres = new GameObject[(int)(mesh.vertices.Length / (1+skipVertex))];
        ClothSphereColliderPair[] clothSpheres = new ClothSphereColliderPair[spheres.Length];
        for (int i = 0; i < spheres.Length; ++i)
        {
            spheres[i] = new GameObject("Sphere");
            spheres[i].transform.position = mesh.vertices[i * (1 + skipVertex)];
            SphereCollider collider = spheres[i].AddComponent<SphereCollider>();
            collider.radius = sphereRadius;
            clothSpheres[i].first = collider;
        }
        cloth.sphereColliders = clothSpheres;
    }

    private void UpdateSpheres()
    {
        for (int i = 0; i < spheres.Length; ++i)
        {
            spheres[i].transform.position = mesh.vertices[i * (1 + skipVertex)];
        }
    }

    void Update()
    {
        //UpdateSpheres();
    }
}
