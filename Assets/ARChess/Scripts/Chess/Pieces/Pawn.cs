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
            if(board[currentX, currentY + direction] == null)
                r.Add(new Vector2Int(currentX, currentY + direction));
            
            // Two in front
            if (board[currentX, currentY + direction] == null)
            {
                // Your Team
                if(team == startingTeam && currentY == 1 && board[currentX, currentY + direction * 2] == null)
                    r.Add(new Vector2Int(currentX, currentY + direction * 2));
                
                // Enemy Team
                if(team != startingTeam && currentY == tileCountY - 2 && board[currentX, currentY + direction * 2] == null)
                    r.Add(new Vector2Int(currentX, currentY + direction * 2));
            }
            
            // Kill move
            if(currentX != tileCountX - 1)
                if(board[currentX + 1, currentY + direction] != null && board[currentX + 1, currentY + direction].team != team)
                    r.Add(new Vector2Int(currentX + 1, currentY + direction));
            if(currentX != 0)
                if(board[currentX - 1, currentY + direction] != null && board[currentX - 1, currentY + direction].team != team)
                    r.Add(new Vector2Int(currentX - 1, currentY + direction));

            return r;
        }
    }
}
