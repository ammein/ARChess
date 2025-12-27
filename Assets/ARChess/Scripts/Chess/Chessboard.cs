using System;
using System.Collections;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace ARChess.Scripts.Chess
{
    public class Chessboard : MonoBehaviour
    {
        [Header("Art Stuff")] 
        [SerializeField] 
        private Material tileMaterial;
        
        // LOGIC
        private const int TILE_COUNT_X = 8;
        private const int TILE_COUNT_Y = 8;
        private GameObject[,] tiles;
        private Camera currentCamera;
        private Vector2Int currentHover;
        private BoxCollider chessCollider;
        private GameObject ChessTiles;
        private GameObject ChessVisuals;
        private GameObject ChessAttach;
        
        [Header("Size")]
        [SerializeField]
        [Tooltip("Tile size of the chessboard")]
        private int m_tileSize = 1;
        
        public BoxCollider ChessCollider => chessCollider;

        private bool _updateLocation;
        private Vector2 _location;

        public GameObject AttachObject
        {
            get => ChessAttach;
            set => ChessAttach = value;
        }

        public int TileSize
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
            GenerateAllTiles(m_tileSize);
        }

        [Conditional("UNITY_EDITOR")]
        private void LogThis(string message, Object context)
        {
            Debug.Log(message, context);
        }

        private void Update()
        {
            if (!currentCamera)
            {
                LogThis("No Camera Assigned", this);
                return;
            }

            // #if UNITY_EDITOR
            // Ray ray = currentCamera.ScreenPointToRay(Input.mousePosition);
            // HitTile(ray, Info);
            // #else
            // Ray ray = currentCamera.ScreenPointToRay(Input.GetTouch(0).position);
            // HitTile(ray, Info);
            // #endif
        }

        public RaycastHit Info { get; set; }

        private void HitTile(Ray ray, RaycastHit info)
        {
            // To prevent raycast to infinite distance, we have to make the endpoint only react to Tile or 100 max distance
            if (Physics.Raycast(ray, out info, 100, LayerMask.GetMask("Tile")))
            {
                LogThis("Tile", this);
                // Get the indexes of the tile I've hit
                Vector2Int hitPosition = LookupTileIndex(info.transform.gameObject);

                // If we're hovering a tile after not hovering any tiles
                if (currentHover == -Vector2Int.one)
                {
                    LogThis("Hover", this);
                    currentHover = hitPosition;
                    // Change Layer to "Hover"
                    tiles[hitPosition.x, hitPosition.y].layer = LayerMask.GetMask("Hover");
                }

                // If we were already hovering a tile, change the previous one
                if (currentHover == hitPosition) return;
                tiles[currentHover.x, currentHover.y].layer = LayerMask.GetMask("Tile");
                currentHover = hitPosition;
                // Change Layer to "Hover"
                tiles[hitPosition.x, hitPosition.y].layer = LayerMask.GetMask("Hover");
                LogThis("Hover", this);
            }
            else
            {
                LogThis("Tile", this);
                if (currentHover == -Vector2Int.one) return;
                tiles[currentHover.x, currentHover.y].layer = LayerMask.GetMask("Tile");
                currentHover = -Vector2Int.one;
            }
        }

        // Generate the board
        public bool GenerateAllTiles(int tileSize, int tileCountX = TILE_COUNT_X, int tileCountY = TILE_COUNT_Y)
        {
            m_tileSize = tileSize;
            tiles = new GameObject[tileCountX, tileCountY];

            // Calculate half the total size to center the chessboard
            int halfWidth = (tileCountX * (int)tileSize) / 2;
            int halfHeight = (tileCountY * (int)tileSize) / 2;

            for (int x = 0; x < tileCountX; x++)
            {
                for (int y = 0; y < tileCountY; y++)
                {
                    // Calculate the position to center the tiles
                    float posX = (x * tileSize) - halfWidth + (tileSize / 2);
                    float posY = (y * tileSize) - halfHeight + (tileSize / 2);

                    tiles[x, y] = GenerateSingleTiles(tileSize, x, y, posX, posY);
                }
            }

            AddChessBound(tiles, tileCountX, tileCountY);

            for (int x = 0; x < tileCountX; x++)
            {
                for (int y = 0; y < tileCountY; y++)
                {
                    GenerateBoundsBoxCollider(tiles[x, y], x, y);
                }
            }

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
                    meshBounds.center += tilePosition; // Offset the bounds to the tile's world position

                    // Encapsulate the bounds
                    totalBounds.Encapsulate(meshBounds);
                }
            }

            // Set the size of the collider based on the total bounds
            chessCollider.size = totalBounds.size;
            
            chessCollider.center = totalBounds.center;
            
            chessCollider.providesContacts = true;
        }


        private void GenerateBoundsBoxCollider(GameObject tileObject, int x, int y)
        {
            GameObject tileBounds = new GameObject(string.Format("X:{0} Y:{1}", x, y));
            
            tileBounds.transform.SetParent(ChessTiles.transform);
            
            tileBounds.AddComponent<MeshFilter>().mesh = tileObject.GetComponent<MeshFilter>().mesh;
            tileBounds.AddComponent<MeshRenderer>().material = tileMaterial;
            
            // Add Bounds from ZERO using current gameObject transform position
            Bounds totalBounds = new Bounds(tileObject.transform.position, Vector3.zero);
            // Whenever the camera is assign to "Tile" layer, the object will change the layer into Tile
            tileBounds.layer = LayerMask.NameToLayer("Tile");
            // Add Box Collider Component
            BoxCollider boxCollider = tileBounds.AddComponent<BoxCollider>();
            boxCollider.center = tileObject.transform.position;
            boxCollider.isTrigger = true;
        }

        private GameObject GenerateSingleTiles(float tileSize, int x, int y, float posX, float posY)
        {
            GameObject tileObject = new GameObject(string.Format("Tile: ({0}, {1})", x, y));

            // Set tile gameobject as children of ChessTiles gameobject
            tileObject.transform.SetParent(ChessVisuals.transform);
            tileObject.transform.position = new Vector3(posX, 0, posY);

            Mesh mesh = new Mesh();
    
            // Add Meshes and Materials
            tileObject.AddComponent<MeshFilter>().mesh = mesh;
            tileObject.AddComponent<MeshRenderer>().material = tileMaterial;

            // Calculate half the tile size
            float halfTileSize = tileSize / 2.0f;

            // Define vertices with height
            Vector3[] vertices = new Vector3[4];
            vertices[0] = new Vector3(-halfTileSize, 0, -halfTileSize); // Bottom-left
            vertices[1] = new Vector3(-halfTileSize, 0, halfTileSize);  // Top-left
            vertices[2] = new Vector3(halfTileSize, 0, -halfTileSize);   // Bottom-right
            vertices[3] = new Vector3(halfTileSize, 0, halfTileSize);    // Top-right

            // Assign Index on vertices like (0 -> 1 -> 2) & (1 -> 3 -> 2)
            int[] tris = new int[] { 0, 1, 2, 1, 3, 2 };

            // Assign to mesh
            mesh.vertices = vertices;
            mesh.triangles = tris;

            // Set a height for the tiles
            mesh.RecalculateNormals();
            mesh.RecalculateBounds(); // Ensure the bounds are recalculated

            return tileObject;
        }


        // Operations
        private Vector2Int LookupTileIndex(GameObject hitInfo)
        {
            for (int x = 0; x < TILE_COUNT_X; x++)
            for (int y = 0; y < TILE_COUNT_Y; y++)
                if (tiles[x, y] == hitInfo)
                    return new Vector2Int(x, y);

            return -Vector2Int.one; // Invalid
        }
    }
}