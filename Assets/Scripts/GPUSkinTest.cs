using System;
using System.Collections.Generic;
using ST.GPUSkin;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// GPU skinning test: spawns many <see cref="GPUSkinPlayerBase"/> instances and provides an IMGUI sidebar with two batch animation actions.
/// Calls <see cref="GPUSkinMgr.S"/>.<see cref="GPUSkinMgr.OnUpdate"/> each frame.
/// </summary>
public class GPUSkinTest : MonoBehaviour
{
    #region Inspector (prefabs)

    [Tooltip("Cloned 100 times each. Use bone or vertex GPUSkin prefab with GPUSkinPlayer.")]
    public GameObject gpuSkinGo1;
    [Tooltip("Same as above. Both can be set to compare the two skinning paths.")]
    public GameObject gpuSkinGo2;

    #endregion

    #region Inspector (IMGUI layout)

    /// <summary>Reference design size (width, height) for safe-area–aware scaling; avoids hard-coded screen pixels.</summary>
    public Vector2 designResolution = new Vector2(1920f, 1080f);

    /// <summary>Base font size at scale 1; actual size is multiplied by the layout scale from <see cref="BuildGuiLayout"/>.</summary>
    [Min(8)]
    public int guiFontSizeBase = 18;

    #endregion

    #region Runtime (players and clip list)

    /// <summary>All spawned <see cref="GPUSkinPlayerBase"/> references for batch UI actions.</summary>
    readonly List<GPUSkinPlayerBase> m_Players = new List<GPUSkinPlayerBase>();

    int m_SelectedAnimNameIndex;
    int m_CachedPlayerCountForAnimNames = -1;
    string[] m_OnlyRandomAnimNameList = Array.Empty<string>();
    Vector2 m_OnlyRandomAnimScroll;

    #endregion

    #region IMGUI style cache

    GUIStyle m_StyleBox;
    GUIStyle m_StyleSectionBox;
    GUIStyle m_StyleSectionTitle;
    GUIStyle m_StyleLabel;
    GUIStyle m_StyleButton;
    GUIStyle m_StyleListRow;
    GUIStyle m_StyleListRowActive;
    Texture2D m_HighlightBackground;

    float m_CachedStyleScale = -1f;
    int m_CachedFontBase = -1;

    #endregion

    #region Layout constants (reference pixels, then multiplied by <see cref="GuiLayout.Scale"/>)

    const float kRefMargin = 10f;
    const float kRefPanelW = 460f;
    const float kRefPanelH = 560f;
    const float kRefLineH = 36f;
    const float kRefSectionGap = 14f;
    const float kRefAnimScrollH = 140f;
    const float kScaleMin = 0.25f;
    const float kScaleMax = 4f;

    #endregion

    #region Spawn constants

    const int kInstancesPerSourcePrefab = 100;
    const float kSpawnPosRange = 5f;
    const float kPrefabScaleMin = 0.2f;
    const float kPrefabScaleMax = 0.5f;

    #endregion

    #region Layout struct

    struct GuiLayout
    {
        public float Scale;
        public float LineH;
        public float SectionGap;
        public float AnimScrollH;
        public Rect PanelRect;
    }

    #endregion

    #region Unity lifecycle

    void Start()
    {
        InitGpuSkinList(gpuSkinGo1);
        InitGpuSkinList(gpuSkinGo2);
    }

    void Update()
    {
        GPUSkinMgr.S.OnUpdate();
    }

    #endregion

    #region OnGUI

    void OnGUI()
    {
        var layout = BuildGuiLayout();
        EnsureGuiStyles(layout.Scale);
        GUILayout.BeginArea(layout.PanelRect, m_StyleBox);
        DrawPanelTitle(layout);
        DrawSection1RandomPlayWithRandomStart(layout);
        GUILayout.Space(layout.SectionGap);
        DrawSection2OnlyRandomStartFrame(layout);
        GUILayout.Space(layout.SectionGap * 0.5f);
        DrawFooter(layout);
        GUILayout.EndArea();
    }

    #endregion

    #region Layout (safe area, panel rect)

    void FixDesignResolutionIfInvalid()
    {
        if (designResolution.x <= 0.1f || designResolution.y <= 0.1f)
            designResolution = new Vector2(1920f, 1080f);
    }

    /// <summary>Converts <see cref="Screen.safeArea"/> (bottom-left origin) to OnGUI (top-left origin).</summary>
    static Rect ScreenSafeAreaToGuiTopLeft()
    {
        Rect s = Screen.safeArea;
        return new Rect(s.x, Screen.height - s.y - s.height, s.width, s.height);
    }

