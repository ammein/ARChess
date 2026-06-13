using System;
using System.Collections.Generic;
using UnityEngine;

namespace ARChess.Scripts.Chess.Pieces
{
    public class Pawn : ChessPiece
    {
        public override List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tileCountX, int tileCountY, ChessTeam startingTeam)
        {
            List<Vector2Int> r = new List<Vector2Int>();

            int direction = (team == startingTeam) ? 1 : -1;
            
            // One in front
            if (!board[currentX, currentY + direction])
            {
                Debug.Log("One In Front - Pawn");
                r.Add(new Vector2Int(currentX, currentY + direction));
            }
            
            // Two in front
            if (!board[currentX, currentY + direction])
            {
                // Your Team
                if(team == startingTeam && currentY == 1 && !board[currentX, currentY + direction * 2])
                    r.Add(new Vector2Int(currentX, currentY + direction * 2));
                
                // Enemy Team
                if(team != startingTeam && currentY == tileCountY - 2 && !board[currentX, currentY + direction * 2])
                    r.Add(new Vector2Int(currentX, currentY + direction * 2));
            }
            
            // Kill move
            if(currentX != tileCountX - 1)
                if(board[currentX + 1, currentY + direction] && board[currentX + 1, currentY + direction].team != team)
                    r.Add(new Vector2Int(currentX + 1, currentY + direction));
            if(currentX != 0)
                if(board[currentX - 1, currentY + direction] && board[currentX - 1, currentY + direction].team != team)
                    r.Add(new Vector2Int(currentX - 1, currentY + direction));

            return r;
        }

        public override SpecialMove GetSpecialMoves(ref ChessPiece[,] board, ref List<Vector2Int[]> moveList,
            ref List<Vector2Int> availableMoves, ChessTeam startingTeam)
        {
            int direction = (team == startingTeam) ? 1 : -1;
            
            // Promotion
            if ((team == ChessTeam.Black && currentY == 6) || (team == ChessTeam.White && currentY == 1))
                return SpecialMove.Promotion;
            
            // En Passant
            if (moveList.Count > 0)
            {
                Vector2Int[] lastMove = moveList[moveList.Count - 1];
                if (board[lastMove[1].x, lastMove[1].y].type is ChessPieceType.Pawn) // If the last piece moved was a pawn
                {
                    if (Mathf.Abs(lastMove[0].y - lastMove[1].y) == 2) // If the last move was a +2 in either direction
                    {
                        if (board[lastMove[1].x, lastMove[1].y].team != team) // If the move was from other team
                        {
                            if (lastMove[1].y == currentY)// If both pawns are on the same Y
                            {
                                if (lastMove[1].x == currentX - 1) // Landed Left
                                {
                                    availableMoves.Add(new Vector2Int(currentX - 1, currentY + direction));
                                    return SpecialMove.EnPassant;
                                }
                                
                                if (lastMove[1].x == currentX + 1) // Landed right
                                {
                                    availableMoves.Add(new Vector2Int(currentX + 1, currentY + direction));
                                    return SpecialMove.EnPassant;
                                }
                            } 
                        }
                    }
                }
            }
            
            return SpecialMove.None;
        }
    }
}
