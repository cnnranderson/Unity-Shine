using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace CreativeSpore.SuperTilemapEditor
{
    public class RandomBrush : TilesetBrush
    {
        public List<int> RandomTiles = new List<int>();

        #region IBrush

        public override int PreviewTileId()
        {
            return RandomTiles.Count > 0 ? RandomTiles[0] : Tileset.k_TileId_Empty;
        }

        public override uint Refresh(Tilemap tilemap, int gridX, int gridY, uint tileData)
        {
            if (RandomTiles.Count > 0)
            {
                int randIdx = UnityEngine.Random.Range(0, RandomTiles.Count);
                return (tileData & ~Tileset.k_TileDataMask_TileId) | (uint)RandomTiles[randIdx];
            }
            return tileData;
        }

        #endregion
    }
}