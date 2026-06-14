using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ARChess.Scripts.Chess.Pieces;
using ARChess.Scripts.Lights;
using ARChess.Scripts.Project;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using ARChess.Scripts.Utility;

namespace ARChess.Scripts.Chess
{
    public enum SpecialMove
    {
        None = 0,
        EnPassant,
        Castling,
        Promotion,
    }
    
    public class Chessboard : MonoBehaviour
    {
        [Serializable]
        public struct Piece
        {
            public GameObject prefabs;
            public ChessPiece.AppearanceState[] appearance;
            public ChessPieceType type;
        }

        [Serializable]
        public class TeamMaterials
        {
            public Material material;
            public ChessTeam team;
        }

        [Header("Art Stuff")] [SerializeField] private Material tileMaterial;
        [SerializeField] [Range(0.01f, 1f)] private float deathSize = 0.3f;
        [SerializeField] private float deathDistance = 0.3f;
        [SerializeField] private float dragOffset = 1.5f;

        [Header("Prefabs & Materials")] [SerializeField]
        List<Piece> pieces = new List<Piece>();

        [SerializeField] private List<TeamMaterials> teamMaterials = new List<TeamMaterials>();

        [Header("Chess Settings")] [SerializeField] [Tooltip("Tile size of the chessboard")]
        private float m_tileSize = 1;

        [SerializeField] [Tooltip("Y Offset of the chessboard")]
        private float yOffset = 0f;

        [SerializeField] [Tooltip("Board Center of the chessboard")]
        private Vector3 boardCenter = Vector3.zero;
        
        [SerializeField] [Tooltip("Project State Options")]
        private ProjectStateOptions projectStateOptions;

        [HideInInspector]
        public ChessTeam startingTeam;
        
        [HideInInspector]
        public GameObject yourTurnUI;
        
        [HideInInspector]
        public string playerWins = "";
        
        [HideInInspector]
        public string teamWins = "";

        /// <summary>
        /// Event invoked after a piece is MoveTo
        /// </summary>
        public event Action<ChessPiece, int, int> objectPlaced;

        // LOGIC
        private ChessPiece[,] chessPieces;
        private ChessPiece currentlyDragging;
        private List<Vector2Int> availableMoves = new List<Vector2Int>();
        private const int TILE_COUNT_X = 8;
        private const int TILE_COUNT_Y = 8;
        private GameObject[,] tiles;
        private GameObject[,] tilesBounds;
        private Camera currentCamera;
        private Vector2Int currentHover;
        private BoxCollider chessCollider;
        private GameObject ChessTiles;
        private GameObject ChessVisuals;
        private GameObject ChessAttach;
        private Vector3 bounds;
        private bool _isDragging;
        private List<ChessPiece> deadWhites = new List<ChessPiece>();
        private List<ChessPiece> deadBlacks = new List<ChessPiece>();
        private GameObject _directionalLight;
        private AmbientLightEstimation _ambientLightEstimation;
        private bool isWhiteTurn;
        
        private SpecialMove specialMove;
        private List<Vector2Int[]> moveList = new List<Vector2Int[]>();

        public BoxCollider ChessCollider => chessCollider;

        public GameObject AttachObject
        {
            get => ChessAttach;
            set => ChessAttach = value;
        }

        public float TileSize
        {
            get => m_tileSize;
            set => m_tileSize = value;
        }

        public bool MyTurn { get; set; }
        
        public bool EndGame { get; set; }

        public Vector2 TileCount => new(TILE_COUNT_X, TILE_COUNT_Y);
        
        public List<TeamMaterials> PieceMaterials => teamMaterials;

        private void Awake()
        {
            isWhiteTurn = true;
            MyTurn = startingTeam == ChessTeam.White;
            try
            {
                ChessTiles = GameObject.Find("All Chess Tiles");
            }
            catch (Exception e)
            {
                Debug.LogError(
                    "GameObject that named <color=\"blue\">All Chess Tiles</color> could not be found.\n" + e);
            }

            try
            {
                ChessAttach = GameObject.Find("Chess Attach");
            }
            catch (Exception e)
            {
                Debug.LogError(
                    "GameObject that named <color=\"blue\">Chess Attach</color> could not be found. And you must set XR Grab Interactable script for the transform attach to this object\n" +
                    e);
            }

            try
            {
                ChessVisuals = GameObject.Find("Chess Visuals");
            }
            catch (Exception e)
            {
                Debug.LogError("GameObject that named <color=\"blue\">Chess Visuals</color> could not be found.\n" + e);
            }

            currentCamera = Camera.main;

            // Generate All Tiles
            var generated = GenerateAllTiles(m_tileSize);

            if (generated)
            {
                // Spawn All Pieces
                SpawnAllPieces();

                // Position All Pieces
                PositionAllPieces();

                // Animate Pieces to appear
                AnimateAllPiece();
            }
        }

        private void Start()
        {
            // Find Light
            _directionalLight = FindAnyObjectByType<Light>().transform.gameObject;
            if (_directionalLight)
                _ambientLightEstimation = _directionalLight.GetComponent<AmbientLightEstimation>();
        }

