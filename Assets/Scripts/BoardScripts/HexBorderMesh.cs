using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class HexBorderMesh : MonoBehaviour
{
    public float outerRadius = 1.1f;
    public float innerRadius = 0.95f;
    public bool isFlatTopped = false;

    private void Awake()
    {
        CreateFlatHexRing();
    }

    private void CreateFlatHexRing()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        Mesh mesh = new Mesh { name = "HexBorderRing" };

        Vector3[] vertices = new Vector3[12];
        int[] triangles = new int[36];
        Vector2[] uvs = new Vector2[12];

        for (int i = 0; i < 6; i++)
        {
            int outerIndex = i;
            int innerIndex = i + 6;

            vertices[outerIndex] = GetPoint(outerRadius, i);
            vertices[innerIndex] = GetPoint(innerRadius, i);

            uvs[outerIndex] = new Vector2(1, (float)i / 6f);
            uvs[innerIndex] = new Vector2(0, (float)i / 6f);
        }

        for (int i = 0; i < 6; i++)
        {
            int triIndex = i * 6;
            int nextI = (i + 1) % 6;

            triangles[triIndex] = i;
            triangles[triIndex + 1] = nextI;
            triangles[triIndex + 2] = i + 6;

            triangles[triIndex + 3] = nextI;
            triangles[triIndex + 4] = nextI + 6;
            triangles[triIndex + 5] = i + 6;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
    }

    private Vector3 GetPoint(float radius, int index)
    {
        float angle_deg = isFlatTopped ? 60 * index : 60 * index - 30;
        float angle_rad = Mathf.PI / 180f * angle_deg;
        return new Vector3(radius * Mathf.Cos(angle_rad), 0f, radius * Mathf.Sin(angle_rad));
    }
}
