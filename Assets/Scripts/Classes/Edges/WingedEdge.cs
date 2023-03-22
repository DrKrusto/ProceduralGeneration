using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static UnityEditor.PlayerSettings;
using static UnityEngine.RectTransform;

namespace WingedEdge
{
    public class WingedEdge
    {
        public int index;
        public Vertex startVertex;
        public Vertex endVertex;
        public Face leftFace;
        public Face rightFace;
        public WingedEdge startCWEdge;
        public WingedEdge startCCWEdge;
        public WingedEdge endCWEdge;
        public WingedEdge endCCWEdge;
    }

    public class Vertex
    {
        public int index;
        public Vector3 position;
        public WingedEdge edge;
    }

    public class Face
    {
        public int index;
        public WingedEdge edge;
    }

    public class WingedEdgeMesh
    {
        public List<Vertex> vertices;
        public List<WingedEdge> edges;
        public List<Face> faces;
        public WingedEdgeMesh(Mesh mesh)    // constructeur prenant un mesh Vertex-Face en paramètre
        {
            this.vertices = new List<Vertex>();
            this.edges = new List<WingedEdge>();
            this.faces = new List<Face>();

            Vector3[] vertices = mesh.vertices;
            int[] quads = mesh.GetIndices(0);

            // Vertices
            for (int i = 0; i < vertices.Length; i++)
            {
                Vertex tmp = new Vertex();
                tmp.position = vertices[i];
                tmp.index = i;
                this.vertices.Add(tmp);
            }

            // Faces
            for (int i = 0; i < quads.Length / 4; i++)
            {
                Face tmp = new Face();
                tmp.index = i;
                this.faces.Add(tmp);
            }

            int index = 0;
            // WingedEdge
            for (int i = 0; i < quads.Length / 4; i++)
            {
                List<WingedEdge> existingEdges = existingEdge(quads,i);
                if(isClockwise(existingEdges))
                {
                    foreach(WingedEdge edge in existingEdges)
                    {
                        edge.rightFace = this.faces[i];
                    }
                    int max = existingEdges.Count;
                    for (int j = 4; j > max; j--)
                    {
                        WingedEdge tmpEdge = new WingedEdge();
                        tmpEdge.index = index++;
                        tmpEdge.rightFace = this.faces[i];
                        if (this.faces[i].edge == null) this.faces[i].edge = tmpEdge;
                        List<Vector3> startPos = existingEdgeStartPos(existingEdges);
                        if(!startPos.Contains(this.vertices[quads[4 * i + 0]].position))
                        {
                            tmpEdge.startVertex = this.vertices[quads[4 * i + 0]];
                            tmpEdge.endVertex = this.vertices[quads[4 * i + 1]];
                            if (this.vertices[quads[4 * i + 0]].edge == null) this.vertices[quads[4 * i + 0]].edge = tmpEdge;
                            if (this.vertices[quads[4 * i + 1]].edge == null) this.vertices[quads[4 * i + 1]].edge = tmpEdge;
                        }
                        else if (!startPos.Contains(this.vertices[quads[4 * i + 1]].position))
                        {
                            tmpEdge.startVertex = this.vertices[quads[4 * i + 1]];
                            tmpEdge.endVertex = this.vertices[quads[4 * i + 2]];
                            if (this.vertices[quads[4 * i + 1]].edge == null) this.vertices[quads[4 * i + 1]].edge = tmpEdge;
                            if (this.vertices[quads[4 * i + 2]].edge == null) this.vertices[quads[4 * i + 2]].edge = tmpEdge;
                        }
                        else if (!startPos.Contains(this.vertices[quads[4 * i + 2]].position))
                        {
                            tmpEdge.startVertex = this.vertices[quads[4 * i + 2]];
                            tmpEdge.endVertex = this.vertices[quads[4 * i + 3]];
                            if (this.vertices[quads[4 * i + 2]].edge == null) this.vertices[quads[4 * i + 2]].edge = tmpEdge;
                            if (this.vertices[quads[4 * i + 3]].edge == null) this.vertices[quads[4 * i + 3]].edge = tmpEdge;
                        }
                        else if (!startPos.Contains(this.vertices[quads[4 * i + 3]].position))
                        {
                            tmpEdge.startVertex = this.vertices[quads[4 * i + 3]];
                            tmpEdge.endVertex = this.vertices[quads[4 * i + 0]];
                            if (this.vertices[quads[4 * i + 3]].edge == null) this.vertices[quads[4 * i + 3]].edge = tmpEdge;
                            if (this.vertices[quads[4 * i + 0]].edge == null) this.vertices[quads[4 * i + 0]].edge = tmpEdge;
                        }
                        existingEdges.Add(tmpEdge);
                        this.edges.Add(tmpEdge);
                    }
                    foreach (WingedEdge edge in existingEdges)
                    {
                        edge.endCCWEdge = existingEdges.FirstOrDefault(researchEdge => researchEdge.startVertex == edge.endVertex);
                        edge.startCWEdge = existingEdges.FirstOrDefault(researchEdge => researchEdge.endVertex == edge.startVertex);
                    }
                }
                else
                {
                    foreach (WingedEdge edge in existingEdges)
                    {
                        edge.leftFace = this.faces[i];
                    }
                    int max = existingEdges.Count;
                    for (int j = 4; j > max; j--)
                    {
                        WingedEdge tmpEdge = new WingedEdge();
                        tmpEdge.index = index++;
                        tmpEdge.leftFace = this.faces[i];
                        if (this.faces[i].edge == null) this.faces[i].edge = tmpEdge;
                        List<Vector3> startPos = existingEdgeStartPos(existingEdges);
                        if (!startPos.Contains(this.vertices[quads[4 * i + 0]].position))
                        {
                            tmpEdge.startVertex = this.vertices[quads[4 * i + 0]];
                            tmpEdge.endVertex = this.vertices[quads[4 * i + 3]];
                            if (this.vertices[quads[4 * i + 0]].edge == null) this.vertices[quads[4 * i + 0]].edge = tmpEdge;
                            if (this.vertices[quads[4 * i + 3]].edge == null) this.vertices[quads[4 * i + 3]].edge = tmpEdge;
                        }
                        else if (!startPos.Contains(this.vertices[quads[4 * i + 1]].position))
                        {
                            tmpEdge.startVertex = this.vertices[quads[4 * i + 1]];
                            tmpEdge.endVertex = this.vertices[quads[4 * i + 0]];
                            if (this.vertices[quads[4 * i + 1]].edge == null) this.vertices[quads[4 * i + 1]].edge = tmpEdge;
                            if (this.vertices[quads[4 * i + 0]].edge == null) this.vertices[quads[4 * i + 0]].edge = tmpEdge;
                        }
                        else if (!startPos.Contains(this.vertices[quads[4 * i + 2]].position))
                        {
                            tmpEdge.startVertex = this.vertices[quads[4 * i + 2]];
                            tmpEdge.endVertex = this.vertices[quads[4 * i + 1]];
                            if (this.vertices[quads[4 * i + 2]].edge == null) this.vertices[quads[4 * i + 2]].edge = tmpEdge;
                            if (this.vertices[quads[4 * i + 1]].edge == null) this.vertices[quads[4 * i + 1]].edge = tmpEdge;
                        }
                        else if (!startPos.Contains(this.vertices[quads[4 * i + 3]].position))
                        {
                            tmpEdge.startVertex = this.vertices[quads[4 * i + 3]];
                            tmpEdge.endVertex = this.vertices[quads[4 * i + 2]];
                            if (this.vertices[quads[4 * i + 3]].edge == null) this.vertices[quads[4 * i + 3]].edge = tmpEdge;
                            if (this.vertices[quads[4 * i + 2]].edge == null) this.vertices[quads[4 * i + 2]].edge = tmpEdge;
                        }
                        existingEdges.Add(tmpEdge);
                        this.edges.Add(tmpEdge);
                    }
                    foreach (WingedEdge edge in existingEdges)
                    {
                        edge.endCWEdge = existingEdges.FirstOrDefault(researchEdge => researchEdge.startVertex == edge.endVertex);
                        edge.startCCWEdge = existingEdges.FirstOrDefault(researchEdge => researchEdge.endVertex == edge.startVertex);
                    }
                }
            }


        }

