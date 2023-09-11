using UnityEngine;
using System.Collections.Generic;
using System;

// Generates adjacency rules from sample modules
public class SampleManager3D
{
    private readonly GameObject[] sampleModules;
    private readonly Dictionary<string, Tuple<GameObject, int>> gameObjects;
    private Dictionary<string, Dictionary<Dir, List<string>>> moduleRules;
    private string tag;
    private readonly float yOffset;
    private readonly float objectSize;
    private readonly int emptyProbability;
    private readonly int buildingHeight;
    private readonly int wallWeight;
    private readonly int streetGroundRatio;
    private readonly int cornerProbability;

    public SampleManager3D (string tag, float objectSize, float yOffset, int emptyProbability, int buildingHeight, int wallWeight, int streetGroundRatio, int cornerProbability)
    {
        sampleModules = GameObject.FindGameObjectsWithTag(tag);
        gameObjects = new Dictionary<string, Tuple<GameObject, int>>();
        this.tag = tag;
        this.objectSize = objectSize;
        this.yOffset = yOffset;
        this.emptyProbability = emptyProbability;
        this.buildingHeight = buildingHeight;
        this.wallWeight = wallWeight;
        this.streetGroundRatio = streetGroundRatio;
        this.cornerProbability = cornerProbability;
    }

    public Dictionary<string, Tuple<GameObject, int>> GetObjects()
    {
        return gameObjects;
    }

    // Generates a list of objects and their adjacency rules
    public Dictionary<string, Dictionary<Dir, List<string>>> GenerateRulesFromSamples()
    {
        moduleRules = new Dictionary<string, Dictionary<Dir, List<string>>>();
        //List<String> rotatedSampleModuleNames = new List<String>();
        //String sampleName;

        foreach (GameObject sampleModule in sampleModules)
        {
            if (!gameObjects.ContainsKey(sampleModule.name))
            {
                if (sampleModule.name.Contains("empty"))
                    gameObjects.Add(sampleModule.name, new(sampleModule, emptyProbability));

                else if (sampleModule.name.Contains("roof"))
                    gameObjects.Add(sampleModule.name, new(sampleModule, buildingHeight));

                else if (sampleModule.name.Contains("buildarea"))
                    gameObjects.Add(sampleModule.name, new(sampleModule, streetGroundRatio));

                else if (sampleModule.name.Contains("street"))
                {
                    if (sampleModule.name.Contains("corner") || sampleModule.name.Contains("side") || sampleModule.name.Contains("cross"))
                        gameObjects.Add(sampleModule.name, new(sampleModule, (100 - streetGroundRatio)));
                    else if (sampleModule.name.Contains("straight"))
                        gameObjects.Add(sampleModule.name, new(sampleModule, (100 - streetGroundRatio)));
                }

                else
                    gameObjects.Add(sampleModule.name, new(sampleModule, wallWeight));  
            }
                

            String moduleName = sampleModule.name + GetRotations((int)sampleModule.transform.eulerAngles.y);

            Dictionary<Dir, List<string>> adjacents = GetAdjacents(moduleName);     // war moduleName


            MergeRules(moduleName, adjacents);
            ReverseRules(moduleName, adjacents);

            string name = moduleName[0..^1];
            int rotation = int.Parse(moduleName[^1..]);

            Dictionary<Dir, List<string>> rotatedRules;

            for (int i = 0; i <= 3; i++)
            {

                if (i != rotation)
                {
                    rotatedRules = RotateRules(adjacents, i, rotation);
                    //moduleRules.Add(name + i.ToString(), RotateRules(adjacents, i, rotation));
                    MergeRules(name + i.ToString(), rotatedRules);
                    ReverseRules(name + i.ToString(), rotatedRules);
                }
            }
            //}
        }

        //if (tag.Contains("2"))
        //{
        //    foreach (var rule in moduleRules)
        //    {
        //        Debug.Log("Module:::::::::::::::" + rule);
        //        foreach (var dir in rule.Value)
        //        {
        //            Debug.Log("Direction:::::::::::::::" + dir);
        //            foreach (var dirRule in dir.Value)
        //            {
        //                Debug.Log(dirRule);
        //            }
        //        }
        //    }
        //}
        //Debug.Log(moduleRules.Count);

        return moduleRules;
    }

