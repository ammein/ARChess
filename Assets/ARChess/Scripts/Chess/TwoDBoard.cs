using System;
using System.Collections.Generic;
using ARChess.Scripts.Chess.Pieces;
using ARChess.Scripts.Utility;
using UnityEngine;
using UnityEngine.UI;

namespace ARChess.Scripts.Chess
{
    public enum CellType
    {
        Black,
        White
    }

    public enum ChessTeam
    {
        Black,
        White
    }

    [RequireComponent(typeof(VerticalLayoutGroup))]
    public class TwoDBoard : MonoBehaviour
    {
        [Serializable]
        public class Cell
        {
            public GameObject prefab;
            public CellType type;
        }

        [Serializable]
        public class Piece
        {
            public Sprite pieceSprite;
            public ChessPieceType type;
            public ChessTeam team;
        }

        [Header("Chessboard Settings")] [SerializeField] [Tooltip("Border Size")]
        public int borderSize;

        [SerializeField] [Tooltip("Cell Size")]
        public Vector2 cellSize;

        [SerializeField] [Tooltip("Border")] public Material border;
        
        [SerializeField] [Tooltip("Empty Cell")] public Material emptyCell;
        
        private ChessTeam startingTeam;

        [Header("Cell Prefabs")] [SerializeField]
        public List<Cell> cellPrefabs = new List<Cell>();

        [Header("Piece Sprites")] [SerializeField]
        public List<Piece> pieces = new List<Piece>();

        private bool _generated;
        private GameObject[,] _tiles;
        private GameObject[,] _spritePieces;
        private bool _subscribed;
        private Chessboard chessboard;

        private void Update()
        {
            if(!chessboard)
                chessboard = FindFirstObjectByType<Chessboard>();
            
            if (!_subscribed && chessboard)
            {
                chessboard.objectPlaced += ObjectPlaced;
                _subscribed = true;
            }
    
            if (chessboard&& chessboard.TileCount.x > 0 && !_generated)
                StartTiles();
        }

        private void OnDisable()
        {
            DestroyGeneratedTiles();
            if (chessboard != null && _subscribed)
            {
                chessboard.objectPlaced -= ObjectPlaced;
                _subscribed = false;
            }
            _generated = false;
        }

        private void StartTiles()
        {
            Log.LogThis("Starting tiles", this);
            startingTeam = chessboard.startingTeam;
            // Create new Board Game Object
            GameObject board = new GameObject("Board");
            board.transform.SetParent(transform);
            board.transform.localScale = Vector3.one;
            board.AddComponent<GridLayoutGroup>();
            // Fit Content
            ContentSizeFitter fit = board.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            // Ensure Grid Layout
            EnsureGridLayout(board.GetComponent<GridLayoutGroup>(), true);
            // Generate Tiles
            Generate2DTiles(board);
            // Assign Board Material for Border
            board.AddComponent<UnityEngine.UI.Image>().material = border;

            // Force all canvases to update layout
            Canvas.ForceUpdateCanvases();

            // NOW the rect height is properly calculated
            transform.GetComponent<VerticalLayoutGroup>().spacing = -board.GetComponent<RectTransform>().rect.height;

            GameObject pieceObject = GenerateParentPiece();
            
            // Generate Pieces
            GeneratePieces(pieceObject);
            _generated = true;
        }

        private GameObject GenerateParentPiece()
        {
            // Create new parent for Piece Game Objects
            GameObject parentPiece = new GameObject("Pieces");
            parentPiece.transform.SetParent(transform);
            parentPiece.transform.localScale = Vector3.one;
            parentPiece.AddComponent<GridLayoutGroup>();
            // Fit Content
            ContentSizeFitter fitPieces = parentPiece.AddComponent<ContentSizeFitter>();
            fitPieces.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitPieces.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            EnsureGridLayout(parentPiece.GetComponent<GridLayoutGroup>(), false);
            
            return parentPiece;
        }


        private void EnsureGridLayout(GridLayoutGroup gridLayoutGroup, bool border)
        {
            if (border)
                gridLayoutGroup.padding = new RectOffset(borderSize, borderSize, borderSize, borderSize);
            gridLayoutGroup.cellSize = cellSize;
            gridLayoutGroup.SetLayoutVertical();
            gridLayoutGroup.startCorner = GridLayoutGroup.Corner.LowerLeft;
            gridLayoutGroup.childAlignment = TextAnchor.LowerLeft;
            gridLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayoutGroup.constraintCount = (int)chessboard.TileCount.y;
        }

        private void Generate2DTiles(GameObject parent)
        {
            Vector2 tileCounts = chessboard.TileCount;
            CellType type = cellPrefabs[0].type;
            _tiles = new GameObject[(int)tileCounts.x, (int)tileCounts.y];

            for (int x = 0; x < tileCounts.x; x++)
            {
                for (int y = 0; y < tileCounts.y; y++)
                {
                    GameObject prefabToInstantiate = cellPrefabs.Find(cell => cell.type == type).prefab;
                    GameObject cell = Instantiate(prefabToInstantiate, parent.transform);
                    cell.name = $"Tile {x},{y}";
                    _tiles[x, y] = cell;
                    // Next Iteration
                    type = cellPrefabs.Find(nextCell =>
                        y < tileCounts.y - 1 ? nextCell.type != type : nextCell.type == type).type;
                }
            }
        }

