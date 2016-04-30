using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace CreativeSpore.SuperTilemapEditor
{

    public class A2X2Brush : TilesetBrush
    {
        // '╔', '╗' | 2, 3,
        // '╚', '╝' | 0, 1,
        public int[] TileIds = new int[]
    {
        Tileset.k_TileId_Empty, // 3
        Tileset.k_TileId_Empty, // 6
        Tileset.k_TileId_Empty, // 9
        Tileset.k_TileId_Empty, // 12
    };

        #region IBrush

        public override int PreviewTileId()
        {
            return TileIds[0];
        }

        public override uint Refresh(Tilemap tilemap, int gridX, int gridY, uint tileData)
        {
            int brushId = (int)((tileData & Tileset.k_TileDataMask_BrushId) >> 16);
            //NOTE: Now, taking TileIds[0] by default, it means the tile collider will be taken from TileIds[0]
            return (tileData & Tileset.k_TileDataMask_Flags) | (uint)((brushId << 16) | TileIds[0]);
        }

        public override Rect[] GetSubtileUVs(Tilemap tilemap, int gridX, int gridY, uint tileData)
        {
            if (System.Array.IndexOf(TileIds, Tileset.k_TileId_Empty) >= 0)
            {
                return null;
            }

            _DoUpdateSubtileUVsCache();

            int brushId = (int)((tileData & Tileset.k_TileDataMask_BrushId) >> 16);
            int brushId_N = (int)((uint)(tilemap.GetTileData(gridX, gridY + 1) & Tileset.k_TileDataMask_BrushId) >> 16);
            int brushId_E = (int)((uint)(tilemap.GetTileData(gridX + 1, gridY) & Tileset.k_TileDataMask_BrushId) >> 16);
            int brushId_S = (int)((uint)(tilemap.GetTileData(gridX, gridY - 1) & Tileset.k_TileDataMask_BrushId) >> 16);
            int brushId_W = (int)((uint)(tilemap.GetTileData(gridX - 1, gridY) & Tileset.k_TileDataMask_BrushId) >> 16);

            // diagonals
            int brushId_NE = (int)((uint)(tilemap.GetTileData(gridX + 1, gridY + 1) & Tileset.k_TileDataMask_BrushId) >> 16);
            int brushId_SE = (int)((uint)(tilemap.GetTileData(gridX + 1, gridY - 1) & Tileset.k_TileDataMask_BrushId) >> 16);
            int brushId_SW = (int)((uint)(tilemap.GetTileData(gridX - 1, gridY - 1) & Tileset.k_TileDataMask_BrushId) >> 16);
            int brushId_NW = (int)((uint)(tilemap.GetTileData(gridX - 1, gridY + 1) & Tileset.k_TileDataMask_BrushId) >> 16);

            Rect[] rUvs = new Rect[4];
            rUvs[0] = (brushId_SW == brushId && brushId_S == brushId && brushId_W == brushId) ? m_connectSubTileUv[0] : m_disconnectSubTileUv[0];
            rUvs[1] = (brushId_SE == brushId && brushId_S == brushId && brushId_E == brushId) ? m_connectSubTileUv[1] : m_disconnectSubTileUv[1];
            rUvs[2] = (brushId_NW == brushId && brushId_N == brushId && brushId_W == brushId) ? m_connectSubTileUv[2] : m_disconnectSubTileUv[2];
            rUvs[3] = (brushId_NE == brushId && brushId_N == brushId && brushId_E == brushId) ? m_connectSubTileUv[3] : m_disconnectSubTileUv[3];

            return rUvs;
        }

        private int m_lastCacheFrameCount;
        private Rect[] m_connectSubTileUv = new Rect[4];
        private Rect[] m_disconnectSubTileUv = new Rect[4];
        private void _DoUpdateSubtileUVsCache()
        {
            // Create cache with Uv rect for each corner
            if (m_lastCacheFrameCount != Time.frameCount)
            {
                m_lastCacheFrameCount = Time.frameCount;

                m_connectSubTileUv[0] = Tileset.Tiles[TileIds[3]].uv;
                m_connectSubTileUv[1] = Tileset.Tiles[TileIds[2]].uv;
                m_connectSubTileUv[2] = Tileset.Tiles[TileIds[1]].uv;
                m_connectSubTileUv[3] = Tileset.Tiles[TileIds[0]].uv;
                for (int i = 0; i < 4; ++i)
                {
                    Rect r = m_connectSubTileUv[i];
                    r.size /= 2;
                    r.position += new Vector2(i == 1 || i == 3 ? r.width : 0f, i == 2 || i == 3 ? r.height : 0f);
                    m_connectSubTileUv[i] = r;
                }

                m_disconnectSubTileUv[0] = Tileset.Tiles[TileIds[0]].uv;
                m_disconnectSubTileUv[1] = Tileset.Tiles[TileIds[1]].uv;
                m_disconnectSubTileUv[2] = Tileset.Tiles[TileIds[2]].uv;
                m_disconnectSubTileUv[3] = Tileset.Tiles[TileIds[3]].uv;
                for (int i = 0; i < 4; ++i)
                {
                    Rect r = m_disconnectSubTileUv[i];
                    r.size /= 2;
                    r.position += new Vector2(i == 1 || i == 3 ? r.width : 0f, i == 2 || i == 3 ? r.height : 0f);
                    m_disconnectSubTileUv[i] = r;
                }
            }
        }

        #endregion
    }
}