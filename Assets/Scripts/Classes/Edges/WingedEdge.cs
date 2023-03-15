using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

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
                if(isClockwise(existingEdges,i))
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
                            tmpEdge.endVertex = this.vertices[quads[4 * i + 2]];
                            if (this.vertices[quads[4 * i + 0]].edge == null) this.vertices[quads[4 * i + 0]].edge = tmpEdge;
                            if (this.vertices[quads[4 * i + 2]].edge == null) this.vertices[quads[4 * i + 2]].edge = tmpEdge;
                        }
                        else if (!startPos.Contains(this.vertices[quads[4 * i + 1]].position))
                        {
                            tmpEdge.startVertex = this.vertices[quads[4 * i + 1]];
                            tmpEdge.endVertex = this.vertices[quads[4 * i + 0]];
                            if (this.vertices[quads[4 * i + 0]].edge == null) this.vertices[quads[4 * i + 0]].edge = tmpEdge;
                            if (this.vertices[quads[4 * i + 1]].edge == null) this.vertices[quads[4 * i + 1]].edge = tmpEdge;
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
                            tmpEdge.endVertex = this.vertices[quads[4 * i + 1]];
                            if (this.vertices[quads[4 * i + 1]].edge == null) this.vertices[quads[4 * i + 1]].edge = tmpEdge;
                            if (this.vertices[quads[4 * i + 3]].edge == null) this.vertices[quads[4 * i + 3]].edge = tmpEdge;
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
                            tmpEdge.endVertex = this.vertices[quads[4 * i + 1]];
                            if (this.vertices[quads[4 * i + 0]].edge == null) this.vertices[quads[4 * i + 0]].edge = tmpEdge;
                            if (this.vertices[quads[4 * i + 1]].edge == null) this.vertices[quads[4 * i + 1]].edge = tmpEdge;
                        }
                        else if (!startPos.Contains(this.vertices[quads[4 * i + 1]].position))
                        {
                            tmpEdge.startVertex = this.vertices[quads[4 * i + 1]];
                            tmpEdge.endVertex = this.vertices[quads[4 * i + 3]];
                            if (this.vertices[quads[4 * i + 1]].edge == null) this.vertices[quads[4 * i + 1]].edge = tmpEdge;
                            if (this.vertices[quads[4 * i + 3]].edge == null) this.vertices[quads[4 * i + 3]].edge = tmpEdge;
                        }
                        else if (!startPos.Contains(this.vertices[quads[4 * i + 2]].position))
                        {
                            tmpEdge.startVertex = this.vertices[quads[4 * i + 2]];
                            tmpEdge.endVertex = this.vertices[quads[4 * i + 0]];
                            if (this.vertices[quads[4 * i + 0]].edge == null) this.vertices[quads[4 * i + 0]].edge = tmpEdge;
                            if (this.vertices[quads[4 * i + 2]].edge == null) this.vertices[quads[4 * i + 2]].edge = tmpEdge;
                        }
                        else if (!startPos.Contains(this.vertices[quads[4 * i + 3]].position))
                        {
                            tmpEdge.startVertex = this.vertices[quads[4 * i + 3]];
                            tmpEdge.endVertex = this.vertices[quads[4 * i + 2]];
                            if (this.vertices[quads[4 * i + 2]].edge == null) this.vertices[quads[4 * i + 2]].edge = tmpEdge;
                            if (this.vertices[quads[4 * i + 3]].edge == null) this.vertices[quads[4 * i + 3]].edge = tmpEdge;
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
            foreach (WingedEdge edge in this.edges)
            { 
                for (int i = 0;i < 4;i++)
                {
                    if (this.vertices[meshIndices[4 * index + i]].position == edge.startVertex.position)
                    {
                        returnedEdges.Add(edge);
                    }
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

        bool isClockwise(List<WingedEdge> edges, int index)
        {
            if (edges == null) return true;

            if (edges[0].rightFace == this.faces[index]) return true;
            else return false;
        }

        public Mesh ConvertToFaceVertexMesh()
        {
            Mesh faceVertexMesh = new Mesh();
            // magic happens
            return faceVertexMesh;
        }

        public string ConvertToCSVFormat(string separator = "\t")
        {
            string str = "";
            //magic happens
            return str;
        }

        public void DrawGizmos(bool drawVertices, bool drawEdges, bool drawFaces)
        {
            //magic happens
        }
    }
}