        List<WingedEdge> existingEdge(int[] meshIndices, int index)
        {
            if (this.edges == null) return null;

            List<WingedEdge> returnedEdges = new List<WingedEdge>();
            
            
            for (int i = 0 ; i < 4 ; i++)
            {
                for (int j =0 ; j < 4; j++)
                {
                    Vertex v1 = this.vertices[meshIndices[4 * index + i]];
                    Vertex v2 = this.vertices[meshIndices[4 * index + j]];
                    returnedEdges.AddRange(this.edges.Where(p => p.startVertex.index == v1.index && p.endVertex.index == v2.index));
                }
            }
            return returnedEdges;
        }

        List<Vector3> existingEdgeStartPos(List<WingedEdge> existingEdges)
        {
            if (existingEdges == null) return null;

            List<Vector3> returnedExistingPos = new List<Vector3>();
            foreach (WingedEdge edge in existingEdges)
            {
                returnedExistingPos.Add(edge.startVertex.position);
            }
            return returnedExistingPos;
        }

        bool isClockwise(List<WingedEdge> edges)
        {
            if (edges == null || edges.Count == 0) return true;
            bool rightFace = true;
            foreach(WingedEdge edge in edges)
            {
                if(edge.rightFace != null)
                {
                    rightFace = false;
                }
            }
            if (rightFace) return true;
            else return false;
        }

