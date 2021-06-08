﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class PlayerManager : NetworkBehaviour
{
    public GameObject hand;
    public GameObject tabletop;
    public GameObject enemyHand;
    public GameObject enemytabletop;
    public GameObject mulliganPanel;
    public GameObject cardPrefab;
    Card tempCard;
    private GameObject zoomCard;
    public GameObject placeHolder;

    [SyncVar]
    public int playersReady;

    [Header("PlayerConnection")]
    public static PlayerManager localPlayer;
    public static TurnManager myTurnManager;
    [SyncVar] public string matchID;
    [SyncVar] public int playerIndex;
    [Header("In-match")] 
    [SyncVar] public bool isMyTurn = false;
    [SyncVar] public int diceRoll;
    [SyncVar] public List<int> myDeck;
    
    
    private NetworkMatchChecker networkMatchChecker;

    [SerializeField] private GameObject playerLobbyUI;

    private void Awake()
    {
        networkMatchChecker = GetComponent<NetworkMatchChecker>();
    }

    public override void OnStartClient()
    {
        if (isLocalPlayer)
        {
            localPlayer = this;
            //Debug.Log(("IT IS LOCAL PLAYER"));
        }
        else
        {
            playerLobbyUI = UILobby.Instance.SpawnPlayerUIPrefab(this);
           // Debug.Log(("NOT LOCAL PLAYER"));
        }
    }

    public override void OnStopClient()
    {
        ClientDisconnect();
    }

    public override void OnStopServer()
    {
        ServerDisconnect();
    }

    private void OnEnable()
    {
        //Tell our 'OnLevelFinishedLoading' function to start listening for a scene change as soon as this script is enabled.
        SceneManager.sceneLoaded += OnLevelFinishedLoading;
    }

    private void OnDisable()
    {
        //Tell our 'OnLevelFinishedLoading' function to stop listening for a scene change as soon as this script is disabled. Remember to always have an unsubscription for every delegate you subscribe to!
        SceneManager.sceneLoaded -= OnLevelFinishedLoading;
    }

    void OnLevelFinishedLoading(Scene scene, LoadSceneMode mode)
    {
        if (scene.buildIndex == 3)
        {
            Debug.Log($"<color=red> 2 game scene finised loading</color>");
           // CmdSpawnObjects();
            GameManager.Instance.StartGame();
            if (isClientOnly)
            {
                hand = UIGame.Instance.hand;
                enemyHand = UIGame.Instance.enemyHand;
                tabletop = UIGame.Instance.tableTop;
                enemytabletop = UIGame.Instance.enemyTableTop;
                mulliganPanel = UIGame.Instance.mulliganPanel;
                //isMyTurn = true;
                //CmdChangeTurn();
            }
        }
    }

    /*[Command]
    public void CmdSpawnObjects()
    {
        if (isServer)
        {
            Debug.Log($"im in server - preparing to spawn items");
            NetworkServer.SpawnObjects();
            RpcSpawnObjects();
        }
    }*/

    /*[ClientRpc]
    public void RpcSpawnObjects()
    {
        if (isClient)
        {
            setPlayersActive();
            Debug.Log($"Im in clientRPC - preparing to get spawned items");
            NetworkClient.PrepareToSpawnSceneObjects();
        }
    }

    [Command]
    public void setPlayersActive()
    {
        NetworkServer.SetClientReady(connectionToClient);
        Debug.Log($"im in server - setting client {connectionToClient} ready");
    }*/
    
    /*public override void OnStartClient()
    {
        base.OnStartClient();

        hand = GameObject.FindWithTag("Hand");
        tabletop = GameObject.FindWithTag("Tabletop");
        enemyHand = GameObject.FindWithTag("EnemyHand");
        enemytabletop = GameObject.FindWithTag("EnemyTabletop");
        mulliganPanel = GameObject.FindWithTag("MulliganPanel");
        if (hasAuthority)
        {
            //StartCoroutine(DelayedRegistration());
            //GameManager.Instance.ChangeGameState(GameManager.GameState.Mulligan);
        }
        if (GameManager.Instance.DebugMode)
        {
            isMyTurn = true;
            CmdChangeTurn();
        }
        if (isClientOnly)
        {
            isMyTurn = true;
            CmdChangeTurn();
        }
    }*/

    //private IEnumerator DelayedRegistration()
    //{
    //    while (GameManager.Instance == null)
    //    {
    //        yield return null;
    //    }
    //    GameManager.Instance.ChangeGameState(GameManager.GameState.Mulligan);
    //}

        
    public void SetPlayerReady()
    {
        CmdPlayerReady();
    }

    public void DealCards(int id)
    {
        if (GameManager.Instance.currentGameState == GameManager.GameState.Mulligan)
        {
            CmdMulliganCards(id);
        }
        else if (GameManager.Instance.currentGameState == GameManager.GameState.PlayerTurn)
        {
            CmdDealCards(id);
        }
        else
        {
            Debug.Log("CmdDealCards Unknown GameState");
        }
    }

    public void PlayCard(GameObject card, Transform placeholderParent, int index)
    {
        GameManager.Instance.ReloadText();
        CmdPlayCard(card, placeholderParent, index);
    }

    public void PreviewCard(int id)
    {
        //Debug.Log("PreviewCard cardToPreview is: " + cardToPreview);
        CmdSpawnPreview(id);
    }

    [Server]
    public override void OnStartServer()
    {

    }

    #region Commands

    [Command]
    public void CmdPlayerReady()
    {
        playersReady++;
        RpcPlayerReady(playersReady);
    }

    [ClientRpc]
    public void RpcPlayerReady(int currentPlayersReady)
    {
        if (currentPlayersReady == 2 && isLocalPlayer)
        {
            Debug.Log($"Players are {currentPlayersReady} and starting mulligan");
            GameManager.Instance.ChangeGameString("Mulligan");
        }
        else
        {
            Debug.Log($"Players are {currentPlayersReady} and waiting OR not local");
        }
    }

    [Command]
    public void CmdMulliganCards(int id)
    {
        foreach (Card card in CardDatabase.Instance.cardList)
        {
            if (card.id == id)
            {
                GameObject go;
                go = Instantiate(cardPrefab);
                NetworkServer.Spawn(go, connectionToClient);
                go.GetComponent<CardDisplay>().card = card;

                //Debug.Log("I've drawn a " + card);
                RpcShowCard(go, id, "Mulligan");
            }
        }
    }

    [Command]
    public void CmdDealCards(int id)
    {
        Debug.Log("Inside CMD DEAL CARDS");
        foreach (Card card in CardDatabase.Instance.cardList)
        {
            if (card.id == id)
            {
                GameObject go;
                go = Instantiate(cardPrefab);
                NetworkServer.Spawn(go, connectionToClient);
                go.GetComponent<CardDisplay>().card = card;
                Debug.Log("Im in Dealt");
                //Debug.Log("I've drawn a " + card);
                RpcShowCard(go, id, "Dealt");
            }
        }
    }

    [Command]
    public void CmdShuffle()
    {
        //for (int i = 0; i < deck.Count; i++)
        //{
        //    int randomIndex = Random.Range(i, deck.Count);
        //    tempCard = deck[i];
        //    deck[i] = deck[randomIndex];
        //    deck[randomIndex] = tempCard;
        //}
    }
    
    [Command]
    public void CmdReturnCardsToDeck()
    {

    }

    public void CmdZoomCard(GameObject cardToZoom, float posX, float posY, Quaternion rot)
    {

        zoomCard = Instantiate(cardPrefab, new Vector3(posX, posY), rot, hand.transform.parent);
        zoomCard.GetComponent<CardDisplay>().CopyStats(cardToZoom.GetComponent<CardDisplay>().card);
        //zoomCard.GetComponent<CardDisplay>().card = cardToZoom.GetComponent<CardDisplay>().card;
        zoomCard.GetComponent<CardDisplay>().setHealth(cardToZoom.GetComponent<CardDisplay>().GetHealth());
        // Debug.Log("zoomcard health: " + zoomCard.GetComponent<CardDisplay>().GetHealth());
        // Debug.Log("card to zoom gethealth: " + cardToZoom.GetComponent<CardDisplay>().GetHealth());
        //zoomCard.GetComponent<CardDisplay>().setAttack(cardToZoom.GetComponent<CardDisplay>().GetAttack());
        //NetworkServer.Spawn(zoomCard, connectionToClient);
        //Debug.Log(zoomCard.name);
        zoomCard.layer = LayerMask.NameToLayer("Zoom");
        TargetZoomCard(connectionToClient, zoomCard);

    }

    public void CmdDestroyZoomCard()
    {
       // Debug.Log("CmdDestroyZoomCard " + zoomCard.name);
        Destroy(zoomCard);
    }

    [Command]
    public void CmdPlayCard(GameObject card,Transform placeholderParent,int index)
    {
        RpcPlayCard(card, placeholderParent, index);
    }

    [Command]
    public void CmdSpawnPreview(int id)
    {
        Debug.Log("Before authority check, also id is: " + id);
        if (hasAuthority)
        {
            Debug.Log("CmdSpawnPreview id is: " + id);
            placeHolder = Instantiate(cardPrefab);
            NetworkServer.Spawn(placeHolder, connectionToClient);

            foreach (Card card in CardDatabase.Instance.cardList)
            {
                if (card.id == id)
                {
                    placeHolder.GetComponent<CardDisplay>().card = card;
                }
            }
        }
        else
        {
            Debug.Log("den exw authority , also id is: " + id);
        }
    }

    [Command]
    public void CmdChangeTurn()
    {
        RpcChangeTurn();
    }

    [Command]
    public void CmdApplyDamage(GameObject attacker, GameObject target)
    {
        RpcApplyDamage(attacker, target);
    }

    [Command]
    public void CmdDestroyFirstDraw()
    {
        RpcDestroyFirstDraw();
    }
    #endregion

    #region Client

    public void TargetZoomCard(NetworkConnection target, GameObject card)
    {
        if (hasAuthority)
        {
            //Debug.Log("TargetZoomCard " + this.netId + hasAuthority);
            //card.GetComponent<Image>().color = Color.red;
            card.GetComponent<CanvasGroup>().blocksRaycasts = false;
            card.GetComponent<Draggable>().enabled = false;
            RectTransform rect = card.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(card.GetComponent<RectTransform>().sizeDelta.x, card.GetComponent<RectTransform>().sizeDelta.y);
            rect.localScale = new Vector3(2, 2, 2);
        }
    }

    [ClientRpc]
    void RpcPlayCard(GameObject card, Transform placeholderParent, int index)
    {
        if (hasAuthority)
        {
            card.transform.SetParent(placeholderParent);
            card.transform.SetSiblingIndex(index);
        }
        else
        {
            if(placeholderParent == hand.transform)
            {
                Debug.Log("Eimai sto RPCplaycard, NO authority, enemyhand");
                card.transform.SetParent(enemyHand.transform, false);
                card.transform.SetSiblingIndex(index);
            }
            else if(placeholderParent == tabletop.transform)
            {
                Debug.Log("Eimai sto RPCplaycard, NO authority, enemytabletop");
                card.GetComponent<CanvasGroup>().blocksRaycasts = true;
                card.transform.SetParent(enemytabletop.transform, false);
                card.transform.Rotate(0f, 0f, 180f);
                card.transform.SetSiblingIndex(index);
                card.GetComponent<CardDisplay>().FlipCard();
            }
        }
    }

    [ClientRpc]
    void RpcShowCard(GameObject go,int id, string Type)
    {
        if (Type == "Dealt")
        {
            if (hasAuthority)
            {
                foreach (Card card in CardDatabase.Instance.cardList)
                {
                    if (card.id == id)
                    {
                        go.GetComponent<CardDisplay>().card = card;
                        
                    }
                }
                if (hand.transform.childCount < GameManager.Instance.maxCardsInHand)
                {
                    go.transform.SetParent(hand.transform, false);
                }
                else
                {//Here i can burn the card, because my hand is full
                    Debug.Log("HAND IS FULL!");
                    Destroy(go);
                } 

            }
            else
            {
                foreach (Card card in CardDatabase.Instance.cardList)
                {
                    if (card.id == id)
                    {
                        go.GetComponent<CardDisplay>().card = card;

                    }
                }
                if (enemyHand.transform.childCount < GameManager.Instance.maxCardsInHand)
                {
                    go.GetComponent<CanvasGroup>().blocksRaycasts = false;
                    go.transform.SetParent(enemyHand.transform, false);
                    go.GetComponent<CardDisplay>().FlipCard();
                }
                else
                {//Here i can burn the card, because my opponents hand is full
                    Debug.Log("ENEMY HAND IS FULL!");
                    Destroy(go);
                }
            }
        }
        else if (Type == "Mulligan")
        {
            if (hasAuthority)
            {
                Debug.Log("RPCshowcard Mulligan HAS authority");
                foreach (Card card in CardDatabase.Instance.cardList)
                {
                    if (card.id == id)
                    {
                        go.GetComponent<CardDisplay>().card = card; //does it really do something here?
                    }
                }
                go.transform.SetParent(mulliganPanel.transform, true);
                go.GetComponent<CanvasGroup>().blocksRaycasts = false;
            }
            else
            {
                Debug.Log("RPCshowcard Mulligan NO authority Destroy");
                //Destroy(go);
                foreach (Card card in CardDatabase.Instance.cardList)
                {
                    if (card.id == id)
                    {
                        go.GetComponent<CardDisplay>().card = card; //does it really do something here?
                    }
                }
                go.transform.SetParent(enemyHand.transform, false);
                go.GetComponent<CanvasGroup>().blocksRaycasts = false;
                go.GetComponent<CardDisplay>().FlipCard();
            }
        }
    }

    [ClientRpc]
    void RpcChangeTurn()
    {
        PlayerManager pm = NetworkClient.connection.identity.GetComponent<PlayerManager>();
        pm.isMyTurn = !(pm.isMyTurn);

        if (GameManager.Instance == null) return;
        //GameManager.Instance.currentGameState = GameManager.GameState.PlayerTurn;
        GameManager.Instance.ChangeGameState(GameManager.GameState.PlayerTurn);
        if (GameManager.Instance.maxMana < 10 && pm.isMyTurn)
            GameManager.Instance.maxMana++;
        Debug.Log($"max mana is {GameManager.Instance.maxMana}");
        GameManager.Instance.currentBattlePhase = GameManager.BattlePhase.None;
        
        GameManager.Instance.currentMana = GameManager.Instance.maxMana;
        GameManager.Instance.ReloadText();
    }

    [ClientRpc]
    public void RpcApplyDamage(GameObject attacker, GameObject target)
    {
        if (attacker.tag == "Card")//currently only cards can attack
        {
            //Animator anim2 = attacker.GetComponent<Animator>();
            //anim2.SetBool("isAttacking", true);
            //anim2.SetBool("isAttacking", false);

            if (target.tag == "Card")//if both are cards, both needs to be damaged
            {
                //    Debug.Log("attacker damage is: " + attacker.GetComponent<CardDisplay>().GetAttack());
                //    Debug.Log("target damage is: " + target.GetComponent<CardDisplay>().GetAttack());
                //    Debug.Log("attacker health is is: " + attacker.GetComponent<CardDisplay>().GetHealth());
                //    Debug.Log("target health is is: " + target.GetComponent<CardDisplay>().GetHealth());
                
                target.GetComponent<CardDisplay>().setHealth(target.GetComponent<CardDisplay>().GetHealth() - attacker.GetComponent<CardDisplay>().GetAttack());
                //Animator anim1 = target.GetComponent<Animator>();
                //anim1.SetBool("isAttacking", true);

                attacker.GetComponent<CardDisplay>().setHealth(attacker.GetComponent<CardDisplay>().GetHealth() - target.GetComponent<CardDisplay>().GetAttack());
                

                
                if (attacker.GetComponent<CardDisplay>().GetHealth() <= 0)
                {
                    attacker.GetComponent<Dissolve>().StartDissolving(); //attacker life is 0
                    //Destroy(attacker);
                    
                }
                if (target.GetComponent<CardDisplay>().GetHealth() <= 0)
                {
                    target.GetComponent<Image>().material = null;
                    target.GetComponent<Dissolve>().StartDissolving(); //target life is 0
                    //Destroy(target);
                    Destroy(zoomCard);
                }
                else
                {
                    if(zoomCard!=null)
                        zoomCard.GetComponent<CardDisplay>().setHealth(target.GetComponent<CardDisplay>().GetHealth());
                }
            }
            else if (target.tag == "EnemyPlayer")//if attacker is card BUT target is player
            {
                if (hasAuthority)//I am doing the attac so my opponent takes damage
                {
                    GameManager.Instance.enemyPortrait.GetComponent<PlayerPortrait>().TakeDamage(attacker.GetComponent<CardDisplay>().GetAttack());
                    //target.GetComponent<PlayerPortrait>().SetHealth(target.GetComponent<PlayerPortrait>().GetHealth() - attacker.GetComponent<CardDisplay>().GetAttack());

                    if (target.GetComponent<PlayerPortrait>().GetHealth() <= 0)
                    {
                        Debug.Log("I WON");
                        GameManager.Instance.ChangeGameState(GameManager.GameState.EndGame);
                        GameManager.Instance.WonGame();
                    }
                }
                else//I am receiving the attack so I have to take damage
                {
                    GameManager.Instance.playerPortrait.GetComponent<PlayerPortrait>().TakeDamage(attacker.GetComponent<CardDisplay>().GetAttack());
                    //GameManager.Instance.playerPortrait.GetComponent<PlayerPortrait>().SetHealth(GameManager.Instance.playerPortrait.GetComponent<PlayerPortrait>().GetHealth()- attacker.GetComponent<CardDisplay>().GetAttack());

                    if (GameManager.Instance.playerPortrait.GetComponent<PlayerPortrait>().GetHealth() <= 0)
                    {
                        Debug.Log("I Lost :(");
                        GameManager.Instance.ChangeGameState(GameManager.GameState.EndGame);
                        GameManager.Instance.LostGame();
                    }
                }
            }
        }
    }

    [ClientRpc]
    public void RpcDestroyFirstDraw()
    {//if i muligan, for the other player destroy the cards in his enemy hand (which is me)
        if (!hasAuthority) 
        {
            for (int i = enemyHand.transform.childCount - 1; i >= 0; --i)
            {
                Transform child = enemyHand.transform.GetChild(i);
                if (child.gameObject.tag == "Card")
                {
                    Destroy(child.gameObject);
                }
            }

        }
    }

    #endregion
    
    #region Host
    /*Host game commands and RPC's*/
    public void HostGame(bool publicMatch)
    {
        string matchID = MatchMaker.GetRandomMatchID();
        CmdHostGame(matchID,publicMatch);
    }

    [Command]
    void CmdHostGame(string _matchID, bool publicMatch)
    {
        matchID = _matchID;
        if(MatchMaker.instance.HostGame(_matchID, gameObject,publicMatch , out playerIndex))
        {
            Debug.Log($"<color=green>Game hosted successfully | ID: {_matchID}</color>");
            networkMatchChecker.matchId = _matchID.ToGuid();
            TargetHostGame(true,_matchID, playerIndex);
        }
        else
        {
            Debug.Log("<color=red>Game host failed | ID already exists!</color>");
            TargetHostGame(false,_matchID, playerIndex);
        }
    }

    [TargetRpc]
    void TargetHostGame(bool success,string _matchID, int _playerIndex)
    {
        this.playerIndex = _playerIndex;
        matchID = _matchID;
        //Debug.Log($"MatchID: {matchID} == {_matchID}");
        UILobby.Instance.HostSuccess(success, _matchID);
    }
    #endregion
    
    #region Join 
    /*Join game commands and RPC's*/
    public void JoinGame(string _inputID)
    {
        CmdJoinGame(_inputID);
    }

    [Command]
    void CmdJoinGame(string _matchID)
    {
        matchID = _matchID;
        if(MatchMaker.instance.JoinGame(_matchID, gameObject, out playerIndex))
        {
            //Debug.Log("<color=green>Game joined successfully</color>");
            networkMatchChecker.matchId = _matchID.ToGuid();
            TargetJoinGame(true,_matchID, playerIndex);
        }
        else
        {
            //Debug.Log("<color=red>Game join failed</color>");
            TargetJoinGame(false,_matchID, playerIndex);
        }
    }

    [TargetRpc]
    void TargetJoinGame(bool success,string _matchID,int _playerIndex)
    {
        this.playerIndex = _playerIndex;
        matchID = _matchID;
        //Debug.Log($"MatchID: {matchID} == {_matchID}");
        UILobby.Instance.JoinSuccess(success,_matchID);
    }
    #endregion

    #region SearhMatch

    public void SearchGame()
    {
        CmdSearchGame();
    }

    [Command]
    void CmdSearchGame()
    {
        if(MatchMaker.instance.SearchGame(gameObject, out playerIndex,out matchID))
        {
            //Debug.Log("<color=green>Game found</color>");
            networkMatchChecker.matchId = matchID.ToGuid();
            TargetSearchGame(true,matchID, playerIndex);
        }
        else
        {
            //Debug.Log("<color=red>Game not found</color>");
            TargetSearchGame(false,matchID, playerIndex);
        }
    }

    [TargetRpc]
    void TargetSearchGame(bool success,string _matchID,int _playerIndex)
    {
        this.playerIndex = _playerIndex;
        matchID = _matchID;
        Debug.Log($"MatchID: {matchID} == {_matchID}");
        UILobby.Instance.SearchSuccess(success,_matchID);
    }

    #endregion
    
    #region BeginGame
    /*Begin game commands and RPC's*/
    public void BeginGame()
    {
        CmdBeginGame();
    }
    
    [Command]
    void CmdBeginGame()
    {
        MatchMaker.instance.BeginGame(matchID);
        Debug.Log("<color=red>Game Beginning failed</color>");
    }

    public void StartGame(List<PlayerManager> _players, TurnManager _turnManager)
    {
        if(isServer)
        {
            diceRoll = Random.Range(0, 99);
        }
        TargetBeginGame(_players,_turnManager, _players[0].diceRoll);
    }

    /*[Command]
    public void CmdSetPlayersNotReady()
    {
        NetworkServer.SetClientNotReady(connectionToClient);
    }*/
    [TargetRpc]
    void TargetBeginGame(List<PlayerManager> _players,TurnManager _turnManager, int roll)
    {
        Debug.Log($"MatchID: {matchID} | Beginning");

        myTurnManager = _turnManager;
        _turnManager.players = _players;
        if (roll < 50)
        {
            _players[0].isMyTurn = true;
            _players[1].isMyTurn = false;
            Debug.Log($"<color=yellow> Host plays first </color>");
        }
        else
        {
            _players[0].isMyTurn = false;
            _players[1].isMyTurn = true;
            Debug.Log($"<color=yellow> Client plays first </color>");
        }

        if (isLocalPlayer)
        {
            Debug.Log("Setting client not ready");
            //CmdSetPlayersNotReady();
        }
        SceneManager.LoadScene(3, LoadSceneMode.Additive);
    }
    #endregion

    #region DisconnectGame

    public void DisconnectGame()
    {
        CmdDisconnectGame();
    }
    [Command]
    void CmdDisconnectGame()
    {
        ServerDisconnect();
    }

    void ServerDisconnect()
    {
        try
        {
            MatchMaker.instance.PlayerDisconnected(this,matchID);
            RpcDisconnectGame();
            networkMatchChecker.matchId = string.Empty.ToGuid();

        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
    [ClientRpc]
    void RpcDisconnectGame()
    {
        ClientDisconnect();
    }

    void ClientDisconnect()
    {
        if (playerLobbyUI != null)
        {
            Destroy(playerLobbyUI);
        }
    }

    #endregion

    #region DeckManagement 
    //not implemented right, needs rework when refactoring

    /*public void GetDeck(TurnManager turnManager)
    {
        CmdGetDeck(turnManager);
    }

    [Command]
    void CmdGetDeck(TurnManager turnManager)
    {
        Debug.Log($"hi from server. TM is {turnManager.name}");
        foreach (var id in DeckManager.Instance.currentSelectedDeck.PlayerDeck)
        {
           Debug.Log($"card id is: {id}");
        }

        //TargetGetDeck();
    }

    //[TargetRpc]
    /*void TargetGetDeck()
    {
        myTurnManager.deck = myDeck;
    }#1#*/
    #endregion
}
