using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using DentedPixel;
using System.Linq;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using UnityEditor;
using WingedEdge;
using Unity.VisualScripting;
using HalfEdge;

delegate Vector3 ComputePosDelegate(float kX, float kZ);
delegate float3 ComputePosDelegate_SIMD(float3 k);
public enum TypeOfMesh
{
    Quad,
    Cube,
    Chips,
    RegularPolygon,
    Pacman
}

[RequireComponent(typeof(MeshFilter))]

public class MeshGenerator : MonoBehaviour
{

    MeshFilter m_Mf;
    [SerializeField] bool m_DisplayMeshInfo = true;
    [SerializeField] bool m_DisplayMeshEdges = true;
    [SerializeField] bool m_DisplayMeshVertices = true;
    [SerializeField] bool m_DisplayMeshFaces = true;
    [SerializeField] int m_NumberOfSubdivide = 0;
    [SerializeField] TypeOfMesh m_TypeOfMesh;

    HalfEdgeMesh halfEdgeMesh;

    
    void Start()
    {
        m_Mf = GetComponent<MeshFilter>();
        Mesh mesh = null;
        switch (m_TypeOfMesh)
        {
            case (TypeOfMesh.Quad):
                mesh = CreateNormalizedGridXZ_QUADS(int3(2, 1, 3));
                break;
            case (TypeOfMesh.Cube):
                mesh = CreateBox(new Vector3(2, 3, 1));
                break;
            case (TypeOfMesh.Chips):
                mesh = CreateChips(new Vector3(2, 3, 1));
                break;
            case (TypeOfMesh.RegularPolygon):
                mesh = CreateRegularPolygon(new Vector3(2, 1, 3), 5);
                break;
            case (TypeOfMesh.Pacman):
                mesh = CreatePacman(new Vector3(2,1,3),5);
                break;
        }
        
        m_Mf.mesh = mesh;
        halfEdgeMesh = new HalfEdgeMesh(mesh);
        for(int i = 0; i < m_NumberOfSubdivide; i++)
        {
            halfEdgeMesh.SubdivideCatmullClark();
        }
        GUIUtility.systemCopyBuffer = halfEdgeMesh.ConvertToCSVFormat();
        m_Mf.mesh = halfEdgeMesh.ConvertToFaceVertexMesh();

        gameObject.AddComponent<MeshCollider>();
    }


