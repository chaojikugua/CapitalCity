﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Priority_Queue;

[System.Serializable]
public class StructureSave : ObjSave {

    public int sizex, sizey, days, weeks, months;
    public bool activeRandomWalker, activeSmartWalker;
    public float FireRisk, CollapseRisk, AmountStored;

    public StructureSave(GameObject go) : base(go) {

        Structure s = go.GetComponent<Structure>();

        sizex = s.Sizex;
        sizey = s.Sizey;
        days = s.Days;
        weeks = s.Weeks;
        months = s.Months;
        FireRisk = s.FireRisk;
        CollapseRisk = s.CollapseRisk;
		AmountStored = s.AmountStored;

		activeRandomWalker = s.ActiveRandomWalker;
        activeSmartWalker = s.ActiveSmartWalker;

    }
}


public class Structure : Obj {

    [Header("Structure")]

    public int collapseRiskMax;
    public int fireRiskMax;
    public bool canBeDemolished = true;
    public bool radiusActive;
	public bool autoRotate = true;
    public int[] desirability = new int[1];


	public int stockpile = 100;
	public float AmountStored { get; set; }

	public int Sizex { get; set; }
    public int Sizey { get; set; }
    public int Days { get; set; }
    public int Weeks { get; set; }
    public int Months { get; set; }

    public bool IsPreview { get; set; }

    public float FireRisk { get; set; }
    public float CollapseRisk { get; set; }

    //stuff for spawning random walkers
    public bool ActiveRandomWalker { get; set; }
    public bool ActiveSmartWalker { get; set; }
    public string RandomWalker;

	public override void Activate() {

        base.Activate();

        transform.parent = world.structures.transform;
        ActiveRandomWalker = false;

        collapseRiskMax = (int)(collapseRiskMax * Difficulty.GetModifier());
        fireRiskMax = (int)(fireRiskMax * Difficulty.GetModifier());
        DoDesirability();

		//if (GetComponent<BoxCollider>() == null)
		//	AddBoxCollider();

	}

    public override void Load(ObjSave o) {
        base.Load(o);

        StructureSave s = (StructureSave)o;

        Sizex = s.sizex;
        Sizey = s.sizey;
        Days = s.days;
        Weeks = s.weeks;
        Months = s.months;
        FireRisk = s.FireRisk;
        CollapseRisk = s.CollapseRisk;
		AmountStored = s.AmountStored;

		ActiveRandomWalker = s.activeRandomWalker;
        ActiveSmartWalker = s.activeSmartWalker;

        world.Map.RenameArea(name, X, Y, Sizex, Sizey);

        collapseRiskMax = (int)(collapseRiskMax * Difficulty.GetModifier());
        fireRiskMax = (int)(fireRiskMax * Difficulty.GetModifier());

		//if (GetComponent<BoxCollider>() == null)
		//	AddBoxCollider();

	}

	void AddBoxCollider() {
		
		BoxCollider bc = gameObject.AddComponent<BoxCollider>();
		bc.size = new Vector3(Sizex, .5f, Sizey);
		bc.center = new Vector3(0, .25f, 0);

	}

	private void Update() {

		if(Settings.oldGraphics)
			UpdateRotation();

        TimeDelta += Time.deltaTime;
        if (time == null)
            return;
        if (TimeDelta >= TimeController.DayTime) {

            TimeDelta = 0;
            Days++;
            DoEveryDay();

            if (Days >= TimeController.WeekTime) {

                Days = 0;
                Weeks++;
                DoEveryWeek();

            }

            //convert weeks to month
            if (Weeks >= TimeController.MonthTime) {

                Weeks = 0;
                Months++;
                DoEveryMonth();

            }

            if(Months >= TimeController.MonthTime * TimeController.SeasonTime) {

                Months = 0;
                DoEveryYear();

            }

        }

    }

    public void OnDestroy() {

        UponDestruction();

    }

    public virtual void UponDestruction() {

        if (IsPreview)
            return;

        if (world != null) {
            if (!world.PlaceOnRoads) {

                for (int a = X; a < X + Sizex; a++)
                    for (int b = Y; b < Y + Sizey; b++) {
                        world.Map.roads[a, b] = 0;
                        world.Map.cleanliness[a, b] = 0;
                    }

            }

            else
                world.PlaceOnRoads = false;
		}

		//remove from immigration list upon destruction
		if (immigration.Requests.Contains(this))
			immigration.Requests.Remove(this);

	}