    GuiLayout BuildGuiLayout()
    {
        FixDesignResolutionIfInvalid();
        Rect safeGui = ScreenSafeAreaToGuiTopLeft();
        float s = Mathf.Min(safeGui.width / designResolution.x, safeGui.height / designResolution.y);
        if (s <= 0f)
            s = 1f;
        s = Mathf.Clamp(s, kScaleMin, kScaleMax);

        float m = kRefMargin * s;
        float panelW = kRefPanelW * s;
        float panelH = kRefPanelH * s;
        float x = safeGui.x + safeGui.width - panelW - m;
        float y = safeGui.y + m;

        return new GuiLayout
        {
            Scale = s,
            LineH = kRefLineH * s,
            SectionGap = kRefSectionGap * s,
            AnimScrollH = kRefAnimScrollH * s,
            PanelRect = new Rect(x, y, panelW, panelH)
        };
    }

    #endregion

    #region OnGUI sections

    void DrawPanelTitle(GuiLayout L)
    {
        GUILayout.Label("GPU Skin Instances", m_StyleSectionTitle, GUILayout.Height(L.LineH));
    }

    void DrawSection1RandomPlayWithRandomStart(GuiLayout L)
    {
        GUILayout.BeginVertical(m_StyleSectionBox);
        GUILayout.Label("1) Random clip + random start frame", m_StyleSectionTitle, GUILayout.Height(L.LineH));
        GUILayout.Label(
            "Per instance: pick a random animation, then a random start frame on that clip.",
            m_StyleLabel,
            GUILayout.Height(L.LineH * 1.35f));
        if (GUILayout.Button("All: random clip + random start frame", m_StyleButton, GUILayout.Height(L.LineH)))
        {
            foreach (var p in m_Players)
            {
                if (p != null)
                    p.RandomPlayWithRandomStart();
            }
        }
        GUILayout.EndVertical();
    }

    void DrawSection2OnlyRandomStartFrame(GuiLayout L)
    {
        GUILayout.BeginVertical(m_StyleSectionBox);
        GUILayout.Label("2) Random start frame only", m_StyleSectionTitle, GUILayout.Height(L.LineH));
        GUILayout.Label(
            "Select a clip name in the list, then all instances play that clip and only randomize the start frame along the timeline.",
            m_StyleLabel,
            GUILayout.Height(L.LineH * 1.65f));
        RebuildOnlyRandomAnimNameListIfNeeded();
        DrawOnlyRandomAnimNameScrollList(L);
        if (GUILayout.Button("All: random start frame (selected clip)", m_StyleButton, GUILayout.Height(L.LineH)))
            ApplyOnlyRandomStartFrameToAll();
        GUILayout.EndVertical();
    }

    void DrawOnlyRandomAnimNameScrollList(GuiLayout L)
    {
        m_OnlyRandomAnimScroll = GUILayout.BeginScrollView(
            m_OnlyRandomAnimScroll,
            GUILayout.Height(L.AnimScrollH),
            GUILayout.ExpandWidth(true));
        if (m_OnlyRandomAnimNameList == null || m_OnlyRandomAnimNameList.Length == 0)
        {
            GUILayout.Label("(no clip names)", m_StyleLabel, GUILayout.ExpandWidth(true));
        }
        else
        {
            for (int i = 0; i < m_OnlyRandomAnimNameList.Length; i++)
            {
                bool active = m_SelectedAnimNameIndex == i;
                GUIStyle row = active ? m_StyleListRowActive : m_StyleListRow;
                if (GUILayout.Button(m_OnlyRandomAnimNameList[i], row, GUILayout.Height(L.LineH), GUILayout.ExpandWidth(true)))
                    m_SelectedAnimNameIndex = i;
            }
        }
        GUILayout.EndScrollView();
    }

    void ApplyOnlyRandomStartFrameToAll()
    {
        if (m_OnlyRandomAnimNameList == null
            || m_OnlyRandomAnimNameList.Length == 0
            || m_SelectedAnimNameIndex >= m_OnlyRandomAnimNameList.Length
            || string.IsNullOrEmpty(m_OnlyRandomAnimNameList[m_SelectedAnimNameIndex]))
        {
            return;
        }
        string name = m_OnlyRandomAnimNameList[m_SelectedAnimNameIndex];
        foreach (var p in m_Players)
        {
            if (p == null)
                continue;
            p.Play(name);
            p.SetRandomStartFrame();
        }
    }

