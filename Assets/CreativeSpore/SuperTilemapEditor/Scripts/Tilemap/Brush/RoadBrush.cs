using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CreativeSpore.SuperTilemapEditor
{

    public class RoadBrush : TilesetBrush
    {
        // '°', '├', '═', '┤', | 0, 2, 10, 8,
        // '┬', '╔', '╦', '╗', | 4, 6, 14, 12,
        // '║', '╠', '╬', '╣', | 5, 7, 15, 13,
        // '┴', '╚', '╩', '╝', | 1, 3, 11, 9,
        public int[] TileIds = Enumerable.Repeat(Tileset.k_TileId_Empty, 16).ToArray();

        #region IBrush

        public override int PreviewTileId()
        {
            return TileIds[0];
        }

        public override uint Refresh(Tilemap tilemap, int gridX, int gridY, uint tileData)
        {
            int brushId = (int)((tileData & Tileset.k_TileDataMask_BrushId) >> 16);
            int brushIdTop = (int)((uint)(tilemap.GetTileData(gridX, gridY + 1) & Tileset.k_TileDataMask_BrushId) >> 16);
            int brushIdRight = (int)((uint)(tilemap.GetTileData(gridX + 1, gridY) & Tileset.k_TileDataMask_BrushId) >> 16);
            int brushIdBottom = (int)((uint)(tilemap.GetTileData(gridX, gridY - 1) & Tileset.k_TileDataMask_BrushId) >> 16);
            int brushIdLeft = (int)((uint)(tilemap.GetTileData(gridX - 1, gridY) & Tileset.k_TileDataMask_BrushId) >> 16);

            int idx = 0;
            if (brushIdTop == brushId) idx = 1;
            if (brushIdRight == brushId) idx |= 2;
            if (brushIdBottom == brushId) idx |= 4;
            if (brushIdLeft == brushId) idx |= 8;

            int tileId = TileIds[idx];
            return (tileData & Tileset.k_TileDataMask_Flags) | (uint)((brushId << 16) | tileId);
        }       

        #endregion
    }
}