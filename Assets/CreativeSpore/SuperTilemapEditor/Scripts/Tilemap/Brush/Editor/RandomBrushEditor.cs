using UnityEngine;
using System.Collections;
using UnityEditor;
using UnityEditorInternal;

namespace CreativeSpore.SuperTilemapEditor
{

    [CustomEditor(typeof(RandomBrush))]
    public class RandomBrushEditor : Editor
    {
        [MenuItem("Assets/Create/SuperTilemapEditor/Brush/RandomBrush")]
        public static RandomBrush CreateAsset()
        {
            return EditorUtils.CreateAssetInSelectedDirectory<RandomBrush>();
        }

        RandomBrush m_brush;
        ReorderableList m_randTileList;
        Tileset m_prevTileset;

        void OnEnable()
        {
            m_brush = (RandomBrush)target;
            if (m_brush.Tileset != null)
            {
                m_brush.Tileset.OnTileSelected += OnTileSelected;
            }

            m_randTileList = new ReorderableList(serializedObject, serializedObject.FindProperty("RandomTiles"), true, true, true, true);
            m_randTileList.drawHeaderCallback += (Rect rect) =>
            {
                EditorGUI.LabelField(rect, "Random Tiles", EditorStyles.boldLabel);
            };
            m_randTileList.drawElementCallback += (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                Rect rTile = rect; rTile.width = rTile.height = m_brush.Tileset.VisualTileSize.y;
                int tileId = m_brush.RandomTiles[index];
                if (tileId != Tileset.k_TileId_Empty)
                {
                    Rect tileUV = m_brush.Tileset.Tiles[tileId].uv;
                    GUI.Box(new Rect(rTile.position - Vector2.one, rTile.size + 2 * Vector2.one), "");
                    GUI.DrawTextureWithTexCoords(rTile, m_brush.Tileset.AtlasTexture, tileUV, true);
                }

                Rect rTileId = rect;
                rTileId.x += rTile.width + 20; rTileId.width -= rTile.width + 20;
                rTileId.height = rect.height / 2;
                GUI.Label(rTileId, "Id(" + tileId + ")");
            };
            m_randTileList.onSelectCallback += (ReorderableList list) =>
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
            if (m_randTileList.index >= 0 && m_randTileList.index < m_brush.RandomTiles.Count)
            {
                m_brush.RandomTiles[m_randTileList.index] = newTileId;
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

            EditorGUILayout.Space();

            GUILayoutUtility.GetRect(1f, 1f, GUILayout.Width(visualTileSize.x), GUILayout.Height(visualTileSize.y));
            Rect rSelectedTile = GUILayoutUtility.GetLastRect();
            int currTileId = m_brush.PreviewTileId();
            if (currTileId != Tileset.k_TileId_Empty)
            {
                Rect tileUV = m_brush.Tileset.Tiles[currTileId].uv;
                rSelectedTile.center = new Vector2(EditorGUIUtility.currentViewWidth / 2, rSelectedTile.center.y);
                GUI.Box(new Rect(rSelectedTile.position - Vector2.one, rSelectedTile.size + 2 * Vector2.one), "");
                GUI.DrawTextureWithTexCoords(rSelectedTile, m_brush.Tileset.AtlasTexture, tileUV, true);
            }

            EditorGUILayout.Space();

            // Draw List
            m_randTileList.elementHeight = visualTileSize.y + 10f;
            if (!m_randTileList.HasKeyboardControl())
            {
                m_randTileList.index = -1;
            }
            m_randTileList.DoLayoutList();

            EditorGUILayout.HelpBox("Select a tile from list and then select a tile from tile selection window.", MessageType.Info);
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