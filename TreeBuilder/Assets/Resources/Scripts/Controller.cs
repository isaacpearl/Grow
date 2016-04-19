﻿using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

public class Controller : MonoBehaviour {


	public static float ORB_BASE_PROB = .7f;
	public static float WATER_BASE_PROB = 50f;

    public int weather = 0; //0 is default, 1 is sunny, 2 is rainy

	public bool initialized;

	public Hex mouseOver;
	public bool placing;
	public Hex placingFrom; 
	//public Hex airRoot;
	//public Hex groundRoot;
	public Hex root;
	public Branch airRootBranch;
	public Branch groundRootBranch;
	public Branch currentBranch;
	public int treeHeight;
	public int currentCost;
    GameObject hexFolder;

	List<Cloud> cloudList;
	public const float CLOUD_MAXLENGTH = 11f;
	public const float CLOUD_TIME = Cloud.STEP_INTERVAL * CLOUD_MAXLENGTH;
	public float timeSinceLastCloud;
	//List<GameObject> hexes;

    GameObject lightFolder;
    List<Light> lights;

	GameObject waterFolder;

	public const int COST_LIN = 1;
	public const float COST_EX = 1.1f;

    public int sunEnergy = 10;
    string sunDisplay;

	public int waterEnergy = 10;
	string waterDisplay;

	public AudioManager audioM;
	public GameObject audioObject;

    public const int WORLD_HEIGHT = 80; // the number of vertical tiles
	public const int WORLD_WIDTH = 50;   // number of horizontal tiles 

	public Hex[,] hexArray;

    private float clock = 0;
    private float currentTime = 0;

    void Start () {
		initialized = false;
		hexFolder = new GameObject();
		populateTiles ();
		placing = false;
		treeHeight = 0;

		addWaterToWorld ();

		audioObject = new GameObject ();
		audioObject.name = "audio manager";
		audioObject.AddComponent<AudioSource>();
		audioM = audioObject.AddComponent<AudioManager> ();
		audioM.init (this);

        //Folder to store all hexes

        hexFolder.name = "Hexes";
		//hexes = new List<GameObject>();

        //SunDrops
        lightFolder = new GameObject();
        lightFolder.name = "Sundrops";
        lights = new List<Light>();
        InvokeRepeating("sunGenerator", 0f, 0.5f);

		//Water
		waterFolder = new GameObject();
		waterFolder.name = "Waterdrops";

		cloudList = new List<Cloud> ();
		timeSinceLastCloud = 0f;

		initialized = true;

    }
	
	// Update is called once per frame
	void Update () {

		//CAMERA STUFF
		if (Input.GetButton("Vertical")){
			if (!(Camera.main.orthographicSize < 0.5f && Input.GetAxis("Vertical") < 0)) {
				Camera.main.orthographicSize += 0.1f * Input.GetAxis ("Vertical");
			}
		}
		if (Input.mousePosition.x > Camera.main.pixelWidth * 9f / 10f && Input.mousePosition.x < Camera.main.pixelWidth) {
			Camera.main.transform.Translate(new Vector3(0.1f, 0, 0));
		}
		if (Input.mousePosition.x < Camera.main.pixelWidth / 10f && Input.mousePosition.x > 0) {
			Camera.main.transform.Translate (new Vector3 (-0.1f, 0, 0));
		}
		if (Input.mousePosition.y > Camera.main.pixelHeight * 9f / 10f && Input.mousePosition.y < Camera.main.pixelHeight) {
			Camera.main.transform.Translate (new Vector3 (0, 0.1f, 0));
		}
		if (Input.mousePosition.y < Camera.main.pixelHeight / 10f && Input.mousePosition.y > 0) {
			Camera.main.transform.Translate (new Vector3 (0, -0.1f, 0));
		}

		//BRANCH-DRAWING STUFF
		if (Input.GetMouseButtonDown (0)) {
			Vector3 worldPos = Camera.main.ScreenToWorldPoint (Input.mousePosition);
			float mouseX = worldPos.x;
			float mouseY = worldPos.y;
			worldPos.z = 0;

			if (mouseOver != null) {
				print (mouseOver.coordX + " " + mouseOver.coordY);
			}

			if (!placing) {
				if (mouseOver != null && checkStart (mouseOver)) {
					placingFrom = mouseOver;
					placing = true;

					GameObject branchObject = new GameObject ();
					branchObject.AddComponent<LineRenderer> ();
					Branch branch = branchObject.AddComponent<Branch> ();
					branch.init (placingFrom, this);
					currentBranch = branch;

					currentCost = findCost (placingFrom);
					print ("the cost of that would be " + currentCost);
				}
			}
		}

		if (Input.GetMouseButtonUp(0)){
			if (placing){
				Hex end = mouseOver;
				if (end != null && checkFinish (placingFrom, end)) {
					placingFrom.addBranch (end, currentBranch);
					currentBranch.confirm (end);
					end.updateWidth ();
					if (end.type == Hex.GROUND) {
						sunEnergy -= currentCost;
					} else {
						waterEnergy -= currentCost;
					}
				} else {
					Destroy (currentBranch.gameObject);
				}
				currentBranch = null;
				placing = false;
				currentCost = 0;
			}
		}

        sunDisplay = "Sunlight: " + sunEnergy;
		waterDisplay = "Water: " + waterEnergy;

		//cloud stuff
		timeSinceLastCloud += Time.deltaTime;
		if (timeSinceLastCloud >= CLOUD_TIME) {
			if (UnityEngine.Random.Range (0, 3) == 0) {
				createCloud ();
				timeSinceLastCloud = 0f;
			} else {
				timeSinceLastCloud -= 3f;
			}
		}

	}

