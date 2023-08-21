using System.Collections.Generic;
using UnityEngine;

public class GridManager
{
    // Grid dimensions
    private readonly int width;
    private readonly int height;
    private readonly int depth;

    private readonly bool[,] visited;
    private readonly List<List<Vector2Int>> cellGroups;

    private readonly Vector3Int[] adjacentOffsets = {
        new Vector3Int(1, 0, 0),
        new Vector3Int(-1, 0, 0),
        new Vector3Int(0, 0, 1),
        new Vector3Int(0, 0, -1)
    };

    public GridManager(int width, int height, int depth)
    {
        this.width = width;
        this.height = height;
        this.depth = depth;

        visited = new bool[width, depth];
        cellGroups = new List<List<Vector2Int>>();
    }

    // Creates grid for first generation step
    public Vector3Int[,,] CreateGrid()
    {
        Vector3Int[,,] grid = new Vector3Int[width, height, depth];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    grid[x, y, z] = new Vector3Int(x, y, z);
                }
            }
        }

        return grid;
    }

    // Returns a list with all conected cells groups based on first generation step
    public List<List<Vector2Int>> FindEnclosedCellGroups(int[,] values)
    {
        for (int i = 0; i < values.GetLength(0); i++)
        {
            for (int j = 0; j < values.GetLength(1); j++)
            {
                if (values[i, j] == 0 && !visited[i, j])
                {
                    List<Vector2Int> cells = new();

                    DFS(i, j, cells, visited, values);
                    cellGroups.Add(cells);
                }
            }
        }

        return cellGroups;
    }

    private void DFS(int i, int j, List<Vector2Int> cells, bool[,] visited, int[,] values)
    {
        visited[i, j] = true;
        cells.Add(new Vector2Int(i, j));

        foreach (Vector3Int offset in adjacentOffsets)
        {
            int nx = i + offset.x;
            int nz = j + offset.z;

            if (IsWithinBounds(nx, nz) && values[nx, nz] == 0 && !visited[nx, nz])
                DFS(nx, nz, cells, visited, values);
        }
    }

    // Checks if values x, y are inside the grid
    private bool IsWithinBounds(int x, int y)
    {
        return (x >= 0 && x < width && y > 0 && y < depth);
    }

    // Creates a list of grids based on cellGroups
    // Used for second generating step
    public List<List<Vector3Int>> CreateGrids(List<List<Vector2Int>> cellGroups, int height)
    {
        List<List<Vector3Int>> grids = new();

        foreach (List<Vector2Int> group in cellGroups)
        {
            List<Vector3Int> grid = new();

            foreach (Vector2Int cell in group)
            {
                for (int i = 0; i < height; i++)
                {
                    grid.Add(new Vector3Int(cell.x, i, cell.y));
                }
            }

            grids.Add(grid);
        }

        return grids;
    }

    public int[,] DivideGrid(int[,] values)
    {
        int divWidth = values.GetLength(0) * 4;
        int divDepth = values.GetLength(1) * 4;

        int[,] dividedGrid = new int[divWidth, divDepth];

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < depth; j++)
            {
                int value = values[i, j];

                for (int k = 0; k < 4; k++)
                {
                    for (int l = 0; l < 4; l++)
                    {
                        dividedGrid[4 * i + k, 4 * j + l] = value;
                    }
                }

            }
        }

        return dividedGrid;
    }
}
