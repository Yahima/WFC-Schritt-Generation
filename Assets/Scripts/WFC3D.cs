using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

public class WFC3D : MonoBehaviour
{
    // Grid dimensions
    public int xSize, ySize, zSize;
    public int height;

    public string step1Tag;
    public string step2Tag;

    public float objectSize1;
    public float objectSize2;

    public float yOffset1;
    public float yOffset2;

    public float offset1;
    public float offset2;

    // Generates grid
    private GridManager gridManager;

    //Generates adjacency rules from sample
    private SampleManager3D sampleManager;

    // All module names
    private List<Tuple<string, int>> moduleTypes;
    private List<Tuple<string, int>> overModuleTypes;

    private List<Tuple<string, int>> edgeModuleTypes;
    private List<Tuple<string, int>> groundModuleTypes;

    private Dictionary<string, Tuple<GameObject, int>> gameObjects;

    // Module adjacency rules for each direction
    private Dictionary<string, Dictionary<Dir, List<string>>> rules;

    // Grid
    private Vector3Int[,,] blocks;

    // Modules
    private Module[,,] modules;
    private Module[,,] newModules;

    // Blocks (coordinates) with the lowest entropy
    private List<Vector3Int> lowEntropyList;

    // Backtraking
    private List<string> errorStates;
    private List<History3D> history;

    private bool step1Done = false;

    private List<Vector3Int> currentCells;
    private List<List<Vector3Int>> cellBlocks;
    private int currentIndex = 0;

    private string lastState = "";

    // Use this for initialization
    void Start()
    {
        gridManager = new GridManager(xSize, ySize, zSize);
        blocks = gridManager.CreateGrid();

        sampleManager = new SampleManager3D(step1Tag, objectSize1, yOffset1);

        rules = sampleManager.GenerateRulesFromSamples();
        gameObjects = sampleManager.GetObjects();
        moduleTypes = new List<Tuple<string, int>>();
        groundModuleTypes = new List<Tuple<string, int>>();
        overModuleTypes = new List<Tuple<string, int>>();

        foreach (var rule in rules)
            moduleTypes.Add(new(rule.Key, gameObjects[rule.Key[0..^1]].Item2));


        modules = new Module[xSize, ySize, zSize];
        newModules = new Module[xSize, ySize, zSize];

        edgeModuleTypes = moduleTypes.Where(tuple => !tuple.Item1.Contains("ground")).ToList();
        // currentCells = new List<Vector3Int>();

        foreach (var block in blocks)
        {

            int x = block.x;
            int y = block.y;
            int z = block.z;

            //currentCells.Add(new Vector3Int(x, y, z));

            if ((x == 0 || x == blocks.GetLength(0) - 1 || z == 0 || z == blocks.GetLength(2) - 1) && y == 0)
                modules[x, y, z] = new Module(new Vector3Int(x, y, z), edgeModuleTypes, true, objectSize1, 0, 0);

            else
                modules[x, y, z] = new Module(new Vector3Int(x, y, z), moduleTypes, false, objectSize1, 0, 0);
        }

        lowEntropyList = new List<Vector3Int>();

        errorStates = new List<string>();
        history = new List<History3D>();

        cellBlocks = new List<List<Vector3Int>>();
        currentCells = new List<Vector3Int>();

        UpdateValids();


    }