	void populateTiles(){
		hexArray = new Hex[WORLD_WIDTH, WORLD_HEIGHT];
		for (int i = -WORLD_WIDTH/2; i < WORLD_WIDTH/2; i++) {
			for (int j = -WORLD_HEIGHT/2; j < WORLD_HEIGHT/2; j++) {
				hexArray[i + WORLD_WIDTH/2, j + WORLD_HEIGHT/2] = placeHex (i, j);
			}
		}
		initializeRoots ();
	}

	Hex placeHex(int x, int y){

		int cartX = x;
		int cartY = y;

		float actX = (float)cartX * 0.75f;
		float actY = (float)cartY * Mathf.Sqrt(3)/2f;
		if (x % 2 != 0) {
			actY -= Mathf.Sqrt(3)/4f;
		}
		
		GameObject hexObject = new GameObject ();
		Hex hex = hexObject.AddComponent<Hex> ();
		hex.transform.position = new Vector3 (actX, actY, 0);
		hex.init (x, y, actX, actY, this);

        //Tried to put all the hexes in a folder, didnt work for some reason

        //hexes.Add(hexObject);
        hexObject.name = "Hex";
        hexObject.transform.parent = hexFolder.transform;

        return hex;
	}

	bool checkStart(Hex start){
		return (start.occupied);
	}
		

	bool checkFinish(Hex start, Hex end){
		if (start.type == Hex.AIR) {
			return (!end.occupied &&
			checkAdjacent (start, end) &&
			start.type == end.type &&
			waterEnergy >= currentCost);
		} else {
			return (!end.occupied &&
				checkAdjacent (start, end) &&
				start.type == end.type &&
				sunEnergy >= currentCost);
		}
	}

	bool checkAdjacent(Hex start, Hex end){
		float dist = Mathf.Sqrt (Mathf.Pow (end.realX - start.realX, 2) + Mathf.Pow (end.realY - start.realY, 2));
		return (dist < 1f);
	}
		
	public Hex hexAt(int coordX, int coordY){
		return hexArray[coordX + WORLD_WIDTH / 2, coordY + WORLD_HEIGHT / 2];
	}
	
    private int calculateType(Hex h)
    {
        float wProb = getWaterProb(h);
        float oProb = getOrbProb(h);
        float r = UnityEngine.Random.Range(5f, 100f);
        if (wProb > r)
        {
            return 2;
        }
        r = UnityEngine.Random.Range(5f, 100f);
        if (oProb > r)
        {
            return 3;
        }
        else {
            return 1;
        }
    }

    private float getOrbProb(Hex h)
    {
		return ORB_BASE_PROB * Vector3.Distance (new Vector3 (0, 0, 0), h.transform.position);
    }

    private float getWaterProb(Hex h)
    {
		float y = h.transform.position.y;
        if (y >= -1)
        {
            return 0;
        }
		return WATER_BASE_PROB * ((y+WORLD_HEIGHT/2) / WORLD_HEIGHT)-1;
    }

    void sunGenerator()
    {
        float x = UnityEngine.Random.Range(-WORLD_WIDTH / 2, WORLD_WIDTH / 2);
        createSun(0.75f * x, (WORLD_HEIGHT/2)*Mathf.Sqrt(3) / 2f);
    }


