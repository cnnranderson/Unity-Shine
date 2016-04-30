using UnityEngine;
using System.Collections;
using UnityEditor;

namespace CreativeSpore.SuperTilemapEditor
{

    public class BrushTileGridControl
    {
        [System.Flags]
        public enum eNeighbourFlags
        {
            North = 1,
            East = 1 << 1,
            South = 1 << 2,
            West = 1 << 3,
        }

        static Texture2D[] s_tileSymbolTextures = null;

        public bool ShowHelpBox = true;

        public static int GetTileNeighboursFlags(bool west, bool south, bool east, bool north)
        {
            return (west ? 1 << 3 : 0) | (south ? 1 << 2 : 0) | (east ? 1 << 1 : 0) | (north ? 1 << 0 : 0);
        }

        public static Texture2D GetTileSymbolTexture(bool west, bool south, bool east, bool north)
        {
            int idx = GetTileNeighboursFlags(west, south, east, north);
            return GetTileSymbolTexture(idx);
        }

        public static Texture2D GetTileSymbolTexture(int tileNeighboursFlags)
        {
            if (s_tileSymbolTextures == null)
            {
                s_tileSymbolTextures = new Texture2D[16];
                for (int i = 0; i < s_tileSymbolTextures.Length; ++i)
                {
                    Texture2D tex = new Texture2D(3, 3);
                    tex.hideFlags = HideFlags.DontSave;
                    tex.anisoLevel = 0;
                    tex.filterMode = FilterMode.Point;
                    s_tileSymbolTextures[i] = tex;

                    bool west = ((eNeighbourFlags)i & eNeighbourFlags.West) != 0;
                    bool south = ((eNeighbourFlags)i & eNeighbourFlags.South) != 0;
                    bool east = ((eNeighbourFlags)i & eNeighbourFlags.East) != 0;
                    bool north = ((eNeighbourFlags)i & eNeighbourFlags.North) != 0;

                    Color x = new Color(0, 0, 0, 0.2f);
                    Color d = new Color(0, 0, 0, 0.1f);
                    Color o = new Color(1, 1, 1, 0.1f);
                    Color[] colors = new Color[]
                {
                    o/*6*/, o/*7*/, o/*8*/,
                    o/*3*/, o/*4*/, o/*5*/,
                    o/*0*/, o/*1*/, o/*2*/,
                };
                    /*
                    if( i == 0 )
                    {
                        colors[4] = x;
                    }
                    else*/
                    {
                        // lateral
                        {
                            if (!north)
                            {
                                colors[6] = colors[7] = colors[8] = x;
                            }
                            if (!east)
                            {
                                colors[8] = colors[5] = colors[2] = x;
                            }
                            if (!south)
                            {
                                colors[0] = colors[1] = colors[2] = x;
                            }
                            if (!west)
                            {
                                colors[0] = colors[3] = colors[6] = x;
                            }
                        }
                        // diagonals
                        {
                            if (north && west)
                            {
                                colors[6] = d;
                            }
                            if (north && east)
                            {
                                colors[8] = d;
                            }
                            if (east && south)
                            {
                                colors[2] = d;
                            }
                            if (south && west)
                            {
                                colors[0] = d;
                            }
                        }
                    }

                    tex.SetPixels(colors);
                    tex.Apply();
                }
            }
            return s_tileSymbolTextures[tileNeighboursFlags];
        }

        ~BrushTileGridControl()
        {
            Tileset = null;
        }

        public Tileset Tileset
        {
            get
            {
                return m_tileset;
            }

            set
            {
                if (m_tileset != value)
                {
                    if (m_tileset != null)
                    {
                        m_tileset.OnTileSelected -= OnTileSelected;
                    }
                    if (value != null)
                    {
                        value.OnTileSelected += OnTileSelected;
                    }
                    m_tileset = value;
                }
            }
        }

        Tileset m_tileset;
        int m_selectedTileIdx = -1;
        int m_tileIdOff = 0;
        int m_tileIdOffSkipIdx = 0;
        int[] m_brushTileIds;
        Object m_target;
        bool m_hasChanged = false;

        void OnTileSelected(Tileset source, int prevTileIdx, int newTileIdx)
        {
            if (m_selectedTileIdx >= 0)
            {
                m_tileIdOff = 0;
                if (m_brushTileIds[m_selectedTileIdx] != Tileset.k_TileId_Empty && newTileIdx != m_brushTileIds[m_selectedTileIdx])
                {
                    m_tileIdOff = newTileIdx - m_brushTileIds[m_selectedTileIdx];
                    m_tileIdOffSkipIdx = m_selectedTileIdx;
                }
                Undo.RecordObject(m_target, "TileChanged");
                m_brushTileIds[m_selectedTileIdx] = newTileIdx;
                m_hasChanged = true;
            }
            EditorUtility.SetDirty(m_target);
        }