    // Update is called once per frame
    void Update()
    {
        if (!CheckFullyCollapsed())
        {
            UpdateEntropy();

            if (lowEntropyList.Count <= 0)
                return;

            System.Random random = new System.Random();
            int index = random.Next(0, lowEntropyList.Count);
            Vector3Int currentCell = lowEntropyList[0];

            bool error = true;

            while (modules[currentCell.x, currentCell.y, currentCell.z].GetValidTypes().Count > 0 && error)
            {
                modules[currentCell.x, currentCell.y, currentCell.z].Collapse();

                if (errorStates.Contains(CurrentState()))
                    modules[currentCell.x, currentCell.y, currentCell.z].RemoveType(modules[currentCell.x, currentCell.y, currentCell.z].GetTileType());

                else
                {
                    if (currentCell.y == 0 && modules[currentCell.x, currentCell.y, currentCell.z].GetTileType().Contains("empty"))
                    {
                        for (int i = 1; i < height; i++)
                        {
                            modules[currentCell.x, i, currentCell.z].CollapseTo("empty0");
                        }
                    }
                    history.Add(new History3D(CurrentState(), new(currentCell, modules[currentCell.x, currentCell.y, currentCell.z].GetTileType())));
                    error = false;
                }
            }

            if (error)
            {
                modules[currentCell.x, currentCell.y, currentCell.z].ResetModule();

                if (!errorStates.Contains(CurrentState()))
                    errorStates.Add(CurrentState());

                Vector3Int lastHistoryCell = history[^1].Step.Item1;
                modules[lastHistoryCell.x, lastHistoryCell.y, lastHistoryCell.z].ResetModule();
                history.Remove(history[^1]);


            }

            if (modules[currentCell.x, currentCell.y, currentCell.z].IsCollapsed() && !modules[currentCell.x, currentCell.y, currentCell.z].GetTileType().Contains("empty"))
                modules[currentCell.x, currentCell.y, currentCell.z].SetObject(gameObjects[modules[currentCell.x, currentCell.y, currentCell.z].GetTileType()[0..^1]].Item1);

            UpdateValids();
/*
            if (lastState != CurrentState())
                foreach (var module in modules)
                    if (module.collapsed && module.model == null && !module.GetTileType().Contains("empty"))
                        module.SetObject(gameObjects[module.GetTileType()[0..^1]].Item1);*/

            lastState = CurrentState();

            if (step1Done)
            {
                if (CheckBlockCollapsed(currentCells))
                {
                    if (currentIndex < cellBlocks.Count - 1)
                    {
                        currentIndex++;
                        currentCells = cellBlocks[currentIndex];
                    }
                }
            }


        }

        else
        {
            if (!step1Done)
            {
                Debug.Log("step1");
                int[,] grid = new int[xSize, zSize];

                foreach (var module in modules)
                {
                    int x = module.GetGridPosition().x;
                    int y = module.GetGridPosition().z;
                    string type = module.GetTileType()[0..^1];

                    if (type == "ground")
                        grid[x, y] = 0;
                    else
                        grid[x, y] = 1;
                }

                int[,] dividedGrid = gridManager.DivideGrid(grid);

                sampleManager = new SampleManager3D(step2Tag, objectSize2, yOffset2);
                rules = sampleManager.GenerateRulesFromSamples();
                gameObjects = sampleManager.GetObjects();
                moduleTypes = new List<Tuple<string, int>>();



                foreach (var rule in rules)
                    moduleTypes.Add(new(rule.Key, gameObjects[rule.Key[0..^1]].Item2));

                

                groundModuleTypes = moduleTypes.Where(tuple => tuple.Item1.Contains("ground")).ToList();


                overModuleTypes = moduleTypes.Where(tuple => !tuple.Item1.Contains("ground")).ToList();



                /*  foreach (var group in cellGroups)
                  {
                      foreach (var cell in group)
                          modules[cell.x, 0, cell.y].ResetModule();
                  }*/


                gridManager = new GridManager(dividedGrid.GetLength(0), height, dividedGrid.GetLength(1));
                blocks = gridManager.CreateGrid();
                modules = new Module[blocks.GetLength(0), blocks.GetLength(1), blocks.GetLength(2)];
                newModules = new Module[blocks.GetLength(0), blocks.GetLength(1), blocks.GetLength(2)];

                List<List<Vector2Int>> cellGroups = gridManager.FindEnclosedCellGroups(dividedGrid);
                cellBlocks = gridManager.CreateGrids(cellGroups, height);
                foreach (var block in blocks)
                {
                    int x = block.x;
                    int y = block.y;
                    int z = block.z;

                    if (dividedGrid[x, z] == 1)
                    {
                        modules[x, y, z] = new Module(new Vector3Int(x, y, z), moduleTypes, true, objectSize2, offset1, offset2);
                        modules[x, y, z].CollapseTo("empty0");
                    }

                    else
                    {
                        if (y == 0)
                        {
                            modules[x, y, z] = new Module(new Vector3Int(x, y, z), groundModuleTypes, false, objectSize2, offset1, offset2);
                        }
                        else if (y == blocks.GetLength(1) - 1)
                        {
                            modules[x, y, z] = new Module(new Vector3Int(x, y, z), moduleTypes, true, objectSize2, offset1, offset2);
                            modules[x, y, z].CollapseTo("empty0");
                        }
                        else
                        {
                            modules[x, y, z] = new Module(new Vector3Int(x, y, z), overModuleTypes, false, objectSize2, offset1, offset2);
                        }
                    }
                }

                lowEntropyList = new List<Vector3Int>();

                errorStates = new List<string>();
                history = new List<History3D>();
                currentIndex = 0;
                currentCells = cellBlocks[currentIndex];

                step1Done = true;
                UpdateValids();

            }
        }
    }

