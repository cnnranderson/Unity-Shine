using UnityEngine;
using System.Collections;
using UnityEditor;
using UnityEditorInternal;

namespace CreativeSpore.SuperTilemapEditor
{
    [CustomEditor(typeof(Tileset))]
    public class TilesetEditor : Editor
    {
        [MenuItem("Assets/Create/SuperTilemapEditor/Tileset")]
        public static Tileset CreateTileset()
        {
            return EditorUtils.CreateAssetInSelectedDirectory<Tileset>();
        }

        private TilesetControl m_tilesetCtrl = new TilesetControl();

        static bool s_gridFoldOut = true;
        static bool s_tilePaletteFoldOut = true;

        public override void OnInspectorGUI()
        {
            Tileset tileset = (Tileset)target;
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("AtlasTexture"));
            if(EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                tileset.UpdateTilesetConfigFromAtlasImportSettings();
            }
            if (tileset.AtlasTexture == null)
            {
                EditorGUILayout.HelpBox("Select an atlas texture for the tileset", MessageType.Info);
            }
            else
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_pixelsPerUnit"));
                s_gridFoldOut = EditorGUILayout.Foldout(s_gridFoldOut, "Grid");
                if (s_gridFoldOut)
                {
                    tileset.TilePxSize = _GetPositiveIntVect2(EditorGUILayout.Vector2Field("Pixel Size", tileset.TilePxSize));
                    tileset.SliceOffset = _GetPositiveIntVect2(EditorGUILayout.Vector2Field("Offset", tileset.SliceOffset));
                    tileset.SlicePadding = _GetPositiveIntVect2(EditorGUILayout.Vector2Field("Padding", tileset.SlicePadding));

                    if (GUILayout.Button("Slice Atlas"))
                    {
                        tileset.Slice();
                    }
                }

