﻿using UnityEngine;
using System.Collections;

public class TileModel : MonoBehaviour
{
	Hex hex;
	Material mat;
	public const int CLOUD_MODEL = 1;
	public const int GROUND_MODEL = 2;
	public int modelType;
	public bool shrinking;

	public void init (Hex h, int modelType)
	{
		hex = h;
		this.modelType = modelType;
		transform.parent = hex.transform;				
		transform.localPosition = new Vector3 (0, 0, 0);		
		name = "Hex Model";									

		mat = GetComponent<Renderer> ().material;
		mat.shader = Shader.Find ("Transparent/Diffuse");
		mat.mainTexture = Resources.Load<Texture2D> ("Textures/white_hexagon");	
		if (modelType == GROUND_MODEL) {
			mat.color = new Color (0.7f, 0.5f, 0.3f);	
			mat.renderQueue = RenderCoordinator.GROUND_RQ;
		}
		if (modelType == CLOUD_MODEL) {
			mat.color = new Color (0.9f, 0.9f, 0.9f, 0.85f);
			mat.renderQueue = RenderCoordinator.CLOUD_RQ;
		}
	}

	void Start(){
		shrinking = false;
	}

	public void shrink(){
		if (modelType == CLOUD_MODEL) {
			shrinking = true;
		}
	}

	void Update(){
		if (shrinking) {
			this.transform.localScale *= 0.95f;
			if(this.transform.localScale.x < 0.1)
			{
				Destroy(this.gameObject);
			}
		}
	}
}