    // Gets a list of blocks with the lowest entropy (number of valid modules)
    private void UpdateEntropy()
    {
        int lowest = int.MaxValue;
        lowEntropyList.Clear();

        foreach (var module in modules)
        {
            if (step1Done && !currentCells.Contains(module.gridPosition))
                continue;

            if ((module.collapsed == true) || (module.GetValidTypes().Count > lowest))
                continue;

            if (module.GetValidTypes().Count < lowest)
            {
                lowest = module.GetValidTypes().Count;
                lowEntropyList.Clear();
                lowEntropyList.Add(module.gridPosition);
            }

            else if (module.GetValidTypes().Count == lowest)
                lowEntropyList.Add(module.gridPosition);
        }
    }

    // Gets a list of valid modules for a direction
    public List<string> GetValidsForDirection(string type, Dir dir)
    {
        return rules[type][dir];
    }

    private string CurrentState()
    {
        string state = "";
        string separator = "-";
        foreach (var module in modules)
        {
            if (!module.IsCollapsed())
                state += "x" + separator;
            else
                state += moduleTypes.FindIndex(tuple => tuple.Item1 == module.GetTileType()).ToString() + separator;
        }

        state = state.Remove(state.Length - 1);
        return state;
    }

    // Sets valid modules for each block by checking all directions
    private void UpdateValids()
    {
        Array.Copy(modules, newModules, modules.Length);
        List<string> valids = new();

        foreach (Vector3Int block in blocks)
        {
            int x = block.x;
            int y = block.y;
            int z = block.z;

            if (step1Done && !currentCells.Contains(block))
                continue;

            if (CheckEdgeCollapsed() || (modules[x, y, z].IsEdge() && !CheckEdgeCollapsed()))
            {
                if (modules[x, y, z].collapsed)
                    newModules[x, y, z] = modules[x, y, z];

                else
                {
                    List<string> options = new();

                    if (!CheckEdgeCollapsed())
                        options = edgeModuleTypes.Select(tuple => tuple.Item1).ToList();

                    else
                        options = modules[x, y, z].GetModuleTypes().Select(tuple => tuple.Item1).ToList();

                    if (x > 0)
                    {
                        valids.Clear();
                        if (modules[x - 1, y, z].collapsed)
                            valids.AddRange(GetValidsForDirection(modules[x - 1, y, z].GetTileType(), Dir.Right));

                        else
                            valids = options;

                        valids = valids.Distinct().ToList();
                        options = options.Intersect(valids).ToList();
                    }

                    if (x < blocks.GetLength(0) - 1)
                    {
                        valids.Clear();
                        if (modules[x + 1, y, z].collapsed)
                            valids.AddRange(GetValidsForDirection(modules[x + 1, y, z].GetTileType(), Dir.Left));

                        else
                            valids = options;

                        valids = valids.Distinct().ToList();
                        options = options.Intersect(valids).ToList();
                    }

                    if (y > 0)
                    {
                        valids.Clear();
                        if (modules[x, y - 1, z].collapsed)
                            valids.AddRange(GetValidsForDirection(modules[x, y - 1, z].GetTileType(), Dir.Up));

                        else
                            valids = options;

                        valids = valids.Distinct().ToList();
                        options = options.Intersect(valids).ToList();
                    }

                    if (y < blocks.GetLength(1) - 1)
                    {
                        valids.Clear();
                        if (modules[x, y + 1, z].collapsed)
                            valids.AddRange(GetValidsForDirection(modules[x, y + 1, z].GetTileType(), Dir.Down));

                        else
                            valids = options;

                        valids = valids.Distinct().ToList();
                        options = options.Intersect(valids).ToList();
                    }

                    if (z > 0)
                    {
                        valids.Clear();
                        if (modules[x, y, z - 1].collapsed)
                            valids.AddRange(GetValidsForDirection(modules[x, y, z - 1].GetTileType(), Dir.Forward));

                        else
                            valids = options;

                        valids = valids.Distinct().ToList();
                        options = options.Intersect(valids).ToList();
                    }

                    if (z < blocks.GetLength(2) - 1)
                    {
                        valids.Clear();
                        if (modules[x, y, z + 1].collapsed)
                            valids.AddRange(GetValidsForDirection(modules[x, y, z + 1].GetTileType(), Dir.Back));

                        else
                            valids = options;

                        valids = valids.Distinct().ToList();
                        options = options.Intersect(valids).ToList();
                    }

                    newModules[x, y, z].SetValidTypes(options);
                }
            }
        }

        modules = newModules;
    }