    public virtual void DoEveryDay() {

        //Debug.Log("does this every day");
        

    }

    public virtual void DoEveryWeek() {

        //Debug.Log("does this every week");
        
    }

    public virtual void DoEveryMonth() {

        //Debug.Log("does this every month");

        FireCounter();
        CollapseCounter();
        IncreaseCollapseRisk();
        IncreaseFireRisk();

    }

    public virtual void DoEveryYear() {



    }

    public List<Node> GetAdjRoadTiles() {

        List<Node> tiles = new List<Node>();
        World map = world.Map;

        //this demon magic works, don't think about it too much but it just checks surrounding tiles for roads
        for (int a = X; a < X + Sizex; a++) {
            if (map.OutOfBounds(a, Y - 1))
                continue;
            if (map.IsUnblockedRoadAt(a, Y - 1))
                tiles.Add(new Node(a, Y - 1));
        }

        for (int b = Y; b < Y + Sizey; b++) {
            if (map.OutOfBounds(X + Sizex, b))
                continue;
            if (map.IsUnblockedRoadAt(X + Sizex, b))
                tiles.Add(new Node(X + Sizex, b));
        }

        for (int a = X; a < X + Sizex; a++) {
            if (map.OutOfBounds(a, Y + Sizey))
                continue;
            if (map.IsUnblockedRoadAt(a, Y + Sizey))
                tiles.Add(new Node(a, Y + Sizey));
        }

        for (int b = Y; b < Y + Sizey; b++) {
            if (map.OutOfBounds(X - 1, b))
                continue;
            if (map.IsUnblockedRoadAt(X - 1, b))
                tiles.Add(new Node(X - 1, b));
        }

        if (map.IsUnblockedRoadAt(X, Y) && Sizex == 1 && Sizey == 1)
            tiles.Add(new Node(X, Y));

        return tiles;
    }

    public List<Node> GetAdjBareGroundTiles() {

        List<Node> tiles = new List<Node>();
        World map = world.Map;

        for (int a = X; a < X + Sizex; a++) {
            if (map.OutOfBounds(a, Y - 1))
                continue;
            if (map.terrain[a, Y - 1] != (int)Terrain.Water && string.IsNullOrEmpty(map.structures[a, Y - 1]))
                tiles.Add(new Node(a, Y - 1));
        }

        for (int b = Y; b < Y + Sizey; b++) {
            if (map.OutOfBounds(X + Sizex, b))
                continue;
            if (map.terrain[X + Sizex, b] != (int)Terrain.Water && string.IsNullOrEmpty(map.structures[X + Sizex, b]))
                tiles.Add(new Node(X + Sizex, b));
        }

        for (int a = X; a < X + Sizex; a++) {
            if (map.OutOfBounds(a, Y + Sizey))
                continue;
            if (map.terrain[a, Y + Sizey] != (int)Terrain.Water && string.IsNullOrEmpty(map.structures[a, Y + Sizey]))
                tiles.Add(new Node(a, Y + Sizey));
        }

        for (int b = Y; b < Y + Sizey; b++) {
            if (map.OutOfBounds(X - 1, b))
                continue;
            if (map.terrain[X - 1, b] != (int)Terrain.Water && string.IsNullOrEmpty(map.structures[X - 1, b]))
                tiles.Add(new Node(X - 1, b));
        }

        return tiles;
    }

    public List<Node> GetAdjTiles() {

        List<Node> tiles = new List<Node>();
        World map = world.Map;

        for (int a = X; a < X + Sizex; a++) {
            if (map.OutOfBounds(a, Y - 1))
                continue;
            if (map.terrain[a, Y - 1] != (int)Terrain.Water)
                tiles.Add(new Node(a, Y - 1));
        }

        for (int b = Y; b < Y + Sizey; b++) {
            if (map.OutOfBounds(X + Sizex, b))
                continue;
            if (map.terrain[X + Sizex, b] != (int)Terrain.Water)
                tiles.Add(new Node(X + Sizex, b));
        }

        for (int a = X; a < X + Sizex; a++) {
            if (map.OutOfBounds(a, Y + Sizey))
                continue;
            if (map.terrain[a, Y + Sizey] != (int)Terrain.Water)
                tiles.Add(new Node(a, Y + Sizey));
        }

        for (int b = Y; b < Y + Sizey; b++) {
            if (map.OutOfBounds(X - 1, b))
                continue;
            if (map.terrain[X - 1, b] != (int)Terrain.Water)
                tiles.Add(new Node(X - 1, b));
        }

        return tiles;
    }

