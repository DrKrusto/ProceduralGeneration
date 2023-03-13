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