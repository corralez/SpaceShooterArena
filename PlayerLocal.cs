using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using System;
using Rewired;

public class PlayerLocal : MonoBehaviour
{
    // REWIRED
    public int playerId; // player ID for REWIRED
    private Player player; // access to REWIRED controls
    private Vector3 moveVector;

    GameController controller;
    // UI VARIABLES
    public float moveSpeed = 7f;
    private int lives = 3; // how many lives you have
    public Text livesText;
    public int health = 100; // bar instead of int?
    public Slider healthSlider;
    public Image healthbarFill; 
    public Text arrowNum;
    private Rigidbody rbody;
    public int bullets = 5; // the number of arrows a player has
    private int myLayer; // this ships layer for switching back
    // team variables
    public int myTeam; // 0 = team1, 1 = team2
    // invincible
    private bool invincible = false;
    private float invincibleTimer = 2f;
    // STUNNED Variables
    private bool stunned = false;
    private float stunTimer = 2.5f; // time you are stunned
    private GameObject bubble;
    // SHIELD VARIABLES
    private GameObject shield; // to find the object itself
    private bool shieldOn = false; // if no damage taken, shield or respawn
    private int shieldHealth; // how many hits before it dissapears
    // CONTROLLER VARIABLES
    public int joystickNumber;
    public string joystickAString;
    // DASH, ARROW FORCE, FELL OUT, SPAWN
    private GameObject arrow; // fake arrow
    private readonly float maxDashAmount = 0.5f; // max how long you can dash, pickup can increase
    private float curDashAmount; // current amount out of max
    private bool charging = false; // to tell when you are holding trigger
    private readonly float chargeSpeed = 2; // how fast the arrow charges
    private float arrowSize = 1f; // size of fake arrow
    private readonly float force = 750; // how much initial force we push the player away from arrow, is mutiplied in the code by arrow size
    private bool fellOut = false; // if you fell out so you can respawn
    private float respawnTimer = 4; // grace period before you have to respawn
    public Transform[] playerSpawnNum; // location of all the player spawns
    
    ObjectPooler objectPooler;
    // particles and colors
    private Renderer rend;
    [SerializeField]
    private Color shipColor;
    private ParticleSystem charged;
    private ParticleSystem lThruster, rThruster;
    private ParticleSystem lSmoke, rSmoke;
    private ParticleSystem explosion;
    private AudioClip explosionSound;
    private AudioClip shootSound;
    private AudioClip powerupSound;

    private GameObject explosionPrefab; // to show up when it gets shot
    public int lastHitBy; // the int of the last attack by, 0 being ship1

    public event Action<ControllerStatusChangedEventArgs> ControllerDisconnectedEvent;