	void addWaterToWorld(){
		Hex h;
 		for (int i = -WORLD_WIDTH/2; i < WORLD_WIDTH/2; i++) {
			for (int j = -WORLD_HEIGHT/2; j < WORLD_HEIGHT/2; j++) {
				h = hexArray [i + WORLD_WIDTH / 2, j + WORLD_HEIGHT / 2];
				int type = calculateType (h);
				if (type == 2) {
					createWater (h);
				} else if (type == 3) {
					createOrb (h);
				}
			}
		}
	}

    void addSunEnergy(float scale)
    {
        int value = (int)(scale * 10);
        sunEnergy += value;
    }

	void addWaterEnergy(int amount){
		waterEnergy += amount;
	}

	public int findCost(Hex h){
		return ((int)Mathf.Pow (h.findHeight (), COST_EX) * COST_LIN);
	}
    
	void OnGUI () {
        
        GUI.contentColor = Color.red;
        GUI.Label(new Rect(Screen.width -100, Screen.height/2, 100, 20), sunDisplay);
		GUI.Label(new Rect(Screen.width -100, Screen.height/2-40, 100, 20), waterDisplay);

    }

	void createCloud(){

		int height = UnityEngine.Random.Range (WORLD_HEIGHT / 4, WORLD_HEIGHT / 2 - 1);
		int length = UnityEngine.Random.Range (3, (int)CLOUD_MAXLENGTH);

		var cloudObject = new GameObject ();
		cloudList.Add (cloudObject.AddComponent<Cloud>());
		cloudList [cloudList.Count - 1].init (this, height, length);
	}

	void createSun(float x, float y)
	{
		GameObject lightObject = new GameObject();
		Light newLight = lightObject.AddComponent<Light>();
		newLight.init(x, y, this);

		BoxCollider2D box = lightObject.AddComponent<BoxCollider2D>();         //Colliders
		box.size = new Vector2(0.5f, 0.5f);
		lightObject.SetActive(true);
		box.isTrigger = true;

		Rigidbody2D rig = lightObject.AddComponent<Rigidbody2D>();
		rig.isKinematic = true;

		lights.Add(newLight);
		newLight.name = "Sundrop " + lights.Count;
		newLight.transform.parent = lightFolder.transform;
	}

	void createWater (Hex h){
		GameObject waterObject = new GameObject ();
		Water newWater = waterObject.AddComponent<Water> ();
		newWater.init (h, this);
		/*
		BoxCollider2D box = waterObject.AddComponent<BoxCollider2D> ();
		box.size = new Vector2 (0.5f, 0.5f);
		//waterObject.setActive (true);
		box.isTrigger = true;

		Rigidbody2D rig = waterObject.AddComponent<Rigidbody2D> ();
		rig.isKinematic = true;
		*/
		newWater.name = "Waterdrop";
		//newWater.transform.parent = waterFolder.transform;
	}

	void createOrb (Hex h){
		GameObject orbObject = new GameObject ();
		Orb newOrb = orbObject.AddComponent<Orb> ();
		newOrb.init(h, UnityEngine.Random.Range(0, 4), this);

		newOrb.name = "Orb" + newOrb.type;
	}

    public void resetWeather()
    {
        StartCoroutine(Weatherwait());
    }

    IEnumerator Weatherwait()
    {
        print("Starting");
        yield return new WaitForSeconds(10);
        weather = 0;
        print("Stopping");
        
    }

    private void initializeRoots(){
		GameObject rootHexObject = new GameObject ();
		root = rootHexObject.AddComponent<Hex> ();
		root.transform.position = new Vector3 (0, -Mathf.Sqrt (3) / 4f, 0);
		root.rootInit (0, Mathf.Sqrt (3) / 4f, this);

		GameObject branchObject = new GameObject ();
		branchObject.AddComponent<LineRenderer> ();
		airRootBranch = branchObject.AddComponent<Branch> ();
		airRootBranch.init (root, this);
		root.addBranch (hexArray [WORLD_WIDTH / 2, WORLD_HEIGHT / 2], airRootBranch);
		airRootBranch.confirm (hexArray [WORLD_WIDTH / 2, WORLD_HEIGHT / 2]);

		GameObject branchObject2 = new GameObject ();
		branchObject2.AddComponent<LineRenderer> ();
		groundRootBranch = branchObject2.AddComponent<Branch> ();
		groundRootBranch.init (root, this);
		root.addBranch (hexArray [WORLD_WIDTH / 2, WORLD_HEIGHT / 2 - 1], groundRootBranch);
		groundRootBranch.confirm (hexArray [WORLD_WIDTH / 2, WORLD_HEIGHT / 2 - 1]);
	}

}