    private void ReverseRules(string moduleName, Dictionary<Dir, List<string>> adjacents)
    {
        Dir newDir = Dir.Forward;
        foreach (var dir in adjacents.Keys)
        {
            switch (dir)
            {
                case Dir.Forward: newDir = Dir.Back; break;
                case Dir.Back: newDir = Dir.Forward; break;
                case Dir.Left: newDir = Dir.Right; break;
                case Dir.Right: newDir = Dir.Left; break;
                case Dir.Up: newDir = Dir.Down; break;
                case Dir.Down: newDir = Dir.Up; break;
            }

            foreach (var tile in adjacents[dir])
            {
                Dictionary<Dir, List<string>> revRules = new Dictionary<Dir, List<string>>();
                revRules.Add(Dir.Forward, new List<string>());
                revRules.Add(Dir.Back, new List<string>());
                revRules.Add(Dir.Left, new List<string>());
                revRules.Add(Dir.Right, new List<string>());
                revRules.Add(Dir.Up, new List<string>());
                revRules.Add(Dir.Down, new List<string>());
                List<string> newRule = revRules[newDir];
                newRule.Add(moduleName);
                MergeRules(tile, revRules);
            }
        }
    }

    private void MergeRules(string moduleName, Dictionary<Dir, List<string>> adjacents)
    {
        if (!moduleRules.ContainsKey(moduleName))
        {
            moduleRules.Add(moduleName, adjacents);
        }
        else
        {
            Dictionary<Dir, List<string>> existingAdjacents = moduleRules[moduleName];
            foreach (var dir in adjacents.Keys)
            {
                List<string> existingRules = existingAdjacents[dir];
                foreach (var rule in adjacents[dir])
                {
                    if (!existingRules.Contains(rule))
                    {
                        existingRules.Add(rule);
                    }
                }
            }
        }
    }

