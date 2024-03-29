﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BasicWorldSave {

    public World world;
    public ActionSelecterControllerSave actionSelecter;
    public MoneySave money;
    public DiplomacySave diplomacy;
    public ScenarioSave scenario;
    public List<StructureSave> structures = new List<StructureSave>();
	public List<string> Whitelist;

    public BasicWorldSave() { }

    public BasicWorldSave(GameObject go) {

        WorldController wc = go.GetComponent<WorldController>();

        world = wc.Map;
        money = new MoneySave(wc.money);
        actionSelecter = new ActionSelecterControllerSave(wc.actionSelecter);
        diplomacy = new DiplomacySave(wc.diplomacy);
        scenario = new ScenarioSave(wc.scenario);
		Whitelist = ResourcesDatabase.Whitelist;

        foreach (Transform t in wc.structures.transform) {

            GameObject str = t.gameObject;
            structures.Add(new StructureSave(str.gameObject));

        }

    }

}

[System.Serializable]
public class WorldProgressSave : BasicWorldSave {
    
    public CameraSave camera;
    public TimeSave time;
	public PopulationSave population;
    public ImmigrationSave immigration;
    public TradeSave trade;
	public ResearchSave research;
	
	public DictContainer<string, float> productivities;
	public DictContainer<string, float> automation;

	public DateTime saveDate;

	//notifications
	public List<Notification> Events;

	//structures
	public List<WorkplaceSave> workplaces = new List<WorkplaceSave>();
    public List<StorageBuildingSave> storagebuildings = new List<StorageBuildingSave>();
    public List<GeneratorSave> generators = new List<GeneratorSave>();
    public List<StableSave> stables = new List<StableSave>();
    public List<CropSave> crops = new List<CropSave>();
    public List<HouseSave> houses = new List<HouseSave>();
    public List<WTPSave> wtps = new List<WTPSave>();
    public List<CanalSave> canals = new List<CanalSave>();
    public List<FarmhouseSave> farmhouses = new List<FarmhouseSave>();
	public List<JobcentreSave> jobcentres = new List<JobcentreSave>();

	//walkers
	public List<AnimalSave> animals = new List<AnimalSave>();
    public List<WalkerSave> walkers = new List<WalkerSave>();

    public WorldProgressSave(GameObject go) {

        WorldController w = go.GetComponent<WorldController>();

        world = w.Map;

		productivities = new DictContainer<string, float>(ProductivityController.productivities);
		automation = new DictContainer<string, float>(ProductivityController.automationValue);

		saveDate = DateTime.Now;
		Debug.Log(saveDate.ToString("MM/dd/yyyy"));

		Events = w.notifications.Events;

		//SAVE IN-GAME STUFF
		time = new TimeSave(w.timeController);
		population = new PopulationSave(w.population);
        money = new MoneySave(w.money);
        camera = new CameraSave(w.cameraController);
        immigration = new ImmigrationSave(w.immigration);
        actionSelecter = new ActionSelecterControllerSave(w.actionSelecter);
        trade = new TradeSave(w.trade);
        diplomacy = new DiplomacySave(w.diplomacy);
        scenario = new ScenarioSave(w.scenario);
		research = new ResearchSave(w.research);

        //SAVE OBJECTS FROM PARENT
        //structures
        foreach(Transform t in w.structures.transform) {

            GameObject str = t.gameObject;

            if (str.GetComponent<StorageBuilding>() != null)
                storagebuildings.Add(new StorageBuildingSave(str));

            else if (str.GetComponent<Stable>() != null)
                stables.Add(new StableSave(str));

            else if (str.GetComponent<Crop>() != null)
                crops.Add(new CropSave(str));

            else if (str.GetComponent<Generator>() != null)
                generators.Add(new GeneratorSave(str));

            else if (str.GetComponent<WTP>() != null)
                wtps.Add(new WTPSave(str));

            else if (str.GetComponent<Canal>() != null)
                canals.Add(new CanalSave(str));

            else if (str.GetComponent<House>() != null)
                houses.Add(new HouseSave(str.gameObject));

            else if (str.GetComponent<Farmhouse>() != null)
                farmhouses.Add(new FarmhouseSave(str));

            else if (str.GetComponent<Workplace>() != null)
                workplaces.Add(new WorkplaceSave(str));

			else if (str.GetComponent<Jobcentre>() != null)
				jobcentres.Add(new JobcentreSave(str));

			else if (str.GetComponent<Structure>() != null)
                structures.Add(new StructureSave(str));

            else
                Debug.Log(str.name + " shouldn't be under structures");

        }

        //walkers
        foreach(Transform t in w.walkers.transform) {

            GameObject wlkr = t.gameObject;

            if (wlkr.GetComponent<Animal>() != null)
                animals.Add(new AnimalSave(wlkr));

            else if (wlkr.GetComponent<Walker>() != null)
                walkers.Add(new WalkerSave(wlkr));

            else
                Debug.Log(wlkr.name + " shouldn't be under walkers");
        }

    }
}
