using UnityEngine;
using System.Collections;
using UnityEditor;

namespace CreativeSpore.SuperTilemapEditor
{

    public class TilePropertiesWindow : EditorWindow
    {

        public static TilePropertiesWindow Instance
        {
            get { return s_instance; }
        }
        private static TilePropertiesWindow s_instance;

        [MenuItem("Window/SuperTilemapEditor/Tile Properties Window")]
        static void Init()
        {
            Show(null);
        }

        public static void Show(Tileset tileset)
        {
            s_instance = (TilePropertiesWindow)EditorWindow.GetWindow(typeof(TilePropertiesWindow), false, "Tile Properties", true);
            s_instance.m_tilePropertiesControl.Tileset = tileset;
            if (tileset == null)
            {
                s_instance.OnSelectionChange();
            }
            s_instance.wantsMouseMove = true;
        }

        public TilePropertiesControl TilePropertiesControl { get { return m_tilePropertiesControl; } }

        [SerializeField]
        TilePropertiesControl m_tilePropertiesControl = new TilePropertiesControl();

        void OnSelectionChange()
        {
            if (Selection.activeObject is Tileset)
            {
                m_tilePropertiesControl.Tileset = Selection.activeObject as Tileset;
            }
            else if (Selection.activeObject is TilesetBrush)
            {
                m_tilePropertiesControl.Tileset = (Selection.activeObject as TilesetBrush).Tileset;
            }
            else if (Selection.activeObject is GameObject)
            {
                Tilemap tilemap = (Selection.activeObject as GameObject).GetComponent<Tilemap>();
                if (tilemap != null)
                {
                    m_tilePropertiesControl.Tileset = tilemap.Tileset;
                }
            }
            Repaint();
        }

        void OnGUI()
        {
            if (m_tilePropertiesControl.Tileset == null)
            {
                EditorGUILayout.HelpBox("Select a tileset to edit.", MessageType.Info);
                if (Event.current.type == EventType.Repaint)
                {
                    OnSelectionChange();
                }
                Repaint();
                return;
            }

            m_tilePropertiesControl.Display();

            Repaint();
        }
    }
}