using System.Collections.Generic;
using UnityEngine;

namespace ARChess.Scripts.Chess.Pieces
{
    public class King: ChessPiece
    {
        public override List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tileCountX, int tileCountY, ChessTeam startingTeam)
        {
            List<Vector2Int> r = new List<Vector2Int>();

            // Right
            if (currentX + 1 < tileCountX)
            {
                // Right
                if(!board[currentX + 1, currentY])
                    r.Add(new Vector2Int(currentX + 1, currentY));
                else if(board[currentX + 1, currentY].team != team)
                    r.Add(new Vector2Int(currentX + 1, currentY));
                
                // Top Right
                if(currentY + 1 < tileCountY)
                    if(!board[currentX + 1, currentY + 1])
                        r.Add(new Vector2Int(currentX + 1, currentY + 1));
                    else if(board[currentX + 1, currentY + 1].team != team)
                        r.Add(new Vector2Int(currentX + 1, currentY + 1));
                
                // Bottom Right
                if(currentY - 1 >= 0)
                    if(!board[currentX + 1, currentY - 1])
                        r.Add(new Vector2Int(currentX + 1, currentY - 1));
                    else if(board[currentX + 1, currentY - 1].team != team)
                        r.Add(new Vector2Int(currentX + 1, currentY - 1));
            }
            
            // Left
            if (currentX - 1 >= 0)
            {
                // Left
                if(!board[currentX - 1, currentY])
                    r.Add(new Vector2Int(currentX - 1, currentY));
                else if(board[currentX - 1, currentY].team != team)
                    r.Add(new Vector2Int(currentX - 1, currentY));
                
                // Top Right
                if(currentY + 1 < tileCountY)
                    if(!board[currentX - 1, currentY + 1])
                        r.Add(new Vector2Int(currentX - 1, currentY + 1));
                    else if(board[currentX - 1, currentY + 1].team != team)
                        r.Add(new Vector2Int(currentX - 1, currentY + 1));
                
                // Bottom Right
                if(currentY - 1 >= 0)
                    if(!board[currentX - 1, currentY - 1])
                        r.Add(new Vector2Int(currentX - 1, currentY - 1));
                    else if(board[currentX - 1, currentY - 1].team != team)
                        r.Add(new Vector2Int(currentX - 1, currentY - 1));
            }
            
            // Up
            if(currentY + 1 < tileCountY)
                if(!board[currentX, currentY + 1] ||  board[currentX, currentY + 1].team != team)
                   r.Add(new Vector2Int(currentX, currentY + 1));
            
            // Down
            if(currentY - 1 >= 0)
                if(!board[currentX, currentY - 1] ||  board[currentX, currentY - 1].team != team)
                    r.Add(new Vector2Int(currentX, currentY - 1));
            
            return r;
        }

        public override SpecialMove GetSpecialMoves(ref ChessPiece[,] board, ref List<Vector2Int[]> moveList, ref List<Vector2Int> availableMoves,
            ChessTeam startingTeam)
        {
            SpecialMove r = SpecialMove.None;

            int ourY = team == startingTeam ? 0 : 7;

            var kingMove = moveList.Find(m => m[0].x == 4 && m[0].y == ourY);
            var leftRook = moveList.Find(m => m[0].x == 0 && m[0].y == ourY);
            var rightRook = moveList.Find(m => m[0].x == 7 && m[0].y == ourY);

            if (kingMove == null && currentX == 4)
            {
                // Left Rook
                if (leftRook == null)
                {
                    // Make sure there is actually a piece standing there before checking its type!
                    if (board[0, ourY] && board[0, ourY].type == ChessPieceType.Rook)
                    {
                        if (!board[3, ourY] && !board[2, ourY] && !board[1, ourY])
                        {
                            availableMoves.Add(new Vector2Int(2, ourY));
                            r = SpecialMove.Castling;
                        }
                    }
                }
                    
                // Right Rook
                if (rightRook == null)
                {
                    if (board[7, ourY] && board[7, ourY].type == ChessPieceType.Rook)
                    {
                        if (!board[5, ourY] && !board[6, ourY])
                        {
                            availableMoves.Add(new Vector2Int(6, ourY));
                            r = SpecialMove.Castling;
                        }
                    }
                }
            }
            
            return r;
        }
    }
}
