using UnityEngine;
using System.Collections;
using UnityEditor;

namespace CreativeSpore.SuperTilemapEditor
{

    [CustomEditor(typeof(A2X2Brush))]
    public class A2X2BrushEditor : Editor
    {
        [MenuItem("Assets/Create/SuperTilemapEditor/Brush/A2X2Brush")]
        public static A2X2Brush CreateAsset()
        {
            return EditorUtils.CreateAssetInSelectedDirectory<A2X2Brush>();
        }

        A2X2Brush m_brush;

        BrushTileGridControl m_brushTileGridControl = new BrushTileGridControl();
        void OnEnable()
        {
            m_brush = (A2X2Brush)target;
        }

        void OnDisable()
        {
            m_brushTileGridControl.Tileset = null; // avoid receiving OnTileSelection
        }
        /*
        static char[] s_tileEmptyChar = new char[]
        {
            '╔', '╗',
            '╚', '╝',
        };
        */
        static int[] s_tileIdxMap = new int[]
        {
            2, 3,
            0, 1,
        };
            static int[] s_symbolIdxMap = new int[]
        {
            6, 12,
            3, 9,
        };

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (m_brush.Tileset == null)
            {
                EditorGUILayout.HelpBox("Select a tileset first", MessageType.Info);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("Tileset"));
                serializedObject.ApplyModifiedProperties();
                return;
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("Tileset"));
            EditorGUILayout.Space();

            m_brushTileGridControl.Tileset = m_brush.Tileset;
            m_brushTileGridControl.Display(target, m_brush.TileIds, s_tileIdxMap, 2, 2, m_brush.Tileset.VisualTileSize, s_symbolIdxMap);

            Repaint();
            serializedObject.ApplyModifiedProperties();
            if (GUI.changed)
            {
                EditorUtility.SetDirty(target);
            }
        }
    }
}