    public List<Node> GetAdjWaterTiles() {

        List<Node> tiles = new List<Node>();
        World map = world.Map;

        for (int a = X; a < X + Sizex; a++) {
            if (map.OutOfBounds(a, Y - 1))
                continue;
            if (map.terrain[a, Y - 1] == (int)Terrain.Water && string.IsNullOrEmpty(map.structures[a, Y - 1]))
                tiles.Add(new Node(a, Y - 1));
        }

        for (int b = Y; b < Y + Sizey; b++) {
            if (map.OutOfBounds(X + Sizex, b))
                continue;
            if (map.terrain[X + Sizex, b] == (int)Terrain.Water && string.IsNullOrEmpty(map.structures[X + Sizex, b]))
                tiles.Add(new Node(X + Sizex, b));
        }

        for (int a = X; a < X + Sizex; a++) {
            if (map.OutOfBounds(a, Y + Sizey))
                continue;
            if (map.terrain[a, Y + Sizey] == (int)Terrain.Water && string.IsNullOrEmpty(map.structures[a, Y + Sizey]))
                tiles.Add(new Node(a, Y + Sizey));
        }

        for (int b = Y; b < Y + Sizey; b++) {
            if (map.OutOfBounds(X - 1, b))
                continue;
            if (map.terrain[X - 1, b] == (int)Terrain.Water && string.IsNullOrEmpty(map.structures[X - 1, b]))
                tiles.Add(new Node(X - 1, b));
        }

        return tiles;
    }

    public void SpawnRandomWalker() {

		if (string.IsNullOrEmpty(RandomWalker))
			return;
		
		List<Node> entrances = GetAdjRoadTiles();

		//proceed only if there are available roads
		if (entrances.Count == 0)
			return;

		GameObject go = world.SpawnObject("Walkers/RandomWalkers", RandomWalker, entrances[0]);
		Walker w = go.GetComponent<Walker>();
		w.world = world;
		w.Origin = this;
		w.Activate();
		
    }

    public void FireCounter() {

        int roll = Random.Range(1, 100);

        if (FireRisk == 0)
            roll = 100;

        if (roll <= FireRisk)
            CatchOnFire();

    }

    void IncreaseFireRisk() {

        if (FireRisk < fireRiskMax)
            FireRisk += Difficulty.GetModifier();

        if (FireRisk > fireRiskMax)
            FireRisk = fireRiskMax;

    }


    void CollapseCounter() {

        int roll = Random.Range(1, 100);

        if (CollapseRisk == 0)
            roll = 100;

		if (roll <= CollapseRisk)
			Collapse();

    }

    void IncreaseCollapseRisk() {

        if (CollapseRisk < collapseRiskMax)
            CollapseRisk += Difficulty.GetModifier();

        if (CollapseRisk > collapseRiskMax)
            CollapseRisk = collapseRiskMax;

    }

	public void CatchOnFire() {

		world.CatchOnFire(X, Y, Sizex, Sizey);

	}

	public void Collapse() {

		world.CollapseStructure(X, Y, Sizex, Sizey);

	}

	public void TurnToRubble() {

		world.TurnToRubble(X, Y, Sizex, Sizey);

	}
    
    public virtual void ReceiveItem(ItemOrder io) { Debug.LogError(name + " received an item (" + io + ") but doesn't know what to do with it"); }

	public virtual void RemoveItem(ItemOrder io) { Debug.LogError(name + " was told to remove an item (" + io + ") but does not know how"); }

