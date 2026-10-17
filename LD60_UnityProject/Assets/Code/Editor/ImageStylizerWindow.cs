using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LD60.EditorTools
{
    public class ImageStylizerWindow : EditorWindow
    {
        private enum OutputSize
        {
            Source,
            Scale,
            Custom
        }

        private enum PreviewMode
        {
            Result,
            Original
        }

        private const string DefaultOutputFolder = "Assets/Art/Images";
        private const string SourceGuidKey = "LD60.ImageStylizer.SourceGuid";
        private const string MaterialGuidKey = "LD60.ImageStylizer.MaterialGuid";
        private const string OutputFolderKey = "LD60.ImageStylizer.OutputFolder";
        private const string OutputNameKey = "LD60.ImageStylizer.OutputName";
        private const string TexturePropertyKey = "LD60.ImageStylizer.TextureProperty";
        private const string ShaderPassKey = "LD60.ImageStylizer.ShaderPass";
        private const string SizeModeKey = "LD60.ImageStylizer.SizeMode";
        private const string ScaleKey = "LD60.ImageStylizer.Scale";
        private const string CustomWidthKey = "LD60.ImageStylizer.CustomWidth";
        private const string CustomHeightKey = "LD60.ImageStylizer.CustomHeight";
        private const double MaterialPollInterval = 0.05d;

        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly string[] PreviewModeLabels = { "Result", "Original" };

        private Texture2D _source;
        private Material _material;
        private MaterialEditor _materialEditor;
        private Material _renderMaterial;
        private RenderTexture _preview;
        private string[] _passNames;
        private int _passNamesShaderId;
        private int _materialFingerprint;
        private double _nextMaterialPollTime;
        private string _outputFolder = DefaultOutputFolder;
        private string _outputName = string.Empty;
        private string _textureProperty = string.Empty;
        private int _shaderPass;
        private OutputSize _sizeMode = OutputSize.Source;
        private PreviewMode _previewMode = PreviewMode.Result;
        private float _scale = 1f;
        private int _customWidth = 1024;
        private int _customHeight = 1024;
        private Vector2 _settingsScroll;
        private bool _previewDirty = true;
        private bool _saveRequested;
        private string _lastSavedPath = string.Empty;

        [MenuItem("Tools/Image Stylizer")]
        private static void Open()
        {
            GetWindow<ImageStylizerWindow>().Show();
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("Image Stylizer");
            minSize = new Vector2(780f, 440f);
            LoadPrefs();
            RebuildMaterialEditor();
            _previewDirty = true;
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            SavePrefs();
            ReleasePreview();
            ReleaseRenderMaterial();
            DestroyMaterialEditor();
        }

        private void OnEditorUpdate()
        {
            if (_saveRequested)
            {
                _saveRequested = false;
                Save();
                Repaint();
            }

            if (!_previewDirty)
                PollMaterialChanges();
            if (!_previewDirty)
                return;

            RenderPreview();
            Repaint();
        }

        private void PollMaterialChanges()
        {
            if (_material == null)
                return;
            if (EditorApplication.timeSinceStartup < _nextMaterialPollTime)
                return;

            _nextMaterialPollTime = EditorApplication.timeSinceStartup + MaterialPollInterval;

            if (ComputeMaterialFingerprint() != _materialFingerprint)
                _previewDirty = true;
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            DrawSettingsPane();
            DrawPreviewPane();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSettingsPane()
        {
            float paneWidth = Mathf.Clamp(position.width * 0.42f, 300f, 460f);
            EditorGUILayout.BeginVertical(GUILayout.Width(paneWidth));
            _settingsScroll = EditorGUILayout.BeginScrollView(_settingsScroll);

            EditorGUILayout.LabelField("Input", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            Texture2D pickedSource = (Texture2D)EditorGUILayout.ObjectField("Source Image", _source, typeof(Texture2D), false);
            if (EditorGUI.EndChangeCheck())
                SetSource(pickedSource);

            EditorGUI.BeginChangeCheck();
            Material pickedMaterial = (Material)EditorGUILayout.ObjectField("Material", _material, typeof(Material), false);
            if (EditorGUI.EndChangeCheck())
                SetMaterial(pickedMaterial);

            EditorGUI.BeginChangeCheck();
            GUIContent propertyLabel = new GUIContent("Texture Property", "Shader property that receives the source image. Empty fills _MainTex and _BaseMap automatically.");
            _textureProperty = EditorGUILayout.TextField(propertyLabel, _textureProperty);
            if (EditorGUI.EndChangeCheck())
                _previewDirty = true;

            DrawShaderPassField();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            _outputFolder = EditorGUILayout.TextField("Folder", _outputFolder);
            if (GUILayout.Button("...", GUILayout.Width(26f)))
                PickOutputFolder();
            EditorGUILayout.EndHorizontal();

            _outputName = EditorGUILayout.TextField("File Name", _outputName);

            EditorGUI.BeginChangeCheck();
            _sizeMode = (OutputSize)EditorGUILayout.EnumPopup("Size", _sizeMode);
            if (_sizeMode == OutputSize.Scale)
                _scale = EditorGUILayout.Slider("Scale", _scale, 0.05f, 8f);
            if (_sizeMode == OutputSize.Custom)
            {
                _customWidth = EditorGUILayout.IntField("Width", _customWidth);
                _customHeight = EditorGUILayout.IntField("Height", _customHeight);
            }
            if (EditorGUI.EndChangeCheck())
                _previewDirty = true;

            if (_source != null)
                EditorGUILayout.LabelField("Resolution", GetOutputWidth() + " x " + GetOutputHeight());

            EditorGUILayout.Space();

            string blocker = GetBlockingIssue();
            if (blocker != null)
                EditorGUILayout.HelpBox(blocker, MessageType.Info);

            using (new EditorGUI.DisabledScope(blocker != null))
            {
                if (GUILayout.Button("Bake & Save PNG", GUILayout.Height(26f)))
                    _saveRequested = true;
            }

            if (!string.IsNullOrEmpty(_lastSavedPath))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Saved", _lastSavedPath, EditorStyles.miniLabel);
                if (GUILayout.Button("Select", GUILayout.Width(52f)))
                    PingSaved();
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Material Parameters", EditorStyles.boldLabel);

            if (_materialEditor == null)
                EditorGUILayout.HelpBox("Assign a material to tweak its parameters here.", MessageType.None);
            else
            {
                EditorGUILayout.LabelField("Shader", _material.shader == null ? "<missing>" : _material.shader.name);
                if (_materialEditor.PropertiesGUI())
                    _previewDirty = true;
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawPreviewPane()
        {
            EditorGUILayout.BeginVertical();

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            _previewMode = (PreviewMode)GUILayout.Toolbar((int)_previewMode, PreviewModeLabels, EditorStyles.toolbarButton, GUILayout.Width(150f));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60f)))
                _previewDirty = true;
            EditorGUILayout.EndHorizontal();

            Rect area = GUILayoutUtility.GetRect(32f, 32f, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            EditorGUI.DrawRect(area, new Color(0.13f, 0.13f, 0.13f, 1f));

            Texture shown = _previewMode == PreviewMode.Original ? _source : _preview;
            if (shown == null)
                EditorGUI.LabelField(area, "No preview", EditorStyles.centeredGreyMiniLabel);
            else
            {
                EditorGUI.DrawTextureTransparent(FitRect(area, shown.width, shown.height), shown, ScaleMode.StretchToFill);
                Rect caption = new Rect(area.x + 6f, area.yMax - 18f, area.width - 12f, 16f);
                GUI.Label(caption, shown.width + " x " + shown.height, EditorStyles.miniLabel);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawShaderPassField()
        {
            if (_material == null || _material.shader == null)
                return;

            int passCount = _material.passCount;
            if (passCount <= 1)
                return;

            RefreshPassNames(passCount);

            EditorGUI.BeginChangeCheck();
            GUIContent passLabel = new GUIContent("Shader Pass", "Blitting runs one pass at a time. Multi-pass shaders such as URP and Shader Graph need their main forward pass, which is normally the first one.");
            int pickedPass = EditorGUILayout.Popup(passLabel, GetShaderPass(), _passNames);
            if (EditorGUI.EndChangeCheck())
            {
                _shaderPass = pickedPass;
                _previewDirty = true;
            }
        }

        private void RefreshPassNames(int passCount)
        {
            int shaderId = _material.shader.GetInstanceID();
            if (_passNames != null && _passNames.Length == passCount && _passNamesShaderId == shaderId)
                return;

            _passNamesShaderId = shaderId;
            _passNames = new string[passCount];
            for (int i = 0; i < passCount; i++)
            {
                string passName = _material.GetPassName(i);
                _passNames[i] = string.IsNullOrEmpty(passName) ? i + ": <unnamed>" : i + ": " + passName;
            }
        }

        private int GetShaderPass()
        {
            if (_material == null)
                return 0;

            int passCount = _material.passCount;
            if (passCount <= 0)
                return 0;

            return Mathf.Clamp(_shaderPass, 0, passCount - 1);
        }

        private void SetSource(Texture2D source)
        {
            _source = source;
            if (_source != null)
                _outputName = _source.name + "_stylized";
            _previewDirty = true;
        }

        private void SetMaterial(Material material)
        {
            _material = material;
            ReleaseRenderMaterial();
            RebuildMaterialEditor();
            _materialFingerprint = ComputeMaterialFingerprint();
            _previewDirty = true;
        }

        private void RenderPreview()
        {
            _previewDirty = false;
            _materialFingerprint = ComputeMaterialFingerprint();

            if (!CanRender())
            {
                ReleasePreview();
                return;
            }

            int width = Mathf.Clamp(GetOutputWidth(), 1, 16384);
            int height = Mathf.Clamp(GetOutputHeight(), 1, 16384);

            if (_preview == null || _preview.width != width || _preview.height != height)
            {
                ReleasePreview();
                _preview = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                _preview.hideFlags = HideFlags.HideAndDontSave;
                _preview.Create();
            }

            RenderThroughMaterial(_preview);
        }

        private void RenderThroughMaterial(RenderTexture target)
        {
            SyncRenderMaterial();

            RenderTexture previousTarget = RenderTexture.active;
            Graphics.Blit(_source, target, _renderMaterial, GetShaderPass());
            RenderTexture.active = previousTarget;
        }

        private void SyncRenderMaterial()
        {
            if (_renderMaterial == null || _renderMaterial.shader != _material.shader)
            {
                ReleaseRenderMaterial();
                _renderMaterial = new Material(_material);
                _renderMaterial.hideFlags = HideFlags.HideAndDontSave;
            }
            else
            {
                _renderMaterial.CopyPropertiesFromMaterial(_material);
                _renderMaterial.shaderKeywords = _material.shaderKeywords;
                _renderMaterial.renderQueue = _material.renderQueue;
            }

            ApplySourceTexture(_renderMaterial);
        }

        private void ApplySourceTexture(Material target)
        {
            if (!string.IsNullOrEmpty(_textureProperty))
            {
                if (target.HasProperty(_textureProperty))
                    target.SetTexture(_textureProperty, _source);
                else
                    Debug.LogError($"{nameof(ImageStylizerWindow)}: shader '{target.shader.name}' has no texture property '{_textureProperty}' in {nameof(ApplySourceTexture)}. " +
                                   "Required to feed the source image into the material. " +
                                   "Fix: clear the Texture Property field to use the automatic _MainTex / _BaseMap fallback, or type the exact property name declared by the shader.");
                return;
            }

            if (target.HasProperty(MainTexId))
                target.SetTexture(MainTexId, _source);
            if (target.HasProperty(BaseMapId))
                target.SetTexture(BaseMapId, _source);
        }

        private Texture2D Bake()
        {
            if (!CanRender())
                return null;

            int width = Mathf.Clamp(GetOutputWidth(), 1, 16384);
            int height = Mathf.Clamp(GetOutputHeight(), 1, 16384);

            RenderTexture target = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            RenderTexture previousTarget = RenderTexture.active;

            RenderThroughMaterial(target);

            RenderTexture.active = target;
            Texture2D baked = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
            baked.hideFlags = HideFlags.HideAndDontSave;
            baked.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            baked.Apply(false);
            RenderTexture.active = previousTarget;

            RenderTexture.ReleaseTemporary(target);

            return baked;
        }

        private void Save()
        {
            Texture2D baked = Bake();
            if (baked == null)
            {
                Debug.LogError($"{nameof(ImageStylizerWindow)}: baking produced no texture in {nameof(Save)}. " +
                               "Required to write the PNG file. " +
                               "Fix: assign both a source image and a material with a valid shader, then bake again.");
                return;
            }

            string folder = NormalizeFolder(_outputFolder);
            string projectPath = folder + "/" + _outputName + ".png";
            string sourcePath = AssetDatabase.GetAssetPath(_source);

            if (projectPath == sourcePath)
            {
                DestroyImmediate(baked);
                Debug.LogError($"{nameof(ImageStylizerWindow)}: output path '{projectPath}' points at the source image in {nameof(Save)}. " +
                               "Writing there would overwrite the original artwork. " +
                               "Fix: change the output file name or the output folder.");
                return;
            }

            string absolutePath = ToAbsolutePath(projectPath);
            string absoluteFolder = Path.GetDirectoryName(absolutePath);
            if (!Directory.Exists(absoluteFolder))
                Directory.CreateDirectory(absoluteFolder);

            File.WriteAllBytes(absolutePath, baked.EncodeToPNG());
            DestroyImmediate(baked);
            _lastSavedPath = projectPath;

            if (projectPath.StartsWith("Assets/"))
            {
                AssetDatabase.ImportAsset(projectPath, ImportAssetOptions.ForceUpdate);
                PingSaved();
            }
            else
                AssetDatabase.Refresh();
        }

        private void PingSaved()
        {
            if (!_lastSavedPath.StartsWith("Assets/"))
                return;

            Object saved = AssetDatabase.LoadAssetAtPath<Object>(_lastSavedPath);
            if (saved != null)
                EditorGUIUtility.PingObject(saved);
        }

        private void PickOutputFolder()
        {
            string currentFolder = ToAbsolutePath(NormalizeFolder(_outputFolder));
            string startFolder = Directory.Exists(currentFolder) ? currentFolder : Application.dataPath;
            string picked = EditorUtility.OpenFolderPanel("Output Folder", startFolder, string.Empty);
            if (string.IsNullOrEmpty(picked))
                return;

            picked = picked.Replace('\\', '/');
            string dataPath = Application.dataPath.Replace('\\', '/');
            if (picked.StartsWith(dataPath))
                picked = "Assets" + picked.Substring(dataPath.Length);

            _outputFolder = picked;
            GUI.FocusControl(null);
        }

        private bool CanRender()
        {
            return _source != null && _material != null && _material.shader != null;
        }

        private int ComputeMaterialFingerprint()
        {
            if (_material == null)
                return 0;

            int fingerprint = _material.ComputeCRC();
            fingerprint = fingerprint * 31 + (_material.shader == null ? 0 : _material.shader.GetInstanceID());

            return fingerprint;
        }

        private string GetBlockingIssue()
        {
            if (_source == null)
                return "Assign a source image.";
            if (_material == null)
                return "Assign a material.";
            if (_material.shader == null)
                return "The assigned material has no shader.";
            if (string.IsNullOrEmpty(_outputName))
                return "Enter an output file name.";
            if (string.IsNullOrEmpty(NormalizeFolder(_outputFolder)))
                return "Enter an output folder.";

            return null;
        }

        private int GetOutputWidth()
        {
            if (_sizeMode == OutputSize.Custom || _source == null)
                return Mathf.Max(1, _customWidth);
            if (_sizeMode == OutputSize.Scale)
                return Mathf.Max(1, Mathf.RoundToInt(_source.width * _scale));

            return _source.width;
        }

        private int GetOutputHeight()
        {
            if (_sizeMode == OutputSize.Custom || _source == null)
                return Mathf.Max(1, _customHeight);
            if (_sizeMode == OutputSize.Scale)
                return Mathf.Max(1, Mathf.RoundToInt(_source.height * _scale));

            return _source.height;
        }

        private void RebuildMaterialEditor()
        {
            DestroyMaterialEditor();
            if (_material != null)
                _materialEditor = (MaterialEditor)Editor.CreateEditor(_material);
        }

        private void DestroyMaterialEditor()
        {
            if (_materialEditor == null)
                return;

            DestroyImmediate(_materialEditor);
            _materialEditor = null;
        }

        private void ReleaseRenderMaterial()
        {
            if (_renderMaterial == null)
                return;

            DestroyImmediate(_renderMaterial);
            _renderMaterial = null;
        }

        private void ReleasePreview()
        {
            if (_preview == null)
                return;

            if (RenderTexture.active == _preview)
                RenderTexture.active = null;

            _preview.Release();
            DestroyImmediate(_preview);
            _preview = null;
        }

        private void LoadPrefs()
        {
            _source = LoadAsset<Texture2D>(SourceGuidKey);
            _material = LoadAsset<Material>(MaterialGuidKey);
            _outputFolder = EditorPrefs.GetString(OutputFolderKey, DefaultOutputFolder);
            _outputName = EditorPrefs.GetString(OutputNameKey, string.Empty);
            _textureProperty = EditorPrefs.GetString(TexturePropertyKey, string.Empty);
            _shaderPass = EditorPrefs.GetInt(ShaderPassKey, 0);
            _sizeMode = (OutputSize)EditorPrefs.GetInt(SizeModeKey, (int)OutputSize.Source);
            _scale = EditorPrefs.GetFloat(ScaleKey, 1f);
            _customWidth = EditorPrefs.GetInt(CustomWidthKey, 1024);
            _customHeight = EditorPrefs.GetInt(CustomHeightKey, 1024);

            if (string.IsNullOrEmpty(_outputName) && _source != null)
                _outputName = _source.name + "_stylized";
        }

        private void SavePrefs()
        {
            EditorPrefs.SetString(SourceGuidKey, GetAssetGuid(_source));
            EditorPrefs.SetString(MaterialGuidKey, GetAssetGuid(_material));
            EditorPrefs.SetString(OutputFolderKey, _outputFolder);
            EditorPrefs.SetString(OutputNameKey, _outputName);
            EditorPrefs.SetString(TexturePropertyKey, _textureProperty);
            EditorPrefs.SetInt(ShaderPassKey, _shaderPass);
            EditorPrefs.SetInt(SizeModeKey, (int)_sizeMode);
            EditorPrefs.SetFloat(ScaleKey, _scale);
            EditorPrefs.SetInt(CustomWidthKey, _customWidth);
            EditorPrefs.SetInt(CustomHeightKey, _customHeight);
        }

        private static T LoadAsset<T>(string key) where T : Object
        {
            string guid = EditorPrefs.GetString(key, string.Empty);
            if (string.IsNullOrEmpty(guid))
                return null;

            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path))
                return null;

            return AssetDatabase.LoadAssetAtPath<T>(path);
        }

        private static string GetAssetGuid(Object asset)
        {
            if (asset == null)
                return string.Empty;

            string path = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(path))
                return string.Empty;

            return AssetDatabase.AssetPathToGUID(path);
        }

        private static string NormalizeFolder(string folder)
        {
            if (string.IsNullOrEmpty(folder))
                return string.Empty;

            return folder.Replace('\\', '/').TrimEnd('/');
        }

        private static string ToAbsolutePath(string projectPath)
        {
            if (!projectPath.StartsWith("Assets/") && projectPath != "Assets")
                return projectPath;

            string projectRoot = Directory.GetParent(Application.dataPath).FullName.Replace('\\', '/');
            return projectRoot + "/" + projectPath;
        }

        private static Rect FitRect(Rect area, int width, int height)
        {
            Rect padded = new Rect(area.x + 8f, area.y + 8f, Mathf.Max(1f, area.width - 16f), Mathf.Max(1f, area.height - 26f));
            float fit = Mathf.Min(padded.width / width, padded.height / height);
            float fittedWidth = width * fit;
            float fittedHeight = height * fit;

            return new Rect(padded.x + (padded.width - fittedWidth) * 0.5f, padded.y + (padded.height - fittedHeight) * 0.5f, fittedWidth, fittedHeight);
        }
    }
}
