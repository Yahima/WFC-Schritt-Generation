using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

public class Module
{
    public bool collapsed;
    public string type;
    public List<Tuple<string, int>> typeData;
    public List<string> validTypes;
    public Dictionary<string, Dictionary<Dir, List<string>>> rules;
    public Vector3Int gridPosition;
    private readonly bool isEdge;
    private Vector3 factor;
    private Vector3 offset;
    public GameObject model;

    public Vector3Int errorCell;    //hat valids auf 0 gebracht

    public Module(Vector3Int position, List<Tuple<string, int>> types, bool isEdge, float factor, float offset, float offset2)
    {
        collapsed = false;
        gridPosition = position;
        typeData = types;
        validTypes = new List<string>();

        foreach (var data in typeData)
            validTypes.Add(data.Item1);

        this.isEdge = isEdge;
        this.factor = new Vector3(factor, factor, factor);
        this.offset = new Vector3(-offset, offset2, -offset);
        type = "";
        model = null;
    }

    public void Collapse()
    {
        System.Random random = new System.Random();

        List<string> weightedTypesList = new();
        int emptyProbability = 0;
        List<string> emptyTypesList = new();

        foreach (var type in validTypes)
        {
            int weight = typeData.FirstOrDefault(tuple => tuple.Item1 == type)?.Item2 ?? 1;

            if (type.Contains("empty"))
            {
                emptyProbability = weight;
                emptyTypesList.Add(type);
                continue;
            }
            else if (type.Contains("roof"))
            {
                weight = Math.Max(1, weight - Math.Abs(gridPosition.y - weight));

            }

            for (int i = 0; i < weight * 100; i++)
            {
                weightedTypesList.Add(type);
            }
        }

        if (emptyProbability == 100)
        {
            weightedTypesList.Clear();
            foreach (var emptyType in emptyTypesList)
                weightedTypesList.Add(emptyType);
        }
        else if (emptyProbability > 0 && weightedTypesList.Count > 0)
        {
            int maxI = weightedTypesList.Count / (100 - emptyProbability) * emptyProbability / emptyTypesList.Count;
            for (int i = 0; i < maxI; i++)
            {
                foreach(var emptyType in emptyTypesList)
                    weightedTypesList.Add(emptyType);
            }

        } else
        {
            foreach (var emptyType in emptyTypesList)
                weightedTypesList.Add(emptyType);
        }
        if (weightedTypesList.Count > 0)
        {
            type = weightedTypesList[random.Next(0, weightedTypesList.Count - 1)];
            collapsed = true;
        }
        //Debug.Log(type);
    }

    public void CollapseTo(string type)
    {
        this.type = type;
        collapsed = true;
    }

    public void CollapseToType(string type)
    {
        ResetModule();
        this.type = type;
        collapsed = true;
    }

    public bool CollapseOther(string type)
    {
        ResetModule();

        if (validTypes.Count > 1)
        {
            List<string> newTypes = validTypes;
            newTypes.Remove(type);

            System.Random random = new System.Random();
            this.type = newTypes[random.Next(0, newTypes.Count)];
            collapsed = true;
        }

        return collapsed;
    }

    public void ResetModule()
    {
        collapsed = false;
        if (model != null)
            GameObject.Destroy(model);
        validTypes = typeData.Select(tuple => tuple.Item1).ToList();
        type = "";

        model = null;
    }

    public void SetObject(GameObject obj)
    {
        int rotation = int.Parse(type[^1..]);
        float angle = 0;

        switch (rotation)
        {
            case 0:
                angle = 0;
                break;
            case 1:
                angle = 90;
                break;
            case 2:
                angle = 180; ;
                break;
            case 3:
                angle = 270;
                break;
            default:
                break;
        }
        model = GameObject.Instantiate(obj, Vector3.Scale(gridPosition, factor) + offset, Quaternion.Euler(new Vector3(0, angle, 0)));
        model.transform.localScale = factor;
    }

    public bool IsObjNull()
    {
        return (model == null);
    }

    public List<string> GetValidTypes()
    {
        return validTypes;
    }

    public void RemoveType(string type)
    {
        validTypes.Remove(type);
    }

    public string GetTileType()
    {
        return type;
    }

    public bool IsCollapsed()
    {
        return collapsed;
    }

    public Vector3Int GetGridPosition()
    {
        return gridPosition;
    }

    public void SetValidTypes(List<string> types)
    {
        validTypes = types;
    }

    public bool IsEdge()
    {
        return isEdge;
    }

    public List<Tuple<string, int>> GetModuleTypes()
    {
        return typeData;
    }

    public void SetObjectNull()
    {
        GameObject.Destroy(model);
    }
}
