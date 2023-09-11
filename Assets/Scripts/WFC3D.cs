using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using TMPro;

public class WFC3D : MonoBehaviour
{
    // Grid dimensions
    

    public string step1Tag;
    public string step2Tag;

    public float objectSize1;
    public float objectSize2;

    public float yOffset1;
    public float yOffset2;

    public float offset1;
    public float offset2;

    public TMP_InputField widthInput;
    public TMP_InputField depthInput;
    public TMP_InputField heightInput;
    public TMP_InputField buildingHeightInput;
    public TMP_InputField streetGroundInput;
    public TMP_InputField emptyInput;
    public TMP_InputField cornerInput;

    private int xSize, ySize, zSize;
    private int height;
    private int buildingHeight;
    private int emptyProbability;
    private int streetGroundRatio;
    private int cornerProbability;

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
    private Module[,,] streetModules;


    // Blocks (coordinates) with the lowest entropy
    private List<Vector3Int> lowEntropyList;

    // Backtraking
    private List<string> errorStates;
    private List<History3D> history;

    private List<String> historyLog;    // zum testen

    //private bool step1Done = false;

    private List<Vector3Int> currentCells;
    private List<List<Vector3Int>> cellBlocks;
    private int currentIndex = 0;

    private HashSet<Vector3Int> entropyCells;

    private string lastState = "";
    private string errorState = "";

    private Dictionary<Vector3Int, int> errorCount;

    // current step for update-loop
    private int step = 0;

    // Use this for initialization
    void Start()
    {
        Debug.Log("step 0");

        Debug.Log(int.Parse(widthInput.text));
        xSize = int.Parse(widthInput.text);
        ySize = 1;
        zSize = int.Parse(depthInput.text);
        height = int.Parse(heightInput.text);
        buildingHeight = Mathf.CeilToInt(int.Parse(buildingHeightInput.text) * height / 100);
        Debug.Log(buildingHeight);
        emptyProbability = int.Parse(emptyInput.text);
        streetGroundRatio = int.Parse(streetGroundInput.text);
        cornerProbability = int.Parse(cornerInput.text);

        gridManager = new GridManager(xSize, ySize, zSize);
        blocks = gridManager.CreateGrid();

        sampleManager = new SampleManager3D(step1Tag, objectSize1, yOffset1, emptyProbability, buildingHeight, 2, streetGroundRatio, cornerProbability);

        rules = sampleManager.GenerateRulesFromSamples();
        gameObjects = sampleManager.GetObjects();
        moduleTypes = new List<Tuple<string, int>>();
        groundModuleTypes = new List<Tuple<string, int>>();
        overModuleTypes = new List<Tuple<string, int>>();

        foreach (var rule in rules)
            moduleTypes.Add(new(rule.Key, gameObjects[rule.Key[0..^1]].Item2));

        edgeModuleTypes = moduleTypes.Where(tuple => !tuple.Item1.Contains("buildarea")).ToList();

        modules = new Module[xSize, ySize, zSize];
        //newModules = new Module[xSize, ySize, zSize];

        foreach (var block in blocks)
        {

            int x = block.x;
            int y = block.y;
            int z = block.z;


            if ((x == 0 || x == blocks.GetLength(0) - 1 || z == 0 || z == blocks.GetLength(2) - 1) && y == 0)
                modules[x, y, z] = new Module(new Vector3Int(x, y, z), edgeModuleTypes, true, objectSize1, 0, 0);

            else
                modules[x, y, z] = new Module(new Vector3Int(x, y, z), moduleTypes, false, objectSize1, 0, 0);
        }

        streetModules = new Module[xSize, ySize, zSize];
        lowEntropyList = new List<Vector3Int>();

        errorStates = new List<string>();
        errorCount = new Dictionary<Vector3Int, int>();
        history = new List<History3D>();
        historyLog = new List<string>();

        cellBlocks = new List<List<Vector3Int>>();
        currentCells = new List<Vector3Int>();
        entropyCells = new HashSet<Vector3Int>();
        cellBlocks.Add(currentCells);   // damit ein Element enthalten ist

        foreach (Vector3Int block in blocks)
        {
            currentCells.Add(block);
            entropyCells.Add(block);
        }

        UpdateValids();
    }

