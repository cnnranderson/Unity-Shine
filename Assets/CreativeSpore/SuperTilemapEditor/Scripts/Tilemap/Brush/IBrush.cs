using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace CreativeSpore.SuperTilemapEditor
{

    public interface IBrush
    {
        /// <summary>
        /// The tile Id used to show the preview tile in the tile palette
        /// </summary>
        /// <returns></returns>
        int PreviewTileId();
        /// <summary>
        /// Called when brush is painted
        /// </summary>
        /// <param name="chunk"></param>
        /// <param name="chunkGx"></param>
        /// <param name="chunkGy"></param>
        /// <param name="tileData"></param>
        /// <returns></returns>
        uint OnPaint(TilemapChunk chunk, int chunkGx, int chunkGy, uint tileData);
        /// <summary>
        /// Called when brush is erased
        /// </summary>
        /// <param name="chunk"></param>
        /// <param name="chunkGx"></param>
        /// <param name="chunkGy"></param>
        /// <param name="tileData"></param>
        void OnErase(TilemapChunk chunk, int chunkGx, int chunkGy, uint tileData);
        /// <summary>
        /// This is called by the tilemap chunks when a tile needs to be refreshed. Return the updated tile data.
        /// </summary>
        /// <param name="tilemap"></param>
        /// <param name="gridX"></param>
        /// <param name="gridY"></param>
        /// <param name="tileData"></param>
        /// <returns></returns>
        uint Refresh(Tilemap tilemap, int gridX, int gridY, uint tileData);
        /// <summary>
        /// Return if the tile should be updated for animation
        /// </summary>
        /// <returns></returns>
        bool IsAnimated();
        /// <summary>
        /// Return the tile UV for the current frame
        /// </summary>
        /// <returns></returns>
        Rect GetAnimUV();
        /// <summary>
        /// If a tile is divided in 4 subtiles, this is returning an array of 4 UVS, one per each subtile, from bottom to top, from left to right
        /// </summary>
        /// <param name="tilemap"></param>
        /// <param name="gridX"></param>
        /// <param name="gridY"></param>
        /// <param name="tileData"></param>
        /// <returns></returns>    
        Rect[] GetSubtileUVs(Tilemap tilemap, int gridX, int gridY, uint tileData);
    }
}