using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CreativeSpore.SuperTilemapEditor
{

    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshFilter))]
    [AddComponentMenu("")] // Disable attaching it to a gameobject
    [ExecuteInEditMode] //NOTE: this is needed so OnDestroy is called and there is no memory leaks
    public class TilemapChunk : MonoBehaviour
    {
        #region Public Properties
        public Tileset Tileset
        {
            get { return ParentTilemap.Tileset; }
        }

        public Tilemap ParentTilemap;
        /// <summary>
        /// The x position inside the parent tilemap
        /// </summary>
        public int GridPosX;
        /// <summary>
        /// The y position inside the parent tilemap
        /// </summary>
        public int GridPosY;

        public int GridWidth { get { return m_width; } }
        public int GridHeight { get { return m_height; } }

        public int SortingLayerID
        {
            get { return m_meshRenderer.sortingLayerID; }
            set { m_meshRenderer.sortingLayerID = value; }
        }

        public string SortingLayerName
        {
            get { return m_meshRenderer.sortingLayerName; }
            set { m_meshRenderer.sortingLayerName = value; }
        }

        public int OrderInLayer
        {
            get { return m_meshRenderer.sortingOrder; }
            set { m_meshRenderer.sortingOrder = value; }
        }

        public MeshFilter MeshFilter { get { return m_meshFilter; } }

        public Vector2 CellSize { get { return ParentTilemap.CellSize; } }
        /// <summary>
        /// Stretch the size of the tile UV the pixels indicated in this value. This trick help to fix pixel artifacts.
        /// Most of the time a value of 0.5 pixels will be fine, but in case of a big zoom-out level, a higher value will be necessary
        /// </summary>
        public float InnerPadding { get { return ParentTilemap.InnerPadding; } }
        #endregion

        #region Private Fields
        [SerializeField, HideInInspector]
        private MeshFilter m_meshFilter;
        [SerializeField, HideInInspector]
        private MeshRenderer m_meshRenderer;

        [SerializeField, HideInInspector]
        private int m_width = 8;
        [SerializeField, HideInInspector]
        private int m_height = 4;
        [SerializeField, HideInInspector]
        private List<uint> m_tileDataList = new List<uint>();
        [SerializeField, HideInInspector]
        private List<TileObjData> m_tileObjList = new List<TileObjData>();

        private List<GameObject> m_tileObjToBeRemoved = new List<GameObject>();

        [System.Serializable]
        private class TileObjData
        {
            public int tilePos;
            public TilePrefabData tilePrefabData;
            public GameObject obj = null;
        }        

        //+++ MeshCollider
        [SerializeField, HideInInspector]
        private MeshCollider m_meshCollider;
        private List<Vector3> m_meshCollVertices;
        private List<int> m_meshCollTriangles;
        //---

        //+++ 2D Edge colliders
        [SerializeField]
        private bool m_hasEdgeColliders;
        //---

        private List<Vector3> m_vertices;
        private List<Vector2> m_uv;
        private List<int> m_triangles;
        // private List<Color32> m_colors; TODO: add color vertex support

        struct AnimTileData
        {
            public int VertexIdx;
            public TilesetBrush Brush;
        }
        private List<AnimTileData> m_animatedTiles = new List<AnimTileData>();
        #endregion

        #region Monobehaviour Methods

        void OnWillRenderObject()
        {
            if (m_animatedTiles.Count > 0) //TODO: add fps attribute to update animated tiles when necessary
            {

                Dictionary<TilesetBrush, Rect> animTileCache = new Dictionary<TilesetBrush, Rect>();
                for (int i = 0; i < m_animatedTiles.Count; ++i)
                {
                    AnimTileData animTileData = m_animatedTiles[i];
                    Rect tileUV;
                    if (!animTileCache.TryGetValue(animTileData.Brush, out tileUV))
                    {
                        //NOTE: GetAnimUV will be called only once per brush, because after this call, the brush will be in the cache dictionary
                        tileUV = animTileData.Brush.GetAnimUV();
                        animTileCache[animTileData.Brush] = tileUV;
                    }
                    float u0 = tileUV.xMin + Tileset.AtlasTexture.texelSize.x * InnerPadding;
                    float v0 = tileUV.yMin + Tileset.AtlasTexture.texelSize.y * InnerPadding;
                    float u1 = tileUV.xMax - Tileset.AtlasTexture.texelSize.x * InnerPadding;
                    float v1 = tileUV.yMax - Tileset.AtlasTexture.texelSize.y * InnerPadding;
                    m_uv[animTileData.VertexIdx + 0] = new Vector2(u0, v0);
                    m_uv[animTileData.VertexIdx + 1] = new Vector2(u0, v1);
                    m_uv[animTileData.VertexIdx + 2] = new Vector2(u1, v1);
                    m_uv[animTileData.VertexIdx + 3] = new Vector2(u1, v0);
                }
                m_meshFilter.sharedMesh.uv = m_uv.ToArray();

            }
        }

        // NOTE: OnDestroy is not called in editor without [ExecuteInEditMode]
        void OnDestroy()
        {
            //avoid memory leak
            DestroyMesh();
            DestroyColliderMesh();
        }

        // This is needed to refresh tilechunks after undo / redo actions
        static bool s_isOnValidate = false; // fix issue when destroying unused resources from the invalidate call
        void OnValidate()
        {
            Event e = Event.current;
            if (e != null && e.type == EventType.ExecuteCommand && (e.commandName == "Duplicate" || e.commandName == "Paste"))
            {
                _DoDuplicate();
            }
            m_needsRebuildMesh = true;
            m_needsRebuildColliders = true;
            s_isOnValidate = true;
            UpdateMesh();
            UpdateColliders();
            s_isOnValidate = false;
        }

        private void _DoDuplicate()
        {
            // When copying a tilemap, the sharedMesh will be the same as the copied tilemap, so it has to be created a new one
            m_meshFilter.sharedMesh = null; // NOTE: if not nulled before the new Mesh, the previous mesh will be destroyed
            m_meshFilter.sharedMesh = new Mesh();
            m_meshFilter.sharedMesh.hideFlags = HideFlags.DontSave;
            m_meshFilter.sharedMesh.name = ParentTilemap.name + "_Copy_mesh";
            m_needsRebuildMesh = true;
            if (m_meshCollider != null)
            {
                m_meshCollider.sharedMesh = null; // NOTE: if not nulled before the new Mesh, the previous mesh will be destroyed
                m_meshCollider.sharedMesh = new Mesh();
                m_meshCollider.sharedMesh.hideFlags = HideFlags.DontSave;
                m_meshCollider.sharedMesh.name = ParentTilemap.name + "_Copy_collmesh";
            }
            m_needsRebuildColliders = true;
            //---
        }

        void OnEnable()
        {
#if UNITY_EDITOR
            if (m_meshRenderer != null)
            {
                EditorUtility.SetSelectedWireframeHidden(m_meshRenderer, true);
            }
#endif
            m_meshRenderer = GetComponent<MeshRenderer>();
            m_meshFilter = GetComponent<MeshFilter>();
            m_meshCollider = GetComponent<MeshCollider>();

            if (m_tileDataList == null || m_tileDataList.Count != m_width * m_height)
            {
                SetDimensions(m_width, m_height);
            }

            // if not playing, this will be done later by OnValidate
            if (Application.isPlaying)
            {
                m_needsRebuildMesh = true;
                m_needsRebuildColliders = true;
                UpdateMesh();
                UpdateColliders();
            }
        }

        public void Reset()
        {
            SetDimensions(m_width, m_height);           

#if UNITY_EDITOR
            if (m_meshRenderer != null)
            {
                EditorUtility.SetSelectedWireframeHidden(m_meshRenderer, true);
            }
#endif

            m_meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            m_meshRenderer.receiveShadows = false;

            m_needsRebuildMesh = true;
            m_needsRebuildColliders = true;
        }
        #endregion

        #region Public Methods

        public void DrawColliders()
        {
            if (ParentTilemap.ColliderType == eColliderType._3D)
            {
                if (m_meshCollider != null && m_meshCollider.sharedMesh != null && m_meshCollider.sharedMesh.normals.Length > 0f)
                {
                    Gizmos.color = new Color(0f, 1f, 0f, 0.8f);
                    Gizmos.DrawWireMesh(m_meshCollider.sharedMesh, transform.position, transform.rotation, transform.lossyScale);
                    Gizmos.color = Color.white;
                }
            }
            else if(ParentTilemap.ColliderType == eColliderType._2D)
            {
                Gizmos.color = new Color(0f, 1f, 0f, 0.8f);
                Gizmos.matrix = gameObject.transform.localToWorldMatrix;
                EdgeCollider2D[] edgeColliders = GetComponents<EdgeCollider2D>();
                for(int i = 0; i < edgeColliders.Length; ++i)
                {
                    EdgeCollider2D edgeCollider = edgeColliders[i];
                    if (edgeCollider.enabled)
                    {
                        Vector2[] points = edgeCollider.points;
                        for (int j = 0; j < (points.Length - 1); ++j)
                        {
                            Gizmos.DrawLine(points[j], points[j + 1]);
                        }
                    }
                }
                Gizmos.matrix = Matrix4x4.identity;
                Gizmos.color = Color.white;
            }
        }

        /// <summary>
        /// Next time UpdateMesh is called, the tile mesh will be rebuild
        /// </summary>
        public void InvalidateMesh()
        {
            m_needsRebuildMesh = true;
        }

        /// <summary>
        /// Next time UpdateColliderMesh is called, the collider mesh will be rebuild
        /// </summary>
        public void InvalidateMeshCollider()
        {
            m_needsRebuildColliders = true;
        }

        public void SetSharedMaterial(Material material)
        {
            m_meshRenderer.sharedMaterial = material;
            m_needsRebuildMesh = true;
        }

        public void SetDimensions(int width, int height)
        {
            int size = width * height;
            if (size > 0 && size * 4 < 65000) //NOTE: 65000 is the current maximum vertex allowed per mesh and each tile has 4 vertex
            {
                m_width = width;
                m_height = height;
                m_tileDataList = Enumerable.Repeat(Tileset.k_TileData_Empty, size).ToList();
            }
            else
            {
                Debug.LogWarning("Invalid parameters!");
            }
        }

        public void SetTileData(Vector2 vLocalPos, uint tileData)
        {
            SetTileData((int)(vLocalPos.x / CellSize.x), (int)(vLocalPos.y / CellSize.y), tileData);
        }

        public void SetTileData(int locGridX, int locGridY, uint tileData)
        {
            if (locGridX >= 0 && locGridX < m_width && locGridY >= 0 && locGridY < m_height)
            {
                int tileIdx = locGridY * m_width + locGridX;
                //TODO: improve by checking also if the brushId is the same there is no need to rebuild the mesh
                //TODO2?: if there is a new brushId, set tileId to 0xFFFF and also for the neighbors, then add again in FillData to refresh only the tiles with brushId if tileId is 0xFFFF
                //          Add a reference to the neigbours tilechunks for optimization


                int tileId = (int)(tileData & Tileset.k_TileDataMask_TileId);
                Tile tile = tileId != Tileset.k_TileId_Empty? Tileset.Tiles[tileId] : null;

                int prevTileId = (int)(m_tileDataList[tileIdx] & Tileset.k_TileDataMask_TileId);
                Tile prevTile = prevTileId != Tileset.k_TileId_Empty? Tileset.Tiles[prevTileId] : null;
                
                if( tile != null && tile.prefabData.prefab != null)
                {
                    CreateTileObject(tileIdx, tile.prefabData);                   
                }
                else
                {
                    DestroyTileObject(tileIdx);                    
                }

                int brushId = Tileset.GetBrushIdFromTileData(tileData);
                int prevBrushId = Tileset.GetBrushIdFromTileData(m_tileDataList[tileIdx]);

                if (brushId != prevBrushId)
                {
                    if (prevBrushId != Tileset.k_BrushId_Empty)
                    {
                        TilesetBrush prevBrush = ParentTilemap.Tileset.FindBrush(prevBrushId);
                        if (prevBrush != null)
                        {
                            prevBrush.OnErase(this, locGridX, locGridY, tileData);
                        }
                    }

                    if (brushId != Tileset.k_BrushId_Empty)
                    {
                        TilesetBrush brush = ParentTilemap.Tileset.FindBrush(brushId);
                        if (brush != null)
                        {
                            tileData = brush.OnPaint(this, locGridX, locGridY, tileData);
                        }
                    }

                    // Refresh Neighbors ( and itself if needed )
                    for (int yf = -1; yf <= 1; ++yf)
                    {
                        for (int xf = -1; xf <= 1; ++xf)
                        {
                            if ((xf | yf) == 0 && brushId > 0)
                            {
                                // Refresh itself
                                tileData = (tileData & ~Tileset.k_TileFlag_Updated);
                            }
                            else
                            {
                                int gx = (locGridX + xf);
                                int gy = (locGridY + yf);
                                int idx = gy * m_width + gx;
                                bool isInsideChunk = (gx >= 0 && gx < m_width && gy >= 0 && gy < m_height);
                                uint neighborTileData = isInsideChunk ? m_tileDataList[idx] : ParentTilemap.GetTileData(GridPosX + locGridX + xf, GridPosY + locGridY + yf);
                                int neighborBrushId = Tileset.GetBrushIdFromTileData(neighborTileData);
                                if (brushId > 0 && (neighborBrushId == brushId) || prevBrushId > 0 && (neighborBrushId == prevBrushId))
                                {
                                    neighborTileData = (neighborTileData & ~Tileset.k_TileFlag_Updated); // force a refresh
                                    if (isInsideChunk)
                                    {
                                        m_tileDataList[idx] = neighborTileData;
                                    }
                                    else
                                    {
                                        ParentTilemap.SetTileData(GridPosX + gx, GridPosY + gy, neighborTileData);
                                    }
                                }
                            }
                        }
                    }
                }
                else if(brushId != 0)
                {
                    // Refresh itself
                    tileData = (tileData & ~Tileset.k_TileFlag_Updated);
                }

                m_needsRebuildMesh |= (m_tileDataList[tileIdx] != tileData) || (tileData & Tileset.k_TileDataMask_TileId) == Tileset.k_TileId_Empty;
                m_needsRebuildColliders |= m_needsRebuildMesh &&
                (
                    (prevBrushId != Tileset.k_BrushId_Empty) || (brushId != Tileset.k_BrushId_Empty) // there is a brush (a brush could change the collider data later)
                    || (tile != null && tile.collData.type != eTileCollider.None) || (prevTile != null && prevTile.collData.type != eTileCollider.None) // prev. or new tile has colliders
                );

                if (ParentTilemap.ColliderType != eColliderType.None && m_needsRebuildColliders)
                {
                    // Refresh Neighbors tilechunk colliders, to make the collider autotiling
                    // Only if neighbor is outside this tilechunk
                    for (int yf = -1; yf <= 1; ++yf)
                    {
                        for (int xf = -1; xf <= 1; ++xf)
                        {
                            if ((xf | yf) != 0) // skip this tile position xf = yf = 0
                            {
                                int gx = (locGridX + xf);
                                int gy = (locGridY + yf);
                                bool isInsideChunk = (gx >= 0 && gx < m_width && gy >= 0 && gy < m_height);
                                if (!isInsideChunk)
                                {
                                    ParentTilemap.InvalidateChunkAt(GridPosX + gx, GridPosY + gy, false, true);
                                }
                            }
                        }
                    }
                }

                // Update tile data
                m_tileDataList[tileIdx] = tileData;
            }
        }

        public uint GetTileData(Vector2 vLocalPos)
        {
            return GetTileData((int)(vLocalPos.x / CellSize.x), (int)(vLocalPos.y / CellSize.y));
        }

        public uint GetTileData(int locGridX, int locGridY)
        {
            if (locGridX >= 0 && locGridX < m_width && locGridY >= 0 && locGridY < m_height)
            {
                int tileIdx = locGridY * m_width + locGridX;
                return m_tileDataList[tileIdx];
            }
            else
            {
                return Tileset.k_TileData_Empty;
            }
        }
     
        /// <summary>
        /// Update all tile objects if tile prefab data was changed and create tile objects for tiles with new prefab data.
        /// Call this method only when a tile prefab data has been changed to update changes. You may need to call UpdateMesh after this.
        /// </summary>
        public void RefreshTileObjects()
        {
            // Destroy tile objects where tile prefab is now null
            for(int i = 0; i < m_tileObjList.Count; ++i )
            {
                TileObjData tileObjData = m_tileObjList[i];
                uint tileData = m_tileDataList[tileObjData.tilePos];
                int tileId = (int)(tileData & Tileset.k_TileDataMask_TileId);
                Tile tile = tileId != Tileset.k_TileId_Empty? Tileset.Tiles[tileId] : null;
                if(tile == null || tile.prefabData.prefab == null)
                {
                    DestroyTileObject(tileObjData.tilePos);
                }
            }

            // Recreate or update all tile objects
            for(int tileIdx = 0; tileIdx < m_tileDataList.Count; ++tileIdx)
            {
                uint tileData = m_tileDataList[tileIdx];
                int tileId = (int)(tileData & Tileset.k_TileDataMask_TileId);
                Tile tile = tileId != Tileset.k_TileId_Empty ? Tileset.Tiles[tileId] : null;
                if(tile != null && tile.prefabData.prefab != null)
                {
                    CreateTileObject(tileIdx, tile.prefabData);
                }
            }
        }

        private bool m_needsRebuildMesh = false;
        /// <summary>
        /// Update the mesh and return false if all tiles are empty
        /// </summary>
        /// <returns></returns>
        public bool UpdateMesh()
        {
            if (ParentTilemap == null)
            {
                if (transform.parent == null) gameObject.hideFlags = HideFlags.None; //Unhide orphan tilechunks. This shouldn't happen
                ParentTilemap = transform.parent.GetComponent<Tilemap>();
            }
            gameObject.layer = ParentTilemap.gameObject.layer;

            if (m_meshFilter.sharedMesh == null)
            {
                //Debug.Log("Creating new mesh for " + name);
                m_meshFilter.sharedMesh = new Mesh();
                m_meshFilter.sharedMesh.hideFlags = HideFlags.DontSave;
                m_meshFilter.sharedMesh.name = ParentTilemap.name + "_mesh";
                m_needsRebuildMesh = true;
            }

            if (m_meshRenderer.sharedMaterial == null)
            {
                m_meshRenderer.sharedMaterial = ParentTilemap.Material;
            }
            m_meshRenderer.enabled = ParentTilemap.IsVisible;

            if (m_needsRebuildMesh)
            {
                m_needsRebuildMesh = false;
                if (FillMeshData())
                {
                    Mesh mesh = m_meshFilter.sharedMesh;
                    mesh.Clear();
                    mesh.vertices = m_vertices.ToArray();
                    mesh.uv = m_uv.ToArray();
                    mesh.triangles = m_triangles.ToArray();
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        private bool m_needsRebuildColliders = false;
        public bool UpdateColliders()
        {
            if (ParentTilemap == null)
            {
                ParentTilemap = transform.parent.GetComponent<Tilemap>();
            }
            gameObject.layer = ParentTilemap.gameObject.layer;


            //+++ Free unused resources
            if (ParentTilemap.ColliderType != eColliderType._3D)
            {
                if (m_meshCollider != null)
                {
                    if (!s_isOnValidate)
                    {
                        DestroyImmediate(m_meshCollider);
                    }
                    else
                    {
                        m_meshCollider.enabled = false;
                    }
                }
            }

            if (ParentTilemap.ColliderType != eColliderType._2D)
            {
                if (m_hasEdgeColliders)
                {
                    EdgeCollider2D[] edgeColliders = GetComponents<EdgeCollider2D>();
                    for (int i = 0; i < edgeColliders.Length; ++i)
                    {
                        if (!s_isOnValidate)
                        {
                            DestroyImmediate(edgeColliders[i]);
                        }
                        else
                        {
                            edgeColliders[i].enabled = false;
                        }
                    }
                    m_hasEdgeColliders = s_isOnValidate;
                }
            }
            //---

            if (ParentTilemap.ColliderType == eColliderType._3D)
            {
                if (m_meshCollider == null)
                {
                    m_meshCollider = GetComponent<MeshCollider>();
                    if (m_meshCollider == null && ParentTilemap.ColliderType == eColliderType._3D)
                    {
                        m_meshCollider = gameObject.AddComponent<MeshCollider>();
                    }
                }

                //NOTE: m_meshCollider.sharedMesh is equal to m_meshFilter.sharedMesh when the script is attached or reset
                if (m_meshCollider != null && (m_meshCollider.sharedMesh == null || m_meshCollider.sharedMesh == m_meshFilter.sharedMesh))
                {
                    m_meshCollider.sharedMesh = new Mesh();
                    m_meshCollider.sharedMesh.hideFlags = HideFlags.DontSave;
                    m_meshCollider.sharedMesh.name = ParentTilemap.name + "_collmesh";
                    m_needsRebuildColliders = true;
                }
            } 
            
            if (m_needsRebuildColliders)
            {
                m_needsRebuildColliders = false;
                bool isEmpty = FillColliderMeshData();
                if (ParentTilemap.ColliderType == eColliderType._3D)
                {
                    Mesh mesh = m_meshCollider.sharedMesh;
                    mesh.Clear();
                    mesh.vertices = m_meshCollVertices.ToArray();
                    mesh.triangles = m_meshCollTriangles.ToArray();
                    mesh.RecalculateNormals(); // needed by Gizmos.DrawWireMesh
                    m_meshCollider.sharedMesh = null; // for some reason this fix showing the green lines of the collider mesh
                    m_meshCollider.sharedMesh = mesh;
                }
                return isEmpty;
            }
            return true;
        }

        #endregion

        #region Private Methods

        #region Tile Prefab Factory Methods

        private GameObject CreateTileObject(int locGridX, int locGridY, TilePrefabData tilePrefabData)
        {
            if (locGridX >= 0 && locGridX < m_width && locGridY >= 0 && locGridY < m_height)
            {
                int tileIdx = locGridY * m_width + locGridX;
                return CreateTileObject(tileIdx, tilePrefabData);
            }
            else
            {
                return null;
            }
        }

        private GameObject CreateTileObject(int tileIdx, TilePrefabData tilePrefabData)
        {
            if (tilePrefabData.prefab != null)
            {
                TileObjData tileObjData = m_tileObjList.Find(x => x.tilePos == tileIdx);
                if (tileObjData == null || tileObjData.tilePrefabData != tilePrefabData)
                {
                    int gx = tileIdx % m_width;
                    int gy = tileIdx / m_width;
                    Vector3 chunkLocPos = new Vector3((gx + .5f) * CellSize.x, (gy + .5f) * CellSize.y);
                    if (tilePrefabData.offsetMode == TilePrefabData.eOffsetMode.Pixels)
                    {
                        float ppu = Tileset.TilePxSize.x / CellSize.x;
                        chunkLocPos += tilePrefabData.offset / ppu;
                    }
                    else //if (tilePrefabData.offsetMode == TilePrefabData.eOffsetMode.Units)
                    {
                        chunkLocPos += tilePrefabData.offset;
                    }
                    Vector3 worldPos = transform.TransformPoint(chunkLocPos);
                    GameObject tileObj;
#if UNITY_EDITOR
                    tileObj = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(tilePrefabData.prefab);
                    tileObj.transform.position = worldPos;
                    tileObj.transform.rotation = transform.rotation;
                    // allow destroy the object with undo operations
                    if (ParentTilemap.IsUndoEnabled)
                    {
                        UnityEditor.Undo.RegisterCreatedObjectUndo(tileObj, Tilemap.k_UndoOpName + ParentTilemap.name);
                    }
#else
                    tileObj = (GameObject)Instantiate(tilePrefabData.prefab, worldPos, transform.rotation);
#endif
                    tileObj.transform.parent = transform.parent;
                    tileObj.transform.localRotation = tilePrefabData.prefab.transform.localRotation;
                    tileObj.transform.localScale = tilePrefabData.prefab.transform.localScale;
                    if (tileObjData != null)
                    {
                        m_tileObjToBeRemoved.Add(tileObjData.obj);
                        tileObjData.obj = tileObj;
                        tileObjData.tilePrefabData = tilePrefabData;
                    }
                    else
                    {
                        m_tileObjList.Add(new TileObjData() { tilePos = tileIdx, obj = tileObj, tilePrefabData = tilePrefabData });
                    }
                    return tileObj;
                }
            }
            return null;
        }

        private void DestroyTileObject(int locGridX, int locGridY)
        {
            if (locGridX >= 0 && locGridX < m_width && locGridY >= 0 && locGridY < m_height)
            {
                int tileIdx = locGridY * m_width + locGridX;
                DestroyTileObject(tileIdx);
            }
        }

        private void DestroyTileObject(int tileIdx)
        {
            TileObjData tileObjData = m_tileObjList.Find(x => x.tilePos == tileIdx);
            if (tileObjData != null)
            {
                m_tileObjToBeRemoved.Add(tileObjData.obj);
                m_tileObjList.Remove(tileObjData);
            }
        }

        /// <summary>
        /// Call DestroyTileObject(int tileIdx) to destroy tile objects. This should be called only by UpdateMesh.
        /// NOTE: this delayed destruction is fixing an undo / redo issue
        /// </summary>
        /// <param name="obj"></param>
        private void DestroyTileObject(GameObject obj)
        {
            if (obj != null)
            {
#if UNITY_EDITOR
                if (ParentTilemap.IsUndoEnabled)
                {
                    //Note: tested in UNITY 5.2 For some reason, after this is called, all changes made to the chunk overwrite the original state of the data
                    // for that reason, this should be called after all changes are made
                    UnityEditor.Undo.DestroyObjectImmediate(obj);
                }
                else
                {
                    DestroyImmediate(obj);
                }
#else
                DestroyImmediate(obj);
#endif
            }
        }

        #endregion

        private void DestroyMesh()
        {
            MeshFilter meshFilter = GetComponent<MeshFilter>();
            if (meshFilter.sharedMesh != null)
            {
                //Debug.Log("Destroy Mesh of " + name);
                DestroyImmediate(meshFilter.sharedMesh);
            }
        }

        private void DestroyColliderMesh()
        {
            MeshCollider meshCollider = GetComponent<MeshCollider>();
            if (meshCollider != null && meshCollider.sharedMesh != null)
            {
                //Debug.Log("Destroy Mesh of " + name);
                DestroyImmediate(meshCollider.sharedMesh);
            }
        }

        /// <summary>
        /// Fill the mesh data and return false if all tiles are empty
        /// </summary>
        /// <returns></returns>
        private bool FillMeshData()
        {
            //Debug.Log( "[" + ParentTilemap.name + "] FillData -> " + name);
            if (Tileset == null)
            {
                return false;
            }

            m_meshRenderer.sharedMaterial = ParentTilemap.Material;
            m_meshRenderer.sharedMaterial.mainTexture = Tileset.AtlasTexture;

            int totalTiles = m_width * m_height;
            if (m_vertices == null)
            {
                m_vertices = new List<Vector3>(totalTiles * 4);
                m_uv = new List<Vector2>(totalTiles * 4);
                m_triangles = new List<int>(totalTiles * 6);
            }
            else
            {
                m_vertices.Clear();
                m_triangles.Clear();
                m_uv.Clear();
            }

            //+++ MeshCollider
            if (m_meshCollVertices == null)
            {
                m_meshCollVertices = new List<Vector3>(totalTiles * 4);
                m_meshCollTriangles = new List<int>(totalTiles * 6);
            }
            else
            {
                m_meshCollVertices.Clear();
                m_meshCollTriangles.Clear();
            }
            TileColliderData testCollData = new TileColliderData();
            testCollData.vertices = new Vector2[4] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0) };
            //---

            m_animatedTiles.Clear();
            //Dictionary<int, Rect> tileCache = new Dictionary<int, Rect>(); // it's faster not using tileCache because it's a direct access
            Dictionary<int, TilesetBrush> brushCache = new Dictionary<int, TilesetBrush>();
            bool isEmpty = true;
            for (int ty = 0, tileIdx = 0; ty < m_height; ++ty)
            {
                for (int tx = 0; tx < m_width; ++tx, ++tileIdx)
                {
                    uint tileData = m_tileDataList[tileIdx];
                    if (tileData != Tileset.k_TileData_Empty)
                    {
                        int brushId = (int)((tileData & Tileset.k_TileDataMask_BrushId) >> 16);
                        int tileId = (int)(tileData & Tileset.k_TileDataMask_TileId);
                        TilesetBrush tileBrush = null;
                        if (brushId > 0)
                        {
                            if (!brushCache.TryGetValue(brushId, out tileBrush))
                            {
                                //Debug.Log(" Cache miss! ");
                                tileBrush = Tileset.FindBrush(brushId);
                                if (tileBrush == null)
                                {
                                    Debug.LogWarning(" BrushId " + brushId + " not found! ");
                                    m_tileDataList[tileIdx] = tileData & ~Tileset.k_TileDataMask_BrushId;
                                }
                                brushCache[brushId] = tileBrush;
                            }
                            if (tileBrush != null && (tileData & Tileset.k_TileFlag_Updated) == 0)
                            {
                                tileData |= Tileset.k_TileFlag_Updated;// set updated flag
                                tileData = m_tileDataList[tileIdx] = tileBrush.Refresh(ParentTilemap, GridPosX + tx, GridPosY + ty, tileData);
                                tileId = (int)(tileData & Tileset.k_TileDataMask_TileId);
                            }
                        }

                        isEmpty = false;

                        if (tileBrush != null && tileBrush.IsAnimated())
                        {
                            m_animatedTiles.Add(new AnimTileData() { VertexIdx = m_vertices.Count, Brush = tileBrush });
                        }

                        Tile tile = (tileId != Tileset.k_TileId_Empty)? Tileset.Tiles[tileId] : null;

                        Rect tileUV;
                        Rect[] subtileUVs = tileBrush != null ? tileBrush.GetSubtileUVs(ParentTilemap, GridPosX + tx, GridPosY + ty, tileData) : null;
                        if (subtileUVs == null)
                        {
                            if (tile != null)
                            {
                                if (tile.prefabData.prefab == null || tile.prefabData.showTileWithPrefab //hide the tiles with prefabs ( unless showTileWithPrefab is true )
                                    || tileBrush && tileBrush.IsAnimated()) // ( skip if it's an animated brush )
                                {
                                    tileUV = tile.uv;
                                    _AddTileToMesh(tileUV, tx, ty, tileData, Vector2.zero, CellSize);
                                }
                            }
                        }
                        else
                        {
                            for (int i = 0; i < subtileUVs.Length; ++i)
                            {
                                tileUV = subtileUVs[i];
                                _AddTileToMesh(tileUV, tx, ty, tileData, new Vector2(i == 1 || i == 3 ? CellSize.x / 2f : 0f, i == 2 || i == 3 ? CellSize.y / 2f : 0f), CellSize / 2);
                            }
                        }
                    }
                }
            }

            //NOTE: the destruction of tileobjects needs to be done here to avoid a Undo/Redo bug. Check inside DestroyTileObject for more information.
            for (int i = 0; i < m_tileObjToBeRemoved.Count; ++i )
            {
                DestroyTileObject(m_tileObjToBeRemoved[i]);
            }
            m_tileObjToBeRemoved.Clear();
     
            return !isEmpty;
        }

        private void _AddTileToMesh(Rect tileUV, int tx, int ty, uint tileData, Vector2 subtileOffset, Vector2 subtileCellSize)
        {
            float px0 = tx * CellSize.x + subtileOffset.x;
            float py0 = ty * CellSize.y + subtileOffset.y;
            float px1 = px0 + subtileCellSize.x;
            float py1 = py0 + subtileCellSize.y;

            int vertexIdx = m_vertices.Count;

            m_vertices.Add(new Vector3(px0, py0, 0));
            m_vertices.Add(new Vector3(px0, py1, 0));
            m_vertices.Add(new Vector3(px1, py1, 0));
            m_vertices.Add(new Vector3(px1, py0, 0));

            m_triangles.Add(vertexIdx + 2);
            m_triangles.Add(vertexIdx + 1);
            m_triangles.Add(vertexIdx + 0);
            m_triangles.Add(vertexIdx + 0);
            m_triangles.Add(vertexIdx + 3);
            m_triangles.Add(vertexIdx + 2);

            bool flipH = (tileData & Tileset.k_TileFlag_FlipH) != 0;
            bool flipV = (tileData & Tileset.k_TileFlag_FlipV) != 0;
            bool rot90 = (tileData & Tileset.k_TileFlag_Rot90) != 0;

            float u0 = tileUV.xMin + Tileset.AtlasTexture.texelSize.x * InnerPadding;
            float v0 = tileUV.yMin + Tileset.AtlasTexture.texelSize.y * InnerPadding;
            float u1 = tileUV.xMax - Tileset.AtlasTexture.texelSize.x * InnerPadding;
            float v1 = tileUV.yMax - Tileset.AtlasTexture.texelSize.y * InnerPadding;

            if (flipH)
            {
                float v = v0;
                v0 = v1;
                v1 = v;
            }
            if (flipV)
            {
                float u = u0;
                u0 = u1;
                u1 = u;
            }

            if (rot90)
            {
                m_uv.Add(new Vector2(u1, v0));
                m_uv.Add(new Vector2(u0, v0));
                m_uv.Add(new Vector2(u0, v1));
                m_uv.Add(new Vector2(u1, v1));
            }
            else
            {
                m_uv.Add(new Vector2(u0, v0));
                m_uv.Add(new Vector2(u0, v1));
                m_uv.Add(new Vector2(u1, v1));
                m_uv.Add(new Vector2(u1, v0));
            }
        }

        private static Vector2[] s_fullCollTileVertices = new Vector2[] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0) };
        private bool FillColliderMeshData()
        {
            //Debug.Log( "[" + ParentTilemap.name + "] FillColliderMeshData -> " + name);
            if (Tileset == null || ParentTilemap.ColliderType == eColliderType.None)
            {
                return false;
            }

            List<LinkedList<Vector2>> openEdges = null;
            EdgeCollider2D[] edgeColliders = null;
            if (ParentTilemap.ColliderType == eColliderType._3D)
            {
                int totalTiles = m_width * m_height;
                if (m_meshCollVertices == null)
                {
                    m_meshCollVertices = new List<Vector3>(totalTiles * 4);
                    m_meshCollTriangles = new List<int>(totalTiles * 6);
                }
                else
                {
                    m_meshCollVertices.Clear();
                    m_meshCollTriangles.Clear();
                }
            }
            else //if (ParentTilemap.ColliderType == eColliderType._2D)
            {
                m_hasEdgeColliders = true;
                openEdges = new List<LinkedList<Vector2>>(10);
                edgeColliders = GetComponents<EdgeCollider2D>();
            }

            float halvedCollDepth = ParentTilemap.ColliderDepth / 2f;
            bool isEmpty = true;
            for (int ty = 0, tileIdx = 0; ty < m_height; ++ty)
            {
                for (int tx = 0; tx < m_width; ++tx, ++tileIdx)
                {
                    uint tileData = m_tileDataList[tileIdx];
                    if (tileData != Tileset.k_TileData_Empty)
                    {
                        int tileId = (int)(tileData & Tileset.k_TileDataMask_TileId);
                        if (tileId != Tileset.k_TileId_Empty)
                        {
                            TileColliderData tileCollData = Tileset.Tiles[tileId].collData;
                            if (tileCollData.type != eTileCollider.None)
                            {
                                if ((tileData & (Tileset.k_TileFlag_FlipH | Tileset.k_TileFlag_FlipV | Tileset.k_TileFlag_Rot90)) != 0)
                                {
                                    tileCollData = tileCollData.Clone();
                                    if ((tileData & Tileset.k_TileFlag_FlipH) != 0) tileCollData.FlipH();
                                    if ((tileData & Tileset.k_TileFlag_FlipV) != 0) tileCollData.FlipV();
                                    if ((tileData & Tileset.k_TileFlag_Rot90) != 0) tileCollData.Rot90();
                                }
                                isEmpty = false;
                                int neighborCollFlags = 0; // don't remove, even using neighborTileCollData, neighborTileCollData is not filled if tile is empty
                                bool isSurroundedByFullColliders = true;
                                Vector2[] neighborSegmentMinMax = new Vector2[4];
                                TileColliderData[] neighborTileCollData = new TileColliderData[4];
                                for (int i = 0; i < 4; ++i)
                                {
                                    uint neighborTileData;
                                    switch (i)
                                    {
                                        case 0:  // Up Tile
                                            neighborTileData = (tileIdx + m_width) < m_tileDataList.Count ? 
                                            m_tileDataList[tileIdx + m_width]
                                            :
                                            ParentTilemap.GetTileData( GridPosX + tx, GridPosY + ty + 1); break;
                                        case 1: // Right Tile
                                            neighborTileData = (tileIdx + 1) % m_width != 0 ? //(tileIdx + 1) < m_tileDataList.Count ? 
                                            m_tileDataList[tileIdx + 1]
                                            :
                                            ParentTilemap.GetTileData(GridPosX + tx + 1, GridPosY + ty); break;
                                        case 2: // Down Tile
                                            neighborTileData = tileIdx >= m_width ? 
                                            m_tileDataList[tileIdx - m_width]
                                            :
                                            ParentTilemap.GetTileData(GridPosX + tx, GridPosY + ty - 1); break;  
                                        case 3: // Left Tile
                                            neighborTileData = tileIdx % m_width != 0 ? //neighborTileId = tileIdx >= 1 ? 
                                            m_tileDataList[tileIdx - 1]
                                            :
                                            ParentTilemap.GetTileData(GridPosX + tx - 1, GridPosY + ty); break;
                                        default: neighborTileData = Tileset.k_TileData_Empty; break;
                                    }

                                    int neighborTileId = (int)(neighborTileData & Tileset.k_TileDataMask_TileId);
                                    if (neighborTileId != Tileset.k_TileId_Empty)
                                    {
                                        Vector2 segmentMinMax;
                                        TileColliderData neighborTileCollider;
                                        neighborTileCollider = Tileset.Tiles[neighborTileId].collData;
                                        if ((neighborTileData & (Tileset.k_TileFlag_FlipH | Tileset.k_TileFlag_FlipV | Tileset.k_TileFlag_Rot90)) != 0)
                                        {
                                            neighborTileCollider = neighborTileCollider.Clone();
                                            if ((neighborTileData & Tileset.k_TileFlag_FlipH) != 0) neighborTileCollider.FlipH();
                                            if ((neighborTileData & Tileset.k_TileFlag_FlipV) != 0) neighborTileCollider.FlipV();
                                            if ((neighborTileData & Tileset.k_TileFlag_Rot90) != 0) neighborTileCollider.Rot90();
                                        }
                                        neighborTileCollData[i] = neighborTileCollider;
                                        isSurroundedByFullColliders &= (neighborTileCollider.type == eTileCollider.Full);

                                        if (neighborTileCollider.type == eTileCollider.None)
                                        {
                                            segmentMinMax = new Vector2(float.MaxValue, float.MinValue); //NOTE: x will be min, y will be max
                                        }
                                        else if (neighborTileCollider.type == eTileCollider.Full)
                                        {
                                            segmentMinMax = new Vector2(0f, 1f); //NOTE: x will be min, y will be max
                                            neighborCollFlags |= (1 << i);
                                        }
                                        else
                                        {
                                            segmentMinMax = new Vector2(float.MaxValue, float.MinValue); //NOTE: x will be min, y will be max
                                            neighborCollFlags |= (1 << i);
                                            for (int j = 0; j < neighborTileCollider.vertices.Length; ++j)
                                            {
                                                Vector2 v = neighborTileCollider.vertices[j];
                                                {
                                                    if (i == 0 && v.y == 0 || i == 2 && v.y == 1) //Top || Bottom
                                                    {
                                                        if (v.x < segmentMinMax.x) segmentMinMax.x = v.x;
                                                        if (v.x > segmentMinMax.y) segmentMinMax.y = v.x;
                                                    }
                                                    else if (i == 1 && v.x == 0 || i == 3 && v.x == 1) //Right || Left
                                                    {
                                                        if (v.y < segmentMinMax.x) segmentMinMax.x = v.y;
                                                        if (v.y > segmentMinMax.y) segmentMinMax.y = v.y;
                                                    }
                                                }
                                            }
                                        }
                                        neighborSegmentMinMax[i] = segmentMinMax;
                                    }
                                    else
                                    {
                                        isSurroundedByFullColliders = false;
                                    }
                                }

                                // Create Mesh Colliders
                                if (isSurroundedByFullColliders)
                                {
                                    //Debug.Log(" Surrounded! " + tileIdx);
                                }
                                else
                                {
                                    float px0 = tx * CellSize.x;
                                    float py0 = ty * CellSize.y;
                                    Vector2[] collVertices = tileCollData.type == eTileCollider.Full ? s_fullCollTileVertices : tileCollData.vertices;
                                    for (int i = 0; i < collVertices.Length; ++i)
                                    {
                                        Vector2 s0 = collVertices[i];
                                        Vector2 s1 = collVertices[i == (collVertices.Length - 1) ? 0 : i + 1];

                                        // full collider optimization
                                        if ((tileCollData.type == eTileCollider.Full) &&
                                            (
                                            (i == 0 && neighborTileCollData[3].type == eTileCollider.Full) || // left tile has collider
                                            (i == 1 && neighborTileCollData[0].type == eTileCollider.Full) || // top tile has collider
                                            (i == 2 && neighborTileCollData[1].type == eTileCollider.Full) || // right tile has collider
                                            (i == 3 && neighborTileCollData[2].type == eTileCollider.Full)  // bottom tile has collider
                                            )
                                        )
                                        {
                                            continue;
                                        }
                                        // polygon collider optimization
                                        else // if( tileCollData.type == eTileCollider.Polygon ) Or Full colliders if neighbor is not Full as well
                                        {
                                            Vector2 n, m;
                                            if (s0.y == 1f && s1.y == 1f) // top side
                                            {
                                                if ((neighborCollFlags & 0x1) != 0) // top tile has collider
                                                {
                                                    n = neighborSegmentMinMax[0];
                                                    if (n.x < n.y && n.x <= s0.x && n.y >= s1.x)
                                                    {
                                                        continue;
                                                    }
                                                }
                                            }
                                            else if (s0.x == 1f && s1.x == 1f) // right side
                                            {
                                                if ((neighborCollFlags & 0x2) != 0) // right tile has collider
                                                {
                                                    n = neighborSegmentMinMax[1];
                                                    if (n.x < n.y && n.x <= s1.y && n.y >= s0.y)
                                                    {
                                                        continue;
                                                    }
                                                }
                                            }
                                            else if (s0.y == 0f && s1.y == 0f) // bottom side
                                            {
                                                if ((neighborCollFlags & 0x4) != 0) // bottom tile has collider
                                                {
                                                    n = neighborSegmentMinMax[2];
                                                    if (n.x < n.y && n.x <= s1.x && n.y >= s0.x)
                                                    {
                                                        continue;
                                                    }
                                                }
                                            }
                                            else if (s0.x == 0f && s1.x == 0f) // left side
                                            {
                                                if ((neighborCollFlags & 0x8) != 0) // left tile has collider
                                                {
                                                    n = neighborSegmentMinMax[3];
                                                    if (n.x < n.y && n.x <= s0.y && n.y >= s1.y)
                                                    {
                                                        continue;
                                                    }
                                                }
                                            }
                                            else if (s0.y == 1f && s1.x == 1f) // top - right diagonal
                                            {
                                                if ((neighborCollFlags & 0x1) != 0 && (neighborCollFlags & 0x2) != 0)
                                                {
                                                    n = neighborSegmentMinMax[0];
                                                    m = neighborSegmentMinMax[1];
                                                    if ((n.x < n.y && n.x <= s0.x && n.y == 1f) && (m.x < m.y && m.x <= s1.y && m.y == 1f))
                                                    {
                                                        continue;
                                                    }
                                                }
                                            }
                                            else if (s0.x == 1f && s1.y == 0f) // right - bottom diagonal
                                            {
                                                if ((neighborCollFlags & 0x2) != 0 && (neighborCollFlags & 0x4) != 0)
                                                {
                                                    n = neighborSegmentMinMax[1];
                                                    m = neighborSegmentMinMax[2];
                                                    if ((n.x < n.y && n.x == 0f && n.y >= s0.y) && (m.x < m.y && m.x <= s1.x && m.y == 1f))
                                                    {
                                                        continue;
                                                    }
                                                }
                                            }
                                            else if (s0.y == 0f && s1.x == 0f) // bottom - left diagonal
                                            {
                                                if ((neighborCollFlags & 0x4) != 0 && (neighborCollFlags & 0x8) != 0)
                                                {
                                                    n = neighborSegmentMinMax[2];
                                                    m = neighborSegmentMinMax[3];
                                                    if ((n.x < n.y && n.x == 0f && n.y >= s0.x) && (m.x < m.y && m.x == 0f && m.y >= s1.y))
                                                    {
                                                        continue;
                                                    }
                                                }
                                            }
                                            else if (s0.x == 0f && s1.y == 1f) // left - top diagonal
                                            {
                                                if ((neighborCollFlags & 0x8) != 0 && (neighborCollFlags & 0x1) != 0)
                                                {
                                                    n = neighborSegmentMinMax[3];
                                                    m = neighborSegmentMinMax[0];
                                                    if ((n.x < n.y && n.x <= s0.y && n.y == 1f) && (m.x < m.y && m.x == 0f && m.y >= s1.x))
                                                    {
                                                        continue;
                                                    }
                                                }
                                            }
                                        }

                                        // Update s0 and s1 to world positions
                                        s0.x = px0 + CellSize.x * s0.x; s0.y = py0 + CellSize.y * s0.y;
                                        s1.x = px0 + CellSize.x * s1.x; s1.y = py0 + CellSize.y * s1.y;

                                        if (ParentTilemap.ColliderType == eColliderType._3D)
                                        {
                                            int collVertexIdx = m_meshCollVertices.Count;
                                            m_meshCollVertices.Add(new Vector3(s0.x, s0.y, -halvedCollDepth));
                                            m_meshCollVertices.Add(new Vector3(s0.x, s0.y, halvedCollDepth));
                                            m_meshCollVertices.Add(new Vector3(s1.x, s1.y, halvedCollDepth));
                                            m_meshCollVertices.Add(new Vector3(s1.x, s1.y, -halvedCollDepth));

                                            m_meshCollTriangles.Add(collVertexIdx + 0);
                                            m_meshCollTriangles.Add(collVertexIdx + 1);
                                            m_meshCollTriangles.Add(collVertexIdx + 2);
                                            m_meshCollTriangles.Add(collVertexIdx + 2);
                                            m_meshCollTriangles.Add(collVertexIdx + 3);
                                            m_meshCollTriangles.Add(collVertexIdx + 0);
                                        }
                                        else //if( ParentTilemap.ColliderType == eColliderType._2D )
                                        {
                                            bool edgeUpdated = false;
                                            for (int edgeIdx = openEdges.Count - 1; edgeIdx >= 0 && !edgeUpdated; --edgeIdx)
                                            {
                                                LinkedList<Vector2> edgeSegments = openEdges[edgeIdx];
                                                if( edgeSegments.Last.Value == s0 )
                                                {
                                                    edgeSegments.AddLast(s1); edgeUpdated = true;
                                                }
                                                else if( edgeSegments.Last.Value == s1 )
                                                {
                                                    edgeSegments.AddLast(s0); edgeUpdated = true;
                                                }
                                                else if (edgeSegments.First.Value == s0)
                                                {
                                                    edgeSegments.AddFirst(s1); edgeUpdated = true;
                                                }
                                                else if (edgeSegments.First.Value == s1)
                                                {
                                                    edgeSegments.AddFirst(s0); edgeUpdated = true;
                                                }
                                                //TODO; check for closed edge
                                            }

                                            if( !edgeUpdated )
                                            {
                                                LinkedList<Vector2> newEdge = new LinkedList<Vector2>();
                                                newEdge.AddFirst(s0);
                                                newEdge.AddLast(s1);
                                                openEdges.Add(newEdge);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }


            if (ParentTilemap.ColliderType == eColliderType._2D)
            {
                //Create Edges
                for (int i = 0; i < openEdges.Count; ++i)
                {
                    LinkedList<Vector2> edgeSegments = openEdges[i];
                    bool reuseEdge = i < edgeColliders.Length;
                    EdgeCollider2D edgeCollider = reuseEdge ? edgeColliders[i] : gameObject.AddComponent<EdgeCollider2D>();
                    edgeCollider.enabled = true;
                    edgeCollider.points = edgeSegments.ToArray();
                }

                //Destroy unused edge colliders
                for (int i = openEdges.Count; i < edgeColliders.Length; ++i)
                {
                    if (!s_isOnValidate)
                    {
                        DestroyImmediate(edgeColliders[i]);
                    }
                    else
                    {
                        edgeColliders[i].enabled = false;
                    }
                }
            }

            return !isEmpty;
        }
        #endregion
    }
}