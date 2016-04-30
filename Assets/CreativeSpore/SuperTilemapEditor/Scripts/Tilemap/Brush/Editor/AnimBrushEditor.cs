using UnityEngine;
using System.Collections;
using UnityEditor;
using UnityEditorInternal;
using System.Linq;

namespace CreativeSpore.SuperTilemapEditor
{

    [CustomEditor(typeof(AnimBrush))]
    public class AnimBrushEditor : Editor
    {
        [MenuItem("Assets/Create/SuperTilemapEditor/Brush/AnimBrush")]
        public static AnimBrush CreateAsset()
        {
            return EditorUtils.CreateAssetInSelectedDirectory<AnimBrush>();
        }

        AnimBrush m_brush;
        ReorderableList m_frameList;
        Tileset m_prevTileset;

        void OnEnable()
        {
            m_brush = (AnimBrush)target;
            if (m_brush.Tileset != null)
            {
                m_brush.Tileset.OnTileSelected += OnTileSelected;
            }

            m_frameList = new ReorderableList(serializedObject, serializedObject.FindProperty("AnimFrames"), true, true, true, true);
            m_frameList.drawHeaderCallback += (Rect rect) =>
            {
                EditorGUI.LabelField(rect, "Anim Frames", EditorStyles.boldLabel);
            };
            m_frameList.drawElementCallback += (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                Rect rTile = rect; rTile.width = rTile.height = m_brush.Tileset.VisualTileSize.y;
                int tileId = m_brush.AnimFrames[index].tileId;
                if (tileId != Tileset.k_TileId_Empty)
                {
                    Rect tileUV = m_brush.Tileset.Tiles[tileId].uv;
                    tileUV.position += m_brush.AnimFrames[index].UVOffset;
                    GUI.Box(new Rect(rTile.position - Vector2.one, rTile.size + 2 * Vector2.one), "");
                    GUI.DrawTextureWithTexCoords(rTile, m_brush.Tileset.AtlasTexture, tileUV, true);
                }

                Rect rTileId = rect;
                rTileId.x += rTile.width + 20; rTileId.width -= rTile.width + 20;
                rTileId.height = rect.height / 2;
                GUI.Label(rTileId, "Id(" + tileId + ")");
                Vector2 uvOffset = m_brush.AnimFrames[index].UVOffset;
                Vector2 UvPxOffset = new Vector2(uvOffset.x / m_brush.Tileset.AtlasTexture.texelSize.x, uvOffset.y / m_brush.Tileset.AtlasTexture.texelSize.y);
                rTileId.y += rTileId.height;
                GUI.Label(rTileId, "UVOffset(" + UvPxOffset.x + "," + UvPxOffset.y + ")");

                int btnIdx = 0;
                float btnSize = rect.height / 2;
                for (int gy = 0; gy < 2; ++gy)
                {
                    for (int gx = 0; gx < 3; ++gx, ++btnIdx)
                    {
                        Rect btnRect = new Rect(rect.x + rect.width - (gx + 1) * btnSize, rect.y + gy * rect.height / 2f, btnSize, btnSize);
                        switch (btnIdx)
                        {
                            case 0: if (GUI.Button(btnRect, "R")) uvOffset = Vector2.zero; break;
                            case 1: if (GUI.Button(btnRect, "▲")) uvOffset.y -= m_brush.Tileset.AtlasTexture.texelSize.y; break;
                            case 3: if (GUI.Button(btnRect, "▶")) uvOffset.x -= m_brush.Tileset.AtlasTexture.texelSize.x; break;
                            case 4: if (GUI.Button(btnRect, "▼")) uvOffset.y += m_brush.Tileset.AtlasTexture.texelSize.y; break;
                            case 5: if (GUI.Button(btnRect, "◀")) uvOffset.x += m_brush.Tileset.AtlasTexture.texelSize.x; break;
                        }
                    }
                }
                m_brush.AnimFrames[index].UVOffset = uvOffset;

            };
            m_frameList.onSelectCallback += (ReorderableList list) =>
            {
                TileSelectionWindow.Show(m_brush.Tileset);
                TileSelectionWindow.Instance.Ping();
            };
        }

        void OnDisable()
        {
            if (m_brush.Tileset != null)
            {
                m_brush.Tileset.OnTileSelected -= OnTileSelected;
            }
        }

        private void OnTileSelected(Tileset source, int prevTileId, int newTileId)
        {
            if (m_frameList.index >= 0 && m_frameList.index < m_brush.AnimFrames.Count)
            {
                m_brush.AnimFrames[m_frameList.index].tileId = newTileId;
            }
            EditorUtility.SetDirty(target);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (m_prevTileset != m_brush.Tileset)
            {
                OnDisable();
                OnEnable();
            }
            m_prevTileset = m_brush.Tileset;

            if (m_brush.Tileset == null)
            {
                EditorGUILayout.HelpBox("Select a tileset first", MessageType.Info);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("Tileset"));
                serializedObject.ApplyModifiedProperties();
                return;
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("Tileset"));
            Vector2 visualTileSize = m_brush.Tileset.VisualTileSize;

            EditorGUILayout.PropertyField(serializedObject.FindProperty("AnimFPS"));

            TileSelection tileSelection = ((TilesetBrush)target).Tileset.TileSelection;
            if (tileSelection != null)
            {
                if( GUILayout.Button("Add tile selection as animation frames") )
                {
                    ((AnimBrush)target).AnimFrames.AddRange(tileSelection.tileIds.Select(x => new AnimBrush.TileAnimFrame() { tileId = x }));
                }
            }

            EditorGUILayout.Space();

            GUILayoutUtility.GetRect(1f, 1f, GUILayout.Width(visualTileSize.x), GUILayout.Height(visualTileSize.y));
            Rect rSelectedTile = GUILayoutUtility.GetLastRect();
            int currTileId = m_brush.PreviewTileId();
            if (currTileId != Tileset.k_TileId_Empty)
            {
                Rect tileUV = m_brush.GetAnimUV();
                rSelectedTile.center = new Vector2(EditorGUIUtility.currentViewWidth / 2, rSelectedTile.center.y);
                GUI.Box(new Rect(rSelectedTile.position - Vector2.one, rSelectedTile.size + 2 * Vector2.one), "");
                GUI.DrawTextureWithTexCoords(rSelectedTile, m_brush.Tileset.AtlasTexture, tileUV, true);
            }

            EditorGUILayout.Space();

            // Draw Frames List
            m_frameList.elementHeight = visualTileSize.y + 10f;
            if (!m_frameList.HasKeyboardControl())
            {
                m_frameList.index = -1;
            }
            m_frameList.DoLayoutList();

            EditorGUILayout.HelpBox("Select a frame from list and then select a tile from tile selection window.", MessageType.Info);
            EditorGUILayout.HelpBox("Add and Remove tiles with '+' and '-' buttons.", MessageType.Info);

            Repaint();
            serializedObject.ApplyModifiedProperties();
            if (GUI.changed)
            {
                EditorUtility.SetDirty(target);
            }
        }
    }
}