        private void Update()
        {
            if (_directionalLight && _ambientLightEstimation)
            {
                // Lights follow board
                _directionalLight.transform.position =
                    gameObject.transform.position + _ambientLightEstimation.DynamicLightPosition;
                _directionalLight.transform.rotation = gameObject.transform.rotation *
                                                       Quaternion.Euler(_ambientLightEstimation.DynamicLightRotation);
            }
            
            EnableMatchmakingUI(MyTurn);
        }

        private void EnableMatchmakingUI(bool state)
        {
            if(yourTurnUI is not null && yourTurnUI.activeInHierarchy != state)
            {
                yourTurnUI.SetActive(state);
            }
        }

        public void ChessInteract(Vector2 position, bool interact)
        {
            Ray ray = currentCamera.ScreenPointToRay(new Vector3(position.x, position.y, 0));
            HitTile(ray, Info, interact);
        }

        public RaycastHit Info { get; set; }

        private void HitTile(Ray ray, RaycastHit info, bool touched)
        {
            // To prevent raycast to infinite distance, we have to make the endpoint only react to Tile or 100 max distance
            if (Physics.Raycast(ray, out info, 100,
                    LayerMask.GetMask("Tile", "Selected", "Bound Selected", "Visual Tile", "Highlight")))
            {
                // Get the indexes of the tile I've hit
                Vector2Int hitPosition = LookupTileIndex(info.collider.gameObject);

                // If we're hovering a tile after not hovering any tiles
                if (currentHover == -Vector2Int.one)
                {
                    Log.LogThis($"Tile {hitPosition.x},{hitPosition.y} hit", this);
                    currentHover = hitPosition;
                    // Change Layer to "Hover"
                    tilesBounds[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Bound Selected");
                    tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Selected");
                }

                // If we were already hovering a tile, change the previous one
                if (currentHover != hitPosition)
                {
                    Log.LogThis($"Tile {currentHover.x},{currentHover.y} hit", this);
                    tilesBounds[currentHover.x, currentHover.y].layer = LayerMask.NameToLayer("Tile");
                    tiles[currentHover.x, currentHover.y].layer = ContainsValidMove(ref availableMoves, currentHover) ? LayerMask.NameToLayer("Highlight") :  LayerMask.NameToLayer("Visual Tile");
                    currentHover = hitPosition;
                    // Change Layer to "Hover"
                    tilesBounds[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Bound Selected");
                    tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Selected");
                }
                
                // If current touch hit the piece
                if (touched && chessPieces[hitPosition.x, hitPosition.y])
                {
                    // If currentlyDragging is already dragging, cancel it...
                    if (currentlyDragging) return;
                        
                    // Is it our turn?
                    if ((chessPieces[hitPosition.x, hitPosition.y].team == ChessTeam.White && isWhiteTurn) || (chessPieces[hitPosition.x, hitPosition.y].team == ChessTeam.Black && !isWhiteTurn))
                    {
                        currentlyDragging = chessPieces[hitPosition.x, hitPosition.y];
                            
                        // Get a list of where I can go, highlight tiles as well
                        availableMoves = currentlyDragging.GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y, startingTeam);
                        
                        // Get a list of special moves as well
                        specialMove =
                            currentlyDragging.GetSpecialMoves(ref chessPieces, ref moveList, ref availableMoves, startingTeam);

                        PreventCheck();

                        HighlightTiles();
                    }
                }

                // If the piece is dropped, player has made move. Now do move checks...
                if (currentlyDragging && !touched)
                {
                    Vector2Int previousPosition =
                        new Vector2Int(currentlyDragging.currentX, currentlyDragging.currentY);
                    
                    var validMove = MoveTo(currentlyDragging, hitPosition.x, hitPosition.y);
                    
                    switch (validMove)
                    {
                        case false:
                            currentlyDragging.SetPosition(GetTileCenter(previousPosition.x, previousPosition.y));
                            break;
                        case true:
                            MyTurn = currentlyDragging.team != startingTeam;
                            break;
                    }

                    currentlyDragging = null;
                    RemoveHighlightTiles();
                }
            }
            else
            {
                // If the current hover/selected is not valid
                if (currentHover != -Vector2Int.one)
                {
                    tilesBounds[currentHover.x, currentHover.y].layer = LayerMask.NameToLayer("Tile");
                    tiles[currentHover.x, currentHover.y].layer = ContainsValidMove(ref availableMoves, currentHover) ? LayerMask.NameToLayer("Highlight") :  LayerMask.NameToLayer("Visual Tile");
                    currentHover = -Vector2Int.one;
                }

                // Else, if it not hit the appropriate raycast, and currentlyDragging is selected AND if it is not touched anymore
                if (currentlyDragging && !touched)
                {
                    currentlyDragging.SetPosition(GetTileCenter(currentlyDragging.currentX,
                        currentlyDragging.currentY));
                    currentlyDragging = null;
                    RemoveHighlightTiles();
                }
            }

            // If dragging a piece, animate the position of a piece
            if (currentlyDragging)
            {
                // Get the cell's world position
                Vector3 cellLocalPos = GetTileCenter(currentlyDragging.currentX, currentlyDragging.currentY);
                Vector3 cellWorldPos = currentlyDragging.transform.parent.TransformPoint(cellLocalPos);

                // Create plane at the cell's Y level
                Plane horizontalPlane = new Plane(Vector3.up, new Vector3(0, cellWorldPos.y, 0));
                float distance = 0.0f;

                if (horizontalPlane.Raycast(ray, out distance))
                {
                    Vector3 worldPosition = ray.GetPoint(distance) + Vector3.up * dragOffset;

                    // Convert world position to local position relative to the piece's parent
                    Vector3 localPosition = currentlyDragging.transform.parent.InverseTransformPoint(worldPosition);

                    currentlyDragging.SetPosition(localPosition);
                }
            }
        }

        private void HighlightTiles()
        {
            for (int i = 0; i < availableMoves.Count; i++)
                tiles[availableMoves[i].x, availableMoves[i].y].layer = LayerMask.NameToLayer("Highlight");
        }
        
        private void RemoveHighlightTiles()
        {
            for (int i = 0; i < availableMoves.Count; i++)
                tiles[availableMoves[i].x, availableMoves[i].y].layer = LayerMask.NameToLayer("Tile");
            
            availableMoves.Clear();
        }

        // Generate the board
        public bool GenerateAllTiles(float tileSize, int tileCountX = TILE_COUNT_X, int tileCountY = TILE_COUNT_Y)
        {
            yOffset += transform.position.y;
            m_tileSize = tileSize;
            tiles = new GameObject[tileCountX, tileCountY];
            tilesBounds = new GameObject[tileCountX, tileCountY];

            // Calculate half the total size to center the chessboard
            bounds = new Vector3(((float)tileCountX / 2f) * tileSize, 0, ((float)tileCountX / 2f) * tileSize) +
                     boardCenter;

            try
            {
                for (int x = 0; x < tileCountX; x++)
                    for (int y = 0; y < tileCountY; y++)
                        GenerateSingleTiles(tileSize, x, y);

                AddChessBound(tiles, tileCountX, tileCountY);

                // Add Collider into XR Grab Interactable
                XRGrabInteractable interactable = gameObject.GetComponent<XRGrabInteractable>();
                if (interactable != null)
                {
                    interactable.colliders.Add(ChessAttach.GetComponent<BoxCollider>());
                    interactable.predictedVisualsTransform = ChessVisuals.transform;
                    StartCoroutine(ReregisterInteractable(interactable));
                }

                return true;
            }
            catch (System.Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// A Helper method since the XR does not have colliders to be updated dynamically, This function will reregister XR Grab Interactable
        /// into Interaction Manager
        ///
        /// See: https://discussions.unity.com/t/how-to-add-child-colliders-to-a-parent-xrgrabinteractable-collider-list/891324/6
        /// </summary>
        /// <param name="interactable"></param>
        /// <returns></returns>
        private IEnumerator ReregisterInteractable(XRGrabInteractable interactable)
        {
            yield return new WaitForEndOfFrame();
            interactable.interactionManager.UnregisterInteractable(interactable as IXRInteractable);

            yield return new WaitForEndOfFrame();
            interactable.interactionManager.RegisterInteractable(interactable as IXRInteractable);

            yield return null;
        }

        private void AddChessBound(GameObject[,] allTiles, int tileCountX, int tileCountY)
        {
            // Add Box Collider to Empty Game Object
            chessCollider = ChessAttach.AddComponent<BoxCollider>();

            // Initialize Bounds to encompass all tiles
            Bounds totalBounds = new Bounds();

            // Calculate the bounds based on the tile positions
            for (int x = 0; x < tileCountX; x++)
            {
                for (int y = 0; y < tileCountY; y++)
                {
                    if (allTiles[x, y] == null) continue;

                    // Get the mesh bounds in local space
                    Mesh childMesh = allTiles[x, y].GetComponent<MeshFilter>().mesh;

                    // Calculate the bounds in world space
                    Vector3 tilePosition = allTiles[x, y].transform.position; // Use world position
                    Bounds meshBounds = childMesh.bounds;
                    // Create a new Bounds object to encapsulate the mesh bounds in world space
                    Bounds worldBounds = new Bounds(tilePosition + meshBounds.center, meshBounds.size);

                    // Encapsulate the bounds
                    totalBounds.Encapsulate(worldBounds);
                }
            }

            // Set the size of the collider based on the total bounds
            chessCollider.size = totalBounds.size;

            // Set the center of the collider correctly
            chessCollider.center =
                totalBounds.center - ChessAttach.transform.position; // Adjust for the parent's position if necessary

            chessCollider.providesContacts = true;
        }

        private void GenerateSingleTiles(float tileSize, int x, int y)
        {
            // Create Visual Tile
            GameObject tileObject = new GameObject($"Tile: ({x}, {y})");
            tileObject.transform.SetParent(ChessVisuals.transform);
            tileObject.layer = LayerMask.NameToLayer("Visual Tile");

            Mesh mesh = new Mesh();

            tileObject.AddComponent<MeshFilter>().mesh = mesh;
            tileObject.AddComponent<MeshRenderer>().material = tileMaterial;

            Vector3[] vertices = new Vector3[4];
            vertices[0] = new Vector3(x * tileSize, yOffset, y * tileSize) - bounds;
            vertices[1] = new Vector3(x * tileSize, yOffset, (y + 1) * tileSize) - bounds;
            vertices[2] = new Vector3((x + 1) * tileSize, yOffset, y * tileSize) - bounds;
            vertices[3] = new Vector3((x + 1) * tileSize, yOffset, (y + 1) * tileSize) - bounds;

            int[] tris = new int[] { 0, 1, 2, 1, 3, 2 };

            Vector2[] uvs = new Vector2[4];
            uvs[0] = new Vector2(0f, 0f);
            uvs[1] = new Vector2(0f, 1f);
            uvs[2] = new Vector2(1f, 0f);
            uvs[3] = new Vector2(1f, 1f);

            mesh.vertices = vertices;
            mesh.triangles = tris;
            mesh.uv = uvs;
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();

            tiles[x, y] = tileObject;

            // Create Bounds Tile
            GameObject tileBounds = new GameObject($"X:{x} Y:{y}");
            tileBounds.transform.SetParent(ChessTiles.transform);
            tileBounds.AddComponent<MeshFilter>().mesh = mesh;
            tileBounds.AddComponent<MeshRenderer>().material = tileMaterial;
            tileBounds.layer = LayerMask.NameToLayer("Tile");
            tileBounds.transform.position = tileObject.transform.position;

            BoxCollider boxCollider = tileBounds.AddComponent<BoxCollider>();
            boxCollider.isTrigger = true;

            tilesBounds[x, y] = tileBounds;
        }

        public void ToggleContact(bool toggle)
        {
            if (tilesBounds.Length == 0) return;
            for (int x = 0; x < TILE_COUNT_X; x++)
            {
                for (int y = 0; y < TILE_COUNT_Y; y++)
                {
                    tilesBounds[x, y].GetComponent<BoxCollider>().providesContacts = toggle;
                }
            }
        }

        private void SpawnAllPieces()
        {
            chessPieces = new ChessPiece[TILE_COUNT_X, TILE_COUNT_Y];

            // Arrange by materials to assign team dynamically
            for (int team = 0; team < teamMaterials.Count; team++)
            {
                // Always iterate through all X positions
                for (int x = 0; x < TILE_COUNT_X; x++)
                {
                    AssignPieceType(x, teamMaterials[team].team);   
                }
            }
        }


        private void AssignPieceType(int x, ChessTeam checkTeam)
        {
            // Check if this is the starting team to determine Y position
            bool isStartingTeam = (checkTeam == startingTeam);

            switch (x)
            {
                case 0:
                    chessPieces[x, isStartingTeam ? 0 : TILE_COUNT_Y - 1] = SpawnSinglePiece(
                        pieces.Find(p => p.type == ChessPieceType.Rook), checkTeam,
                        teamMaterials.Find(m => m.team == checkTeam).material);
                    goto default;
                case 1:
                    chessPieces[x, isStartingTeam ? 0 : TILE_COUNT_Y - 1] = SpawnSinglePiece(
                        pieces.Find(p => p.type == ChessPieceType.Knight), checkTeam,
                        teamMaterials.Find(m => m.team == checkTeam).material);
                    goto default;
                case 2:
                    chessPieces[x, isStartingTeam ? 0 : TILE_COUNT_Y - 1] = SpawnSinglePiece(
                        pieces.Find(p => p.type == ChessPieceType.Bishop), checkTeam,
                        teamMaterials.Find(m => m.team == checkTeam).material);
                    goto default;
                case 3:
                    chessPieces[x, isStartingTeam ? 0 : TILE_COUNT_Y - 1] = SpawnSinglePiece(
                        pieces.Find(p => p.type == ChessPieceType.Queen), checkTeam,
                        teamMaterials.Find(m => m.team == checkTeam).material);
                    goto default;
                case 4:
                    chessPieces[x, isStartingTeam ? 0 : TILE_COUNT_Y - 1] = SpawnSinglePiece(
                        pieces.Find(p => p.type == ChessPieceType.King), checkTeam,
                        teamMaterials.Find(m => m.team == checkTeam).material);
                    goto default;
                case 5:
                    chessPieces[x, isStartingTeam ? 0 : TILE_COUNT_Y - 1] = SpawnSinglePiece(
                        pieces.Find(p => p.type == ChessPieceType.Bishop), checkTeam,
                        teamMaterials.Find(m => m.team == checkTeam).material);
                    goto default;
                case 6:
                    chessPieces[x, isStartingTeam ? 0 : TILE_COUNT_Y - 1] = SpawnSinglePiece(
                        pieces.Find(p => p.type == ChessPieceType.Knight), checkTeam,
                        teamMaterials.Find(m => m.team == checkTeam).material);
                    goto default;
                case 7:
                    chessPieces[x, isStartingTeam ? 0 : TILE_COUNT_Y - 1] = SpawnSinglePiece(
                        pieces.Find(p => p.type == ChessPieceType.Rook), checkTeam,
                        teamMaterials.Find(m => m.team == checkTeam).material);
                    goto default;
                default:
                    chessPieces[x, isStartingTeam ? 1 : TILE_COUNT_Y - 2] = SpawnSinglePiece(
                        pieces.Find(p => p.type == ChessPieceType.Pawn), checkTeam,
                        teamMaterials.Find(m => m.team == checkTeam).material);
                    break;
            }
        }


        private ChessPiece SpawnSinglePiece(Piece piece, ChessTeam team, Material teamMaterial, bool forceAppear = false)
        {
            ChessPiece cp = Instantiate(piece.prefabs, ChessVisuals.transform).GetComponent<ChessPiece>();
            List<ChessPiece.AppearanceState> appearance = new List<ChessPiece.AppearanceState>();
            ChessPiece.AppearanceState[] appearanceState = piece.appearance;

            foreach (ChessPiece.AppearanceState state in appearanceState)
            {
                appearance.Add(state);
            }

            cp.appearance = appearance;

            cp.type = piece.type;
            cp.team = team;
            
            // If don't match on first team, rotate local rotation
            if(team != startingTeam)
                cp.transform.localRotation = Quaternion.Euler(0, 180f, 0);

            cp.GetComponent<MeshRenderer>().material = teamMaterial;
            if (cp.gameObject.transform.childCount > 0)
                for (int i = 0; i < cp.gameObject.transform.childCount; i++)
                    AssignChildrenMaterial(cp.GetComponent<MeshRenderer>().material,
                        cp.gameObject.transform.GetChild(i).gameObject);
            
            if (forceAppear)
                cp.AppearPiece("_Progress", 0, b => { });
            return cp;
        }

        private void AssignChildrenMaterial(Material material, GameObject child)
        {
            if(child.TryGetComponent(out MeshRenderer meshRenderer))
                meshRenderer.material = material;

            if (child.transform.childCount > 0)
            {
                for (int i = 0; i < child.transform.childCount; i++)
                    AssignChildrenMaterial(material, child.transform.GetChild(i).gameObject);
            }
        }

        private void PositionAllPieces()
        {
            for (int x = 0; x < TILE_COUNT_X; x++)
            for (int y = 0; y < TILE_COUNT_Y; y++)
                if (chessPieces[x, y] != null)
                    PositionSinglePiece(x, y, true);
        }

        private void PositionSinglePiece(int x, int y, bool force = false)
        {
            chessPieces[x, y].currentX = x;
            chessPieces[x, y].currentY = y;
            chessPieces[x, y].SetPosition(GetTileCenter(x, y), force);
        }

        private void AnimateAllPiece()
        {
            for (int x = 0; x < TILE_COUNT_X; x++)
            for (int y = 0; y < TILE_COUNT_Y; y++)
                if (chessPieces[x, y] != null)
                {
                    float duration = chessPieces[x, y].appearance
                        .Find(match => match.appearance.Equals(Appearance.Appear)).duration;
                    chessPieces[x, y].AppearPiece("_Progress", duration, b => { });
                }
        }

        private Vector3 GetTileCenter(int x, int y)
        {
            return new Vector3(x * m_tileSize, yOffset, y * m_tileSize) - bounds +
                   new Vector3(m_tileSize / 2, 0, m_tileSize / 2);
        }
        
        // Checkmate
        private void Checkmate(ChessTeam team)
        {
            if (team != ChessTeam.Stalemate)
                DisplayVictory(team);
            else
                DisplayStale();
        }

        private void DisplayVictory(ChessTeam team)
        {
            EndGame = true;
            StringBuilder teamWinsString = new StringBuilder();
            StringBuilder playerWinsString = new StringBuilder();
            teamWinsString.AppendFormat("{0} team wins!", team.ToString());
            playerWinsString.Append(startingTeam == team ? projectStateOptions.playerName : "Your Opponent");
            teamWins = teamWinsString.ToString();
            playerWins = playerWinsString.ToString();
        }

        private void DisplayStale()
        {
            EndGame = true;
            StringBuilder teamLosesString = new StringBuilder();
            StringBuilder loseTitle = new StringBuilder();
            teamLosesString.AppendFormat(
                "Consider draw for both team since there are no legal moves available");
            loseTitle.Append("Stalemate");
            teamWins = teamLosesString.ToString();
            playerWins = loseTitle.ToString();
        }

        public void OnResetButton()
        {
            EndGame = false;
            
            // Fields reset
            currentlyDragging = null;
            availableMoves.Clear();
            moveList.Clear();
            
            // Clean up
            for(int x = 0; x < TILE_COUNT_X; x++)
                for (int y = 0; y < TILE_COUNT_Y; y++)
                {
                    if (chessPieces[x, y] != null)
                        Destroy(chessPieces[x, y].gameObject);
                    
                    chessPieces[x, y] = null;
                }
            
            foreach (var white in deadWhites)
                Destroy(white.gameObject);

            foreach (var black in deadBlacks)
                Destroy(black.gameObject);
            
            deadWhites.Clear();
            deadBlacks.Clear();
            
            SpawnAllPieces();
            PositionAllPieces();
            AnimateAllPiece();
            isWhiteTurn = true;
            MyTurn = startingTeam == ChessTeam.White;
        }
        
        // Special Moves
        private void ProcessSpecialMove()
        {
            if (specialMove is SpecialMove.EnPassant)
            {
                var newMove = moveList[moveList.Count - 1];
                ChessPiece myPawn = chessPieces[newMove[1].x, newMove[1].y];
                var targetPawnPosition = moveList[moveList.Count - 2];
                ChessPiece enemyPawn = chessPieces[targetPawnPosition[1].x, targetPawnPosition[1].y];

                if (myPawn.currentX == enemyPawn.currentX)
                {
                    if (myPawn.currentY == enemyPawn.currentY - 1 || myPawn.currentY == enemyPawn.currentY + 1)
                    {
                        DestroyPiece(enemyPawn, enemyPawn.team != startingTeam ? ChessTeam.White : ChessTeam.Black);
                    }
                }
            }

            if (specialMove is SpecialMove.Promotion)
            {
                Vector2Int[] lastMove = moveList[moveList.Count - 1];
                ChessPiece targetPawn = chessPieces[lastMove[1].x, lastMove[1].y];

                if (targetPawn.type == ChessPieceType.Pawn)
                {
                    if (targetPawn.team == ChessTeam.Black && lastMove[1].y == 7)
                    {
                        ChessPiece newQueen = SpawnSinglePiece(pieces.Find(p => p.type == ChessPieceType.Queen), ChessTeam.Black, teamMaterials.Find(m => m.team == ChessTeam.Black).material, true);
                        // Small movement transition
                        newQueen.transform.position = chessPieces[lastMove[1].x, lastMove[1].y].transform.position;
                        // Destroy piece
                        Destroy(chessPieces[lastMove[1].x, lastMove[1].y].gameObject);
                        // Position piece
                        chessPieces[lastMove[1].x, lastMove[1].y] = newQueen;
                        PositionSinglePiece(lastMove[1].x, lastMove[1].y);
                    }
                    
                    if (targetPawn.team == ChessTeam.White && lastMove[1].y == 0)
                    {
                        ChessPiece newQueen = SpawnSinglePiece(pieces.Find(p => p.type == ChessPieceType.Queen), ChessTeam.White, teamMaterials.Find(m => m.team == ChessTeam.White).material, true);
                        // Small movement transition
                        newQueen.transform.position = chessPieces[lastMove[1].x, lastMove[1].y].transform.position;
                        // Destroy piece
                        Destroy(chessPieces[lastMove[1].x, lastMove[1].y].gameObject);
                        // Position piece
                        chessPieces[lastMove[1].x, lastMove[1].y] = newQueen;
                        PositionSinglePiece(lastMove[1].x, lastMove[1].y);
                    }
                }
            }

            if (specialMove is SpecialMove.Castling)
            {
                Vector2Int[] lastMove = moveList[moveList.Count - 1];
                
                // Left Rook
                if (lastMove[1].x == 2)
                {
                    // White Side
                    if (lastMove[1].y == 0)
                    {
                        ChessPiece rook = chessPieces[0, 0];
                        chessPieces[3, 0] = rook;
                        PositionSinglePiece(3, 0);
                        chessPieces[0, 0] = null;
                    }
                    // Black Side
                    else if (lastMove[1].y == 7)
                    {
                        ChessPiece rook = chessPieces[0, 7];
                        chessPieces[3, 7] = rook;
                        PositionSinglePiece(3, 7);
                        chessPieces[0, 7] = null;
                    }
                }
                // Right Rook
                else if (lastMove[1].x == 6)
                {
                    // White Side
                    if (lastMove[1].y == 0)
                    {
                        ChessPiece rook = chessPieces[7, 0];
                        chessPieces[5, 0] = rook;
                        PositionSinglePiece(5, 0);
                        chessPieces[7, 0] = null;
                    }
                    // Black Side
                    else if (lastMove[1].y == 7)
                    {
                        ChessPiece rook = chessPieces[7, 7];
                        chessPieces[5, 7] = rook;
                        PositionSinglePiece(5, 7);
                        chessPieces[7, 7] = null;
                    }
                }
            }
        }
        
        private void PreventCheck()
        {
            ChessPiece targetKing = null;
            
            for(int x = 0; x < TILE_COUNT_X; x++)
                for(int y = 0; y < TILE_COUNT_Y; y++)
                    if(chessPieces[x, y])
                        if(chessPieces[x, y].type == ChessPieceType.King)
                            if(chessPieces[x, y].team == currentlyDragging.team)
                                targetKing = chessPieces[x, y];
            
            // Since we're sending ref availableMoves, we will be deleting moves that are putting us in check
            SimulateMoveForSinglePiece(currentlyDragging, ref availableMoves, targetKing);
        }

        private void SimulateMoveForSinglePiece(ChessPiece cp, ref List<Vector2Int> moves, ChessPiece targetKing)
        {
            // Save the current values, to reset after the function call
            int actualX = cp.currentX, actualY = cp.currentY;
            List<Vector2Int> movesToRemove = new List<Vector2Int>();
            
            
            // Going through all the moves, simulate them and check if we're in check
            for (int i = 0; i < moves.Count; i++)
            {
                int simX = moves[i].x, simY = moves[i].y;
                
                Vector2Int kingPositionThisSim = new Vector2Int(targetKing.currentX, targetKing.currentY);
                // Did we simulate the king's move
                if(cp.type == ChessPieceType.King)
                    kingPositionThisSim = new Vector2Int(simX, simY);
                
                // Copy the [,] and not a reference. Simulation only
                ChessPiece[,] simulation = new ChessPiece[TILE_COUNT_X, TILE_COUNT_Y]; // Copy board layout
                List<ChessPiece> simAttackingPieces = new List<ChessPiece>();
                for(int x = 0; x < TILE_COUNT_X; x++)
                    for (int y = 0; y < TILE_COUNT_Y; y++)
                    {
                        if (chessPieces[x, y])
                        {
                            simulation[x, y] = chessPieces[x, y];
                            if(simulation[x, y].team != cp.team)
                                simAttackingPieces.Add(simulation[x, y]);
                        }   
                    }
                
                // Simulate that move, hardcoded values normally
                simulation[actualX, actualY] = null; // Apply to this simulation board, not our actual board
                cp.currentX = simX;
                cp.currentY = simY;
                simulation[simX, simY] = cp;
                
                // Did one of the piece got taken down during our simulation
                var deadPiece = simAttackingPieces.Find(p => p.currentX == simX && p.currentY == simY);
                if(deadPiece)
                    simAttackingPieces.Remove(deadPiece);
                
                // Get all the simulated attacking pieces moves
                List<Vector2Int> simMoves = new List<Vector2Int>();
                for (int a = 0; a < simAttackingPieces.Count; a++)
                {
                    var pieceMoves = simAttackingPieces[a]
                        .GetAvailableMoves(ref simulation, TILE_COUNT_X, TILE_COUNT_Y, startingTeam); // Get available moves based on our simulation board move

                    for (int b = 0; b < pieceMoves.Count; b++)
                        simMoves.Add(pieceMoves[b]);
                }
                
                // Is the king in trouble? if so, remove the move
                if (ContainsValidMove(ref simMoves, kingPositionThisSim))
                {
                    movesToRemove.Add(moves[i]); // If king is in danger, we remove the moves completely
                }
                
                // Restore the actual CP data
                cp.currentX = actualX;
                cp.currentY = actualY;
            }
            
            
            // Remove from the current available moves list
            for (int i = 0; i < movesToRemove.Count; i++)
                moves.Remove(movesToRemove[i]);
        }

        private int CheckForCheckmate()
        {
            Vector2Int[] lastMove = moveList[moveList.Count - 1];
            ChessTeam targetTeam = chessPieces[lastMove[1].x, lastMove[1].y].team == ChessTeam.White
                ? ChessTeam.Black
                : ChessTeam.White;
            ChessTeam currentTeam = chessPieces[lastMove[1].x, lastMove[1].y].team;
            
            List<ChessPiece> attackingPieces = new List<ChessPiece>();
            List<ChessPiece> defendingPieces = new List<ChessPiece>();
            ChessPiece targetKing = null;
            for(int x = 0; x < TILE_COUNT_X; x++)
                for(int y = 0; y < TILE_COUNT_Y; y++)
                    if (chessPieces[x, y])
                    {
                        if (chessPieces[x, y].team == targetTeam)
                        {
                            defendingPieces.Add(chessPieces[x, y]);
                            
                            if(chessPieces[x, y].type == ChessPieceType.King)
                                targetKing = chessPieces[x, y];
                        }
                        else
                        {
                            attackingPieces.Add(chessPieces[x, y]);
                        }
                    }
            
            // Is the king attacked right now?
            List<Vector2Int> currentAvailableMoves = new List<Vector2Int>();
            for (int i = 0; i < attackingPieces.Count; i++)
            {
                var pieceMoves = attackingPieces[i]
                    .GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y, currentTeam); // Get available moves based on our simulation board move

                for (int b = 0; b < pieceMoves.Count; b++)
                    currentAvailableMoves.Add(pieceMoves[b]);
            }

            // Are we in check right now?
            if (ContainsValidMove(ref currentAvailableMoves, new Vector2Int(targetKing.currentX, targetKing.currentY)))
            {
                // King is under attacked, can we move something to help him?
                for (int i = 0; i < defendingPieces.Count; i++)
                {
                    List<Vector2Int> defendingMoves = defendingPieces[i].GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y, targetTeam);
                    // Since we're sending ref availableMoves, we will be deleting moves that are putting us in check
                    SimulateMoveForSinglePiece(defendingPieces[i], ref defendingMoves, targetKing);

                    if (defendingMoves.Count != 0) // Contains defending moves, therefore no checkmate
                        return 0;
                }

                return 1; // Checkmate exit
            }
            else
            {
                for (int i = 0; i < defendingPieces.Count; i++)
                {
                    List<Vector2Int> defendingMoves = defendingPieces[i].GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y, targetTeam);
                    SimulateMoveForSinglePiece(defendingPieces[i], ref defendingMoves, targetKing);
                    if (defendingMoves.Count != 0)
                        return 0;
                }
                return 2; //staleMate Exit
            }
        }

