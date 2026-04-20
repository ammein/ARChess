using System.Collections;
using System.Collections.Generic;
using ARChess.Scripts.Chess.Pieces;
using ARChess.Scripts.Lights;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using ARChess.Scripts.Utility;

namespace ARChess.Scripts.Chess
{
    public enum Piece
    {
        
    }
    
    public class Chessboard : MonoBehaviour
    {
        [Header("Art Stuff")] 
        [SerializeField] 
        private Material tileMaterial;
        [SerializeField] 
        [Range(0.01f, 1f)] 
        private float deathSize = 0.3f;
        [SerializeField]
        private float deathDistance = 0.3f;

        [Header("Prefabs & Materials")]
        [SerializeField] private GameObject[] prefabs;
        [SerializeField] private Material[] teamMaterials;
        
        // LOGIC
        private ChessPiece[,] chessPieces;
        private ChessPiece currentlyDragging;
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
        
        [Header("Chess Settings")]
        [SerializeField]
        [Tooltip("Tile size of the chessboard")]
        private float m_tileSize = 1;

        [SerializeField] 
        [Tooltip("Y Offset of the chessboard")]
        private float yOffset = 0f;
        
        [SerializeField]
        [Tooltip("Board Center of the chessboard")]
        private Vector3 boardCenter = Vector3.zero;
        
        private GameObject _directionalLight;
        private AmbientLightEstimation _ambientLightEstimation;
        
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

        private void Awake()
        {
            ChessTiles = GameObject.Find("All Chess Tiles");
            ChessAttach = GameObject.Find("Chess Attach");
            ChessVisuals = GameObject.Find("Chess Visuals");
            currentCamera = Camera.main;
            
            // Generate All Tiles
            bool generated = GenerateAllTiles(m_tileSize);


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
            if(_directionalLight)
                _ambientLightEstimation = _directionalLight.GetComponent<AmbientLightEstimation>();
        }

        private void Update()
        {
            if (_directionalLight && _ambientLightEstimation)
            {
                // Lights follow board
                _directionalLight.transform.position = gameObject.transform.position + _ambientLightEstimation.DynamicLightPosition;
                _directionalLight.transform.rotation = gameObject.transform.rotation * Quaternion.Euler(_ambientLightEstimation.DynamicLightRotation);   
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
            if (Physics.Raycast(ray, out info, 100, LayerMask.GetMask("Tile", "Selected", "Bound Selected", "Visual Tile")))
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
                    tiles[currentHover.x, currentHover.y].layer = LayerMask.NameToLayer("Visual Tile");
                    currentHover = hitPosition;
                    // Change Layer to "Hover"
                    tilesBounds[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Bound Selected");
                    tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Selected");    
                }

                if (touched)
                {
                    Log.LogThis($"Screen Touched", this);
                    if (chessPieces[hitPosition.x, hitPosition.y] != null)
                    {
                        // Is it our turn?
                        if (true && currentlyDragging == null)
                        {
                            currentlyDragging = chessPieces[hitPosition.x, hitPosition.y];
                        }
                    }
                } 
                
                if (currentlyDragging != null && !touched)
                {
                    Log.LogThis($"Screen Released", this);
                    
                    if (tiles[hitPosition.x, hitPosition.y])
                    {
                        Log.LogThis($"Moving previously dragged: {currentlyDragging} to tile hitPosition: {tiles[hitPosition.x, hitPosition.y].name}", this);
                    }
                    
                    Vector2Int previousPosition = new Vector2Int(currentlyDragging.currentX, currentlyDragging.currentY);
                    
                    bool validMove = MoveTo(currentlyDragging, hitPosition.x, hitPosition.y);
                    if (!validMove)
                    {
                        Log.LogThis("Invalid move",  this);
                        currentlyDragging.SetPosition(GetTileCenter(previousPosition.x, previousPosition.y));
                        currentlyDragging = null;
                    }
                    else
                    {
                        currentlyDragging = null;
                    }
                }
                
            }
            else
            {
                if (currentHover != -Vector2Int.one)
                {
                    tilesBounds[currentHover.x, currentHover.y].layer = LayerMask.NameToLayer("Tile");
                    tiles[currentHover.x, currentHover.y].layer = LayerMask.NameToLayer("Visual Tile");
                    currentHover = -Vector2Int.one;   
                }

                if (currentlyDragging && !touched)
                {
                    currentlyDragging.SetPosition(GetTileCenter(currentlyDragging.currentX, currentlyDragging.currentY));
                    currentlyDragging = null;
                }
            }
        }

