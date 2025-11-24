using UnityEngine;

public class Chessboard : MonoBehaviour
{
    
    // LOGIC
    private const int TILE_COUNT_X = 8;
    private const int TILE_COUNT_Y = 8;
    private GameObject[,] tiles;

    void Awake()
    {
        GenerateAllTiles(1, TILE_COUNT_X, TILE_COUNT_Y);
    }

    private void GenerateAllTiles(float tileSize, int tileCountX, int tileCountY)
    {
        tiles= new GameObject[tileCountX, tileCountY];
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
        tileObject.AddComponent<MeshRenderer>();
        
        // Add simple vertices using index and points
        Vector3[] vertices = new Vector3[4];
        vertices[0] = new Vector3(x * tileSize, 0, y * tileSize);
        vertices[1] = new Vector3(x * tileSize, 0, (y + 1) * tileSize);
        vertices[2] = new Vector3((x + 1) * tileSize, 0, y * tileSize);
        vertices[3] = new Vector3((x + 1) * tileSize, 0, (y + 1) * tileSize);
        
        // Assign Index on vertices like (0 -> 1 -> 2) & (1 -> 3 -> 2)
        int[] tris = new int[] {0 , 1, 2, 1, 3, 2};
        
        // Assign to mesh
        mesh.vertices = vertices;
        mesh.triangles = tris;

        // Add Box Collider Component
        tileObject.AddComponent<BoxCollider>();
        
        return tileObject;
    }
}
