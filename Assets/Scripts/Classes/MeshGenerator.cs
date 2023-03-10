using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using DentedPixel;
using System.Linq;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using UnityEditor;

delegate Vector3 ComputePosDelegate(float kX, float kZ);
delegate float3 ComputePosDelegate_SIMD(float3 k);

[RequireComponent(typeof(MeshFilter))]
public class MeshGenerator : MonoBehaviour
{

    MeshFilter m_Mf;
    [SerializeField] Texture2D m_Heightmap;
    [SerializeField] Transform[] m_SplineCtrlPts;
    LTSpline m_Spline;

    [SerializeField] float m_TranslationSpeed;
    float m_Distance=0;

    [SerializeField] Material m_RiverMaterial;
    [SerializeField] AnimationCurve m_RiverWidth;
    
    [SerializeField] AnimationCurve m_CocktailGlassProfile;
    [SerializeField] bool m_DisplayMeshInfo=true;
    [SerializeField] bool m_DisplayMeshEdges = true;
    [SerializeField] bool m_DisplayMeshVertices = true;
    [SerializeField] bool m_DisplayMeshFaces = true;
    
    void Update()
    {
        //Spline position
        /*
        transform.position = m_Spline.interp(Mathf.PingPong(m_Distance/m_Spline.distance,1));
        m_Distance+=m_TranslationSpeed*Time.deltaTime;
        */

        //River translate

        m_RiverMaterial.mainTextureOffset += Vector2.up*Time.deltaTime*.25f;
    }
    

