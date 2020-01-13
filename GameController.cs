using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using Rewired;

public class GameController : MonoBehaviour
{
    //private Player player; // access to REWIRED controls

    /// <summary>
    /// True = team battle, false = FFA
    /// </summary>
    public static bool teamBattle = false; // if team game
    public GameObject[] players;
    public GameObject[] playerUI; // turn on UI when a new player joins
    public GameObject killCanvas; // the canvas that only holds kills text
    public int[] playerKills; // the number of kills
    public Text[] playerKillText; // text being edited
    //public static GameObject lastWinner; // who won last to turn on the crown between levels
    private GameObject uiCanvas;
    private GameObject teamUICanvas;
    //public List<int> usedControllers; // list of controllers in use
    public int[] shipControlled; // what ship this player controls
    public bool[] controllerUsed; // if the controller is used
    public List<GameObject> Team1; // team 1 for team game
    public List<GameObject> Team2; // team 2 for team game
    public static int team1Lives = 6;
    public static int team2Lives = 6;
    private GameObject pausePanel;
    private GameObject endGamePanel;
    private EventSystem mySystem;
    // timer variables for starting the game
    public static int activePlayers; // how many players are in
    public static int totalLives; // total lives of all players
    public static bool paused = true; // if timescale = 0
    public static bool playersCanMove = false; // if player movement is allowed, timescale should be 1 at the time
    public static bool preGame = true; // if this is before game has started so that it won't run when you pause the game
    public static bool boostCharge = false; // can boost while charging
    private bool canCountDown = false; // true if the game has enough players to start 
    private Text countDownText; // start text that turns to timer
    private float readyToStart = 5f; // 5 seconds countdown to start the game
    // for dropping floor
    public GameObject[] fallingGround; // if I lower the outer and middle
    //private readonly int randomPlatformGroup; // when 
    public Transform endPos; // where it ends
    public static bool startFlash = false; // to do the flash once
    public static bool startDropping = false; // to know when to start dropping
    public static bool droppingLevel = true; // to know if a level drops or is stationary
    //Pickup Stuff
    public List<Transform> availablePos; // where the pickups can be spawned
    public List<Transform> allPos; // array of  
    //private GameObject pickup;
    private float pickupTimer;
    private ObjectPooler pooler;
    //For testing purposes
    public static bool test = false;
    public bool testing = false; // for testing to turn off countdown

