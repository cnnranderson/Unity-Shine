﻿using UnityEngine;
using System.Collections;
using UnityEditor;

namespace CreativeSpore.SuperTilemapEditor
{
    public class TileSelectionWindow : EditorWindow
    {

        #region Static Methods & Fields
        public static TileSelectionWindow Instance
        {
            get { return s_instance; }
        }
        private static TileSelectionWindow s_instance;

        [MenuItem("Window/SuperTilemapEditor/Tile Palette Window")]
        static void Init()
        {
            Show(null);
        }

        public static void Show(Tileset tileset)
        {
            s_instance = (TileSelectionWindow)EditorWindow.GetWindow(typeof(TileSelectionWindow), false, "Tile Palette", true);
            s_instance.m_tilesetControl.Tileset = tileset;
            if (tileset == null)
            {
                s_instance.OnSelectionChange();
            }
            s_instance.wantsMouseMove = true;
        }
        #endregion

        public TilesetControl TilesetControl { get { return m_tilesetControl; } }

        [SerializeField]
        TilesetControl m_tilesetControl = new TilesetControl();

        private int m_pingFramesLeft = 0;
        private static int s_pingFrameNb = 15;

        public void Ping()
        {
            m_pingFramesLeft = s_pingFrameNb;
        }

        void OnSelectionChange()
        {
            if (Selection.activeObject is Tileset)
            {
                m_tilesetControl.Tileset = Selection.activeObject as Tileset;
            }
            else if (Selection.activeObject is TilesetBrush)
            {
                m_tilesetControl.Tileset = (Selection.activeObject as TilesetBrush).Tileset;
            }
            else if (Selection.activeObject is GameObject)
            {
                Tilemap tilemap = (Selection.activeObject as GameObject).GetComponent<Tilemap>();
                if (tilemap != null)
                {
                    m_tilesetControl.Tileset = tilemap.Tileset;
                }
            }
            Repaint();
        }

        void OnGUI()
        {
            Event e = Event.current;
            if (e.type == EventType.Repaint)
            {
                if (m_pingFramesLeft > 0)
                {
                    --m_pingFramesLeft;
                }
            }

            if (m_pingFramesLeft > 0)
            {
                float alpha = 1f - Mathf.Abs(2f * ((float)m_pingFramesLeft / s_pingFrameNb) - 1f);
                GUI.color = new Color(1f, 1f, 0f, alpha);
                GUI.DrawTexture(new Rect(0, 0, maxSize.x, maxSize.y), EditorGUIUtility.whiteTexture, ScaleMode.ScaleToFit, true);
                GUI.color = Color.white;
            }

            if (m_tilesetControl.Tileset == null)
            {
                EditorGUILayout.HelpBox("Select a tileset to edit.", MessageType.Info);
                if (Event.current.type == EventType.Repaint)
                {
                    OnSelectionChange();
                }
                Repaint();
                return;
            }

            m_tilesetControl.Display();

            Repaint();
        }
    }
}