                s_tilePaletteFoldOut = EditorGUILayout.Foldout(s_tilePaletteFoldOut, "Tile Palette");
                if (s_tilePaletteFoldOut)
                {
                    m_tilesetCtrl.Tileset = tileset;
                    m_tilesetCtrl.Display();
                    Repaint();
                }
            }
            serializedObject.ApplyModifiedProperties();
            if (GUI.changed)
            {
                EditorUtility.SetDirty(target);
            }
        }

        private Vector2 _GetPositiveIntVect2(Vector2 v)
        {
            return new Vector2(Mathf.Max(0, (int)v.x), Mathf.Max(0, (int)v.y));
        }

        public static Vector2 DrawTilesetLayout(Tileset tileset, Vector2 scrollPos)
        {
            scrollPos = GUILayout.BeginScrollView(scrollPos);
            if (tileset.Tiles != null)
            {
                GUILayout.Space((tileset.VisualTileSize.y + tileset.VisualTilePadding) * (1 + (tileset.Tiles.Count - 1) / tileset.TileRowLength));
                int curTileIdx = 0;
                for (int i = 0; i < tileset.Tiles.Count; ++i)
                {
                    Tile tile = tileset.Tiles[i];
                    int tx = curTileIdx % tileset.TileRowLength;
                    int ty = curTileIdx / tileset.TileRowLength;
                    Rect rVisualTile = new Rect(tx * (tileset.VisualTileSize.x + tileset.VisualTilePadding), ty * (tileset.VisualTileSize.y + tileset.VisualTilePadding), tileset.VisualTileSize.x, tileset.VisualTileSize.y);
                    bool isRightMouseReleased = Event.current.type == EventType.MouseUp && Event.current.button == 0;
                    if (isRightMouseReleased && rVisualTile.Contains(Event.current.mousePosition))
                    {
                        tileset.SelectedTileId = curTileIdx;
                    }
                    GUI.DrawTextureWithTexCoords(rVisualTile, tileset.AtlasTexture, tile.uv, true);
                    if (tileset.SelectedTileId == i)
                    {
                        Rect rSelection = new Rect(tx * (tileset.VisualTileSize.x + tileset.VisualTilePadding), ty * (tileset.VisualTileSize.y + tileset.VisualTilePadding), (tileset.VisualTileSize.x + tileset.VisualTilePadding), (tileset.VisualTileSize.y + tileset.VisualTilePadding));
                        HandlesEx.DrawRectWithOutline(rSelection, new Color(0f, 0f, 0f, 0.1f), new Color(1f, 1f, 0f, 1f));
                    }
                    ++curTileIdx;
                }
            }
            GUILayout.EndScrollView();
            return scrollPos;
        }

        public static void AddAllBrushesFoundInTheProject(Tileset tileset)
        {
            // Load all TilesetBrush assets found in the project
            string[] guids = AssetDatabase.FindAssets("t:TilesetBrush");
            foreach (string brushGuid in guids)
            {
                string brushAssetPath = AssetDatabase.GUIDToAssetPath(brushGuid);
                AssetDatabase.LoadAssetAtPath<TilesetBrush>(brushAssetPath);
            }
            // Get all loaded brushes
            TilesetBrush[] brushesFound = (TilesetBrush[])Resources.FindObjectsOfTypeAll(typeof(TilesetBrush));
            for (int i = 0; i < brushesFound.Length; ++i)
            {
                if (brushesFound[i].Tileset == tileset)
                {
                    tileset.AddBrush(brushesFound[i]);
                }
            }
        }

        public static ReorderableList CreateTileViewReorderableList(Tileset tileset)
        {
            ReorderableList tileViewRList = new ReorderableList( tileset.TileViews, typeof(TileView), true, true, true, true);
            tileViewRList.onAddDropdownCallback = (Rect buttonRect, ReorderableList l) =>
            {
                GenericMenu menu = new GenericMenu();
                GenericMenu.MenuFunction addTileSelectionFunc = () =>
                {
                    TileSelection tileSelection = tileset.TileSelection.Clone();
                    tileSelection.FlipVertical(); // flip vertical to fit the tileset coordinate system ( from top to bottom )
                    tileset.AddTileView("new TileView", tileSelection);
                    EditorUtility.SetDirty(tileset);
                };
                GenericMenu.MenuFunction removeAllTileViewsFunc = () =>
                {
                    if (EditorUtility.DisplayDialog("Warning!", "Are you sure you want to delete all the TileViews?", "Yes", "No"))
                    {
                        tileset.RemoveAllTileViews();
                        EditorUtility.SetDirty(tileset);
                    }
                };
                if (tileset.TileSelection == null)
                    menu.AddDisabledItem(new GUIContent("Add Tile Selection to TileView"));
                else
                    menu.AddItem(new GUIContent("Add Tile Selection to TileView"), false, addTileSelectionFunc);
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("Remove All TileViews"), false, removeAllTileViewsFunc);
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("Sort By Name"), false, tileset.SortTileViewsByName);
                menu.ShowAsContext();
            };
            tileViewRList.onRemoveCallback = (ReorderableList list) =>
            {
                if (EditorUtility.DisplayDialog("Warning!", "Are you sure you want to delete the TileView?", "Yes", "No"))
                {
                    ReorderableList.defaultBehaviours.DoRemoveButton(list);
                    EditorUtility.SetDirty(tileset);
                }
            };
            tileViewRList.drawHeaderCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, "TileViews", EditorStyles.boldLabel);
                Texture2D btnTexture = tileViewRList.elementHeight == 0f ? EditorGUIUtility.FindTexture("winbtn_win_max_h") : EditorGUIUtility.FindTexture("winbtn_win_min_h");
                if (GUI.Button(new Rect(rect.width - rect.height, rect.y, rect.height, rect.height), btnTexture, EditorStyles.label))
                {
                    tileViewRList.elementHeight = tileViewRList.elementHeight == 0f ? EditorGUIUtility.singleLineHeight : 0f;
                    tileViewRList.draggable = tileViewRList.elementHeight > 0f;
                }
            };
            tileViewRList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                if (tileViewRList.elementHeight == 0f)
                    return;
                Rect rLabel = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
                TileView tileView = tileViewRList.list[index] as TileView;
                if (index == tileViewRList.index)
                {
                    string newName = EditorGUI.TextField(rLabel, tileView.name);
                    if (newName != tileView.name)
                    {
                        tileset.RenameTileView(tileView.name, newName);
                    }
                }
                else
                {
                    EditorGUI.LabelField(rLabel, tileView.name);
                }
            };

            return tileViewRList;
        }

        public static ReorderableList CreateBrushReorderableList(Tileset tileset)
        {
            ReorderableList brushRList = new ReorderableList(tileset.Brushes, typeof(Tileset.BrushContainer), true, true, true, true);            
            brushRList.displayAdd = brushRList.displayRemove = false;
            brushRList.drawHeaderCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, "Brushes", EditorStyles.boldLabel);                
            };
            brushRList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                Tileset.BrushContainer brushContainer = tileset.Brushes[index];
                Rect rTile = rect; rTile.width = rTile.height = tileset.VisualTileSize.y;
                Rect tileUV = brushContainer.BrushAsset.GetAnimUV();
                if (tileUV != default(Rect))
                {
                    GUI.Box(new Rect(rTile.position - Vector2.one, rTile.size + 2 * Vector2.one), "");
                    GUI.DrawTextureWithTexCoords(rTile, tileset.AtlasTexture, tileUV, true);
                }

                Rect rTileId = rect;
                rTileId.x += rTile.width + 20; rTileId.width -= rTile.width + 20;
                rTileId.height = rect.height / 2;
                GUI.Label(rTileId, "Id(" + brushContainer.Id + ")");
            };

            return brushRList;
        }
    }
}