        public void Display(Object target, int[] tileIds, int[] tileIdxMap, int gridWidth, int gridHeight, Vector2 visualTileSize, int[] symbolIdxMap)
        {
            GUI.changed = m_hasChanged;
            m_hasChanged = false;
            m_target = target;
            m_brushTileIds = tileIds;
            Event e = Event.current;
            if (e.isKey && e.keyCode == KeyCode.Delete && m_selectedTileIdx >= 0)
            {
                m_brushTileIds[m_selectedTileIdx] = Tileset.k_TileId_Empty;
                m_hasChanged = true;
                EditorUtility.SetDirty(m_target);
                e.Use();
            }
            bool isRightMouseReleased = e.type == EventType.MouseUp && e.button == 0;
            if (isRightMouseReleased)
            {
                m_selectedTileIdx = -1;
            }
            // Draw Autotile Combination Control
            GUI.backgroundColor = Tileset.BackgroundColor;
            GUILayout.BeginScrollView(Vector2.zero, new GUIStyle("Button"), GUILayout.Width(visualTileSize.x * gridWidth), GUILayout.Height(visualTileSize.y * gridHeight + 1f));
            GUI.backgroundColor = Color.white;
            for (int i = 0; i < gridWidth * gridHeight; ++i)
            {
                int gx = i % gridWidth;
                int gy = i / gridHeight;
                int tileIdx = tileIdxMap[i];
                Rect rVisualTile = new Rect(gx * visualTileSize.x, gy * visualTileSize.y, visualTileSize.x, visualTileSize.y);

                int tileId = m_brushTileIds[tileIdx];
                if (tileId != Tileset.k_TileId_Empty)
                {
                    Rect tileUV = Tileset.Tiles[tileId].uv;
                    GUI.DrawTextureWithTexCoords(rVisualTile, Tileset.AtlasTexture, tileUV, true);
                }
                else if (symbolIdxMap != null)
                {
                    GUI.DrawTexture(rVisualTile, GetTileSymbolTexture((byte)symbolIdxMap[i]), ScaleMode.ScaleToFit, true);
                }

                Color bgColor = new Color(1f - Tileset.BackgroundColor.r, 1f - Tileset.BackgroundColor.g, 1f - Tileset.BackgroundColor.b, Tileset.BackgroundColor.a);
                HandlesEx.DrawRectWithOutline(rVisualTile, m_selectedTileIdx == tileIdx ? new Color(0f, 0f, 0f, 0.1f) : new Color(), m_selectedTileIdx == tileIdx ? new Color(1f, 1f, 0f, 1f) : bgColor);

                if (isRightMouseReleased && rVisualTile.Contains(e.mousePosition))
                {
                    m_selectedTileIdx = tileIdx;
                    EditorWindow wnd = EditorWindow.focusedWindow;
                    TileSelectionWindow.Show(Tileset);
                    TileSelectionWindow.Instance.Ping();
                    wnd.Focus();
                    GUI.FocusControl("");
                }
            }
            GUILayout.EndScrollView();

            bool hasEmptyTiles = ArrayUtility.Contains<int>(m_brushTileIds, Tileset.k_TileId_Empty);
            if (!hasEmptyTiles && m_tileIdOff != 0 && GUILayout.Button("Autocomplete relative to last change"))
            {
                Undo.RecordObject(m_target, "MultipleTileChanged");
                for (int i = 0; i < tileIdxMap.Length; ++i)
                {
                    int tileIdx = tileIdxMap[i];
                    if (tileIdx != m_tileIdOffSkipIdx)
                    {
                        m_brushTileIds[tileIdx] += m_tileIdOff;
                        if (m_brushTileIds[tileIdx] < 0 || m_brushTileIds[tileIdx] >= m_tileset.Tiles.Count)
                        {
                            m_brushTileIds[tileIdx] = Tileset.k_TileId_Empty;
                        }
                    }
                }
                m_tileIdOff = 0;
                EditorUtility.SetDirty(m_target);
            }

            if (ShowHelpBox)
            {
                EditorGUILayout.HelpBox("Select  a tile from the grid, then select a tile from Tile Selection Window to change the tile.\nPress Del key to set an empty tile.", MessageType.Info);
            }
        }
    }
}