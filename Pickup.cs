using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pickup : MonoBehaviour
{
    public int typeint; // to randomize the pickup
    private GameController gameSetUp;
    public Transform mySpawnPos;
    [SerializeField]
    private float destroyTimer = 10;
    private GameObject explosion; // to show up when it gets shot
    // fade color so it dissapears?
    // dont spawn on player

    void Start()
    {
        gameSetUp = GameObject.Find("GameManager").GetComponent<GameController>();
        //AssignType(Random.Range(1, 16));
        //myPart = transform.parent.transform.GetChild(0).GetComponent<ParticleSystem>();
        explosion = (GameObject)Resources.Load("SmallExplosion"); // set explosion prefab
    }
    void Update()
    {
        if (destroyTimer > 0)
        {
            destroyTimer -= Time.deltaTime;
        }
        else if (destroyTimer <= 0)
        {
            ReAddTransform();
        }
    }       
    public void AssignType(int num) // probability spawner
    {
        if (num > 0 && num <= 5) // if 1 - 5 spawn +2
        {
            typeint = 1;
            transform.GetChild(0).gameObject.SetActive(true);
        }
        else if(num >= 6 && num <= 8) // 6-8 spawn +3
        {
            typeint = 2;
            transform.GetChild(1).gameObject.SetActive(true);
        }
        else if(num >= 9 && num <= 13) // 9-13
        {
            typeint = 3;
            transform.GetChild(2).gameObject.SetActive(true);
        }
        else if(num >= 14) // 14 - 15
        {
            typeint = 4;
            transform.GetChild(3).gameObject.SetActive(true);
        }
    }
    public void ReAddTransform()
    {
        if(mySpawnPos != null) // for debugging without adding to list
            gameSetUp.availablePos.Add(mySpawnPos);
        for(int i = 0; i < 4; i++)
        {
            transform.GetChild(i).gameObject.SetActive(false); // turn off prvious picup model
        }

        gameObject.SetActive(false); // deactivate for object pooler
    }
    /// <summary>
    /// Turns off the object and plays explosion before destroying self
    /// </summary>
    public void ArrowDestroy()
    {
        for (int i = 0; i < 4; i++)
        {
            transform.GetChild(i).gameObject.SetActive(false); // turn off prvious picup model
        }
        gameObject.SetActive(false); // deactivate for object pooler
        GameObject explo = Instantiate(explosion, transform.GetChild(0).position, transform.rotation) as GameObject; // create explosion w/sound
        Destroy(explo, 1.25f); // destroy after 1 seconds
    }
}