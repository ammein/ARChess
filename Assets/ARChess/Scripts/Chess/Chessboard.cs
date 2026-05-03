using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ARChess.Scripts.Chess.Pieces;
using ARChess.Scripts.Lights;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using ARChess.Scripts.Utility;

namespace ARChess.Scripts.Chess
{
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

        [HideInInspector]
        public ChessTeam startingTeam;

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

        public Vector2 TileCount => new(TILE_COUNT_X, TILE_COUNT_Y);
        
        public List<TeamMaterials> PieceMaterials => teamMaterials;

        private void Awake()
        {
            isWhiteTurn = true;
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
                    tiles[currentHover.x, currentHover.y].layer = (ContainsValidMove(ref availableMoves, currentHover)) ? LayerMask.NameToLayer("Highlight") :  LayerMask.NameToLayer("Visual Tile");
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

                        HighlightTiles();
                    }
                }

                // If the piece is dropped, player has made move. Now do move checks...
                if (currentlyDragging && !touched)
                {
                    Vector2Int previousPosition =
                        new Vector2Int(currentlyDragging.currentX, currentlyDragging.currentY);
                    
                    bool validMove = MoveTo(currentlyDragging, hitPosition.x, hitPosition.y);
                    if (!validMove)
                        currentlyDragging.SetPosition(GetTileCenter(previousPosition.x, previousPosition.y));
                    
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
                    tiles[currentHover.x, currentHover.y].layer = (ContainsValidMove(ref availableMoves, currentHover)) ? LayerMask.NameToLayer("Highlight") :  LayerMask.NameToLayer("Visual Tile");
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
            GameObject tileObject = new GameObject(string.Format("Tile: ({0}, {1})", x, y));
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
            GameObject tileBounds = new GameObject(string.Format("X:{0} Y:{1}", x, y));
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


        private ChessPiece SpawnSinglePiece(Piece piece, ChessTeam team, Material teamMaterial)
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
            return cp;
        }

        private void AssignChildrenMaterial(Material material, GameObject child)
        {
            if (child.GetComponent<MeshRenderer>() != null)
                child.GetComponent<MeshRenderer>().material = material;

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
            DisplayVictory(team);
        }

        private void DisplayVictory(ChessTeam team)
        {
            
        }

        public void OnResetButton()
        {
            
        }

        public void OnExitButton()
        {
            
        }

        // Operations
        private bool ContainsValidMove(ref List<Vector2Int> moves, Vector2 pos)
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

        private bool MoveTo(ChessPiece cp, int x, int y)
        {
            if (!ContainsValidMove(ref availableMoves, new Vector2Int(x, y)))
                return false;
            
            Vector2Int previousPosition = new Vector2Int(cp.currentX, cp.currentY);

            // Is there another piece in target position?
            if (chessPieces[x, y] != null)
            {
                ChessPiece ocp = chessPieces[x, y];

                if (cp.team == ocp.team)
                    return false;

                // If it's the enemy team
                if (ocp.team != startingTeam)
                {
                    if (ocp.type == ChessPieceType.King)
                        Checkmate(ocp.team);
                    
                    deadWhites.Add(ocp);
                    float destroyedDuration =
                        ocp.appearance.Find(match => match.appearance.Equals(Appearance.Destroyed)).duration;
                    ocp.DestroyPiece("_Progress", destroyedDuration, b =>
                    {
                        if (b)
                        {
                            ocp.SetScale(Vector3.one * deathSize);
                            ocp.SetPosition(
                                new Vector3(8f * m_tileSize, yOffset, -1f * m_tileSize) // Outside of bounds
                                - bounds // Center of the board properly
                                + new Vector3(m_tileSize / 2, 0, m_tileSize / 2) // Center of square
                                + (Vector3.forward * deathDistance) * deadWhites.Count // Direction of the count
                            );
                            float appearDuration = ocp.appearance
                                .Find(match => match.appearance.Equals(Appearance.Appear)).duration;
                            ocp.AppearPiece("_Progress", appearDuration, b => { });
                        }
                    });
                }
                else
                {
                    if (ocp.type == ChessPieceType.King)
                        Checkmate(ocp.team);
                    
                    deadBlacks.Add(ocp);
                    float destroyedDuration =
                        ocp.appearance.Find(match => match.appearance.Equals(Appearance.Destroyed)).duration;
                    ocp.DestroyPiece("_Progress", destroyedDuration, b =>
                    {
                        if (b)
                        {
                            ocp.SetScale(Vector3.one * deathSize);
                            ocp.SetPosition(
                                new Vector3(-1f * m_tileSize, yOffset, 8f * m_tileSize) // Outside of bounds
                                - bounds // Center of the board properly
                                + new Vector3(m_tileSize / 2, 0, m_tileSize / 2) // Center of square
                                + (Vector3.back * deathDistance) * deadBlacks.Count // Direction of the count
                            );
                            float appearDuration = ocp.appearance
                                .Find(match => match.appearance.Equals(Appearance.Appear)).duration;
                            ocp.AppearPiece("_Progress", appearDuration, b => { });
                        }
                    });
                }
            }

            objectPlaced?.Invoke(cp, x, y);

            chessPieces[x, y] = cp;
            chessPieces[previousPosition.x, previousPosition.y] = null;

            PositionSinglePiece(x, y);
            
            isWhiteTurn = !isWhiteTurn;

            return true;
        }
    }
}