    void initStep1()
    {

        streetModules = modules;
        step = 1;
        Debug.Log("step 1");

        int[,] grid = new int[xSize, zSize];

        
        foreach (var module in modules)
        {
            int x = module.GetGridPosition().x;
            int y = module.GetGridPosition().z;
            string type = module.GetTileType()[0..^1];

            if (type == "buildarea")
                grid[x, y] = 0;
            else
                grid[x, y] = 1;
        }

        int[,] dividedGrid = gridManager.DivideGrid(grid);

        sampleManager = new SampleManager3D (step2Tag, objectSize2, yOffset2, emptyProbability, buildingHeight, Mathf.CeilToInt(buildingHeight / 2) + 1, streetGroundRatio , cornerProbability);
        rules = sampleManager.GenerateRulesFromSamples();
        gameObjects = sampleManager.GetObjects();
        moduleTypes = new List<Tuple<string, int>>();

        foreach (var rule in rules)
            moduleTypes.Add(new(rule.Key, gameObjects[rule.Key[0..^1]].Item2));

        groundModuleTypes = moduleTypes.Where(tuple => tuple.Item1.Contains("ground")).ToList();
        groundModuleTypes.AddRange(moduleTypes.Where(tuple => tuple.Item1.Contains("empty")).ToList());
        overModuleTypes = moduleTypes.Where(tuple => !tuple.Item1.Contains("ground")).ToList();

        gridManager = new GridManager(dividedGrid.GetLength(0), height, dividedGrid.GetLength(1));
        blocks = gridManager.CreateGrid();
        modules = new Module[blocks.GetLength(0), blocks.GetLength(1), blocks.GetLength(2)];

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

        currentIndex = 0;
        loadBlock();
    }


    // Update is called once per frame
    void Update()
    {

        Vector3Int tempCell;

        UpdateEntropy();
        if (lowEntropyList.Count > 0 || currentIndex < cellBlocks.Count - 1)
        {

            if (lowEntropyList.Count > 0)
            {


                System.Random random = new System.Random();
                int index = random.Next(0, lowEntropyList.Count);
                Vector3Int currentCell = lowEntropyList[0]; 
                Module currentModule = modules[currentCell.x, currentCell.y, currentCell.z];

                bool error = true;

                while (currentModule.GetValidTypes().Count > 0 && error)
                {
                    currentModule.Collapse();

                    if (errorStates.Contains(CurrentState()))
                        currentModule.RemoveType(currentModule.GetTileType());

                    else
                    {

                        history.Add(new History3D(CurrentState(), new(currentCell, currentModule.GetTileType())));
                        historyLog.Add("Add :" + currentCell.ToString() + " - " + currentModule.GetTileType());
                        entropyCells.Remove(currentCell);
                        error = false;
                    }
                }

                if (error)
                {
                    Vector3Int lastHistoryCell;
                    int errCount = 0;
                    if (errorCount.TryGetValue(currentCell, out errCount))
                    {
                        errorCount[currentCell] = errCount + 1;
                    }
                    else
                    {
                        errorCount.Add(currentCell, 1);
                    }

                    if (errCount < 10)
                    {
                        currentModule.ResetModule();
                        entropyCells.Add(currentCell);

                        errorState = CurrentState();
                        if (!errorStates.Contains(errorState))
                            errorStates.Add(errorState);
                    }

                    if (errCount > 10)
                    {
                        foreach (var cell in currentCells)
                        {
                            modules[cell.x, cell.y, cell.z].ResetModule();
                        }
                        Debug.Log("reload block " + currentIndex);
                        loadBlock();

                    }
                    else
                    {
                        if (history.Count == 0)
                        {
                            Debug.Log("History empty");
                        }

                        lastHistoryCell = history[^1].Step.Item1;
                        modules[lastHistoryCell.x, lastHistoryCell.y, lastHistoryCell.z].ResetModule();
                        history.Remove(history[^1]);
                        historyLog.Add("Rem :" + lastHistoryCell.ToString());
                        entropyCells.Add(lastHistoryCell);
                    }

                    UpdateValids();


                }

                if (currentModule.IsCollapsed() && !currentModule.GetTileType().Contains("empty"))
                    currentModule.SetObject(gameObjects[currentModule.GetTileType()[0..^1]].Item1);

                UpdateValidsNeighbors(currentCell);

                lastState = CurrentState();
            }

            if (step == 1)
            {
                if (CheckBlockCollapsed(currentCells))
                {
                    if (currentIndex < cellBlocks.Count - 1)
                    {
                        currentIndex++;
                        loadBlock();
                    }
                }
            }
        }

        else
        {
            if (step < 1)
            {
                initStep1();
            }
            else if (step < 2)
            {
                Debug.Log("done");
                step = 2;
            }
        }
    }