        // Operations
        private bool ContainsValidMove(ref List<Vector2Int> moves, Vector2Int pos)
        {
            return moves.Any(t => Mathf.Approximately(t.x, pos.x) && Mathf.Approximately(t.y, pos.y));
        }
        
        private Vector2Int LookupTileIndex(GameObject hitInfo)
        {
            for (int x = 0; x < TILE_COUNT_X; x++)
            for (int y = 0; y < TILE_COUNT_Y; y++)
                if (tilesBounds[x, y] == hitInfo)
                    return new Vector2Int(x, y);

            return -Vector2Int.one; // Invalid
        }

        private void DestroyPiece(ChessPiece ocp, ChessTeam team)
        {
            float destroyedDuration =
                ocp.appearance.Find(match => match.appearance.Equals(Appearance.Destroyed)).duration;
            switch (team)
            {
                case ChessTeam.Black:
                    deadBlacks.Add(ocp);
                    ocp.DestroyPiece("_Progress", destroyedDuration, b =>
                    {
                        if (b)
                        {
                            ocp.SetScale(Vector3.one * deathSize);
                            ocp.SetPosition(
                                new Vector3(-1f * m_tileSize, yOffset, 8f * m_tileSize) // Outside of bounds
                                - bounds // Center of the board properly
                                + new Vector3(m_tileSize / 2, 0, m_tileSize / 2) // Center of square
                                + Vector3.back * (deathDistance * deadBlacks.Count) // Direction of the count
                            );
                            float appearDuration = ocp.appearance
                                .Find(match => match.appearance.Equals(Appearance.Appear)).duration;
                            ocp.AppearPiece("_Progress", appearDuration, b => { });
                        }
                    });
                    break;
                
                case ChessTeam.White:
                    deadWhites.Add(ocp);
                    ocp.DestroyPiece("_Progress", destroyedDuration, b =>
                    {
                        if (b)
                        {
                            ocp.SetScale(Vector3.one * deathSize);
                            ocp.SetPosition(
                                new Vector3(8f * m_tileSize, yOffset, -1f * m_tileSize) // Outside of bounds
                                - bounds // Center of the board properly
                                + new Vector3(m_tileSize / 2, 0, m_tileSize / 2) // Center of square
                                + Vector3.forward * (deathDistance * deadWhites.Count) // Direction of the count
                            );
                            float appearDuration = ocp.appearance
                                .Find(match => match.appearance.Equals(Appearance.Appear)).duration;
                            ocp.AppearPiece("_Progress", appearDuration, b => { });
                        }
                    });
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(team), team, null);
            }
        }