    void DrawFooter(GuiLayout L)
    {
        GUILayout.Label("Instance count: " + m_Players.Count, m_StyleLabel, GUILayout.Height(L.LineH));
    }

    #endregion

    #region Animation name list (dedupe + cache)

    void RebuildOnlyRandomAnimNameListIfNeeded()
    {
        if (m_CachedPlayerCountForAnimNames == m_Players.Count && m_OnlyRandomAnimNameList != null)
            return;

        m_CachedPlayerCountForAnimNames = m_Players.Count;
        var set = new HashSet<string>(StringComparer.Ordinal);
        var tmp = new List<GPUSkinInfoDB>(32);
        foreach (var p in m_Players)
        {
            if (p == null)
                continue;
            p.GetInfos(tmp);
            for (int i = 0; i < tmp.Count; i++)
            {
                if (!string.IsNullOrEmpty(tmp[i].name))
                    set.Add(tmp[i].name);
            }
        }
        var sorted = new List<string>(set);
        sorted.Sort(StringComparer.Ordinal);
        m_OnlyRandomAnimNameList = sorted.ToArray();
        if (m_OnlyRandomAnimNameList.Length == 0)
            m_SelectedAnimNameIndex = 0;
        else
            m_SelectedAnimNameIndex = Mathf.Clamp(m_SelectedAnimNameIndex, 0, m_OnlyRandomAnimNameList.Length - 1);
    }

    #endregion

    #region IMGUI style building

    void EnsureGuiStyles(float scale)
    {
        if (m_StyleBox != null
            && Mathf.Abs(m_CachedStyleScale - scale) < 0.01f
            && m_CachedFontBase == guiFontSizeBase)
        {
            return;
        }

        m_CachedStyleScale = scale;
        m_CachedFontBase = guiFontSizeBase;
        int fontSize = Mathf.Max(12, Mathf.RoundToInt(guiFontSizeBase * scale));

        EnsureHighlightBackground();

        m_StyleBox = new GUIStyle(GUI.skin.box) { fontSize = fontSize };
        m_StyleSectionBox = new GUIStyle(GUI.skin.box)
        {
            fontSize = fontSize,
            padding = new RectOffset(10, 10, 8, 8),
            margin = new RectOffset(2, 2, 2, 2)
        };
        m_StyleLabel = new GUIStyle(GUI.skin.label) { fontSize = fontSize, wordWrap = true };
        m_StyleSectionTitle = new GUIStyle(m_StyleLabel)
        {
            fontStyle = FontStyle.Bold,
            fontSize = Mathf.Max(12, fontSize)
        };
        m_StyleButton = new GUIStyle(GUI.skin.button) { fontSize = fontSize, wordWrap = true };

        m_StyleListRow = new GUIStyle(GUI.skin.label)
        {
            fontSize = fontSize,
            wordWrap = true,
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(10, 10, 6, 6)
        };
        m_StyleListRowActive = new GUIStyle(m_StyleListRow)
        {
            normal = { background = m_HighlightBackground, textColor = Color.white },
            hover = { background = m_HighlightBackground, textColor = Color.white },
            active = { background = m_HighlightBackground, textColor = Color.white }
        };
    }

    void EnsureHighlightBackground()
    {
        if (m_HighlightBackground != null)
            return;
        m_HighlightBackground = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        m_HighlightBackground.hideFlags = HideFlags.DontSave;
        m_HighlightBackground.SetPixel(0, 0, new Color(0.18f, 0.45f, 0.72f, 0.65f));
        m_HighlightBackground.Apply(false, false);
    }

    #endregion

    #region Spawning

    void InitGpuSkinList(GameObject go)
    {
        if (go == null)
            return;

        for (int i = 0; i < kInstancesPerSourcePrefab; i++)
        {
            var instGo = GameObject.Instantiate(go);
            if (instGo == null)
                continue;

            instGo.name = go.name + i;
            instGo.transform.SetParent(transform);

            float eulerY = Random.Range(0, 360f);
            instGo.transform.localEulerAngles = new Vector3(0f, eulerY, 0f);

            float scale = Random.Range(kPrefabScaleMin, kPrefabScaleMax);
            instGo.transform.localScale = new Vector3(scale, scale, scale);

            float x = Random.Range(-kSpawnPosRange, kSpawnPosRange);
            float z = Random.Range(-kSpawnPosRange, kSpawnPosRange);
            instGo.transform.localPosition = new Vector3(x, 0f, z);

            var script = instGo.GetComponentInChildren<GPUSkinPlayerBase>();
            if (script != null)
                m_Players.Add(script);
        }
    }

    #endregion
}
