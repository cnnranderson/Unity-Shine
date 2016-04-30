using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace CreativeSpore.SuperTilemapEditor
{

    public class TilesetBrush : ScriptableObject, IBrush
    {

        public Tileset Tileset;

        public ParameterContainer Params = new ParameterContainer();

        #region IBrush

        public virtual int PreviewTileId()
        {
            return new int();
        }

        public virtual uint OnPaint(TilemapChunk chunk, int chunkGx, int chunkGy, uint tileData)
        {
            return tileData;
        }

        public virtual void OnErase(TilemapChunk chunk, int chunkGx, int chunkGy, uint tileData)
        {
            ;
        }

        public virtual uint Refresh(Tilemap tilemap, int gridX, int gridY, uint tileData)
        {
            return tileData;
        }

        public virtual bool IsAnimated()
        {
            return false;
        }

        public virtual Rect GetAnimUV()
        {
            return Tileset && PreviewTileId() != Tileset.k_TileId_Empty ? Tileset.Tiles[PreviewTileId()].uv : default(Rect);
        }

        public virtual Rect[] GetSubtileUVs(Tilemap tilemap, int gridX, int gridY, uint tileData)
        {
            return null;
        }

        #endregion
    }
}