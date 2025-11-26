using UnityEngine;

namespace ARChess.Scripts
{
    public class Chessboard : MonoBehaviour
    {
        [Header("Art Stuff")] [SerializeField] private Material tileMaterial;

        // LOGIC
        private const int TILE_COUNT_X = 8;
        private const int TILE_COUNT_Y = 8;
        private GameObject[,] tiles;
        private Camera currentCamera;
        private Vector2Int currentHover;

        private void Awake()
        {
            GenerateAllTiles(1, TILE_COUNT_X, TILE_COUNT_Y);
        }

        private void Update()
        {
            if (!currentCamera)
            {
                currentCamera = Camera.current;
                return;
            }

            RaycastHit info;
            Ray ray = currentCamera.ScreenPointToRay(Input.GetTouch(0).position);

            // To prevent raycast to infinite distance, we have to make the endpoint only react to Tile or 100 max distance
            if (Physics.Raycast(ray, out info, 100, LayerMask.GetMask("Tile")))
            {
                // Get the indexes of the tile I've hit
                Vector2Int hitPosition = LookupTileIndex(info.transform.gameObject);

                // If we're hovering a tile after not hovering any tiles
                if (currentHover == -Vector2Int.one)
                {
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
            }
            else
            {
                if (currentHover == -Vector2Int.one) return;
                tiles[currentHover.x, currentHover.y].layer = LayerMask.GetMask("Tile");
                currentHover = -Vector2Int.one;
            }
        }

        // Generate the board
        private void GenerateAllTiles(float tileSize, int tileCountX, int tileCountY)
        {
            tiles = new GameObject[tileCountX, tileCountY];
            for (int x = 0; x < tileCountX; x++)
            for (int y = 0; y < tileCountY; y++)
                tiles[x, y] = GenerateSingleTiles(tileSize, x, y);
        }

        private GameObject GenerateSingleTiles(float tileSize, int x, int y)
        {
            GameObject tileObject = new GameObject(string.Format("X:{0}, Y:{1}", x, y));

            // Apply similar transform to script game object's children
            tileObject.transform.SetParent(transform);

            Mesh mesh = new Mesh();

            // Add Mesh as component
            tileObject.AddComponent<MeshFilter>().mesh = mesh;
            tileObject.AddComponent<MeshRenderer>().material = tileMaterial;

            // Add simple vertices using index and points
            Vector3[] vertices = new Vector3[4];
            vertices[0] = new Vector3(x * tileSize, 0, y * tileSize);
            vertices[1] = new Vector3(x * tileSize, 0, (y + 1) * tileSize);
            vertices[2] = new Vector3((x + 1) * tileSize, 0, y * tileSize);
            vertices[3] = new Vector3((x + 1) * tileSize, 0, (y + 1) * tileSize);

            // Assign Index on vertices like (0 -> 1 -> 2) & (1 -> 3 -> 2)
            int[] tris = new int[] { 0, 1, 2, 1, 3, 2 };

            // Assign to mesh
            mesh.vertices = vertices;
            mesh.triangles = tris;

            // Recalculate normals for lighting and other environment affect to the mesh...
            mesh.RecalculateNormals();

            // Whenever the camera is assign to "Tile" layer, the object will change the layer into Tile
            tileObject.layer = LayerMask.NameToLayer("Tile");
            // Add Box Collider Component
            tileObject.AddComponent<BoxCollider>();

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