        private bool MoveTo(ChessPiece cp, int x, int y)
        {
            if (!ContainsValidMove(ref availableMoves, new Vector2Int(x, y)))
                return false;
            
            Vector2Int previousPosition = new Vector2Int(cp.currentX, cp.currentY);

            // Is there another piece in target position?
            if (chessPieces[x, y] is not null)
            {
                ChessPiece ocp = chessPieces[x, y];

                if (cp.team == ocp.team)
                    return false;

                // If it's the enemy team
                if (ocp.type == ChessPieceType.King)
                    Checkmate(cp.team);
                    
                DestroyPiece(ocp, ocp.team != startingTeam ? ChessTeam.White : ChessTeam.Black);
            }

            objectPlaced?.Invoke(cp, x, y);

            chessPieces[x, y] = cp;
            chessPieces[previousPosition.x, previousPosition.y] = null;

            PositionSinglePiece(x, y);
            
            isWhiteTurn = !isWhiteTurn;
            moveList.Add(new Vector2Int[] {previousPosition, new Vector2Int(x, y)});

            ProcessSpecialMove();
            
            switch (CheckForCheckmate())
            {
                default:
                    break;
                case 1:
                    Checkmate(cp.team);
                    break;
                case 2:
                    Checkmate(ChessTeam.Stalemate);
                    break;
            }

            return true;
        }
    }
}