    private Dictionary<Dir, List<string>> RotateRules(Dictionary<Dir, List<string>> adjacents, int targetRotation, int currentRotation)
    {
        Dictionary<Dir, List<string>> rotatedAdjacents = new Dictionary<Dir, List<string>>();
        rotatedAdjacents.Add(Dir.Forward, new List<string>());
        rotatedAdjacents.Add(Dir.Back, new List<string>());
        rotatedAdjacents.Add(Dir.Left, new List<string>());
        rotatedAdjacents.Add(Dir.Right, new List<string>());
        rotatedAdjacents.Add(Dir.Up, new List<string>());
        rotatedAdjacents.Add(Dir.Down, new List<string>());

        int dirRotation = (targetRotation - currentRotation) % 4;
        if (dirRotation < 0)
            dirRotation += 4;

        switch (dirRotation)
        {
            case 1:

                foreach (string moduleName in adjacents[Dir.Right])
                {
                    string name = moduleName[0..^1];
                    int currentModuleRotation = int.Parse(moduleName[^1..]);

                    int rotation = (currentModuleRotation + dirRotation) % 4;
                    if (rotation < 0)
                        rotation += 4;

                    rotatedAdjacents[Dir.Back].Add(name + rotation.ToString());
                }

                foreach (string moduleName in adjacents[Dir.Back])
                {
                    string name = moduleName[0..^1];
                    int currentModuleRotation = int.Parse(moduleName[^1..]);

                    int rotation = (currentModuleRotation + dirRotation) % 4;
                    if (rotation < 0)
                        rotation += 4;

                    rotatedAdjacents[Dir.Left].Add(name + rotation.ToString());
                }

                foreach (string moduleName in adjacents[Dir.Left])
                {
                    string name = moduleName[0..^1];
                    int currentModuleRotation = int.Parse(moduleName[^1..]);

                    int rotation = (currentModuleRotation + dirRotation) % 4;
                    if (rotation < 0)
                        rotation += 4;

                    rotatedAdjacents[Dir.Forward].Add(name + rotation.ToString());
                }

                foreach (string moduleName in adjacents[Dir.Forward])
                {
                    string name = moduleName[0..^1];
                    int currentModuleRotation = int.Parse(moduleName[^1..]);

                    int rotation = (currentModuleRotation + dirRotation) % 4;
                    if (rotation < 0)
                        rotation += 4;

                    rotatedAdjacents[Dir.Right].Add(name + rotation.ToString());
                }

                break;

            case 2:

                foreach (string moduleName in adjacents[Dir.Back])
                {
                    string name = moduleName[0..^1];
                    int currentModuleRotation = int.Parse(moduleName[^1..]);

                    int rotation = (currentModuleRotation + dirRotation) % 4;
                    if (rotation < 0)
                        rotation += 4;

                    rotatedAdjacents[Dir.Forward].Add(name + rotation.ToString());
                }

                foreach (string moduleName in adjacents[Dir.Left])
                {
                    string name = moduleName[0..^1];
                    int currentModuleRotation = int.Parse(moduleName[^1..]);

                    int rotation = (currentModuleRotation + dirRotation) % 4;
                    if (rotation < 0)
                        rotation += 4;

                    rotatedAdjacents[Dir.Right].Add(name + rotation.ToString());
                }

                foreach (string moduleName in adjacents[Dir.Forward])
                {
                    string name = moduleName[0..^1];
                    int currentModuleRotation = int.Parse(moduleName[^1..]);

                    int rotation = (currentModuleRotation + dirRotation) % 4;
                    if (rotation < 0)
                        rotation += 4;

                    rotatedAdjacents[Dir.Back].Add(name + rotation.ToString());
                }

                foreach (string moduleName in adjacents[Dir.Right])
                {
                    string name = moduleName[0..^1];
                    int currentModuleRotation = int.Parse(moduleName[^1..]);

                    int rotation = (currentModuleRotation + dirRotation) % 4;
                    if (rotation < 0)
                        rotation += 4;

                    rotatedAdjacents[Dir.Left].Add(name + rotation.ToString());
                }

                break;

            case 3:

                foreach (string moduleName in adjacents[Dir.Left])
                {
                    string name = moduleName[0..^1];
                    int currentModuleRotation = int.Parse(moduleName[^1..]);

                    int rotation = (currentModuleRotation + dirRotation) % 4;
                    if (rotation < 0)
                        rotation += 4;

                    rotatedAdjacents[Dir.Back].Add(name + rotation.ToString());
                }

                foreach (string moduleName in adjacents[Dir.Forward])
                {
                    string name = moduleName[0..^1];
                    int currentModuleRotation = int.Parse(moduleName[^1..]);

                    int rotation = (currentModuleRotation + dirRotation) % 4;
                    if (rotation < 0)
                        rotation += 4;

                    rotatedAdjacents[Dir.Left].Add(name + rotation.ToString());
                }

                foreach (string moduleName in adjacents[Dir.Right])
                {
                    string name = moduleName[0..^1];
                    int currentModuleRotation = int.Parse(moduleName[^1..]);

                    int rotation = (currentModuleRotation + dirRotation) % 4;
                    if (rotation < 0)
                        rotation += 4;

                    rotatedAdjacents[Dir.Forward].Add(name + rotation.ToString());
                }

                foreach (string moduleName in adjacents[Dir.Back])
                {
                    string name = moduleName[0..^1];
                    int currentModuleRotation = int.Parse(moduleName[^1..]);

                    int rotation = (currentModuleRotation + dirRotation) % 4;
                    if (rotation < 0)
                        rotation += 4;

                    rotatedAdjacents[Dir.Right].Add(name + rotation.ToString());
                }

                break;
            default:
                break;
        }

        foreach (string moduleName in adjacents[Dir.Up])
        {
            string name = moduleName[0..^1];
            int currentModuleRotation = int.Parse(moduleName[^1..]);

            int rotation = (currentModuleRotation + dirRotation) % 4;
            if (rotation < 0)
                rotation += 4;

            rotatedAdjacents[Dir.Up].Add(name + rotation.ToString());
        }

        foreach (string moduleName in adjacents[Dir.Down])
        {
            string name = moduleName[0..^1];
            int currentModuleRotation = int.Parse(moduleName[^1..]);

            int rotation = (currentModuleRotation + dirRotation) % 4;
            if (rotation < 0)
                rotation += 4;

            rotatedAdjacents[Dir.Down].Add(name + rotation.ToString());
        }



        return rotatedAdjacents;
    }