    private void Awake()
    {
        //player = ReInput.controllers.GetAnyButton();
        //player = ReInput.players.GetPlayer(playerId); // REWIRED

        teamBattle = GameObject.Find("Music").GetComponent<Audio>().teamBattleRef;
        uiCanvas = GameObject.Find("Canvas").gameObject; // find the canvas for later uses
        teamUICanvas = GameObject.Find("TeamCanvas").gameObject; // find the canvas for later uses

        test = testing; // make the static variable equal the local variable

        if (!teamBattle) // not team battle leave default
        {
            teamUICanvas.SetActive(false);
            countDownText = uiCanvas.transform.GetChild(4).transform.GetChild(0).GetComponent<Text>(); // use canvas to assign child as countdown text
        }
        else // if team battle change UI
        {
            uiCanvas.SetActive(false);
            for(int i = 0; i < 4; i++)
            {
                playerUI[i] = teamUICanvas.transform.GetChild(i).gameObject;
            }
            countDownText = teamUICanvas.transform.GetChild(4).transform.GetChild(0).GetComponent<Text>();
        }
        if(testing)
        {
            readyToStart = 0;
            paused = false;
            playersCanMove = true;
            countDownText.enabled = false;
            for(int i = 0; i < playerUI.Length; ++i)
            playerUI[i].gameObject.SetActive(true); // turn on the players UI

            for(int i = 0; i < 4; i++)
            {
                if (players[i].gameObject.activeInHierarchy == true)
                    activePlayers++;
            }
            totalLives = (activePlayers * 3);
        }
        mySystem = GameObject.Find("EventSystem").GetComponent<EventSystem>();

        playerKills = new int[4];
    }
    void Start()
    {
        BeginningVariables();
        pooler = ObjectPooler.Instance; // get the object pooler for the pickup
        //pickup = Resources.Load<GameObject>("Pickup");
        allPos = availablePos;
        pickupTimer = Random.Range(7, 20);
        if (!teamBattle)
        {
            pausePanel = uiCanvas.transform.GetChild(5).gameObject; // use UI to find pasue menu
            endGamePanel = uiCanvas.transform.GetChild(6).gameObject; // get game over panel
        }
        else         // find for team battle
        {
            pausePanel = teamUICanvas.transform.GetChild(5).gameObject; // use UI to find pasue menu
            endGamePanel = teamUICanvas.transform.GetChild(6).gameObject; // get game over panel
        }            
        mySystem = GameObject.Find("EventSystem").GetComponent<EventSystem>(); // find the event system

        if(paused == true)
        {
            //Time.timeScale = 0;
        }
        if (fallingGround.Length == 0)
            droppingLevel = false;
        else
            droppingLevel = true;
    }
    void Update()
    {
        if (activePlayers <= 4 && playersCanMove == false && preGame == true && teamBattle == false) // if less than 4 players & not started add 1 in
        {
            //SetUpPlayers();
        }  // add players
        else if(activePlayers <= 4 && playersCanMove == false && preGame == true && teamBattle == true)
        {
            //SetUpTeams();
        }
        if(canCountDown == true && playersCanMove == false && preGame == true) // if more than 1 player and player cannot move
        {
            //print("counting");
            if(paused == true) // this starts pickup for starting spawn countdown
                paused = false;
            //countDownText.gameObject.SetActive(true); // turn on timer
            readyToStart -= Time.deltaTime;

            if(readyToStart > 1) // display numbers
            {
                countDownText.text = readyToStart.ToString("F0");
            }
            else if (readyToStart < 1 && readyToStart > 0) // display go
            {
                countDownText.text = "Go";
            }
            else if (readyToStart <= 0) // turn off UI and allow players to move
            {
                countDownText.gameObject.SetActive(false); // turn off timer
                playersCanMove = true;
                totalLives = (activePlayers * 3); // all players * their 3 lives
                preGame = false; // no more pre game so pause can work properly
            }
        } // counting down
        if(paused == false && Time.timeScale == 0 && preGame == true) // if before game and timeScale = 0
        {
            Time.timeScale = 1;
        }
        if(playersCanMove == true && paused == false && Input.GetButtonDown("AllStart") && preGame == false) // for pausing the game
        {
            Pause();
        }
        else if(playersCanMove == false && paused == true && Input.GetButtonDown("AllStart") && preGame == false)
        {
            Pause();
        }
        if (pickupTimer > 0 && playersCanMove == true)
        {
            pickupTimer -= Time.deltaTime;
        }
        else if (pickupTimer <= 0) // spawn pickup
        {
            SpawnPickup();
        }     
        if (startDropping == true && fallingGround[0].gameObject.activeInHierarchy == true)
        {
            /* if(fallingGround.Length == 2) // if more than 2 
            {
                fallingGround[randomPlatformGroup].transform.position += Vector3.down * Time.deltaTime * 2;
            }
            else */
                fallingGround[0].transform.position += Vector3.down * Time.deltaTime * 2;
        } // dropping the platform
        if(testing == true) // when testing bool is on
            ResetGame(); // to reload the current level
    }
    public void SetUpPlayers() // to spawn/remove players before game
    {
        for (int i = 1; i < 5; i++)
        {
            if (Input.GetButtonDown("J" + i + "A") && players[(i - 1)].gameObject.activeInHierarchy == false) // if not already active
            {
                players[(i - 1)].gameObject.SetActive(true); // turn on player
                playerUI[(i - 1)].gameObject.SetActive(true); // turn on the players UI
                activePlayers += 1; // add 1 to active players
                playerKillText[(i - 1)].gameObject.SetActive(true); // turn on the kill text for that player

                if (activePlayers >= 2) // if paused & more than 1 player than start a countdown
                {
                    readyToStart = 5f; // reset to 5 when new player enters
                    if(canCountDown == false)
                        canCountDown = true; // start the count down
                }
            }
            else if (Input.GetButtonDown("J" + i + "B") && players[(i - 1)].gameObject.activeInHierarchy == true) // remove player
            {
                players[(i - 1)].gameObject.SetActive(false); // turn off player
                playerUI[(i - 1)].gameObject.SetActive(false); // turn off the players UI
                activePlayers -= 1; // remove 1 to active players
                playerKillText[(i - 1)].gameObject.SetActive(false); // turn off the kill text for that player

                if (activePlayers < 2)
                {
                    paused = true;
                    canCountDown = false;
                    countDownText.text = "Press 'A' To Join";
                }
            }
        }
    }
    public void AddPlayer(int playerNum, bool adding)
    {
        if(adding == true)
        {
            players[playerNum].gameObject.transform.GetChild(0).gameObject.SetActive(true); // turn on player
            playerUI[playerNum].gameObject.SetActive(true); // turn on the players UI
            activePlayers += 1; // add 1 to active players
            playerKillText[playerNum].gameObject.SetActive(true); // turn on the kill text for that player

            if (activePlayers >= 2) // if paused & more than 1 player than start a countdown
            {
                readyToStart = 5f; // reset to 5 when new player enters
                if (canCountDown == false)
                    canCountDown = true; // start the count down
            }
        }
        if(adding == false)
        {
            players[playerNum].gameObject.transform.GetChild(0).gameObject.SetActive(false); // turn off player
            playerUI[playerNum].gameObject.SetActive(false); // turn off the players UI
            activePlayers -= 1; // remove 1 to active players
            playerKillText[playerNum].gameObject.SetActive(false); // turn off the kill text for that player

            if (activePlayers < 2)
            {
                paused = true;
                canCountDown = false;
                countDownText.text = "Press 'A' To Join";
            }
        }
    }
    void SetUpTeams() // set up teams for team battle
    {
        // ships 1/2 team 1, ships 4/3 team 2
        // assign ui to the player
        for (int i = 1; i < 5; i++) // controller starts at 1
        {
            if (Input.GetButtonDown("J" + i + "A")) // get individual controller
            {
                for(int j = 0; j <= 4; j++) // 'J' is the Ship and UI, where 'I' is the controller pressed
                {
                    if(players[j].gameObject.activeInHierarchy == false && controllerUsed[(i-1)] == false) // activates the first dissabled ship
                    {
                        players[j].gameObject.SetActive(true); // turn on player
                        players[j].GetComponent<PlayerLocal>().joystickNumber = i; // assign the controller to the ship
                        playerUI[j].gameObject.SetActive(true); // turn on UI
                        playerKillText[(i - 1)].gameObject.SetActive(true); // turn on the kill text for that player

                        if (Team1.Count < 2) // team 1
                        {
                            players[j].layer = 14; // cahnge layer to team 1
                            Team1.Add(players[j]); // add ship to team 1 list
                            players[j].GetComponent<PlayerLocal>().livesText = teamUICanvas.transform.GetChild(7).GetComponent<Text>(); // lives text
                            players[j].GetComponent<PlayerLocal>().myTeam = 0; // assign local player team number for dying

                            if (Team1.Count == 2)
                                countDownText.text = "Team 2 press 'A'";
                        }
                        else // team 2
                        {
                            players[j].layer = 15; // change layer to team 2
                            Team2.Add(players[j]);
                            players[j].GetComponent<PlayerLocal>().livesText = teamUICanvas.transform.GetChild(8).GetComponent<Text>(); // lives text
                            players[j].GetComponent<PlayerLocal>().myTeam = 1; // assign local player team number for dying
                        }
                        players[j].GetComponent<PlayerLocal>().SetLayer();
                        players[j].GetComponent<PlayerLocal>().healthSlider = playerUI[j].transform.GetChild(0).GetComponent<Slider>(); // health bar slider
                        players[j].GetComponent<PlayerLocal>().healthbarFill = playerUI[j].transform.GetChild(0).transform.GetChild(1).transform.GetChild(0).GetComponent<Image>(); // health bar fill
                        players[j].GetComponent<PlayerLocal>().arrowNum = playerUI[j].transform.GetChild(1).GetComponent<Text>(); // arrow num
                        controllerUsed[(i-1)] = true; // set the cotnroller to have been used, so not to use again
                        shipControlled[(i-1)] = (j); // assign the ship to the controller
                        activePlayers += 1; // add 1 to active players
                        break;
                    }
                }
                if(activePlayers >= 4) // if both teams filled start game
                {
                    print("unpausing");
                    readyToStart = 3;
                    if (canCountDown == false)
                        canCountDown = true; // start the count down
                }
            }                        
            else if (Input.GetButtonDown("J" + i + "B") && controllerUsed[(i - 1)] == true) // remove player
            {
                for(int j = 0; j < 4; j++)
                {
                    players[shipControlled[(i-1)]].SetActive(false); // turn off player
                    playerUI[(i - 1)].gameObject.SetActive(false); // turn off the players UI
                    controllerUsed[(i - 1)] = false; // controller is not being used anymore
                    activePlayers -= 1; // remove 1 to active players

                    if (Team1.Contains(players[shipControlled[(i - 1)]])) // if the list contains this ship
                    { Team1.Remove(players[shipControlled[(i - 1)]]); }
                    else // if not go to this list
                    { Team2.Remove(players[shipControlled[(i - 1)]]); }
                }                
                if (activePlayers < 4) // if less than 4 stop countdown
                {
                    paused = true;
                    canCountDown = false;
                    if(Team1.Count >= 2)
                    {
                        countDownText.text = "Team 1 press 'A'";
                    }
                    else if(Team2.Count >= 3)
                    {
                        countDownText.text = "Team 2 press 'A'";
                    }
                }
            } 
        }
    }
    public void AddTeam(int playerNum, bool adding)
    {
        // ships 1/2 team 1, ships 4/3 team 2
        // assign ui to the player
        if (adding == true)
        {                 
            for (int j = 0; j <= 4; j++) // 'J' is the Ship and UI
            {
                if (players[j].gameObject.transform.GetChild(0).gameObject.activeInHierarchy == false && controllerUsed[playerNum] == false) // activates the first dissabled ship
                {
                    players[j].gameObject.transform.GetChild(0).gameObject.SetActive(true); // turn on player
                    players[j].GetComponent<PlayerLocal>().joystickNumber = playerNum; // assign the controller to the ship
                    playerUI[j].gameObject.SetActive(true); // turn on UI
                    playerKillText[playerNum].gameObject.SetActive(true); // turn on the kill text for that player

                    if (Team1.Count < 2) // team 1
                    {
                        players[j].layer = 14; // cahnge layer to team 1
                        Team1.Add(players[j]); // add ship to team 1 list
                        players[j].GetComponent<PlayerLocal>().livesText = teamUICanvas.transform.GetChild(7).GetComponent<Text>(); // lives text
                        players[j].GetComponent<PlayerLocal>().myTeam = 0; // assign local player team number for dying

                        if (Team1.Count == 2)
                            countDownText.text = "Team 2 press 'A'";
                    }
                    else // team 2
                    {
                        players[j].layer = 15; // change layer to team 2
                        Team2.Add(players[j]);
                        players[j].GetComponent<PlayerLocal>().livesText = teamUICanvas.transform.GetChild(8).GetComponent<Text>(); // lives text
                        players[j].GetComponent<PlayerLocal>().myTeam = 1; // assign local player team number for dying
                    }
                    players[j].GetComponent<PlayerLocal>().SetLayer();
                    players[j].GetComponent<PlayerLocal>().healthSlider = playerUI[j].transform.GetChild(0).GetComponent<Slider>(); // health bar slider
                    players[j].GetComponent<PlayerLocal>().healthbarFill = playerUI[j].transform.GetChild(0).transform.GetChild(1).transform.GetChild(0).GetComponent<Image>(); // health bar fill
                    players[j].GetComponent<PlayerLocal>().arrowNum = playerUI[j].transform.GetChild(1).GetComponent<Text>(); // arrow num
                    controllerUsed[playerNum] = true; // set the cotnroller to have been used, so not to use again
                    shipControlled[playerNum] = (j); // assign the ship to the controller
                    activePlayers += 1; // add 1 to active players
                    break;
                }
            }
            if (activePlayers == 4) // if both teams filled start game
            {
                print("unpausing");
                readyToStart = 3;
                if (canCountDown == false)
                    canCountDown = true; // start the count down
            }
        }
        else if(adding == false)
        {
            for (int j = 0; j < 4; j++) // j is the ship and UI
            {
                players[shipControlled[playerNum]].transform.GetChild(0).gameObject.SetActive(false); // turn off player
                playerUI[shipControlled[playerNum]].gameObject.SetActive(false); // turn off the players UI
                controllerUsed[playerNum] = false; // controller is not being used anymore
                activePlayers -= 1; // remove 1 to active players
           
                if (Team1.Contains(players[shipControlled[playerNum]])) // if the list contains this ship
                { Team1.Remove(players[shipControlled[playerNum]]); }
                else // if not go to this list
                { Team2.Remove(players[shipControlled[playerNum]]); }
            }
            if (activePlayers < 4) // if less than 4 stop countdown
            {
                paused = true;
                canCountDown = false;
                if (Team1.Count >= 2)
                {
                    countDownText.text = "Team 1 press 'A'";
                }
                else if (Team2.Count >= 3)
                {
                    countDownText.text = "Team 2 press 'A'";
                }
            }
        }
    }
    public void AddToKills(int playerNum)
    {
        if(!teamBattle)
        {
            playerKills[(playerNum-1)] += 1; // add 1 kill to the player
            //playerKillText[(playerNum - 1)].text = playerKills[(playerNum-1)].ToString();
            playerKillText[(playerNum - 1)].text = "Player " + playerNum.ToString() + " Kills: " + playerKills[(playerNum-1)].ToString();
        }
        else
        {
            playerKills[(playerNum-1)] += 1;// add 1 kill to the player from the team
            playerKillText[((playerNum-1))].text = "Player " + playerNum.ToString() + " Kills: " + playerKills[(playerNum-1)].ToString();
        }        
    }
    void SpawnPickup()
    {
        int randomLoc = Random.Range(0, (availablePos.Count)); // pick a random loc
        //GameObject drop = Instantiate(pickup, availablePos[randomLoc].position, pickup.transform.rotation) as GameObject;
        GameObject drop = pooler.SpawnFromPool("Pickup", availablePos[randomLoc].position, availablePos[randomLoc].rotation); // "spawn" from pooler
        drop.GetComponent<Pickup>().AssignType(Random.Range(1,16));
        drop.GetComponent<Pickup>().mySpawnPos = availablePos[randomLoc]; //assign the transform to the pickup so it can put it back in later
        availablePos.RemoveAt(randomLoc); //remove from list so not to respawn there
        //drop.transform.GetChild(1).GetComponent<Pickup>().mySpawnPos = availablePos[randomLoc]; //assign the transform to the pickup so it can put it back in later
        pickupTimer = Random.Range(9, 16);
    }
    void ResetGame() // to reset the level
    {
        for(int i = 1; i < 4; i++)
        {
            if(Input.GetButtonDown("J" + i + "Select"))
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }
    }
    public void DeathUI(int playerNum)
    {
        playerUI[(playerNum - 1)].transform.GetChild(3).gameObject.SetActive(true);
    }
    public void WinnerUI(int playerNum)
    {
        playerUI[(playerNum)].transform.GetChild(4).gameObject.SetActive(true);
    }
    // UI Stuff
    public void Pause()
    {
        if(paused == false)
        {
            paused = true;  // turn paused static varible to true
            Time.timeScale = 0; // turn time.deltatime to 0
            playersCanMove = false; // maybe need to change playercanmove bool but deltatime is 0 so might not
            pausePanel.SetActive(true); // open pause menu
            mySystem.SetSelectedGameObject(pausePanel.transform.GetChild(1).transform.GetChild(0).gameObject); // the slider
        }
        else if(paused == true) // unpausing
        {
            paused = false;
            Time.timeScale = 1; // unpause
            playersCanMove = true;
            pausePanel.SetActive(false); // close pause menu
            mySystem.SetSelectedGameObject(null); // set slected object to null
        }
    }
    void BeginningVariables() // reset all the static bools when restarting
    {
        activePlayers = 0;
        team1Lives = 6;
        team2Lives = 6;
        totalLives = 0;
        Team1.Clear();
        Team2.Clear();
        paused = true;
        playersCanMove = false;
        preGame = true;
        startFlash = false;
        startDropping = false;
        killCanvas.SetActive(false);
    }
    /// <summary>
    /// When the game ends
    /// </summary>
    public void GameOver()
    {
        if(activePlayers == 1)
        {
            // check which player number is still alive
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i].activeInHierarchy == true)
                {
                    players[i].transform.GetChild(3).GetComponent<MeshRenderer>().enabled = true; // turn crown on
                    WinnerUI(i); // show winner ui
                    break;
                }
            }
            StartCoroutine(EndGame());
        }        
    }
    public void TeamBattleOver(int team) // 0 = team1, 1 = team2
    {
        if(team == 0)
        {
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i].activeInHierarchy == true)
                {
                    players[i].transform.GetChild(3).GetComponent<MeshRenderer>().enabled = true; // turn crown on
                    WinnerUI(i); // show winner ui
                }
            }
            StartCoroutine(EndGame());
        }
        else if(team == 1)
        {
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i].activeInHierarchy == true)
                {
                    players[i].transform.GetChild(3).GetComponent<MeshRenderer>().enabled = true; // turn crown on
                    WinnerUI(i); // show winner ui
                }
            }
            StartCoroutine(EndGame());
        }
    }
    IEnumerator EndGame() // let the player move for a few before stopping move and showing panel
    {
        yield return new WaitForSeconds(3);        
        playersCanMove = false; // turn off bools, canmove
        //paused = true;
        //BeginningVariables();
        Time.timeScale = 0;
        endGamePanel.SetActive(true); // open canvas to allow replay or main menu
        killCanvas.SetActive(true);
        mySystem.SetSelectedGameObject(endGamePanel.transform.GetChild(1).gameObject);
    }
}
// game where you have to push other players into their own goal, near their goal is ammo to replenish. different types of ammo
// hot potatoe bomb pass game where you pass the bomb and it explodes after time, can shoot fire or ice to change the timing
// team asteroid
// capture the flag where AI chases the player with the ball, pass to opponent to get them caught
// versus ddr type where teams give the combinations to each other
//pissing wars where you piss on other players, side view arc streams
// shooting game where you charge your health to kill enemies, spread shot/ big bullet/multi fire

// capture the flag type game
// soccer type game
// team tag game
// 1 defensive 1 offense game
// shooting game
// assign ohter players controls
// bomberman type - get cards to power-up yourself or partner, de-power opponents

// top down iso
// side view
// rigibody mechanincs