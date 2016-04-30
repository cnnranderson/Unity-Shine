using UnityEngine;
using System.Collections;
using UnityEditor;

namespace CreativeSpore.SuperTilemapEditor
{

    [CustomEditor(typeof(RoadBrush))]
    public class RoadBrushEditor : Editor
    {
        [MenuItem("Assets/Create/SuperTilemapEditor/Brush/RoadBrush")]
        public static RoadBrush CreateAsset()
        {
            RoadBrush brush = EditorUtils.CreateAssetInSelectedDirectory<RoadBrush>();
            return brush;
        }

        RoadBrush m_brush;

        BrushTileGridControl m_brushTileGridControl = new BrushTileGridControl();
        void OnEnable()
        {
            m_brush = (RoadBrush)target;
        }

        void OnDisable()
        {
            m_brushTileGridControl.Tileset = null;
        }

        /*
        static char[] s_tileEmptyChar = new char[]
        {
            '°', '╞', '═', '╡',
            '╥', '╔', '╦', '╗',
            '║', '╠', '╬', '╣',
            '╨', '╚', '╩', '╝',
        };
        */
        static int[] s_tileIdxMap = new int[]
        {
            0, 2, 10, 8,
            4, 6, 14, 12,
            5, 7, 15, 13,
            1, 3, 11, 9,
        };

        public override void OnInspectorGUI()
        {
            if (m_brush.Tileset == null)
            {
                EditorGUILayout.HelpBox("Select a tileset first", MessageType.Info);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("Tileset"));
                serializedObject.ApplyModifiedProperties();
                return;
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("Tileset"));

            m_brushTileGridControl.Tileset = m_brush.Tileset;
            m_brushTileGridControl.Display(target, m_brush.TileIds, s_tileIdxMap, 4, 4, m_brush.Tileset.VisualTileSize, s_tileIdxMap);
            EditorGUILayout.Space();

            Repaint();
            serializedObject.ApplyModifiedProperties();
            if (GUI.changed)
            {
                EditorUtility.SetDirty(target);
            }
        }
    }
}