    void Start()
    {
        m_Mf = GetComponent<MeshFilter>();

        //m_Mf.mesh = CreateTriangle();
        //m_Mf.mesh = CreateQuadXZ(new Vector3(4,0,1));
        //m_Mf.mesh = CreateStripXZ(new Vector3(4,0,1),100);
        //m_Mf.mesh = CreateGridXZ(new Vector3(4,0,3), 10, 7);


        //Grid
        /*
        m_Mf.mesh = CreateNormalizedGridXZ(10,7,(kX,kZ)=> {
            return new Vector3(Mathf.Lerp(-5,5,kX),0,Mathf.Lerp(-3,3,kZ)); 
            });
        */

        //Grid
        
        m_Mf.mesh = CreateNormalizedGridXZ_QUADS(int3(10,1,7),(k)=> {
            return lerp(float3(-5,0,-3),float3(5,0,3),k);
            });
        
        GUIUtility.systemCopyBuffer = ConvertToCSV("\t");

        //Cylindre
        /*
        m_Mf.mesh = CreateNormalizedGridXZ(10,7,(kX,kZ)=> {
            float rho = 2;
            float theta = 2 * Mathf.PI * kX;
            float y = 4 * kZ;
            return new Vector3(rho*Mathf.Cos(theta),y,rho*Mathf.Sin(theta)); 
            });
        */


        //Sphere
        /*
        m_Mf.mesh = CreateNormalizedGridXZ(40,20,(kX,kZ)=> {
            float rho = 2;
            float theta = 2 * Mathf.PI * kX;
            float phi = Mathf.PI * (1-kZ);
            return rho*new Vector3(Mathf.Sin(phi)*Mathf.Cos(theta),Mathf.Cos(phi),Mathf.Sin(phi)*Mathf.Sin(theta)); 
            });
        */


        //Tore
        /*
        m_Mf.mesh = CreateNormalizedGridXZ(40,20,(kX,kZ)=> {
            float R = 3;
            float r = 1;
            float theta = kX * 2 * Mathf.PI;
            float alpha = kZ * 2 * Mathf.PI;
            Vector3 OOmega = new Vector3(R* Mathf.Cos(theta), 0, R*Mathf.Sin(theta));
            return OOmega + r * Mathf.Cos(alpha) * OOmega.normalized + r*Mathf.Sin(alpha) * Vector3.up;
            });
        */
        

        //Spiral tube
        /*
        int nTurns = 6;
        m_Mf.mesh = CreateNormalizedGridXZ(40*nTurns,20,(kX,kZ)=> {
            float R = 3;
            float r = 1;
            float theta = kX * 2 * Mathf.PI * nTurns;
            float alpha = kZ * 2 * Mathf.PI;
            Vector3 OOmega = new Vector3(R* Mathf.Cos(theta), 0, R*Mathf.Sin(theta));
            return OOmega + r * Mathf.Cos(alpha) * OOmega.normalized + r*Mathf.Sin(alpha) * Vector3.up + 2 * r * kX * nTurns * Vector3.up;
            });
        */


        //Spiral tube inverse
        /*
        int nTurns = 6;
        m_Mf.mesh = CreateNormalizedGridXZ(40*nTurns,20,(kX,kZ)=> {
            float R = 3;
            float r = 1;
            float theta = kX * 2 * Mathf.PI * nTurns;
            float alpha = (1-kZ) * 2 * Mathf.PI;
            Vector3 OOmega = new Vector3(R* Mathf.Cos(theta), 0, R*Mathf.Sin(theta));
            return OOmega + r * Mathf.Cos(alpha) * OOmega.normalized + r*Mathf.Sin(alpha) * Vector3.up + 2 * r * kX * nTurns * Vector3.up;
            });
        */
        

        //Heightmap
        /*
        m_Mf.mesh = CreateNormalizedGridXZ(1000,1000,(kX,kZ)=> {
            float y = Mathf.Min(4,10*m_Heightmap.GetPixel((int)(kX*m_Heightmap.width),(int)(kZ*m_Heightmap.height)).grayscale);
            return new Vector3(Mathf.Lerp(-20,20,kX),y,Mathf.Lerp(-20,20,kZ)); 
            });
        */


        //Perlin Noise
        /*
        float rnd1 = Random.value*10;
        float rnd2 = Random.value*10;
        float rnd3 = Random.value*10;
        float rnd4 = Random.value*10;
        m_Mf.mesh = CreateNormalizedGridXZ(1000,1000,(kX,kZ)=> {
            float y = 10*Mathf.PerlinNoise(rnd1 + 4*kX , rnd2 + 4*kZ) + 2*Mathf.PerlinNoise(rnd3 + 20*kX , rnd4 + 20*kZ) + .25f*Mathf.PerlinNoise(100*kX , 100*kZ);
            return new Vector3(Mathf.Lerp(-20,20,kX),y,Mathf.Lerp(-20,20,kZ)); 
            });
        */


        //River
        /*
        m_Spline = new LTSpline(m_SplineCtrlPts.Select(item=>item.position).ToArray());
        int nSegmentsZ = 200;
        m_Mf.mesh = CreateNormalizedGridXZ(1,nSegmentsZ,(kX,kZ)=> {
            Vector3 pt1 = m_Spline.interp(kZ);
            Vector3 pt2 = m_Spline.interp(kZ+ (1f/nSegmentsZ));
            Vector3 tangent = (pt2 - pt1).normalized;
            Vector3 n = Vector3.Cross(tangent, Vector3.forward);

            return pt1+n*(kX-.5f)*2*m_RiverWidth.Evaluate(kZ);
            });
        */


        //Cocktail glass
        /*
        m_Mf.mesh = CreateNormalizedGridXZ(20,100,(kX,kZ)=> {
            float rho = m_CocktailGlassProfile.Evaluate(kZ);
            float theta = 2 * Mathf.PI * kX;
            float y = 4 * kZ;
            return new Vector3(rho*Mathf.Cos(theta),y,rho*Mathf.Sin(theta)); 
            });
        */


        gameObject.AddComponent<MeshCollider>();
    }

    Mesh CreateTriangle()
    {
        Mesh mesh = new Mesh();
        mesh.name = "triangle";

        Vector3[] vertices = new Vector3[3];
        int[] triangles = new int[1*3];

        vertices[0] = Vector3.right;
        vertices[1] = Vector3.up;
        vertices[2] = Vector3.forward;

        triangles[0] = 0;
        triangles[1] = 1;
        triangles[2] = 2;

        mesh.vertices = vertices;
        mesh.triangles = triangles;

        return mesh;
    }

