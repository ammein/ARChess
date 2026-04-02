using System;
using System.Collections;
using System.Diagnostics;
using UnityEngine;
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
        private GameObject[,] tilesBounds;
        private Camera currentCamera;
        private Vector2Int currentHover;
        private BoxCollider chessCollider;
        private GameObject ChessTiles;
        private GameObject ChessVisuals;
        private GameObject ChessAttach;
        private Vector3 bounds;
        
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
        
        public BoxCollider ChessCollider => chessCollider;

        private bool _updateLocation;
        private Vector2 _location;

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
        }

        public void ChessInteract(Vector2 position)
        {
            Ray ray = currentCamera.ScreenPointToRay(new Vector3(position.x, position.y, 0));
            HitTile(ray, Info);
        }

        public RaycastHit Info { get; set; }

        private void HitTile(Ray ray, RaycastHit info)
        {
            // To prevent raycast to infinite distance, we have to make the endpoint only react to Tile or 100 max distance
            if (Physics.Raycast(ray, out info, 100, LayerMask.GetMask("Tile", "Selected", "Bound Selected", "Visual Tile")))
            {
                // Get the indexes of the tile I've hit
                Vector2Int hitPosition = LookupTileIndex(info.collider.gameObject);

                // If we're hovering a tile after not hovering any tiles
                if (currentHover == -Vector2Int.one)
                {
                    LogThis($"Tile {hitPosition.x},{hitPosition.y} hit", this);
                    currentHover = hitPosition;
                    // Change Layer to "Hover"
                    tilesBounds[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Bound Selected");
                    tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Selected");
                }

                // If we were already hovering a tile, change the previous one
                if (currentHover == hitPosition) return;
                LogThis($"Tile {currentHover.x},{currentHover.y} hit", this);
                tilesBounds[currentHover.x, currentHover.y].layer = LayerMask.NameToLayer("Tile");
                tiles[currentHover.x, currentHover.y].layer = LayerMask.NameToLayer("Visual Tile");
                currentHover = hitPosition;
                // Change Layer to "Hover"
                tilesBounds[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Bound Selected");
                tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Selected");
            }
            else
            {
                if (currentHover == -Vector2Int.one) return;
                tilesBounds[currentHover.x, currentHover.y].layer = LayerMask.NameToLayer("Tile");
                tiles[currentHover.x, currentHover.y].layer = LayerMask.NameToLayer("Visual Tile");
                currentHover = -Vector2Int.one;
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
            float halfWidth = ((float)tileCountX * tileSize) / 2f;
            float halfHeight = ((float)tileCountY * tileSize) / 2f;
            
            bounds = new Vector3(halfWidth, 0, halfHeight) + boardCenter;

            for (int x = 0; x < tileCountX; x++)
            {
                for (int y = 0; y < tileCountY; y++)
                {
                    // Calculate the position to center the tiles
                    float posX = (x * tileSize) - halfWidth + (tileSize / 2f);
                    float posY = (y * tileSize) - halfHeight + (tileSize / 2f);

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


        private void GenerateBoundsBoxCollider(GameObject tileObject, int x, int y)
        {
            GameObject tileBounds = new GameObject(string.Format("X:{0} Y:{1}", x, y));
            
            tileBounds.transform.SetParent(ChessTiles.transform);
            
            tileBounds.AddComponent<MeshFilter>().mesh = tileObject.GetComponent<MeshFilter>().mesh;
            tileBounds.AddComponent<MeshRenderer>().material = tileMaterial;
            
            Mesh mesh = tileObject.GetComponent<MeshFilter>().mesh;
            
            // Assign UV
            // Reference: https://docs.unity3d.com/ScriptReference/Mesh-uv.html?ampDeviceId=d4ea89ad-4dee-4f88-952c-b59ece6145dc&ampSessionId=1775090412449&ampTimestamp=1775178649783
            Vector3[] vertices = mesh.vertices;
            Vector2[] uvs = new Vector2[vertices.Length];

            for (int i = 0; i < uvs.Length; i++)
            {
                // Apply vertices and centerized the UV so that the effect "Render Objects" apply material correctly
                uvs[i] = new Vector2(vertices[i].x + (float)0.5, vertices[i].z + (float)0.5);
            }
            mesh.uv = uvs;
            
            mesh.RecalculateUVDistributionMetrics();
            
            // Whenever the camera is assign to "Tile" layer, the object will change the layer into Tile
            tileBounds.layer = LayerMask.NameToLayer("Tile");
            // Add Box Collider Component
            BoxCollider boxCollider = tileBounds.AddComponent<BoxCollider>();
            tileBounds.transform.position = tileObject.transform.position;
            tilesBounds[x, y] = tileBounds;
            boxCollider.isTrigger = true;
        }

        private GameObject GenerateSingleTiles(float tileSize, int x, int y, float posX, float posY)
        {
            GameObject tileObject = new GameObject(string.Format("Tile: ({0}, {1})", x, y));

            // Set tile gameobject as children of ChessTiles gameobject
            tileObject.transform.SetParent(ChessVisuals.transform);
            tileObject.transform.position = new Vector3(posX, 0, posY);
            
            tileObject.layer = LayerMask.NameToLayer("Visual Tile");

            Mesh mesh = new Mesh();
    
            // Add Meshes and Materials
            tileObject.AddComponent<MeshFilter>().mesh = mesh;
            tileObject.AddComponent<MeshRenderer>().material = tileMaterial;

            // Calculate half the tile size
            float halfTileSize = tileSize / 2.0f;

            // Define vertices with height
            Vector3[] vertices = new Vector3[4];
            vertices[0] = new Vector3(-halfTileSize, yOffset, -halfTileSize) - bounds; // Bottom-left
            vertices[1] = new Vector3(-halfTileSize, yOffset, halfTileSize) - bounds;  // Top-left
            vertices[2] = new Vector3(halfTileSize, yOffset, -halfTileSize) - bounds;   // Bottom-right
            vertices[3] = new Vector3(halfTileSize, yOffset, halfTileSize) - bounds;    // Top-right

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
                    if (tilesBounds[x, y] == hitInfo)
                        return new Vector2Int(x, y);

            return -Vector2Int.one; // Invalid
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
    }
}