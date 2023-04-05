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
                    Vertex outgoingVertex = this.vertices[originalFaces[(4 * i) + j]];
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
            List<Vector3> listOfVertices = new List<Vector3>();
            foreach (var vertice in this.vertices)
            {
                listOfVertices.Add(vertice.position);
            }
            faceVertexMesh.SetVertices(listOfVertices);
            //faceVertexMesh.SetVertices(this.vertices.Select(p => p.position).ToList());

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

            for (int i = 0; i < this.faces.Count; i++)
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

        public void DrawGizmos(Transform origin, bool drawVertices = true, bool drawEdges = true, bool drawFaces = true)
        {
            GUIStyle style = new GUIStyle();
            style.fontSize = 15;
            style.normal.textColor = Color.red;

            if (drawVertices)
            {
                foreach (Vertex vertice in this.vertices)
                {
                    Vector3 worldPos = origin.TransformPoint(vertice.position);
                    Handles.Label(worldPos, vertice.index.ToString(), style);
                }
            }

            Gizmos.color = Color.black;
            style.normal.textColor = Color.blue;

            if (drawEdges)
            {
                foreach (HalfEdge edge in this.edges)
                {
                    Vector3 pt1 = origin.TransformPoint(edge.sourceVertex.position);
                    Vector3 pt2 = origin.TransformPoint(edge.nextEdge.sourceVertex.position);
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
                        pt += origin.TransformPoint(localEdge.sourceVertex.position);
                        localEdge = localEdge.nextEdge;
                    }
                    Handles.Label(pt / 4.0f, face.index.ToString(), style);
                }
            }
        }

        public void SubdivideCatmullClark()
        {
            CatmullClarkCreateNewPoints(out var facePoint, out var edgePoints, out var vertexPoints);

            // Change vertices position to vertex points
            this.vertices.ForEach(p => p.position = vertexPoints[p.index]);

            int edgesCount = this.edges.Count;
            for (int i = 0; i < edgesCount; i++)
            {
                SplitEdge(this.edges[i], edgePoints[i]);
            }

            int facesCount = this.faces.Count;
            for (int i = 0; i < facesCount; i++)
            {
                SplitFace(this.faces[i], facePoint[i]);
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
                if (twinEdge != null)
                {
                    edge.twinEdge = twinEdge;
                    twinEdge.twinEdge = edge;
                }

            }
        }

        public void CatmullClarkCreateNewPoints(out List<Vector3> facePoints, out List<Vector3> edgePoints, out List<Vector3> vertexPoints)
        {
            // Middle face points
            facePoints = new();
            foreach (var face in this.faces)
            {
                Vector3 verticeToCalculate = new Vector3();
                HalfEdge currentEdge = face.edge;
                for (int i = 0; i < 4; i++)
                {
                    verticeToCalculate += currentEdge.sourceVertex.position;
                    currentEdge = currentEdge.nextEdge;
                }
                facePoints.Add(verticeToCalculate/4f);
            }
            // Edge points & Mid points
            edgePoints = new();
            List<Vector3> midPoints = new(); 
            foreach (var edge in this.edges)
            {
                Vector3 v0 = edge.sourceVertex.position;
                Vector3 v1 = edge.nextEdge.sourceVertex.position;
                midPoints.Add((v0 + v1) / 2f);

                // If on border
                if (edge.twinEdge == null)
                {
                    edgePoints.Add((v0 + v1) / 2f);
                    continue;
                }

                Vector3 c0 = facePoints[edge.face.index];
                Vector3 c1 = facePoints[edge.twinEdge.face.index];
                edgePoints.Add((v0 + v1 + c0 + c1) / 4f);
            }
            // Vertex points
            vertexPoints = new();
            foreach (var vertex in this.vertices)
            {
                List<Vector3> borderMidPoints = new();

                // Déclaration des variables de calculs
                Vector3 Q = Vector3.zero;
                Vector3 R = Vector3.zero;
                Vector3 V = vertex.position;
                float n = 0;

                HalfEdge currentEdge = vertex.outgoingEdge;
                // Verify if on border
                do
                {
                    // Verify if on border
                    if(currentEdge.twinEdge == null)
                    {
                        // We add the current edge mid point
                        borderMidPoints.Add(midPoints[currentEdge.index]);
                        
                        // We rotate around the vertex to find the other edge on border
                        while (currentEdge.prevEdge.twinEdge != null)
                        {
                            currentEdge = currentEdge.prevEdge.twinEdge;
                        }

                        // We add the other edge mid point
                        borderMidPoints.Add(midPoints[currentEdge.prevEdge.index]);
                        break;
                    }

                    // Moyenne des face points des faces adjacentes à la vertice
                    Q += facePoints[currentEdge.face.index];

                    // Moyenne des mid points des edges incidentes à la vertice
                    R += midPoints[currentEdge.index];

                    // Fin de boucle
                    currentEdge = currentEdge.twinEdge.nextEdge;
                    n++;
                } while (currentEdge != vertex.outgoingEdge);

                Vector3 calculatedVertice = Vector3.zero;

                if (borderMidPoints.Count > 0)
                {
                    Vector3 v0 = vertex.position;
                    Vector3 midPointsSum = new Vector3(
                        borderMidPoints.Sum(p => p.x), 
                        borderMidPoints.Sum(p => p.y), 
                        borderMidPoints.Sum(p => p.z)
                    );
                    calculatedVertice = (v0 + midPointsSum) / 3f;
                }
                else
                {
                    Q /= n;
                    R /= n;
                    calculatedVertice = (Q / n) + (2 * R) / n + ((n - 3) * V) / n;
                }

                vertexPoints.Add(calculatedVertice);
            }
        }

        public void SplitEdge(HalfEdge edge, Vector3 splittingPoint)
        {
            Vertex newVertex = new Vertex
            {
                index = this.vertices.Count,
                position = splittingPoint
            };
            this.vertices.Add(newVertex);

            HalfEdge nextEdge = new HalfEdge
            {
                index = this.edges.Count,
                sourceVertex = newVertex,
                face = edge.face,
                prevEdge = edge,
                nextEdge = edge.nextEdge,
                twinEdge = null
            };

            edge.nextEdge = nextEdge;
            edge.twinEdge = null;

            newVertex.outgoingEdge = nextEdge;

            this.edges.Add(nextEdge);
        }

        public void SplitFace(Face face, Vector3 splittingPoint)
        {
            Vertex newVertex = new Vertex
            {
                index = this.vertices.Count,
                position = splittingPoint
            };
            this.vertices.Add(newVertex);

            HalfEdge currentEdge = face.edge.nextEdge;
            int initialEdgeIndex = currentEdge.index;
            Face currentFace = face;

            do
            {
                // Variable to skip edges
                HalfEdge skippedEdge = currentEdge.nextEdge.nextEdge;

                currentFace.edge = currentEdge;

                HalfEdge firstEdge = new HalfEdge
                {
                    index = this.edges.Count,
                    face = currentFace,
                    prevEdge = currentEdge,
                    sourceVertex = currentEdge.nextEdge.sourceVertex
                };

                HalfEdge secondEdge = new HalfEdge
                {
                    index = this.edges.Count + 1,
                    face = currentFace,
                    prevEdge = firstEdge,
                    nextEdge = currentEdge.prevEdge,
                    sourceVertex = newVertex
                };

                newVertex.outgoingEdge = secondEdge;
                firstEdge.nextEdge = secondEdge;
                currentFace.edge = currentEdge;

                // Redifine current edge
                currentEdge.face = currentFace;
                currentEdge.prevEdge.face = currentFace;
                currentEdge.nextEdge = firstEdge;

                this.edges.Add(firstEdge);
                this.edges.Add(secondEdge);

                // Skip to further edge
                currentEdge = skippedEdge;

                // End of loop
                if (currentEdge.index != initialEdgeIndex)
                {
                    currentFace = new Face
                    {
                        index = this.faces.Count
                    };
                    this.faces.Add(currentFace);
                }
            } while (currentEdge.index != initialEdgeIndex);
        }
    }
}