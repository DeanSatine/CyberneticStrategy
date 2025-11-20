using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class SquareBorderMesh : MonoBehaviour
{
    public float outerRadius = 1.1f;
    public float innerRadius = 0.95f;
    public bool autoRotate = true;

    private void Awake()
    {
        CreateFlatSquareRing();

        if (autoRotate)
        {
            transform.rotation = Quaternion.Euler(0f, 45f, 0f);
        }
    }

    private void CreateFlatSquareRing()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        Mesh mesh = new Mesh { name = "SquareBorderRing" };

        Vector3[] vertices = new Vector3[8];
        int[] triangles = new int[24];
        Vector2[] uvs = new Vector2[8];

        for (int i = 0; i < 4; i++)
        {
            int outerIndex = i;
            int innerIndex = i + 4;

            vertices[outerIndex] = GetPoint(outerRadius, i);
            vertices[innerIndex] = GetPoint(innerRadius, i);

            uvs[outerIndex] = new Vector2(1, (float)i / 4f);
            uvs[innerIndex] = new Vector2(0, (float)i / 4f);
        }

        for (int i = 0; i < 4; i++)
        {
            int triIndex = i * 6;
            int nextI = (i + 1) % 4;

            triangles[triIndex] = i;
            triangles[triIndex + 1] = nextI;
            triangles[triIndex + 2] = i + 4;

            triangles[triIndex + 3] = nextI;
            triangles[triIndex + 4] = nextI + 4;
            triangles[triIndex + 5] = i + 4;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
    }

    private Vector3 GetPoint(float radius, int index)
    {
        float angle_deg = 90 * index;
        float angle_rad = Mathf.PI / 180f * angle_deg;
        return new Vector3(radius * Mathf.Cos(angle_rad), 0f, radius * Mathf.Sin(angle_rad));
    }
}
