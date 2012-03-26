﻿using System.Collections.Generic;

namespace ObjLoader.Loader.Data
{
    public class Face
    {
        private readonly List<FaceVertex> _vertices = new List<FaceVertex>();

        public void AddVertex(FaceVertex vertex)
        {
            _vertices.Add(vertex);
        }

        public FaceVertex this[int i]
        {
            get { return _vertices[i]; }
        }
    }

    public struct FaceVertex
    {
        public FaceVertex(int vertexIndex, int textureIndex, int normalIndex) : this()
        {
            VertexIndex = vertexIndex;
            TextureIndex = textureIndex;
            NormalIndex = normalIndex;
        }

        public int VertexIndex { get; set; }
        public int TextureIndex { get; set; }
        public int NormalIndex { get; set; }
    }
}