    private bool CheckFullyCollapsed()
    {
        foreach (var module in modules)
            if (!module.collapsed)
                return false;

        return true;
    }

    private bool CheckEdgeCollapsed()
    {
        foreach (var module in modules)
            if (module.IsEdge() && !module.collapsed)
                return false;

        return true;
    }

    private bool CheckBlockCollapsed(List<Vector3Int> group)
    {
        foreach (Vector3Int cell in group)
        {
            int x = cell.x;
            int y = cell.y;
            int z = cell.z;

            if (!modules[x, y, z].IsCollapsed())
                return false;
        }

        return true;
    }

    public void ResetGrid()
    {
        foreach (var module in modules)
        {
            module.ResetModule();
        }
    }

    private List<Vector3Int> GetCellAdjacents(Vector3Int cell)
    {
        List<Vector3Int> adjacents = new();
        int x = cell.x;
        int y = cell.y;
        int z = cell.z;

        if (x > 0)
            if (modules[x - 1, y, z].IsCollapsed())
                adjacents.Add(modules[x - 1, y, z].GetGridPosition());

        if (x < blocks.GetLength(0) - 1)
            if (modules[x + 1, y, z].IsCollapsed())
                adjacents.Add(modules[x + 1, y, z].GetGridPosition());

        if (z > 0)
            if (modules[x, y, z - 1].IsCollapsed())
                adjacents.Add(modules[x, y, z - 1].GetGridPosition());

        if (z < blocks.GetLength(2) - 1)
            if (modules[x, y, z + 1].IsCollapsed())
                adjacents.Add(modules[x, y, z + 1].GetGridPosition());

        if (y > 0)
            if (modules[x, y - 1, z].IsCollapsed())
                adjacents.Add(modules[x, y - 1, z].GetGridPosition());

        if (y < blocks.GetLength(1) - 1)
            if (modules[x, y + 1, z].IsCollapsed())
                adjacents.Add(modules[x, y + 1, z].GetGridPosition());

        return adjacents;
    }

    private void LoadState(string state)
    {
        string[] states = state.Split("-");
        int index = 0;
        foreach (var module in modules)
        {
            if (states[index].Equals("x"))
                module.ResetModule();
            else
                module.CollapseToType(moduleTypes[int.Parse(states[index])].Item1);

            index++;
        }
    }
}