    // Generates adjacency rules for each direction from sample
    private Dictionary<Dir, List<string>> GetAdjacents(string moduleName)
    {
        Dictionary<Dir, List<string>> adjacents = new Dictionary<Dir, List<string>>();
        adjacents.Add(Dir.Forward, new List<string>());
        adjacents.Add(Dir.Back, new List<string>());
        adjacents.Add(Dir.Left, new List<string>());
        adjacents.Add(Dir.Right, new List<string>());
        adjacents.Add(Dir.Up, new List<string>());
        adjacents.Add(Dir.Down, new List<string>());

        GameObject adjacentObject;

        if (moduleName == "Ground_fassadecorner0")
        {
            Debug.Log("");
        }

        foreach (GameObject sampleModule in sampleModules)
        {
            int rotations = GetRotations((int)sampleModule.transform.eulerAngles.y);
            string sampleModuleName = sampleModule.name + rotations.ToString();

            if (sampleModuleName == moduleName)
            {
                adjacentObject = FindObjectAtPosition(sampleModule.transform.position + (Vector3.forward * objectSize) + new Vector3(0, yOffset, 0));

                if (adjacentObject != null)
                {
                    if (adjacentObject.GetComponent<ModuleData>().symmetry == "x")
                    {
                        for (int i = 0; i <= 3; i++)
                        {
                            string adjacentName = adjacentObject.name + i;

                            if (!adjacents[Dir.Forward].Contains(adjacentName))
                                adjacents[Dir.Forward].Add(adjacentName);
                        }
                    }

                    else if (adjacentObject.GetComponent<ModuleData>().symmetry == "l")
                    {
                        string adjacentName = adjacentObject.name + GetRotations((int)adjacentObject.transform.eulerAngles.y);

                        if (!adjacents[Dir.Forward].Contains(adjacentName))
                            adjacents[Dir.Forward].Add(adjacentName);

                        adjacentName = adjacentObject.name + GetRotations((int)adjacentObject.transform.eulerAngles.y + 180);

                        if (!adjacents[Dir.Forward].Contains(adjacentName))
                            adjacents[Dir.Forward].Add(adjacentName);
                    }

                    else
                    {
                        string adjacentName = adjacentObject.name + GetRotations((int)adjacentObject.transform.eulerAngles.y);

                        if (!adjacents[Dir.Forward].Contains(adjacentName))
                            adjacents[Dir.Forward].Add(adjacentName);
                    }
                }

                adjacentObject = FindObjectAtPosition(sampleModule.transform.position + (Vector3.back * objectSize) + new Vector3(0, yOffset, 0));

                if (adjacentObject != null)
                {
                    if (adjacentObject.GetComponent<ModuleData>().symmetry == "x")
                    {
                        for (int i = 0; i <= 3; i++)
                        {
                            string adjacentName = adjacentObject.name + i;

                            if (!adjacents[Dir.Back].Contains(adjacentName))
                                adjacents[Dir.Back].Add(adjacentName);
                        }
                    }

                    else if (adjacentObject.GetComponent<ModuleData>().symmetry == "l")
                    {
                        string adjacentName = adjacentObject.name + GetRotations((int)adjacentObject.transform.eulerAngles.y);

                        if (!adjacents[Dir.Back].Contains(adjacentName))
                            adjacents[Dir.Back].Add(adjacentName);

                        adjacentName = adjacentObject.name + GetRotations((int)adjacentObject.transform.eulerAngles.y + 180);

                        if (!adjacents[Dir.Back].Contains(adjacentName))
                            adjacents[Dir.Back].Add(adjacentName);
                    }

                    else
                    {
                        string adjacentName = adjacentObject.name + GetRotations((int)adjacentObject.transform.eulerAngles.y);

                        if (!adjacents[Dir.Back].Contains(adjacentName))
                            adjacents[Dir.Back].Add(adjacentName);
                    }
                }

                adjacentObject = FindObjectAtPosition(sampleModule.transform.position + (Vector3.left * objectSize) + new Vector3(0, yOffset, 0));

                if (adjacentObject != null)
                {
                    if (adjacentObject.GetComponent<ModuleData>().symmetry == "x")
                    {
                        for (int i = 0; i <= 3; i++)
                        {
                            string adjacentName = adjacentObject.name + i;

                            if (!adjacents[Dir.Left].Contains(adjacentName))
                                adjacents[Dir.Left].Add(adjacentName);
                        }
                    }

                    else if (adjacentObject.GetComponent<ModuleData>().symmetry == "l")
                    {
                        string adjacentName = adjacentObject.name + GetRotations((int)adjacentObject.transform.eulerAngles.y);

                        if (!adjacents[Dir.Left].Contains(adjacentName))
                            adjacents[Dir.Left].Add(adjacentName);

                        adjacentName = adjacentObject.name + GetRotations((int)adjacentObject.transform.eulerAngles.y + 180);

                        if (!adjacents[Dir.Left].Contains(adjacentName))
                            adjacents[Dir.Left].Add(adjacentName);
                    }

                    else
                    {
                        string adjacentName = adjacentObject.name + GetRotations((int)adjacentObject.transform.eulerAngles.y);

                        if (!adjacents[Dir.Left].Contains(adjacentName))
                            adjacents[Dir.Left].Add(adjacentName);
                    }
                }

                adjacentObject = FindObjectAtPosition(sampleModule.transform.position + (Vector3.right * objectSize) + new Vector3(0, yOffset, 0));

                if (adjacentObject != null)
                {
                    if (adjacentObject.GetComponent<ModuleData>().symmetry == "x")
                    {
                        for (int i = 0; i <= 3; i++)
                        {
                            string adjacentName = adjacentObject.name + i;

                            if (!adjacents[Dir.Right].Contains(adjacentName))
                                adjacents[Dir.Right].Add(adjacentName);
                        }
                    }

                    else if (adjacentObject.GetComponent<ModuleData>().symmetry == "l")
                    {
                        string adjacentName = adjacentObject.name + GetRotations((int)adjacentObject.transform.eulerAngles.y);

                        if (!adjacents[Dir.Right].Contains(adjacentName))
                            adjacents[Dir.Right].Add(adjacentName);

                        adjacentName = adjacentObject.name + GetRotations((int)adjacentObject.transform.eulerAngles.y + 180);

                        if (!adjacents[Dir.Right].Contains(adjacentName))
                            adjacents[Dir.Right].Add(adjacentName);
                    }

                    else
                    {
                        string adjacentName = adjacentObject.name + GetRotations((int)adjacentObject.transform.eulerAngles.y);

                        if (!adjacents[Dir.Right].Contains(adjacentName))
                            adjacents[Dir.Right].Add(adjacentName);
                    }
                }

                adjacentObject = FindObjectAtPosition(sampleModule.transform.position + (Vector3.up * objectSize) + new Vector3(0, yOffset, 0));

                if (adjacentObject != null)
                {
                    if (adjacentObject.GetComponent<ModuleData>().symmetry == "x")
                    {
                        for (int i = 0; i <= 3; i++)
                        {
                            string adjacentName = adjacentObject.name + i;

                            if (!adjacents[Dir.Up].Contains(adjacentName))
                                adjacents[Dir.Up].Add(adjacentName);
                        }
                    }

                    else if (adjacentObject.GetComponent<ModuleData>().symmetry == "l")
                    {
                        string adjacentName = adjacentObject.name + GetRotations((int)adjacentObject.transform.eulerAngles.y);

                        if (!adjacents[Dir.Up].Contains(adjacentName))
                            adjacents[Dir.Up].Add(adjacentName);

                        adjacentName = adjacentObject.name + GetRotations((int)adjacentObject.transform.eulerAngles.y + 180);

                        if (!adjacents[Dir.Up].Contains(adjacentName))
                            adjacents[Dir.Up].Add(adjacentName);
                    }

                    else
                    {
                        string adjacentName = adjacentObject.name + GetRotations((int)adjacentObject.transform.eulerAngles.y);

                        if (!adjacents[Dir.Up].Contains(adjacentName))
                            adjacents[Dir.Up].Add(adjacentName);
                    }
                }

                adjacentObject = FindObjectAtPosition(sampleModule.transform.position + (Vector3.down * objectSize) + new Vector3(0, yOffset, 0));

                if (adjacentObject != null)
                {
                    if (adjacentObject.GetComponent<ModuleData>().symmetry == "x")
                    {
                        for (int i = 0; i <= 3; i++)
                        {
                            string adjacentName = adjacentObject.name + i;

                            if (!adjacents[Dir.Down].Contains(adjacentName))
                                adjacents[Dir.Down].Add(adjacentName);
                        }
                    }

                    else if (adjacentObject.GetComponent<ModuleData>().symmetry == "l")
                    {
                        string adjacentName = adjacentObject.name + GetRotations((int)adjacentObject.transform.eulerAngles.y);

                        if (!adjacents[Dir.Down].Contains(adjacentName))
                            adjacents[Dir.Down].Add(adjacentName);

                        adjacentName = adjacentObject.name + GetRotations((int)adjacentObject.transform.eulerAngles.y + 180);

                        if (!adjacents[Dir.Down].Contains(adjacentName))
                            adjacents[Dir.Down].Add(adjacentName);
                    }

                    else
                    {
                        string adjacentName = adjacentObject.name + GetRotations((int)adjacentObject.transform.eulerAngles.y);

                        if (!adjacents[Dir.Down].Contains(adjacentName))
                            adjacents[Dir.Down].Add(adjacentName);
                    }
                }
            }
        }

        return adjacents;
    }

    // Gets the object at a given position
    private GameObject FindObjectAtPosition(Vector3 position)
    {
        GameObject gameObject = null;
        Collider[] colliders = Physics.OverlapSphere(position, 0.1f);
        if (colliders.Length > 0)
            if (colliders[0].gameObject.CompareTag(tag))
                gameObject = colliders[0].gameObject;

        return gameObject;
    }

    private int GetRotations(int angle)
    {
        int rotations = 0;

        switch (angle)
        {
            case 0:
                rotations = 0;
                break;
            case 90:
                rotations = 1;
                break;
            case 180:
                rotations = 2;
                break;
            case 270:
                rotations = 3;
                break;
            default:
                break;
        }
        return rotations;
    }
}