    private void Awake()
    {
        player = ReInput.players.GetPlayer(playerId); // REWIRED
        // Subscribe to events
        //ReInput.ControllerConnectedEvent += OnControllerConnected;
        //ReInput.ControllerDisconnectedEvent += OnControllerDisconnected;
        //ReInput.ControllerPreDisconnectEvent += OnControllerPreDisconnect;

        controller = GameObject.Find("GameManager").GetComponent<GameController>();
        lThruster = gameObject.transform.GetChild(0).transform.GetChild(0).GetComponent<ParticleSystem>(); // find all the particles
        rThruster = gameObject.transform.GetChild(0).transform.GetChild(1).GetComponent<ParticleSystem>();
        lSmoke = this.gameObject.transform.GetChild(0).transform.GetChild(2).GetComponent<ParticleSystem>();
        rSmoke = this.gameObject.transform.GetChild(0).transform.GetChild(3).GetComponent<ParticleSystem>();
        explosion = this.gameObject.transform.GetChild(2).GetComponent<ParticleSystem>();
        explosionSound = (AudioClip)Resources.Load("explosion");
        shootSound = (AudioClip)Resources.Load("laser-shot");
        powerupSound = (AudioClip)Resources.Load("Powerup");
    }
    void Start()
    {
        SetLayer();
        //if (GameController.test == false)
        //this.gameObject.SetActive(false); // switch off object, if not testing
        rbody = GetComponent<Rigidbody>(); // get the rigidbody
        arrow = gameObject.transform.GetChild(1).gameObject; // get the fake arrow
        joystickAString = joystickNumber.ToString(); // get the player number to a string
        shield = transform.GetChild(0).transform.GetChild(4).gameObject; // assign the shield
        bubble = transform.GetChild(0).transform.GetChild(5).gameObject; // assign the bubble

        objectPooler = ObjectPooler.Instance;
        curDashAmount = maxDashAmount; // so the cur is equal to max at start, pickups can increase or decrease

        rend = GetComponent<Renderer>();
        shipColor = transform.GetChild(0).GetComponent<Renderer>().material.color; // get and save ship color
        charged = arrow.transform.GetChild(0).GetComponent<ParticleSystem>(); // the charged particle
        shipColor.a = 0.58f;
        explosionPrefab = (GameObject)Resources.Load("SmallExplosion"); // set explosion prefab
    }
    void Update()
    {
        if(GameController.teamBattle == false)
        {
            if (player.GetButtonDown("A") && transform.GetChild(0).gameObject.activeInHierarchy == false && GameController.preGame == true)
            {
                print("add");
                controller.AddPlayer(playerId, true);
            }
            if (player.GetButtonDown("B") && transform.GetChild(0).gameObject.activeInHierarchy == true && GameController.preGame == true)
            {
                print("de-add");
                controller.AddPlayer(playerId, false);
            }
        }
        else if(GameController.teamBattle == true)
        {
            if (player.GetButtonDown("A") && GameController.preGame == true)
            {
                print("add team");
                controller.AddTeam(playerId, true);
            }
            if (player.GetButtonDown("B") && GameController.preGame == true)
            {
                //print("de-add team");
                //controller.AddTeam(playerId, false);
            }
        }        

        if (GameController.playersCanMove == true) // if before the game starts
        {
            if(invincible == true && invincibleTimer > 0)
            {
                if(bubble.activeInHierarchy == false) // turn bubble on
                {
                    bubble.SetActive(true);
                }
                invincibleTimer -= Time.deltaTime;

                if(invincibleTimer <= 0)
                {
                    this.gameObject.layer = myLayer;
                    invincible = false;
                    invincibleTimer = 2; // reset timer
                    bubble.SetActive(false);
                }
            }
            if (!stunned && !fellOut)
            {
                Fire();
                Dash();
                ScatterShot();
            }
            else if (stunned)
            {
                stunTimer -= Time.deltaTime;

                if(lSmoke.isPlaying == false && rSmoke.isPlaying == false) // if smoke is off turn on
                {
                    lSmoke.Play();
                    rSmoke.Play();
                }
                if(lThruster.isPlaying == true && rThruster.isPlaying == true) // if thruster is on turn off
                {
                    lThruster.Stop();
                    rThruster.Stop();
                }
                if (stunTimer <= 0) // after stun
                {
                    stunned = false;
                    if (healthSlider.value <= 0)
                    {
                        UpdateHealth(35); // add and update the Ui text
                        if(rbody.mass >= 1)
                            rbody.mass -= 0.5f; // lower the mass slightly each time
                    }
                    lSmoke.Stop();
                    rSmoke.Stop();
                    stunTimer = 2.5f; // rest timer
                }
            }
            if (fellOut == true && lives > 0 && GameController.teamBattle == false) // if off map and has more lives
            {
                respawnTimer -= Time.deltaTime;
                ReSpawn();

                if(respawnTimer <= 0) // force respawn if more than n seconds
                {
                    ReSpawn();
                }
            }
            else if(fellOut == true && GameController.teamBattle == true)
            {
                if(myLayer == 14 && GameController.team1Lives > 1 || myLayer == 15 && GameController.team2Lives > 1) // if one team or other and has more than 1 live
                {
                    respawnTimer -= Time.deltaTime;
                    ReSpawn();

                    if(respawnTimer <= 0)
                    {
                        ReSpawn();
                    }
                }               
            }
            if (curDashAmount <= 0)
            {
                lThruster.Stop();
                rThruster.Stop();
            }
            //MoveAndRotate(); // neds to be out of stunned, has own conditions for that
        }
    }
    void MoveAndRotate()
    {
        if (!stunned && !fellOut)
        {
            /*
            moveVector.x = player.GetAxis("Move Horizontal"); // get input by name or action id
            moveVector.y = player.GetAxis("Move Vertical");

            if (moveVector.x != 0.0f || moveVector.z != 0.0f)
            {
                rbody.velocity = new Vector3(player.GetAxis("Move Horizontal") * Time.deltaTime * moveSpeed, rbody.velocity.y, player.GetAxis("Move Vertical") * Time.deltaTime * moveSpeed);
            } */
            rbody.velocity = new Vector3(player.GetAxis("Move Horizontal") * Time.deltaTime * moveSpeed, rbody.velocity.y, player.GetAxis("Move Vertical") * Time.deltaTime * moveSpeed);

            //rbody.velocity = new Vector3(Input.GetAxis("J" + joystickAString + "Hor") * Time.deltaTime * moveSpeed, rbody.velocity.y, -Input.GetAxis("J" + joystickAString + "Vert") * Time.deltaTime * moveSpeed); // move not based on look
            //Debug.Log("Velocity: " + rbody.velocity.ToString());
            //Vector3 lookDirection = -Vector3.forward * Input.GetAxis("J" + joystickAString + "StickHor") + Vector3.right * Input.GetAxis("J" + joystickAString + "StickVert"); // rotation
            //test.transform.position += lookDirection;

            Vector3 lookDirection = Vector3.right * player.GetAxis("Rotate Horizontal") + Vector3.forward * player.GetAxis("Rotate Vertical");

            if (lookDirection.sqrMagnitude > 0.0f)
            {
                transform.rotation = Quaternion.LookRotation(lookDirection, Vector3.up);
            }
        }
        else if (stunned)
        {
            rbody.velocity = new Vector3(player.GetAxis("Move Horizontal") * 0, rbody.velocity.y, player.GetAxis("Move Vertical") * 0); // move not based on look
            //rbody.velocity = new Vector3(Input.GetAxis("J" + joystickAString + "Hor") * 0, rbody.velocity.y, -Input.GetAxis("J" + joystickAString + "Vert") * 0); // move not based on look
        }
    }
    void Fire()
    {
        if (player.GetAxisRaw("Right Trigger") != 0 && bullets >= 0)
        //if(player.GetButton("Right Trigger") && bullets >= 0)
        //if (Input.GetAxisRaw("J" + joystickAString + ("RTrigger")) != 0 && bullets > 0) // holding to charging shot
        {
            if (invincible == true) // not invincible anymore if you shoot
            {
                this.gameObject.layer = myLayer;
                invincible = false;
                invincibleTimer = 2; // reset timer
                bubble.SetActive(false);
            }
            if (lThruster.isPlaying == true) // if the thrusters are playing
            {
                lThruster.Stop();
                rThruster.Stop();
            }            
            charging = true;

            if (arrow.activeInHierarchy == false)
            {
                arrow.SetActive(true);
            }
            if (arrowSize <= 4) // increase size until max
            {
                arrow.transform.localScale += new Vector3((1 * Time.deltaTime * chargeSpeed), 0, (1 * Time.deltaTime * chargeSpeed)); // OLD incresed height -- arrow.transform.localScale += Vector3.one * Time.deltaTime * chargeSpeed;
                //slider.value += 0.32f * Time.deltaTime * chargeSpeed;
            }
            else if (arrowSize > 4) // max size
            {
                arrow.transform.localScale = new Vector3(4, 0.75f, 4);

                if(charged.isPlaying == false)
                    charged.Play();
                //return;
            }
            float tempSize = arrow.transform.localScale.x; // fake square size added to temp size
            arrowSize = (float)System.Math.Round(tempSize, 1); // rounds to the nearest decimal
        }
        else if(player.GetAxisRaw("Right Bumper") == 0 && charging && bullets > 0)
        //else if (Input.GetAxisRaw("J" + joystickAString + ("RTrigger")) == 0 && charging && bullets > 0) // releasing to shoot
        {
            charging = false; // not charging anymore
            if(charged.isPlaying)
                charged.Stop();

            rbody.AddRelativeForce(-Vector3.forward * 200); // move back when you shoot
            arrow.transform.localScale = new Vector3(1, 0.5f, 1); // reset fake arrow
            if (arrow.activeInHierarchy == true) // turn off arrow
                arrow.SetActive(false);

            GameObject Arrow = objectPooler.SpawnFromPool("Arrow", arrow.transform.position, arrow.transform.rotation);
            Arrow.transform.localScale = new Vector3(arrowSize, 1, arrowSize); // adjust size
            Arrow.name = ("Player" + joystickNumber + " Bullet"); // change name to Player bullet
            Arrow.layer = myLayer; // set up the arrows layer
            Projectile temp = Arrow.GetComponent<Projectile>();
            temp.speed = (arrowSize * 5);   // OLD ---- Arrow.GetComponent<Projectile>().speed = (arrowSize * 4);
            temp.parent = this; // set the parent to the ship holder
            //temp.SetUpColor(joystickNumber, shipColor, this.gameObject); // OLD ---  Arrow.GetComponent<Projectile>().SetUpColor(joystickNumber);
            temp.SetUpColor(joystickNumber, shipColor);
            arrowSize = 1; // reset the arrow size
            GetComponent<AudioSource>().PlayOneShot(shootSound, 0.4f); // play the laser sound once
        }
    }
    void ScatterShot()
    {
        if(player.GetButton("Right Bumper") && bullets >= 3 && !charging)
        //if(Input.GetButtonDown("J" + joystickAString + ("RB")) && bullets >= 3 && !charging) // press for scatter shot
        {
            print("scatter shot");
            GameObject Arrow1 = objectPooler.SpawnFromPool("Arrow", transform.GetChild(4).position, transform.GetChild(4).rotation);
            Arrow1.transform.localScale = new Vector3(1.5f, 1, 1.5f); // adjust size
            Arrow1.name = ("Player" + joystickNumber + " Bullet"); // change name to Player bullet
            Arrow1.layer = myLayer; // set up the arrows layer
            Projectile temp1 = Arrow1.GetComponent<Projectile>();
            temp1.speed = (arrowSize * 10); // double arrow speed
            temp1.parent = this; // set the parent to the ship holder
            temp1.SetUpColor(joystickNumber, shipColor);
            arrowSize = 1; // reset the arrow size
            GetComponent<AudioSource>().PlayOneShot(shootSound, 0.4f); // play the laser sound once

            GameObject Arrow2 = objectPooler.SpawnFromPool("Arrow", transform.GetChild(4).transform.GetChild(0).position, transform.GetChild(4).transform.GetChild(0).rotation);
            Arrow2.transform.localScale = new Vector3(1.5f, 1, 1.5f); // adjust size
            Arrow2.name = ("Player" + joystickNumber + " Bullet"); // change name to Player bullet
            Arrow2.layer = myLayer; // set up the arrows layer
            Projectile temp2 = Arrow2.GetComponent<Projectile>();
            temp2.speed = (arrowSize * 10); // normal arrow speed
            temp2.parent = this; // set the parent to the ship holder
            temp2.SetUpColor(joystickNumber, shipColor);
            arrowSize = 1; // reset the arrow size

            GameObject Arrow3 = objectPooler.SpawnFromPool("Arrow", transform.GetChild(4).transform.GetChild(1).position, transform.GetChild(4).transform.GetChild(1).rotation);
            Arrow3.transform.localScale = new Vector3(1.5f, 1, 1.5f); // adjust size
            Arrow3.name = ("Player" + joystickNumber + " Bullet"); // change name to Player bullet
            Arrow3.layer = myLayer; // set up the arrows layer
            Projectile temp3 = Arrow3.GetComponent<Projectile>();
            temp3.speed = (arrowSize * 10); // normal arrow speed
            temp3.parent = this; // set the parent to the ship holder
            temp3.SetUpColor(joystickNumber, shipColor);
            arrowSize = 1; // reset the arrow size
        }
    }
    void Dash()
    {
        if (player.GetAxisRaw("Left Trigger") != 0 && curDashAmount > 0 && !charging && GameController.boostCharge == false ||
            player.GetAxisRaw("Left Trigger") != 0 && curDashAmount > 0 && GameController.boostCharge == true) // dash button down
        //if (Input.GetAxisRaw("J" + joystickAString + "LTrigger") != 0 && curDashAmount > 0 && !charging && GameController.boostCharge == false ||
            //Input.GetAxisRaw("J" + joystickAString + "LTrigger") != 0 && curDashAmount > 0 && GameController.boostCharge == true) // dash button down
        {
            if (invincible == true) // not invincible anymore if you dash
            {
                this.gameObject.layer = myLayer;
                invincible = false;
                invincibleTimer = 2; // reset timer
                bubble.SetActive(false);
            }
            curDashAmount -= Time.deltaTime; // subtract from dash amount
            lThruster.Play();
            rThruster.Play();
            //moveSpeed = 8; // old before time.deltatime
            moveSpeed = 750;
            //rbody.AddRelativeForce(Vector3.forward * 150, ForceMode.Acceleration); // move ship forward    //rbody.velocity += transform.right * 50;
        }
        // is always playing when you are not dashing since its in update???????
        else if(player.GetAxisRaw("Left Trigger") == 0) // dash up
        //else if(Input.GetAxisRaw("J" + joystickAString + "LTrigger") == 0) // dash up
        {
            if (moveSpeed != 525)
                moveSpeed = 525;
            //if(moveSpeed > 5)
            //moveSpeed = 5;
            if (lThruster.isPlaying == true)
            {
                lThruster.Stop();
                rThruster.Stop();
            }            
            if (curDashAmount < maxDashAmount) // refresh curDashAmount
            {
                curDashAmount += Time.deltaTime;

                if (curDashAmount > maxDashAmount)
                    curDashAmount = maxDashAmount;
            }
        }
        else // plays when you are holding dash but have no dash
        {
            if (moveSpeed != 525)
                moveSpeed = 525;
            //moveSpeed = 5;
        }
    }
    void Pickup(int type) // to decide which pickup was hit and what to do; arrows, health, shield
    {
        if(type == 1) // +2
        {
            if(bullets < 15) // if less than 15 bullets add 2 more
            {
                bullets += 2;
                UpdateUI();
            }            
        }
        else if(type == 2) // +3
        {
            if (bullets < 15) // if less than 15 bullets add 2 more
            {
                bullets += 3;
                UpdateUI();
            }
        }
        else if(type == 3)
        {
            healthSlider.value += 0.20f;
        }
        else if(type == 4)
        {
            if(shieldOn == false) // if no shield turn it on
            {
                print("piked up shield");
                shieldOn = true;
                shieldHealth = 2; // set shield health
                shield.SetActive(true);
            }
        }
    }
    void ReSpawn()
    {
        if (player.GetButtonDown("A")) // checking if fell out in Update()
        //if(Input.GetButtonDown("J" + joystickAString + "A")) // checking if fell out in Update()
        {
            //int temp = joystickNumber - 1; // set the controller number
            //gameObject.transform.position = playerSpawnNum[temp].position; // reset position
            RespawnConstants();
        }
        else if(respawnTimer <= 0) // force respawn
        {
            RespawnConstants();
        }
    }
    private void RespawnConstants()
    {
        int randomSpawn = UnityEngine.Random.Range(0, playerSpawnNum.Length);
        // take away some arrows
        explosion.gameObject.SetActive(false); // turn off explosion
        gameObject.transform.position = playerSpawnNum[randomSpawn].position; // move to random loc
        gameObject.transform.rotation = playerSpawnNum[randomSpawn].rotation; // adjust rotation
        health = 100; // add health back to max
        UpdateHealth(100); // update UI text
        rbody.isKinematic = false;
        stunned = false; // reset not stunned
        fellOut = false; // reset not off map
        this.transform.GetChild(0).GetComponent<MeshRenderer>().enabled = true; // turn on ship mesh
        this.transform.GetChild(1).GetComponent<MeshRenderer>().enabled = true; // turn on arrow renderer
        respawnTimer = 4; // reset time
        invincible = true; // invincible for a while
        rbody.mass = 4; // reset mass
    }
    private void OnCollisionEnter(Collision collision)
    {
        //https://answers.unity.com/questions/1100879/push-object-in-opposite-direction-of-collision.html?childToView=1102047#comment-1102047        
        if(collision.gameObject.tag == "Arrow")
        {
            if (shieldOn == false) // if not shielded take damage normally
            {
                collision.gameObject.SetActive(false); // turn off pooled object

                float tempSpeed = collision.gameObject.GetComponent<Projectile>().speed;
                Vector3 dir = collision.GetContact(0).point - transform.position; // calculate angle between collision point and player

                dir = -dir.normalized; // get the opposite vector3 and then normalize it

                //GetComponent<Rigidbody>().AddForce(dir * (force * tempSpeed)); // add force in the direction and mutiply by force, to push player back
                GetComponent<Rigidbody>().AddForce(dir * (force * tempSpeed)); // add force in the direction and mutiply by force, to push player back

                float damage = Mathf.RoundToInt(tempSpeed * 2);
                UpdateHealth(-damage);

                if(lastHitBy != collision.gameObject.GetComponent<Projectile>().parentNum)
                    lastHitBy = collision.gameObject.GetComponent<Projectile>().parentNum; // set last hit to projectile paretn num
            }
            else // if shield is on
            {
                collision.gameObject.SetActive(false); // turn off pooled object
                shieldHealth -= 1; // take 1 away from shield

                if(shieldHealth < 1)
                {
                    shieldOn = false; // change bool, so can take damage
                    shield.SetActive(false); // turn off shield
                }
            }
        }
        if(collision.gameObject.tag == "Player") // if you run into other player
        {
            if(lastHitBy != collision.gameObject.GetComponent<PlayerLocal>().joystickNumber)
                lastHitBy = collision.gameObject.GetComponent<PlayerLocal>().joystickNumber;
            //rbody.velocity.magnitude;
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if(other.tag == "OffMap" && fellOut == false)
        {
            print(GameController.teamBattle);
            fellOut = true;
            rbody.isKinematic = true; // stops the infinite fall
            explosion.gameObject.SetActive(true); // play explosion
            //GetComponent<AudioSource>().PlayOneShot(explosionSound, 0.3f);
            if(shieldOn == true) // turn shield off if on
            {
                shieldOn = false;
                shield.SetActive(false); // turn off shield
            }
            if(bullets > 5) // if more than a certain number bring arrows back down
            {
                bullets = 5;
                UpdateUI();
            }
            this.transform.GetChild(0).GetComponent<MeshRenderer>().enabled = false; // turn off ship renderer
            if(this.transform.GetChild(1).GetComponent<MeshRenderer>().enabled == true)  // turn off arrow renderer if on
                this.transform.GetChild(1).GetComponent<MeshRenderer>().enabled = false;
            if (charged.isPlaying) // turn off charger if on
                charged.Stop();

            if(GameController.teamBattle == false)
            {
                lives--; // subtract a live
                livesText.text = "Lives: " + lives.ToString(); // update UI
            }
            else if(GameController.teamBattle == true)
            {
                if(myLayer == 14) // team 1
                {
                    GameController.team1Lives -= 1;
                    livesText.text = "Team Lives: " + GameController.team1Lives.ToString(); // update team1 UI
                }
                else if(myLayer == 15) // team 2
                {
                    GameController.team2Lives -= 1;
                    livesText.text = "Team Lives: " + GameController.team2Lives.ToString(); // update team2 UI
                }
            }
            GameController.totalLives -= 1; // take 1 life from total so we can drop level later on
            this.gameObject.layer = 13; // changes layer so that you don't pop up when you respawn

            if(!GameController.teamBattle) // if no more lives add defeated text
            {
                if(lives == 0)
                {
                    controller.DeathUI(joystickNumber);
                    GameObject explo = Instantiate(explosionPrefab, transform.position, transform.rotation) as GameObject; // create explosion w/sound
                    Destroy(explo, 1.25f); // destroy after 1 seconds
                    this.gameObject.SetActive(false); // turn off player gameobject to check who won
                    GameController.activePlayers -= 1; // subtract 1 to active players
                    controller.GameOver(); // checks if no other players active
                }                
            }
            else if(GameController.teamBattle == true) // if one team is dead
            {
                if(myTeam == 0 && GameController.team1Lives <= 1) // when there are no more lives
                {
                    controller.DeathUI(joystickNumber);
                    GameObject explo = Instantiate(explosionPrefab, transform.position, transform.rotation) as GameObject; // create explosion w/sound
                    Destroy(explo, 1.25f); // destroy after 1 seconds
                    this.gameObject.SetActive(false); // turn off player gameobject to check who won
                    GameController.activePlayers -= 1; // subtract 1 to active players
                }
                else if(myTeam == 1 && GameController.team2Lives <= 1)
                {
                    controller.DeathUI(joystickNumber);
                    GameObject explo = Instantiate(explosionPrefab, transform.position, transform.rotation) as GameObject; // create explosion w/sound
                    Destroy(explo, 1.25f); // destroy after 1 seconds
                    this.gameObject.SetActive(false); // turn off player gameobject to check who won
                    GameController.activePlayers -= 1; // subtract 1 to active players
                }
                if (GameController.team1Lives == 0)
                {
                    controller.TeamBattleOver(1);
                    // team 1 lost
                }
                else if(GameController.team2Lives == 0)
                {
                    controller.TeamBattleOver(0);
                    // team 2 lost
                }
            }
            if(lastHitBy != 0)
                controller.AddToKills(lastHitBy); // add kill to the controller and then update UI
            lastHitBy = 0; // reset last hit by
        }
        if (other.tag == "Pickup")
        {
            Pickup(other.gameObject.GetComponent<Pickup>().typeint); // got to pickup void
            other.GetComponent<Pickup>().ReAddTransform(); // re-add the pickups spawn loc back to list
            GetComponent<AudioSource>().PlayOneShot(powerupSound, 0.2f);
        }
    }
    public void UpdateUI()
    {
        arrowNum.text = "Arrows: " + bullets;
    }
    void UpdateHealth(float healthAdd) 
    {
        float temp = healthAdd / 100; // to get a fraction of the health since it is a decimal
        
        healthSlider.value += temp;
        /* if (healthSlider.value >= 0.35f && healthbarFill.color != shipColor)
        {
            healthbarFill.color = shipColor;
        }
        if (healthSlider.value < 0.35f && healthbarFill.color != Color.red) // change health color if low 
        {
            healthbarFill.color = Color.red;
        } */
        if (healthSlider.value <= 0) // get stunned with 0 health
        {
            stunned = true; 
        }
    }
    public void SetLayer()
    {
        myLayer = this.gameObject.layer;
        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).gameObject.layer = myLayer;
        }
    }
    void OnControllerDisconnected(ControllerAssignmentChangedEventArgs args) // is called when a controller is disconnected
    {
        //ebug.Log("A controller was disconnected! Name = " + args.name + " Id = " + args.controllerId + " Type = " + args.controllerType);
    }
    private void FixedUpdate()
    {
        if (GameController.playersCanMove == true)
        {
            MoveAndRotate(); // neds to be out of stunned, has own conditions for that
        }   
    }
}