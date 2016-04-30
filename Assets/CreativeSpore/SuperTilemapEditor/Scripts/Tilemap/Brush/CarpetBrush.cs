using UnityEngine;
using System.Collections;
using System.Linq;

namespace CreativeSpore.SuperTilemapEditor
{
    public class CarpetBrush : RoadBrush
    {
        public int[] InteriorCornerTileIds = Enumerable.Repeat(Tileset.k_TileId_Empty, 4).ToArray();

        #region IBrush

        public override int PreviewTileId()
        {
            return TileIds[6];
            //return TileIds[15] != Tileset.k_TileId_Empty ? TileIds[15] : TileIds[6]; //15 center brush (╬) ; 6 top left brush (╔)
        }

        static int s_brushId;
        static int s_neighIdx;
        static bool[] s_showDiagonal = new bool[4];
        static bool s_needsSubTiles;
        static int s_tileId;

        private void CalculateNeighbourData(Tilemap tilemap, int gridX, int gridY, uint tileData)
        {
            s_needsSubTiles = false;
            s_brushId = (int)((tileData & Tileset.k_TileDataMask_BrushId) >> 16);
            int brushId_N = (int)((uint)(tilemap.GetTileData(gridX, gridY + 1) & Tileset.k_TileDataMask_BrushId) >> 16);
            int brushId_E = (int)((uint)(tilemap.GetTileData(gridX + 1, gridY) & Tileset.k_TileDataMask_BrushId) >> 16);
            int brushId_S = (int)((uint)(tilemap.GetTileData(gridX, gridY - 1) & Tileset.k_TileDataMask_BrushId) >> 16);
            int brushId_W = (int)((uint)(tilemap.GetTileData(gridX - 1, gridY) & Tileset.k_TileDataMask_BrushId) >> 16);
            s_neighIdx = 0;
            if (brushId_N == s_brushId) s_neighIdx |= 1;
            if (brushId_E == s_brushId) s_neighIdx |= 2;
            if (brushId_S == s_brushId) s_neighIdx |= 4;
            if (brushId_W == s_brushId) s_neighIdx |= 8;

            s_needsSubTiles = (s_neighIdx == 0 || s_neighIdx == 1 || s_neighIdx == 2 || s_neighIdx == 4
            || s_neighIdx == 5 || s_neighIdx == 8 || s_neighIdx == 10) ;

            s_tileId = TileIds[s_neighIdx];
            // diagonals
            {
                int brushId_NE = (int)((uint)(tilemap.GetTileData(gridX + 1, gridY + 1) & Tileset.k_TileDataMask_BrushId) >> 16);
                int brushId_SE = (int)((uint)(tilemap.GetTileData(gridX + 1, gridY - 1) & Tileset.k_TileDataMask_BrushId) >> 16);
                int brushId_SW = (int)((uint)(tilemap.GetTileData(gridX - 1, gridY - 1) & Tileset.k_TileDataMask_BrushId) >> 16);
                int brushId_NW = (int)((uint)(tilemap.GetTileData(gridX - 1, gridY + 1) & Tileset.k_TileDataMask_BrushId) >> 16);

                s_showDiagonal[0] = (brushId_SW != s_brushId) && (brushId_S == s_brushId) && (brushId_W == s_brushId);
                s_showDiagonal[1] = (brushId_SE != s_brushId) && (brushId_S == s_brushId) && (brushId_E == s_brushId);
                s_showDiagonal[2] = (brushId_NW != s_brushId) && (brushId_N == s_brushId) && (brushId_W == s_brushId);
                s_showDiagonal[3] = (brushId_NE != s_brushId) && (brushId_N == s_brushId) && (brushId_E == s_brushId);

                s_needsSubTiles = s_needsSubTiles || System.Array.IndexOf(s_showDiagonal, true) >= 0;
            }
        }

        public override uint Refresh(Tilemap tilemap, int gridX, int gridY, uint tileData)
        {
            CalculateNeighbourData(tilemap, gridX, gridY, tileData);            
            return (tileData & Tileset.k_TileDataMask_Flags) | (uint)((s_brushId << 16) | s_tileId);
        }

        // '°', '├', '═', '┤', | 0, 2, 10, 8,
        // '┬', '╔', '╦', '╗', | 4, 6, 14, 12,
        // '║', '╠', '╬', '╣', | 5, 7, 15, 13,
        // '┴', '╚', '╩', '╝', | 1, 3, 11, 9,
        public override Rect[] GetSubtileUVs(Tilemap tilemap, int gridX, int gridY, uint tileData)
        {
            CalculateNeighbourData(tilemap, gridX, gridY, tileData);
            // tiles that need subtile division
            if ( s_needsSubTiles )
            {
                int[] tileIds = null;

                if (s_neighIdx == 0) //°
                {
                    tileIds = new int[] { TileIds[3], TileIds[9], TileIds[6], TileIds[12] };
                }
                else if (s_neighIdx == 4)//┬
                {
                    tileIds = new int[] { TileIds[6], TileIds[12], TileIds[6], TileIds[12] };
                }
                else if (s_neighIdx == 5)//║
                {
                    tileIds = new int[] { TileIds[7], TileIds[13], TileIds[7], TileIds[13] };
                }
                else if (s_neighIdx == 1)//┴
                {
                    tileIds = new int[] { TileIds[3], TileIds[9], TileIds[3], TileIds[9] };
                }
                else if (s_neighIdx == 2)//├
                {
                    tileIds = new int[] { TileIds[3], TileIds[3], TileIds[6], TileIds[6] };
                }
                else if (s_neighIdx == 10)//═
                {
                    tileIds = new int[] { TileIds[11], TileIds[11], TileIds[14], TileIds[14] };
                }
                else if (s_neighIdx == 8)//┤
                {
                    tileIds = new int[] { TileIds[9], TileIds[9], TileIds[12], TileIds[12] };
                }
                // NOTE: this case '╬' cut the tiles different (using corner tiles). 
                // If it is commented, and default case is used, instead or corner tiles, it will use the center tile '╬'
                // Depending on the graphics it could be interesting add a check box to choose between using this or not.
                else if (s_neighIdx == 15)// ╬
                {
                    tileIds = new int[] { InteriorCornerTileIds[0], InteriorCornerTileIds[1], InteriorCornerTileIds[2], InteriorCornerTileIds[3] };
                }
                else
                {
                    tileIds = new int[] { TileIds[s_neighIdx], TileIds[s_neighIdx], TileIds[s_neighIdx], TileIds[s_neighIdx] };
                }

                for(int i = 0; i < s_showDiagonal.Length; ++i)
                {
                    if(s_showDiagonal[i])
                    {
                        tileIds[i] = InteriorCornerTileIds[3-i];
                    }
                }

                Vector2 subTileSize = tilemap.Tileset.CalculateTileTexelSize() / 2f;
                Rect rSubTile = new Rect(Vector2.zero, subTileSize);
                Rect[] rUvs = new Rect[4];
                for (int i = 0; i < rUvs.Length; ++i)
                {
                    int tileId = tileIds[i];
                    if (tileId != Tileset.k_TileId_Empty)
                    {
                        Vector2 uvPos = tilemap.Tileset.Tiles[tileIds[i]].uv.position;
                        if (i == 1 || i == 3) uvPos.x += rSubTile.width;
                        if (i == 2 || i == 3) uvPos.y += rSubTile.height;
                        rSubTile.position = uvPos;
                        rUvs[i] = rSubTile;
                    }
                }
                return rUvs;
            }
            return null;
        }

        #endregion
    }
}