        // Generate the board
        public bool GenerateAllTiles(float tileSize, int tileCountX = TILE_COUNT_X, int tileCountY = TILE_COUNT_Y)
        {
            yOffset += transform.position.y;
            m_tileSize = tileSize;
            tiles = new GameObject[tileCountX, tileCountY];
            tilesBounds = new GameObject[tileCountX, tileCountY];

            // Calculate half the total size to center the chessboard
            bounds = new Vector3(((float)tileCountX / 2f) * tileSize, 0, ((float)tileCountX / 2f) * tileSize) + boardCenter;

            try
            {
                for (int x = 0; x < tileCountX; x++)
                {
                    for (int y = 0; y < tileCountY; y++)
                    {
                        GenerateSingleTiles(tileSize, x, y);
                    }
                }

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
            chessCollider.center = totalBounds.center - ChessAttach.transform.position; // Adjust for the parent's position if necessary
            
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
            vertices[0] = new Vector3(x * tileSize , yOffset, y * tileSize) - bounds;
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
            for (int team = 0; team < teamMaterials.Length; team++)
            {
                for (int x = 0; x < TILE_COUNT_X; x++)
                {
                    switch (x)
                    {
                        case 0:
                            chessPieces[x, team > 0 ? TILE_COUNT_Y - 1 : 0] = SpawnSinglePiece(ChessPieceType.Rook, team);
                            goto default;
                        case 1:
                            chessPieces[x, team > 0 ? TILE_COUNT_Y - 1 : 0] = SpawnSinglePiece(ChessPieceType.Knight, team);
                            goto default;
                        case 2:
                            chessPieces[x, team > 0 ? TILE_COUNT_Y - 1 : 0] = SpawnSinglePiece(ChessPieceType.Bishop, team);
                            goto default;
                        case 3:
                            chessPieces[x, team > 0 ? TILE_COUNT_Y - 1 : 0] = SpawnSinglePiece(ChessPieceType.Queen, team);
                            goto default;
                        case 4:
                            chessPieces[x, team > 0 ? TILE_COUNT_Y - 1 : 0] = SpawnSinglePiece(ChessPieceType.King, team);
                            goto default;
                        case 5:
                            chessPieces[x, team > 0 ? TILE_COUNT_Y - 1 : 0] = SpawnSinglePiece(ChessPieceType.Bishop, team);
                            goto default;
                        case 6:
                            chessPieces[x, team > 0 ? TILE_COUNT_Y - 1 : 0] = SpawnSinglePiece(ChessPieceType.Knight, team);
                            goto default;
                        case 7:
                            chessPieces[x, team > 0 ? TILE_COUNT_Y - 1 : 0] = SpawnSinglePiece(ChessPieceType.Rook, team);
                            goto default;
                        default:
                            chessPieces[x, team > 0 ? TILE_COUNT_Y - 2 : 1] =  SpawnSinglePiece(ChessPieceType.Pawn, team);
                            break;
                    }
                }
            }
        }

        private ChessPiece SpawnSinglePiece(ChessPieceType type, int team)
        {
            ChessPiece cp = Instantiate(prefabs[(int)type - 1], ChessVisuals.transform).GetComponent<ChessPiece>();
            
            cp.type = type;
            cp.team = team;
            
            cp.GetComponent<MeshRenderer>().material = teamMaterials[team];
            
            return cp;
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
                        chessPieces[x, y].AppearPiece("_Progress");
        }

        private Vector3 GetTileCenter(int x, int y)
        {
            return new Vector3(x * m_tileSize, yOffset, y * m_tileSize) - bounds + new Vector3(m_tileSize / 2, 0, m_tileSize / 2);
        }

        // Operations
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
            Vector2Int previousPosition = new Vector2Int(cp.currentX, cp.currentY);

            // Is there another piece in target position?
            if (chessPieces[x, y] != null)
            {
                ChessPiece ocp = chessPieces[x, y];

                if (cp.team == ocp.team)
                    return false;
                
                // If it's the enemy team
                if (ocp.team == 0)
                {
                    deadWhites.Add(ocp);
                    ocp.SetScale(Vector3.one * deathSize);
                    ocp.SetPosition(
                        new Vector3(8f * m_tileSize, yOffset, -1 * m_tileSize) // Outside of bounds
                        - bounds // Center of the board properly
                        + new Vector3(m_tileSize / 2, 0, m_tileSize / 2) // Center of square
                        + (Vector3.forward * deathDistance) * deadWhites.Count // Direction of the count
                        );
                }
                else
                {
                    deadBlacks.Add(ocp);
                    ocp.SetScale(Vector3.one * deathSize);
                }
            }
            
            chessPieces[x, y] = cp;
            chessPieces[previousPosition.x, previousPosition.y] = null;
            
            PositionSinglePiece(x, y);

            return true;
        }

    }
}