    Mesh CreateNormalizedGridXZ_QUADS(int3 nSegments, ComputePosDelegate_SIMD computePos=null )
    {
        Mesh mesh = new Mesh();
        mesh.indexFormat = IndexFormat.UInt32;

        mesh.name = "normalizedGridXZ_QUADS";

        Vector3[] vertices = new Vector3[(nSegments.x + 1) * (nSegments.z + 1)];
        int[] quads = new int[4*nSegments.x*nSegments.z];
        Vector2[] uv = new Vector2[vertices.Length];

        int index = 0;
        for (int i = 0; i < nSegments.z+1; i++)
        {
            for (int j = 0; j < nSegments.x+1; j++)
            {
                float3 k = float3(j,0,i)/ nSegments;
                vertices[index] = computePos != null?computePos(k): k;
                uv[index++] = new Vector2(k.x, k.z);
            }
        }

        index = 0;
        int indexOffset = 0;
        for (int i =0; i < nSegments.z; i++)
        {
            for (int j = 0; j < nSegments.x; j++)
            {
                quads[index++] = j + indexOffset;
                quads[index++] = j + indexOffset + (nSegments.x + 1);
                quads[index++] = (j+1) + indexOffset + (nSegments.x + 1);
                quads[index++] = (j+1) + indexOffset;
            }
            indexOffset += (nSegments.x + 1);
        }

        mesh.vertices = vertices;
        mesh.SetIndices(quads,MeshTopology.Quads,0);
        mesh.uv = uv;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    Mesh CreateBox(Vector3 halfSize)
    {
        Mesh mesh = new Mesh();
        mesh.name = "Box";

        Vector3[] vertices = new Vector3[8];

        vertices[0] = new Vector3(-halfSize.x, -halfSize.y, -halfSize.z);
        vertices[1] = new Vector3(-halfSize.x, halfSize.y, -halfSize.z);
        vertices[2] = new Vector3(halfSize.x, halfSize.y, -halfSize.z);
        vertices[3] = new Vector3(halfSize.x, -halfSize.y, -halfSize.z);
        vertices[4] = new Vector3(halfSize.x, halfSize.y, halfSize.z);
        vertices[5] = new Vector3(halfSize.x, -halfSize.y, halfSize.z);
        vertices[6] = new Vector3(-halfSize.x, halfSize.y, halfSize.z);
        vertices[7] = new Vector3(-halfSize.x, -halfSize.y, halfSize.z);

        mesh.vertices = vertices;

        int[] quads = new int[24];

        quads[0] = 0;
        quads[1] = 1;
        quads[2] = 2;
        quads[3] = 3;

        quads[4] = 3;
        quads[5] = 2;
        quads[6] = 4;
        quads[7] = 5;

        quads[8] = 5;
        quads[9] = 4;
        quads[10] = 6;
        quads[11] = 7;

        quads[12] = 7;
        quads[13] = 6;
        quads[14] = 1;
        quads[15] = 0;

        quads[16] = 0;
        quads[17] = 3;
        quads[18] = 5;
        quads[19] = 7;

        quads[20] = 1;
        quads[21] = 6;
        quads[22] = 4;
        quads[23] = 2;

        mesh.SetIndices(quads, MeshTopology.Quads, 0);

        Vector2[] uvs = new Vector2[vertices.Length];
        uvs[0] = new Vector2(0, 0);
        uvs[1] = new Vector2(1, 0);
        uvs[2] = new Vector2(1, 1);
        uvs[3] = new Vector2(0, 1);
        uvs[4] = new Vector2(0, 0);
        uvs[5] = new Vector2(1, 0);
        uvs[6] = new Vector2(1, 1);
        uvs[7] = new Vector2(0, 1);

        mesh.SetUVs(0, uvs);

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    Mesh CreateChips(Vector3 halfSize)
    {
        Mesh mesh = new Mesh();
        mesh.name = "Chips";

        Vector3[] vertices = new Vector3[8];

        vertices[0] = new Vector3(-halfSize.x, -halfSize.y, -halfSize.z);
        vertices[1] = new Vector3(-halfSize.x, halfSize.y, -halfSize.z);
        vertices[2] = new Vector3(halfSize.x, halfSize.y, -halfSize.z);
        vertices[3] = new Vector3(halfSize.x, -halfSize.y, -halfSize.z);
        vertices[4] = new Vector3(halfSize.x, halfSize.y, halfSize.z);
        vertices[5] = new Vector3(halfSize.x, -halfSize.y, halfSize.z);
        vertices[6] = new Vector3(-halfSize.x, halfSize.y, halfSize.z);
        vertices[7] = new Vector3(-halfSize.x, -halfSize.y, halfSize.z);

        mesh.vertices = vertices;

        int[] quads = new int[12];

        quads[0] = 0;
        quads[1] = 1;
        quads[2] = 2;
        quads[3] = 3;

        quads[4] = 5;
        quads[5] = 4;
        quads[6] = 6;
        quads[7] = 7;

        quads[8] = 1;
        quads[9] = 6;
        quads[10] = 4;
        quads[11] = 2;

        mesh.SetIndices(quads, MeshTopology.Quads, 0);

        Vector2[] uvs = new Vector2[vertices.Length];
        uvs[0] = new Vector2(0, 0);
        uvs[1] = new Vector2(1, 0);
        uvs[2] = new Vector2(1, 1);
        uvs[3] = new Vector2(0, 1);
        uvs[4] = new Vector2(0, 0);
        uvs[5] = new Vector2(1, 0);
        uvs[6] = new Vector2(1, 1);
        uvs[7] = new Vector2(0, 1);

        mesh.SetUVs(0, uvs);

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    Mesh CreateRegularPolygon(Vector3 halfSize, int nSectors)
    {
        Mesh mesh = new Mesh();
        mesh.name = "Polygon";

        Vector3[] vertices = new Vector3[nSectors * 2 + 1];

        float angleCount = 2 * PI / nSectors;

        for (int i = 0; i < nSectors; i++)
        {
            float angle = angleCount * i;
            Vector3 vertex = new Vector3(halfSize.x * cos(angle), 0, halfSize.z * sin(angle));
            vertices[i * 2] = new Vector3(vertex.x + halfSize.x, 0, vertex.z + halfSize.z);
        }

        for (int i = 1; i < vertices.Length; i = i + 2)
        {
            if (i == vertices.Length - 2)
            {
                vertices[i] = new Vector3((vertices[i - 1].x + vertices[0].x) / 2, 0, (vertices[i - 1].z + vertices[0].z) / 2);
            }
            else
            {
                vertices[i] = new Vector3((vertices[i - 1].x + vertices[i + 1].x) / 2, 0, (vertices[i - 1].z + vertices[i + 1].z) / 2);
            }

        }

        vertices[nSectors * 2] = new Vector3(halfSize.x, 0, halfSize.z);

        mesh.vertices = vertices;

        int[] quads = new int[nSectors * 4];

        int index = 0;
        for (int i = 0; i < nSectors; i++)
        {
            if (i == 0)
            {
                quads[i * 4] = nSectors * 2;
                quads[i * 4 + 1] = i * 2 + 1;
                quads[i * 4 + 2] = i * 2;
                quads[i * 4 + 3] = nSectors * 2 - 1;
            }
            else
            {
                quads[i * 4] = nSectors * 2;
                quads[i * 4 + 1] = i * 2 + 1;
                quads[i * 4 + 2] = i * 2;
                quads[i * 4 + 3] = i + index++;
            }
        }

        mesh.SetIndices(quads, MeshTopology.Quads, 0);

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    Mesh CreatePacman(Vector3 halfSize, int nSectors, float startAngle = Mathf.PI / 3, float endAngle = 5 * Mathf.PI / 3)
    {
        Mesh mesh = new Mesh();
        mesh.name = "Polygon";

        Vector3[] vertices = new Vector3[nSectors * 2 + 2];

        float angleCount = 2 * PI / nSectors;

        float halfEdge = (endAngle - startAngle) / nSectors;

        for (int i = 0; i < nSectors + 1; i++)
        {
            float angle = halfEdge * i + startAngle;
            Vector3 vertex = new Vector3(halfSize.x * cos(angle), 0, halfSize.z * sin(angle));
            vertices[i * 2] = new Vector3(vertex.x + halfSize.x, 0, vertex.z + halfSize.z);
        }

        for (int i = 1; i < vertices.Length - 1; i = i + 2)
        {
            {
                vertices[i] = new Vector3((vertices[i - 1].x + vertices[i + 1].x) / 2, 0, (vertices[i - 1].z + vertices[i + 1].z) / 2);
            }

        }

        vertices[nSectors * 2 + 1] = new Vector3(halfSize.x, 0, halfSize.z);

        mesh.vertices = vertices;

        int[] quads = new int[nSectors * 4];

        for (int i = 0; i < nSectors; i++)
        {
            quads[i * 4] = nSectors * 2 + 1;
            quads[i * 4 + 1] = i * 2 + 2;
            quads[i * 4 + 2] = i * 2 + 1;
            quads[i * 4 + 3] = i * 2;
        }

        mesh.SetIndices(quads, MeshTopology.Quads, 0);

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    private void OnDrawGizmos()
    {
        if(halfEdgeMesh != null && m_DisplayMeshInfo)
            halfEdgeMesh.DrawGizmos(m_Mf.transform,m_DisplayMeshVertices,m_DisplayMeshEdges,m_DisplayMeshFaces);
    }

}