    private void loadBlock()
    {
        errorStates = new List<string>();
        errorCount = new Dictionary<Vector3Int, int>();
        history = new List<History3D>();
        historyLog = new List<string>();
        currentCells = cellBlocks[currentIndex];
        entropyCells.Clear();
        foreach (Vector3Int cell in currentCells)
            entropyCells.Add(cell);
        UpdateValids();
        Debug.Log("block " + currentIndex);
    }

    private void ResetNeighbors(Vector3Int cell, int radius)
    {
        for (int x = cell.x - radius; x <= cell.x + radius; x++)
            for (int y = cell.y - radius; y <= cell.y + radius; y++)
                for (int z = cell.z - radius; z <= cell.z + radius; z++)
                {
                    Vector3Int neighbor = new Vector3Int(x, y, z);
                    if (currentCells.Contains(neighbor))
                    {
                        modules[neighbor.x, neighbor.y, neighbor.z].ResetModule();
                        UpdateValidsNeighbors(neighbor);
                    }
                }

    }

    // Gets a list of blocks with the lowest entropy (number of valid modules)
    private void UpdateEntropy()
    {
        int lowest = int.MaxValue;
        lowEntropyList.Clear();

        //foreach (var module in modules)
        foreach (var cell in entropyCells)  // war currentCells
        {
            var module = modules[cell.x, cell.y, cell.z];


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
        Module module;
        //foreach (var module in modules)
        foreach (var cell in currentCells) 
        {
            module = modules[cell.x, cell.y, cell.z];
            if (!module.IsCollapsed())
                state += "x" + separator;
            else
                state += moduleTypes.FindIndex(tuple => tuple.Item1 == module.GetTileType()).ToString() + separator;
        }

        state = state.Remove(state.Length - 1);
        return state;
    }

    private void UpdateValids()
    {

        bool edgeCollapsed = CheckEdgeCollapsed();

        foreach (Vector3Int block in currentCells)   
        {
            UpdateValidsCell(block, edgeCollapsed);
        }
    }

    private void UpdateValidsNeighbors(Vector3Int cell)
    {
        bool edgeCollapsed = CheckEdgeCollapsed();

        UpdateValidsCell(cell, edgeCollapsed);
        Vector3Int neighbor = new Vector3Int(cell.x - 1, cell.y, cell.z);
        if (currentCells.Contains(neighbor))
        {
            UpdateValidsCell(neighbor, edgeCollapsed);
        }
        neighbor = new Vector3Int(cell.x + 1, cell.y, cell.z);
        if (currentCells.Contains(neighbor))
        {
            UpdateValidsCell(neighbor, edgeCollapsed);
        }
        neighbor = new Vector3Int(cell.x, cell.y + 1, cell.z);
        if (currentCells.Contains(neighbor))
        {
            UpdateValidsCell(neighbor, edgeCollapsed);
        }
        neighbor = new Vector3Int(cell.x, cell.y - 1, cell.z);
        if (currentCells.Contains(neighbor))
        {
            UpdateValidsCell(neighbor, edgeCollapsed);
        }
        neighbor = new Vector3Int(cell.x, cell.y, cell.z + 1);
        if (currentCells.Contains(neighbor))
        {
            UpdateValidsCell(neighbor, edgeCollapsed);
        }
        neighbor = new Vector3Int(cell.x, cell.y, cell.z - 1);
        if (currentCells.Contains(neighbor))
        {
            UpdateValidsCell(neighbor, edgeCollapsed);
        }
    }

    private void UpdateValidsCell(Vector3Int block, bool edgeCollapsed)
    {
        List<string> valids = new();

        int x = block.x;
        int y = block.y;
        int z = block.z;


        if (edgeCollapsed || (modules[x, y, z].IsEdge() && !edgeCollapsed))
        {
            if (!modules[x, y, z].collapsed)

            {
                List<string> options = new();

                if (!edgeCollapsed)
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
                    if (options.Count == 0)
                    {
                        modules[x, y, z].errorCell = new Vector3Int(x - 1, y, z);
                    }
                }

                if (options.Count > 0 && x < blocks.GetLength(0) - 1)
                {
                    valids.Clear();
                    if (modules[x + 1, y, z].collapsed)
                        valids.AddRange(GetValidsForDirection(modules[x + 1, y, z].GetTileType(), Dir.Left));

                    else
                        valids = options;

                    valids = valids.Distinct().ToList();
                    options = options.Intersect(valids).ToList();
                    if (options.Count == 0)
                    {
                        modules[x, y, z].errorCell = new Vector3Int(x + 1, y, z);
                    }
                }

                if (options.Count > 0 && y > 0)
                {
                    valids.Clear();
                    if (modules[x, y - 1, z].collapsed)
                        valids.AddRange(GetValidsForDirection(modules[x, y - 1, z].GetTileType(), Dir.Up));

                    else
                        valids = options;

                    valids = valids.Distinct().ToList();
                    options = options.Intersect(valids).ToList();
                    if (options.Count == 0)
                    {
                        modules[x, y, z].errorCell = new Vector3Int(x, y - 1, z);
                    }
                }

                if (options.Count > 0 && y < blocks.GetLength(1) - 1)
                {
                    valids.Clear();
                    if (modules[x, y + 1, z].collapsed)
                        valids.AddRange(GetValidsForDirection(modules[x, y + 1, z].GetTileType(), Dir.Down));

                    else
                        valids = options;

                    valids = valids.Distinct().ToList();
                    options = options.Intersect(valids).ToList();
                    if (options.Count == 0)
                    {
                        modules[x, y, z].errorCell = new Vector3Int(x, y + 1, z);
                    }
                }

                if (options.Count > 0 && z > 0)
                {
                    valids.Clear();
                    if (modules[x, y, z - 1].collapsed)
                        valids.AddRange(GetValidsForDirection(modules[x, y, z - 1].GetTileType(), Dir.Forward));

                    else
                        valids = options;

                    valids = valids.Distinct().ToList();
                    options = options.Intersect(valids).ToList();
                    if (options.Count == 0)
                    {
                        modules[x, y, z].errorCell = new Vector3Int(x, y, z - 1);
                    }
                }

                if (options.Count > 0 && z < blocks.GetLength(2) - 1)
                {
                    valids.Clear();
                    if (modules[x, y, z + 1].collapsed)
                        valids.AddRange(GetValidsForDirection(modules[x, y, z + 1].GetTileType(), Dir.Back));

                    else
                        valids = options;

                    valids = valids.Distinct().ToList();
                    options = options.Intersect(valids).ToList();
                    if (options.Count == 0)
                    {
                        modules[x, y, z].errorCell = new Vector3Int(x, y, z + 1);
                    }
                }

                modules[x, y, z].SetValidTypes(options);
            }
        }
      
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
        if (step > 0)
            return true;

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

    public void Restart()
    {
        foreach (var module in streetModules)
        {
            module.SetObjectNull();
        }

        foreach (var module in modules)
        {
            module.SetObjectNull();
        }

        step = 0;
        lastState = "";
        errorState = "";
        Start();
    }
}