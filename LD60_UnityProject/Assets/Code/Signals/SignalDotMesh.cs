using System.Collections.Generic;
using UnityEngine;

namespace LD60.Signals
{
    public class SignalDotMesh
    {
        private readonly Mesh _mesh;
        private readonly List<Vector3> _vertices;
        private readonly int _capacity;

        public SignalDotMesh(GameObject owner, Material material, Color color, int capacity)
        {
            _capacity = Mathf.Max(1, capacity);
            _vertices = new List<Vector3>(_capacity * 4);

            for (int i = 0; i < _capacity * 4; i++)
                _vertices.Add(Vector3.zero);

            int[] triangles = new int[_capacity * 6];

            for (int i = 0; i < _capacity; i++)
            {
                int v = i * 4;
                int t = i * 6;
                triangles[t] = v;
                triangles[t + 1] = v + 2;
                triangles[t + 2] = v + 1;
                triangles[t + 3] = v + 2;
                triangles[t + 4] = v + 3;
                triangles[t + 5] = v + 1;
            }

            _mesh = new Mesh();
            _mesh.name = "SignalDots";
            _mesh.MarkDynamic();
            _mesh.SetVertices(_vertices);
            _mesh.SetTriangles(triangles, 0);
            _mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 1000f);

            MeshFilter filter = owner.AddComponent<MeshFilter>();
            filter.sharedMesh = _mesh;

            MeshRenderer renderer = owner.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            if (material != null)
            {
                renderer.sharedMaterial = material;
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                block.SetColor(Shader.PropertyToID("_BaseColor"), color);
                renderer.SetPropertyBlock(block);
            }
        }

        public void Rebuild(Vector2[] positions, bool[] alive, int count, float size)
        {
            float half = size * 0.5f;

            for (int i = 0; i < _capacity; i++)
            {
                int v = i * 4;

                if (i >= count || !alive[i])
                {
                    _vertices[v] = Vector3.zero;
                    _vertices[v + 1] = Vector3.zero;
                    _vertices[v + 2] = Vector3.zero;
                    _vertices[v + 3] = Vector3.zero;
                    continue;
                }

                Vector2 p = positions[i];
                _vertices[v] = new Vector3(p.x - half, p.y - half, 0f);
                _vertices[v + 1] = new Vector3(p.x + half, p.y - half, 0f);
                _vertices[v + 2] = new Vector3(p.x - half, p.y + half, 0f);
                _vertices[v + 3] = new Vector3(p.x + half, p.y + half, 0f);
            }

            _mesh.SetVertices(_vertices);
        }
    }
}
