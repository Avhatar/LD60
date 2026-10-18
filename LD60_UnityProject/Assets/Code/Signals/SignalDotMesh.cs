using System.Collections.Generic;
using UnityEngine;

namespace LD60.Signals
{
    public class SignalDotMesh
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int AltColorId = Shader.PropertyToID("_AltColor");
        private static readonly int EmissionId = Shader.PropertyToID("_Emission");
        private static readonly int AltEmissionId = Shader.PropertyToID("_AltEmission");
        private static readonly int EdgeSoftnessId = Shader.PropertyToID("_EdgeSoftness");
        private static readonly int FadeId = Shader.PropertyToID("_Fade");

        private readonly Mesh _mesh;
        private readonly List<Vector3> _vertices;
        private readonly List<Color32> _colors;
        private readonly MeshRenderer _renderer;
        private readonly MaterialPropertyBlock _block;
        private readonly int _capacity;

        public SignalDotMesh(GameObject owner, Material material, int capacity)
        {
            _capacity = Mathf.Max(1, capacity);
            _vertices = new List<Vector3>(_capacity * 4);
            _colors = new List<Color32>(_capacity * 4);

            List<Vector2> uvs = new List<Vector2>(_capacity * 4);
            int[] triangles = new int[_capacity * 6];

            for (int i = 0; i < _capacity; i++)
            {
                _vertices.Add(Vector3.zero);
                _vertices.Add(Vector3.zero);
                _vertices.Add(Vector3.zero);
                _vertices.Add(Vector3.zero);

                _colors.Add(new Color32(0, 0, 0, 0));
                _colors.Add(new Color32(0, 0, 0, 0));
                _colors.Add(new Color32(0, 0, 0, 0));
                _colors.Add(new Color32(0, 0, 0, 0));

                uvs.Add(new Vector2(0f, 0f));
                uvs.Add(new Vector2(1f, 0f));
                uvs.Add(new Vector2(0f, 1f));
                uvs.Add(new Vector2(1f, 1f));

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
            _mesh.SetUVs(0, uvs);
            _mesh.SetColors(_colors);
            _mesh.SetTriangles(triangles, 0);
            _mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 1000f);

            MeshFilter filter = owner.AddComponent<MeshFilter>();
            filter.sharedMesh = _mesh;

            _renderer = owner.AddComponent<MeshRenderer>();
            _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _renderer.receiveShadows = false;

            _block = new MaterialPropertyBlock();

            if (material == null)
            {
                Debug.LogError($"{nameof(SignalDotMesh)}: {nameof(material)} is null in the constructor for '{owner.name}'. " +
                               "Required because signal dots have nothing to draw with and stay invisible. " +
                               $"Fix: assign the SignalDot material on the {nameof(SignalLauncher)} / {nameof(ForeignSignalDirector)} component in the Inspector.");
                return;
            }

            _renderer.sharedMaterial = material;
        }

        public void SetLook(Color color, float emission, float edgeSoftness)
        {
            _block.SetColor(BaseColorId, color);
            _block.SetFloat(EmissionId, emission);
            _block.SetFloat(EdgeSoftnessId, edgeSoftness);
            _renderer.SetPropertyBlock(_block);
        }

        public void SetAltLook(Color color, float emission)
        {
            _block.SetColor(AltColorId, color);
            _block.SetFloat(AltEmissionId, emission);
            _renderer.SetPropertyBlock(_block);
        }

        public void SetFade(float fade)
        {
            _block.SetFloat(FadeId, fade);
            _renderer.SetPropertyBlock(_block);
        }

        public void Rebuild(Vector2[] positions, float[] weights, float[] altBlends, int count, float size)
        {
            float half = size * 0.5f;

            for (int i = 0; i < _capacity; i++)
            {
                int v = i * 4;
                float weight = i < count ? Mathf.Clamp01(weights[i]) : 0f;

                if (weight <= 0f)
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

                byte alpha = (byte)Mathf.RoundToInt(weight * 255f);
                byte blend = (byte)Mathf.RoundToInt(Mathf.Clamp01(altBlends[i]) * 255f);
                Color32 vertexColor = new Color32(blend, 0, 0, alpha);

                _colors[v] = vertexColor;
                _colors[v + 1] = vertexColor;
                _colors[v + 2] = vertexColor;
                _colors[v + 3] = vertexColor;
            }

            _mesh.SetVertices(_vertices);
            _mesh.SetColors(_colors);
        }
    }
}
