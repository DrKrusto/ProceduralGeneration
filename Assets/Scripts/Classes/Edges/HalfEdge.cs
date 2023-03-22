using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;
using Unity.Mathematics;

namespace HalfEdge
{
    public class HalfEdge
    {
        public int index;
        public Vertex sourceVertex;
        public Face face;
        public HalfEdge prevEdge;
        public HalfEdge nextEdge;
        public HalfEdge twinEdge;
    }

    public class Vertex
    {
        public int index;
        public Vector3 position;
        public HalfEdge outgoingEdge;
    }

    public class Face
    {
        public int index;
        public HalfEdge edge;
    }

    public class HalfEdgeMesh
    {
        public List<Vertex> vertices;
        public List<HalfEdge> edges;
        public List<Face> faces;
        public HalfEdgeMesh(Mesh mesh)  // constructeur prenant un mesh Vertex-Face en param�tre
        {
            this.vertices = new();
            this.edges = new();
            this.faces = new();

            int[] originalFaces = mesh.GetIndices(0);
            Vector3[] vertices = mesh.vertices;

            // Each vertices
            for (int i = 0; i < vertices.Length; i++)
            {
                this.vertices.Add(new Vertex
                {
                    index = i,
                    position = vertices[i]
                });
            }

            // For each faces
            for (int i = 0; i < originalFaces.Length/4; i++)
            {
                Face localFace = new() { index = i };

                // For each edges
                for (int j = 0; j < 4; j++)
                {
                    Vertex outgoingVertex = this.vertices.First(p => p.index == originalFaces[4 * i + j]);
                    HalfEdge localEdge = new()
                    {
                        index = this.edges.Count,
                        face = localFace,
                        sourceVertex = outgoingVertex
                    };
                    if (j > 0)
                    {
                        localEdge.prevEdge = this.edges[^1];
                        localEdge.prevEdge.nextEdge = localEdge;
                    }
                    this.edges.Add(localEdge);

                    // We add the reference of the new edge to the source vertex
                    outgoingVertex.outgoingEdge = localEdge;
                }

                // We created every edges, but the first edge doesn't have a prevEdge yet
                this.edges[^4].prevEdge = this.edges[^1];
                this.edges[^1].nextEdge = this.edges[^4];

                // We assign an edge to the face
                localFace.edge = this.edges[^4];

                this.faces.Add(localFace);
            }

            // Now that everything is set we add the twin edges
            foreach (HalfEdge edge in this.edges)
            {
                if (edge.twinEdge != null) continue;

                // We get the two vertex of the edge
                Vertex outgoingVertex = edge.sourceVertex;
                Vertex incomingVertex = edge.nextEdge.sourceVertex;

                // We search an edge that has these two vertices inverted
                HalfEdge twinEdge = this.edges.FirstOrDefault(p => 
                    p.sourceVertex == incomingVertex &&
                    p.nextEdge.sourceVertex == outgoingVertex
                );

                // If found we set the twin edges
                if(twinEdge != null)
                {
                    edge.twinEdge = twinEdge;
                    twinEdge.twinEdge = edge;
                }

            }
        }

        public Mesh ConvertToFaceVertexMesh()
        {
            Mesh faceVertexMesh = new();

            // Set vertices
            faceVertexMesh.SetVertices(this.vertices.Select(p => p.position).ToList());

            // Get faces indices
            List<int> indices = new();
            foreach (Face face in this.faces)
            {
                HalfEdge localEdge = face.edge;
                for (int i = 0; i < 4; i++)
                {
                    indices.Add(localEdge.sourceVertex.index);
                    localEdge = localEdge.nextEdge;
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

            for (int i = 0; i < this.vertices.Count; i++)
            {
                Vertex vertex = this.vertices[i];
                string line = $"{i}{separator}{vertex.position.x:N03} {vertex.position.y:N03} {vertex.position.z:N03}{separator}{vertex.outgoingEdge.index}{separator}{separator}";
                strings.Add(line);
            }

            for (int i = 0; i < this.edges.Count; i++)
            {
                HalfEdge edge = this.edges[i];
                string line = $"{i}{separator}" +
                    $"{edge.sourceVertex.index.ToString() ?? "null"}{separator}" +
                    $"{edge.face?.index.ToString() ?? "null"}{separator}" +
                    $"{edge.prevEdge?.index.ToString() ?? "null"}{separator}" +
                    $"{edge.nextEdge?.index.ToString() ?? "null"}{separator}" +
                    $"{edge.twinEdge?.index.ToString() ?? "null"}{separator}{separator}";

                if (strings.Count > i)
                    strings[i] += line;
                else
                    strings.Add($"{separator}{separator}{separator}{separator}{line}");
            }

            for (int i = this.edges.Count; i < this.faces.Count / 4; i++)
                strings.Add(separator + separator + separator);

            for (int i = 0; i < this.faces.Count / 4; i++)
            {
                int[] localVertices = new int[4];
                HalfEdge localEdge = this.faces[i].edge;
                for (int j = 0; j < 4; j++)
                {
                    localVertices[j] = localEdge.sourceVertex.index;
                    localEdge = localEdge.nextEdge;
                }

                string line = i.ToString() + separator + string.Join(",", localVertices);

                if (strings.Count > i)
                    strings[i] += line;
                else
                    strings.Add($"{separator}{separator}{separator}{separator}{separator}{separator}{separator}{line}");
            }

            return "Vertices" + separator + separator + separator + separator + "Edges" + separator + separator + separator + separator + separator + separator + separator + "Faces\n"
                + "Index" + separator + "Position" + separator + "Outgoing edge index" + separator + separator
                + "Index" + separator + "Source vertex" + separator + "Face" + separator + "Previous edge" + separator + "Next edge" + separator + "Twin edge" + separator + separator
                + "Index" + separator + "Indices des vertices\n"
                + string.Join("\n", strings);
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
                foreach (HalfEdge edge in this.edges)
                {
                    Vector3 pt1 = edge.sourceVertex.position;
                    Vector3 pt2 = edge.nextEdge.sourceVertex.position;
                    Gizmos.DrawLine(pt1, pt2);
                    Handles.Label((pt1 + pt2) / 2.0f, edge.index.ToString(), style);
                }
            }

            if (drawFaces)
            {
                foreach (Face face in this.faces)
                {
                    Vector3 pt = new Vector3(0, 0, 0);
                    HalfEdge localEdge = face.edge;
                    for (int i = 0; i < 4; i++)
                    {
                        pt += localEdge.sourceVertex.position;
                        localEdge = localEdge.nextEdge;
                    }
                    Handles.Label(pt / 4.0f, face.index.ToString(), style);
                }
            }
        }
    }
}