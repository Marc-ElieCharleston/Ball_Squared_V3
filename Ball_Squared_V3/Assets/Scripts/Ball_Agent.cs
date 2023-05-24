using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class Ball_Agent : Agent
{
    public GameObject Target; // permet de récuperer l'objet dans le script
    private GameObject targetInstance; // Permet d'utiliser l'instance de l'objet

    // Gestion des positions sur les axes X et Z
    // Positions de l'apparition de l'agent sur une des quatres plateformes
    private Vector3 agentInitialPositionTop = new Vector3(0, 0, 5.5f);
    private Vector3 agentInitialPositionBot = new Vector3(0, 0, -5.5f);
    private Vector3 agentInitialPositionRight = new Vector3(-5.5f, 0, 0);
    private Vector3 agentInitialPositionLeft = new Vector3(5.5f, 0, 0);
    // Position de l'apparition des targets sur la plateforme principale
    private float targetXPosition = 4.5f;
    private float targetZPosition = 4.5f;

    // Gestion des actions 
    private float horizontalInput;
    private float verticalInput;

    // Gestion des observations
    public float agentPositionX;
    public float agentPositionZ;
    public float targetPositionX;
    public float targetPositionZ;

    // Paramètres
    private float speed = 4f;
    private float YfallLimit = -2.0f; // limite sur l'axe des Y si la boule tombe
    public float maxStep = 5000;
    public float currentStepCounter;
    public float currentReward;

    // Objectif : On va ajouter l'objectif de toucher 5 target le temps d'un épisode
    public float targetCounter;
    private float targetObjectif = 5.0f;

    /// <summary>
    /// Méthode  qui s'active au démarrage de la simulation
    /// </summary>
    void Start()
    {
        
    }

    /// <summary>
    /// Méthode qui s'active à chaque début d'épisode
    /// </summary>
    public override void OnEpisodeBegin()
    {
        Debug.Log("start");
        
        // La position de l'agent doit se trouver sur une des 4 plateformes
        MoveAgentOnRandomPlatform();

        // on réinitialise le compteur d'objectif
        targetCounter = 0;
        currentStepCounter = 0;
        currentReward = 0;

        // Restart la vélocité de l'agent
        this.GetComponent<Rigidbody>().velocity = new Vector3();

        // On va vérifier si un target est déja sur le terrain au début de l'episode
        if (targetInstance != null)
        {
            // Si oui, oui la détruit avant d'instancier une nouvelle
            Destroy(targetInstance);
        }
        // appel de la méthode spawnTarget à chaque début d'épisode
        SpawnTargets();
    }


    /// <summary>
    /// Méthode qui permet de récupérer les observation à chaque 'step'
    /// </summary>
    public override void CollectObservations(VectorSensor sensor)
    {
        Debug.Log("Collect Observation");
        // On récupère la position actuelle de l'agent
        Vector3 agentCurrentPosition = transform.position;
        // Puis on récupère la position X et Z car on ne vas pas se servir de l'axe Y (hauteur)
        agentPositionX = agentCurrentPosition.x;
        agentPositionZ = agentCurrentPosition.z;

        // On récupère la position de la target (dans notre env, il n'y en aura qu'une à la fois)
        Vector3 targetCurrentPosition = targetInstance.transform.position;
        targetPositionX = targetCurrentPosition.x;
        targetPositionZ = targetCurrentPosition.z;

        float distanceX = Mathf.Abs(targetPositionX - agentPositionX);
        float distanceZ = Mathf.Abs(targetPositionZ - agentPositionZ);

        // Une fois les données récupérées, on les ajoutes dans les observations
        sensor.AddObservation(agentPositionX);
        sensor.AddObservation(agentPositionZ);
        sensor.AddObservation(targetPositionX);
        sensor.AddObservation(targetPositionZ);
        sensor.AddObservation(distanceX);
        sensor.AddObservation(distanceZ);
    }

    // Méthode qui permet à l'agent d'effectuer les actions
    public override void OnActionReceived(ActionBuffers actions)
    {
        
        
        // On crée les deux actions continues de notre agent
        // action 1 : +1 -> aller en haut / -1 -> aller en bas
        // action 2 : +1 -> aller à droite / -1 -> aller à gauche
        float verticalInput = actions.ContinuousActions[0];
        float horizontalInput = actions.ContinuousActions[1];

        Debug.Log("vertical value" + verticalInput);
        Debug.Log("vertical value" + horizontalInput);

        // Gestion du déplacement vertical (avant/arrière) de l'agent
        transform.Translate(Vector3.forward * verticalInput * speed * Time.deltaTime);

        // Gestion du déplacement horizontal (droite/gauche) de l'agent
        transform.Translate(Vector3.right * horizontalInput * speed * Time.deltaTime);

        // Incrémentation du compteur de step
        currentStepCounter += 1;

        // Gestion des rewards
        //  -0.01 reward  par pas effectué avec la méthode "SetReward"
        SetReward(-0.005f);
        currentReward += -0.01f;

        // Si l'agent tombe de la plateform, -1.0 reward
        if (transform.position.y < YfallLimit) 
        {
            // Plus besoin de detruire la target car tomber ne met plus fin à l'épisode
            SetReward(- 1.00f);
            currentReward += -1.0f;

            MoveAgentOnRandomPlatform();
        }

        // Si l'agent atteint l'objectif il gagne un gros reward
        if (targetCounter == targetObjectif)
        {
            SetReward(5.0f);
            Destroy(targetInstance);
            EndEpisode();
        }
        // Si l'agent n'atteint pas l'objectif avec la durée max : -1.0 reward
        if (maxStep == currentStepCounter)
        {
            SetReward(-1.0f);
            currentReward += -1.0f;
            EndEpisode();
        }

    }


    /// <summary>
    /// Méthode qui permet de controler l'agent manuellement
    /// </summary>
    /// <param name="actionsOut"></param>
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        Debug.Log("Heuristic ?");
        // Création d'une variable qui représente les actions continues de l'agent
        var agentContinuousAction = actionsOut.ContinuousActions;

        // Déplacement vertical (avancer/reculer) : permet de controller l'agent avec les flèches du clavier
        float verticalInputPlayer = Input.GetAxis("Vertical");
        agentContinuousAction[0] = verticalInputPlayer; // renvoyer à l'agent  l'action vertical que l'on vient d'effectuer

        // Déplacement horizontal (droite/gauche) : idem
        float horizontalInputPlayer = Input.GetAxis("Horizontal");
        agentContinuousAction[1] = horizontalInputPlayer;
    }


    /// <summary>
    /// Méthode qui fera apparaitre une target lorsqu'on l'appel
    /// </summary>
    private void SpawnTargets()
    {
        Debug.Log("SpwanTarget");
        // On va définir les limits de l'apparition aléatoire de la target sur l'axe X et Z
        float targetX = Random.Range(-targetXPosition, targetXPosition); // de -5.5 à +5.5 sur l'axe X
        float targetZ = Random.Range(-targetZPosition, targetZPosition); // de -5.5 à +5.5 sur l'axe Z

        // Création de la position aléatoire de la target (x, y, z)
        Vector3 spawnPosition = new Vector3(targetX, 0, targetZ);

        // On va instancier une target sur une position aléatoire dans une variable
        // Cela permet de détruire l'instance de l'objet, mais pas l'objet en lui même
        targetInstance = Instantiate(Target, spawnPosition, Quaternion.identity);
    }


    /// <summary>
    /// Méthode qui gère la collision entre les objets
    /// </summary>
    /// <param name="collision"></param>
    private void OnCollisionEnter(Collision collision)
    {
        // On va utiliser cette méthode pour faire disparaitre la target lors de la collision
        // et donner des rewards à l'agent en même temps

        // Si l'agent à une collision avec un objet contenant le tag 'target'
        if (collision.gameObject.CompareTag("Target"))
        {
            // Détruit l'objet qui à eu la collision avec l'agent
            Destroy(collision.gameObject);

            // Donne +1.0 reward
            SetReward(1.0f);
            targetCounter += 1;

            SpawnTargets();

            
        }
    }

    /// <summary>
    /// On crée une méthode qui permet à lagent en début d'épisode, ou lorsqu'il tombe
    /// de réapparaitre sur un des 4 points de spawn
    /// </summary>
    private void MoveAgentOnRandomPlatform()
    {
        Debug.Log("MoveRandomAgent");
        // Création d'une liste qui contient nos 4 vecteurs
        List<Vector3> agentSpawnPositions = new List<Vector3>
        {
            agentInitialPositionTop,
            agentInitialPositionBot,
            agentInitialPositionLeft,
            agentInitialPositionRight,
        };

        // Choix aléatoire d'une des 4 plateforme
        Vector3 randomPlatform = agentSpawnPositions[Random.Range(0, agentSpawnPositions.Count)];

        // Déplacement de l'agent sur la plateform choisit
        transform.position = randomPlatform;
    }

}