	public SimplePriorityQueue<StorageBuilding> FindStorageBuildingToAccept(ItemOrder io) {

		int num = io.amount;
		int item = io.item;
		ItemType type = io.type;

		GameObject[] objs = GameObject.FindGameObjectsWithTag("StorageBuilding");
        SimplePriorityQueue<StorageBuilding> queue = new SimplePriorityQueue<StorageBuilding>();

        if (objs.Length == 0)
            return queue;


        foreach (GameObject go in objs) {

            StorageBuilding strg = go.GetComponent<StorageBuilding>();

            //if null, continue
            if (strg == null)
                continue;

            //only add to list if it stores this type
            if (strg.typeStored != type)
                continue;

            if (!strg.Operational)
                continue;

            //only add to list if it has an entrance
            List<Node> entrancesHere = GetAdjRoadTiles();
            List<Node> entrancesThere = strg.GetAdjRoadTiles();
            if (entrancesHere.Count == 0 || entrancesThere.Count == 0)
                continue;

            //only add to list if it can accept amount
            if (!strg.CanAcceptAmount(num, item))
                continue;

            float distance = entrancesHere[0].DistanceTo(entrancesThere[0]);

            queue.Enqueue(strg, distance);

        }

        return queue;

    }

    public SimplePriorityQueue<StorageBuilding> FindStorageBuildingThatHas(ItemOrder io) {

		int num = io.amount;
		int item = io.item;
		ItemType type = io.type;

        GameObject[] objs = GameObject.FindGameObjectsWithTag("StorageBuilding");
        SimplePriorityQueue<StorageBuilding> queue = new SimplePriorityQueue<StorageBuilding>();

        if (objs.Length == 0)
            return queue;
		

        int smallestAmountOfItem = 0;

        foreach (GameObject go in objs) {

            StorageBuilding strg = go.GetComponent<StorageBuilding>();
            if (this is StorageBuilding)
                if (strg == (StorageBuilding)this)
                    continue;

            //if storage building does not store that type of item, continue
            if (strg.typeStored != type)
                continue;

            if (!strg.Operational)
                continue;

            //if storage building or this structure have no entrances, continue
            List<Node> entrancesHere = GetAdjRoadTiles();
            List<Node> entrancesThere = strg.GetAdjRoadTiles();
            if (entrancesHere.Count == 0 || entrancesThere.Count == 0)
                continue;

            if (strg.Inventory[item] == 0)
                continue;

            //if has less than the needed amount of Item and less than other discovered inventories, continue
            if (strg.Inventory[item] < num && strg.Inventory[item] < smallestAmountOfItem)
                continue;

            smallestAmountOfItem = strg.Inventory[item];
            float distance = entrancesHere[0].DistanceTo(entrancesThere[0]);

            queue.Enqueue(strg, distance);

        }
		
        return queue;

    }

    public Carryer SpawnGetter(ItemOrder io) {

		List<Node> entrances = GetAdjRoadTiles();
		if (entrances.Count == 0)
			return null;
		Node start = entrances[0];

		SimplePriorityQueue<StorageBuilding> queue = FindStorageBuildingThatHas(io);

		for (int i = 0; queue.Count > 0 && i < 5 && !ActiveSmartWalker; i++) {

			Structure strg = queue.Dequeue();
			List<Node> exits = strg.GetAdjRoadTiles();
			if (exits.Count == 0)
				continue;

			Queue<Node> path = pathfinder.FindPath(start, exits, "GetterCart");
			if (path.Count == 0)
				continue;

			GameObject go = world.SpawnObject("Walkers", "GetterCart", start);

			Carryer c = go.GetComponent<Carryer>();
			c.world = world;
			c.Order = io;
			c.Origin = this;
			c.Destination = strg;
			c.Activate();
			c.SetPath(path);
			return c;

		}

		return null;

	}

    public Carryer SpawnGiver(ItemOrder io) {

		List<Node> entrances = GetAdjRoadTiles();
		if (entrances.Count == 0)
			return null;
		Node start = entrances[0];

		SimplePriorityQueue<StorageBuilding> queue = FindStorageBuildingToAccept(io);

		for (int i = 0; queue.Count > 0 && i < 5 && !ActiveSmartWalker; i++) {

			Structure strg = queue.Dequeue();
			List<Node> exits = strg.GetAdjRoadTiles();
			if (exits.Count == 0)
				continue;

			Queue<Node> path = pathfinder.FindPath(start, exits, "GiverCart");
			if (path.Count == 0)
				continue;

			GameObject go = world.SpawnObject("Walkers", "GiverCart", start);

			Carryer c = go.GetComponent<Carryer>();
			c.world = world;
			c.Order = io;
			c.Origin = this;
			c.Destination = strg;
			c.Activate();
			c.SetPath(path);
			return c;

		}

		return null;

	}