        public Mesh ConvertToFaceVertexMesh()
        {
            Mesh faceVertexMesh = new Mesh();

            // Set vertices
            faceVertexMesh.SetVertices(this.vertices.Select(p => p.position).ToList());

            // Get faces indices
            List<int> indices = new List<int>();
            foreach (Face face in this.faces)
            {
                WingedEdge localEdge = face.edge;
                if (localEdge.rightFace == face)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        indices.Add(localEdge.startVertex.index);
                        localEdge = localEdge.endCCWEdge;
                    }
                }
                else
                {
                    for (int i = 0; i < 4; i++)
                    {
                        indices.Add(localEdge.endVertex.index);
                        localEdge = localEdge.startCCWEdge;
                    }

                }
            }

            // Set indices
            faceVertexMesh.SetIndices(indices, MeshTopology.Quads, 0);

            faceVertexMesh.RecalculateNormals();
            faceVertexMesh.RecalculateBounds();
            return faceVertexMesh;
        }

        public string ConvertToCSVFormat(string separator = "\t")
        {
            List<string> strings = new List<string>();

            foreach (WingedEdge edge in this.edges)
            {
                strings.Add(edge.index.ToString() + separator
                    + edge.startVertex.index.ToString() + separator
                    + edge.endVertex.index.ToString() + separator
                    + $"{edge.leftFace?.index.ToString() ?? "null"}" + separator
                    + $"{edge.rightFace?.index.ToString() ?? "null"}" + separator
                    + $"{edge.startCWEdge?.index.ToString() ?? "null"}" + separator
                    + $"{edge.startCCWEdge?.index.ToString() ?? "null"}" + separator
                    + $"{edge.endCWEdge?.index.ToString() ?? "null"}" + separator
                    + $"{edge.endCCWEdge?.index.ToString() ?? "null"}" + separator + separator);
            }

            int biggestCount = this.vertices.Count > this.faces.Count ? this.vertices.Count : this.faces.Count;

            for (int i = this.edges.Count; i < biggestCount; i++)
                strings.Add(separator + separator + separator + separator + separator + separator + separator + separator + separator + separator);

            foreach (Face face in this.faces)
            {
                strings[face.index] += face.index.ToString() + separator
                    + face.edge.index.ToString() + separator + separator;
            }


            for (int i = this.faces.Count; i < this.vertices.Count; i++)
                strings[i] += separator + separator + separator;

            foreach (Vertex vertice in this.vertices)
            {
                strings[vertice.index] += vertice.index.ToString() + separator
                    + vertice.position.x.ToString("N03") + ";"
                    + vertice.position.y.ToString("N03") + ";"
                    + vertice.position.z.ToString("N03") + separator
                    + vertice.edge.index.ToString();
            }

            return "WingedEdge" + separator + separator + separator + separator + separator + separator + separator + separator + separator + separator +
                "Faces" + separator + separator + separator +
                "Vertices\n" + 
                "Index" + separator + "Start Vertex index" + separator + "End Vertex index" + separator + "Left face index" + separator + "Right face index" + separator +
                "Start CWE Edge" + separator + "Start CCW Edge" + separator + "End CWE Edge" + separator + "End CCW Edge" + separator + separator +
                "Index" + separator + "Edge index" + separator + separator +
                "Index" + separator + "Position" + separator + "Edge index\n" +
                string.Join("\n", strings);
        }

        public void DrawGizmos(bool drawVertices, bool drawEdges, bool drawFaces)
        {
            GUIStyle style = new GUIStyle();
            style.fontSize = 15;
            style.normal.textColor = Color.red;

            if (drawVertices)
            {
                foreach (Vertex vertice in this.vertices)
                {
                    Handles.Label(vertice.position, vertice.index.ToString(), style);
                }
            }

            Gizmos.color = Color.black;
            style.normal.textColor = Color.blue;

            if (drawEdges)
            {
                foreach (WingedEdge edge in this.edges)
                {
                    Vector3 pt1 = edge.startVertex.position;
                    Vector3 pt2 = edge.endVertex.position;
                    Gizmos.DrawLine(pt1, pt2);
                    Handles.Label((pt1+pt2)/2.0f, edge.index.ToString(), style);
                }
            }

            if (drawFaces)
            {
                foreach (Face face in this.faces)
                {
                    Vector3 pt = new Vector3(0, 0, 0);
                    WingedEdge localEdge = face.edge;
                    if (localEdge.rightFace == face)
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            pt += localEdge.startVertex.position;
                            localEdge = localEdge.endCCWEdge;
                        }
                    }
                    else
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            pt += localEdge.startVertex.position;
                            localEdge = localEdge.endCWEdge;
                        }
                    }
                    Handles.Label(pt / 4.0f, face.index.ToString(), style);
                }
            }
        }
    }
}