    Mesh CreateQuadXZ(Vector3 halfSize)
    {
        Mesh mesh = new Mesh();
        mesh.name = "quadXZ";

        Vector3[] vertices = new Vector3[4];
        int[] triangles = new int[2*3];

        vertices[0] = new Vector3(-halfSize.x,0,-halfSize.z);
        vertices[1] = new Vector3(-halfSize.x,0,halfSize.z);
        vertices[2] = new Vector3(halfSize.x,0,halfSize.z);
        vertices[3] = new Vector3(halfSize.x,0,-halfSize.z);

        triangles[0] = 0;
        triangles[1] = 1;
        triangles[2] = 2;
        
        triangles[3] = 0;
        triangles[4] = 2;
        triangles[5] = 3;

        mesh.vertices = vertices;
        mesh.triangles = triangles;

        return mesh;
    }

    Mesh CreateStripXZ(Vector3 halfSize, int nSegments)
    {
        Mesh mesh = new Mesh();
        mesh.name = "stripXZ";

        Vector3[] vertices = new Vector3[2 + 2*nSegments];
        int[] triangles = new int[2*3*nSegments];

        int index = 0;
        for (int i = 0; i < nSegments+1; i++)
        {
            float kX = (float)i/nSegments;
            float y = .25f* Mathf.Sin(kX*2*Mathf.PI*4);
            vertices[index++] = new Vector3(Mathf.Lerp(-halfSize.x,halfSize.x,kX),y,halfSize.z);
            vertices[index++] = new Vector3(Mathf.Lerp(-halfSize.x,halfSize.x,kX),y,-halfSize.z);
        }

        index = 0;
        for (int i =0; i < nSegments; i++)
        {
            triangles[index++] = i*2;
            triangles[index++] = i*2+2;
            triangles[index++] = i*2+1;

            triangles[index++] = i*2+1;
            triangles[index++] = i*2+2;
            triangles[index++] = i*2+3;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
    
    Mesh CreateGridXZ(Vector3 halfSize, int nSegmentsX, int nSegmentsZ)
    {
        Mesh mesh = new Mesh();
        mesh.name = "gridXZ";

        Vector3[] vertices = new Vector3[(nSegmentsX + 1) * (nSegmentsZ + 1)];
        int[] triangles = new int[2*3*nSegmentsX*nSegmentsZ];
        Vector2[] uv = new Vector2[vertices.Length];

        int index = 0;
        for (int i = 0; i < nSegmentsZ+1; i++)
        {
            float kZ = (float)i/nSegmentsZ;
            for (int j = 0; j < nSegmentsX+1; j++)
            {
                float kX = (float)j/nSegmentsX;
                float y = .25f* Mathf.Sin(kX*kZ*2*Mathf.PI*4);
                vertices[index] = new Vector3(Mathf.Lerp(-halfSize.x,halfSize.x,kX),y,Mathf.Lerp(-halfSize.z,halfSize.z,kZ));
                uv[index++] = new Vector2(kX, kZ);
            }
        }

        index = 0;
        int indexOffset = 0;
        for (int i =0; i < nSegmentsZ; i++)
        {
            for (int j = 0; j < nSegmentsX; j++)
            {
                triangles[index++] = j + indexOffset;
                triangles[index++] = j + indexOffset + (nSegmentsX + 1);
                triangles[index++] = (j+1) + indexOffset + (nSegmentsX + 1);

                triangles[index++] = j + indexOffset;
                triangles[index++] = (j+1) + indexOffset + (nSegmentsX + 1);
                triangles[index++] = (j+1) + indexOffset;
            }
            indexOffset += (nSegmentsX + 1);
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
    
    Mesh CreateNormalizedGridXZ(int nSegmentsX, int nSegmentsZ, ComputePosDelegate computePos=null )
    {
        Mesh mesh = new Mesh();
        mesh.indexFormat = IndexFormat.UInt32;

        mesh.name = "normalizedGridXZ";

        Vector3[] vertices = new Vector3[(nSegmentsX + 1) * (nSegmentsZ + 1)];
        int[] triangles = new int[2*3*nSegmentsX*nSegmentsZ];
        Vector2[] uv = new Vector2[vertices.Length];

        int index = 0;
        for (int i = 0; i < nSegmentsZ+1; i++)
        {
            float kZ = (float)i/nSegmentsZ;
            for (int j = 0; j < nSegmentsX+1; j++)
            {
                float kX = (float)j/nSegmentsX;
                vertices[index] = computePos != null?computePos(kX,kZ): new Vector3(kX,0,kZ);
                uv[index++] = new Vector2(kX, kZ);
            }
        }

        index = 0;
        int indexOffset = 0;
        for (int i =0; i < nSegmentsZ; i++)
        {
            for (int j = 0; j < nSegmentsX; j++)
            {
                triangles[index++] = j + indexOffset;
                triangles[index++] = j + indexOffset + (nSegmentsX + 1);
                triangles[index++] = (j+1) + indexOffset + (nSegmentsX + 1);

                triangles[index++] = j + indexOffset;
                triangles[index++] = (j+1) + indexOffset + (nSegmentsX + 1);
                triangles[index++] = (j+1) + indexOffset;
            }
            indexOffset += (nSegmentsX + 1);
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
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

    /*
    void OnDrawGizmos()
    {
        if(m_Spline == null) return;
        
        int nPts = 100;
        Gizmos.color = Color.green;
        for (int i = 0; i < nPts; i++)
        {
            Vector3 pt1 = m_Spline.interp((float)i/nPts);
            Vector3 pt2 = m_Spline.interp((float)(i+1)/nPts);
            if(i<nPts-1) Gizmos.DrawLine(pt1,pt2);
            Gizmos.DrawSphere(pt1, .05f);
        }
    }
    */

    private void OnDrawGizmos()
    {
        if (!(m_Mf && m_Mf.mesh && m_DisplayMeshInfo)) return;

        Mesh mesh = m_Mf.mesh;
        //Debug.Log(mesh.GetTopology(0));
        Vector3[] vertices = mesh.vertices;
        int[] quads = mesh.GetIndices(0);

        GUIStyle style = new GUIStyle();
        style.fontSize = 15;
        style.normal.textColor = Color.red;

        if(m_DisplayMeshVertices)
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 worldPos =  transform.TransformPoint(vertices[i]);
                Handles.Label(worldPos, i.ToString(), style);
            }

        Gizmos.color = Color.black;
        style.normal.textColor = Color.blue;

       for (int i = 0; i < quads.Length/4; i++)
        {
            int index1 = quads[4 * i];
            int index2 =quads[4 * i+1];
            int index3 =quads[4 * i+2];
            int index4 = quads[4 * i+3];

            Vector3 pt1 = transform.TransformPoint(vertices[index1]);
            Vector3 pt2 =  transform.TransformPoint(vertices[index2]);
            Vector3 pt3 =  transform.TransformPoint(vertices[index3]);
            Vector3 pt4 = transform.TransformPoint(vertices[index4]);

            if (m_DisplayMeshEdges)
            {
                Gizmos.DrawLine(pt1, pt2);
                Gizmos.DrawLine(pt2, pt3);
                Gizmos.DrawLine(pt3, pt4);
                Gizmos.DrawLine(pt4, pt1);
            }

            if (m_DisplayMeshFaces)
            {
                string str = string.Format("{0} ({1},{2},{3},{4})",
                    i, index1, index2, index3, index4);

                Handles.Label((pt1 + pt2 + pt3 + pt4) / 4.0f, str, style);
            }
        }
    }

    string ConvertToCSV(string separator)
    {
        if (!(m_Mf && m_Mf.mesh)) return "";

        Vector3[] vertices = m_Mf.mesh.vertices;
        int[] quads = m_Mf.mesh.GetIndices(0);

        List<string> strings = new List<string>();

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 pos = vertices[i];
            strings.Add(i.ToString()+separator
                + pos.x.ToString("N03")+" "
                + pos.y.ToString("N03")+ " "
                +pos.z.ToString("N03") + separator+separator);
        }

        for (int i = vertices.Length; i < quads.Length/4; i++)
            strings.Add(separator+separator+separator);

        for (int i = 0; i < quads.Length/4; i++)
        {
            strings[i] += i.ToString() + separator
                + quads[4 * i + 0].ToString() + ","
                + quads[4 * i + 1].ToString() + ","
                + quads[4 * i + 2].ToString() + ","
                + quads[4 * i + 3].ToString();
        }
        
        return"Vertices"+separator+separator+ separator+"Faces\n"
            + "Index"+separator+"Position"+ separator+ separator+
            "Index"+separator+"Indices des vertices\n"
            +string.Join("\n", strings);

    }
}