    public void RequestImmigrant() {

        immigration.Requests.Add(this);

    }

	public virtual void ReceiveImmigrant(Adult p) {

		Debug.LogError(name + " received prole " + p + " and can't do anything with it");

	}
    
    public void VisitBuildings() {

        for (int a = X - radiusOfInfluence; a < X + Sizex + radiusOfInfluence; a++)
            for (int b = Y - radiusOfInfluence; b < Y + Sizey + radiusOfInfluence; b++)
                if (world.Map.IsBuildingAt(a, b) && !world.Map.IsRoadAt(a, b)) {

                    if (world.Map.GetBuildingNameAt(a, b) == name)
                        continue;
                    VisitBuilding(a, b);

                }

    }

    void DoDesirability() {

        int radius = desirability.Length;
        if (radius == 0)
            return;
        for (int a = X - radius; a < X + Sizex + radius; a++)
            for (int b = Y - radius; b < Y + Sizey + radius; b++) {

                if (world.Map.OutOfBounds(a, b))
                    continue;

                if (world.Map.GetBuildingNameAt(a, b) == this.name)
                    continue;

                //find the shortest distance, along X or Y. this affects the desirability of the building
                int dist_a = a > X ? a - X - Sizex + 1 : X - a;
                int dist_b = b > Y ? b - Y - Sizey + 1 : Y - b;
                int distance = dist_a > dist_b ? dist_a : dist_b;
                
                world.Map.desirability[a, b] += desirability[distance-1];

            }

    }

    public void DeleteDesirability() {

        int radius = desirability.Length;
        if (radius == 0)
            return;
        for (int a = X - radius; a < X + Sizex + radius; a++)
            for (int b = Y - radius; b < Y + Sizey + radius; b++) {

                if (world.Map.OutOfBounds(a, b))
                    continue;

                if (world.Map.GetBuildingNameAt(a, b) == this.name)
                    continue;

                //find the shortest distance, along X or Y. this affects the desirability of the building
                int dist_a = a > X ? a - X - Sizex + 1 : X - a;
                int dist_b = b > Y ? b - Y - Sizey + 1 : Y - b;
                int distance = dist_a > dist_b ? dist_a : dist_b;
                
                world.Map.desirability[a, b] -= desirability[distance - 1];

            }

    }

    public int LocalDesirability { get {

            int d = world.Map.desirability[X, Y];
            for (int a = X + 1; a < Sizex + X; a++)
                for (int b = Y + 1; b < Sizey + Y; b++)
                    if (world.Map.desirability[a, b] > d)
                        d = world.Map.desirability[a, b];
            return d;

        }

    }

    public virtual float GetActualProductivity(string item) {

		Debug.LogError(this + " called this function but cannot do anything with it.");
		return -1;

	}

	public virtual float GetAutomationValue(string item) {

		Debug.Log(this + " called this function but cannot do anything with it.");
		return 0;

	}
	public void JoinProductivityList(string item) {

		ProductivityController.AddStructureToList(this, item);

	}

	public void LeaveProductivityList(string item) {

		ProductivityController.RemoveStructureFromList(this, item);

	}

	void UpdateRotation() {

		if (Sizex != Sizey)
			return;

		if (!autoRotate)
			return;

		Vector3 normalScale = new Vector3(1, 1, 1);
		Vector3 invertScale = new Vector3(-1, 1, 1);

		Vector3 normalRotation = world.cameraController.transform.transform.eulerAngles;
		Vector3 invertRotation = normalRotation;
		invertRotation.y += 90;

		float degrees = normalRotation.y;

		bool invert = (degrees + 90) % 180 == 0;

		transform.localScale = invert ? invertScale : normalScale;
		transform.localRotation = Quaternion.Euler(invert ? invertRotation : normalRotation);

	}

}