        private void GeneratePieces(GameObject parent)
        {
            _spritePieces = new GameObject[(int)chessboard.TileCount.x, (int)chessboard.TileCount.y];

            // Iterate Y first (outer), then X (inner) - matches grid layout
            for (int y = 0; y < chessboard.TileCount.y; y++)
            {
                for (int x = 0; x < chessboard.TileCount.x; x++)
                {
                    // Determine which team (if any) should be at this position
                    ChessTeam? teamAtPosition = GetTeamAtPosition(x, y);

                    if (teamAtPosition == null)
                    {
                        // Empty cell
                        GameObject emptyObject = new GameObject($"Empty_{x},{y}");
                        emptyObject.transform.SetParent(parent.transform);
                        _spritePieces[x, y] = emptyObject;
                        AddEmptySprite(emptyObject, x, y);
                    }
                    else
                    {
                        // Piece cell
                        ChessTeam team = teamAtPosition.Value;
                        ChessPieceType pieceType = GetPieceTypeAtPosition(x, y, team);

                        Piece piece = pieces.Find(p => p.type == pieceType && p.team == team);
                        if (piece != null)
                        {
                            GameObject pieceObject = new GameObject($"{piece.type}_{x},{y}");
                            pieceObject.transform.SetParent(parent.transform);
                            _spritePieces[x, y] = pieceObject;
                            AddSprite(pieceObject, piece, x, y);
                        }
                    }
                }
            }
        }



        private ChessTeam? GetTeamAtPosition(int x, int y)
        {
            int boardHeight = (int)chessboard.TileCount.y;

            // White team at bottom rows (0-1)
            if (y == 0 || y == 1)
                return startingTeam == ChessTeam.White ? ChessTeam.White : ChessTeam.Black;

            // Black team at top rows (boardHeight-2 to boardHeight-1)
            if (y == boardHeight - 2 || y == boardHeight - 1)
                return startingTeam == ChessTeam.White ? ChessTeam.Black : ChessTeam.White;

            // Empty cells in between
            return null;
        }

        private ChessPieceType GetPieceTypeAtPosition(int x, int y, ChessTeam team)
        {
            int boardHeight = (int)chessboard.TileCount.y;

            // Determine if this is back row or pawn row
            bool isBackRow = (team == startingTeam && y == 0) ||
                             (team != startingTeam && y == boardHeight - 1);
            bool isPawnRow = (team == startingTeam && y == 1) ||
                             (team != startingTeam && y == boardHeight - 2);

            // Pawns
            if (isPawnRow)
                return ChessPieceType.Pawn;

            // Back row pieces
            if (isBackRow)
            {
                return x switch
                {
                    0 or 7 => ChessPieceType.Rook,
                    1 or 6 => ChessPieceType.Knight,
                    2 or 5 => ChessPieceType.Bishop,
                    3 => ChessPieceType.Queen,
                    4 => ChessPieceType.King,
                    _ => ChessPieceType.Pawn
                };
            }

            return ChessPieceType.Pawn;
        }

        private void AddSprite(GameObject pieceObject, Piece chessPiece, int x, int y)
        {
            pieceObject.AddComponent<UnityEngine.UI.Image>().sprite = chessPiece.pieceSprite;
            _spritePieces[x, y] = pieceObject;
        }

        private void AddEmptySprite(GameObject pieceObject, int x, int y)
        {
            pieceObject.AddComponent<UnityEngine.UI.Image>().material = emptyCell;
            _spritePieces[x, y] = pieceObject;
        }
        
        private void AssignSprite(int x, int y, Sprite sprite)
        {
            _spritePieces[x,y].GetComponent<UnityEngine.UI.Image>().sprite = sprite;
            if (_spritePieces[x, y].GetComponent<UnityEngine.UI.Image>().material != null)
                _spritePieces[x, y].GetComponent<UnityEngine.UI.Image>().material = null;
        }

        private void AssignEmptySprite(int x, int y)
        {
            _spritePieces[x,y].GetComponent<UnityEngine.UI.Image>().material = emptyCell;
            if (_spritePieces[x, y].GetComponent<UnityEngine.UI.Image>().sprite != null)
                _spritePieces[x, y].GetComponent<UnityEngine.UI.Image>().sprite = null;
        }

        private void DestroyGeneratedTiles()
        {
            if (transform.Find("Board") != null)
                Destroy(transform.Find("Board").gameObject);
            if(transform.Find("Pieces") != null)
                Destroy(transform.Find("Pieces").gameObject);
        }

        public void OnReset()
        {
            for(int y = 0; y < chessboard.TileCount.y; y++)
                for (int x = 0; x < chessboard.TileCount.x; x++)
                {
                    if (_spritePieces[x, y].GetComponent<UnityEngine.UI.Image>().sprite != null)
                    {
                        Destroy(_spritePieces[x, y].gameObject);
                    }
                    
                    _spritePieces[x, y] = null;
                }
            
            if(transform.Find("Pieces") != null)
                Destroy(transform.Find("Pieces").gameObject);

            GameObject parentPiece = GenerateParentPiece();
            
            GeneratePieces(parentPiece);
        }

        // Operations
        private void ObjectPlaced(ChessPiece previousPiece, int x, int y)
        {
            UnityEngine.UI.Image previousImage = _spritePieces[previousPiece.currentX, previousPiece.currentY].GetComponent<UnityEngine.UI.Image>();
            AssignSprite(x, y, previousImage.sprite);
            AssignEmptySprite(previousPiece.currentX, previousPiece.currentY);
        }
    }
}