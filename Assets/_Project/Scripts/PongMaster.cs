
using static LogUtility;
using UnityEngine;
using System.Net;

public class PongMaster : MonoBehaviour
{
    public enum Connection : byte
    {
        INVALID,
        REQUEST,
        ACCEPT,
        REJECT,
        DISCONNECT,
        UPDATE,
        HEARTBEAT
    }

    public enum Error
    {
        OK,
        WAITING_FOR_RESPONSE,
        CLIENT_NOT_ENABLED,
        SERVER_NOT_ENABLED,
        CLIENT_ALREADY_ENABLED,
        SERVER_ALREADY_ENABLED,
        MISSING_ACCESS_POINT,
        ACCESS_POINT_ERROR
    }

    [Header("References")]
    [SerializeField] private PongPaddle playerOnePaddle = null;
    [SerializeField] private PongPaddle playerTwoPaddle = null;
    [SerializeField] private PongBall pongBall = null;
    [SerializeField] private Scoreboard scoreboard = null;
    [SerializeField] private AccessPoint accessPoint = null;
    [SerializeField] private PacketBuffer bufferObject = null;
    [SerializeField] private PlayerInput localPlayerInput = null;
    [SerializeField] private GameObject connectUI = null;
    [SerializeField] private GameObject disconnectUI = null;

    [Header("Client configuration")]
    [InspectorButton("ToggleClient", ButtonWidth = 100.0f)]
#pragma warning disable 414
    [SerializeField] private bool _toggleClient = false;
#pragma warning restore 414
    public string connectToAddress = "127.0.0.1";
    [Range(1024, ushort.MaxValue)] public ushort connectToPort = 42069;

    [Header("Server configuration")]
    [InspectorButton("ToggleServer", ButtonWidth = 100.0f)]
#pragma warning disable 414
    [SerializeField] private bool _toggleServer = false;
#pragma warning restore 414
    [SerializeField] [Range(1024, ushort.MaxValue)] private ushort serverPort = 42069;

    [Header("Server status")]
    [SerializeField] [ReadOnly] private bool _isClientActive = false;
    public bool IsClientActive
    {
        get => _isClientActive;
        private set => _isClientActive = value;
    }
    [SerializeField] [ReadOnly] private bool _isClientConnected = false;
    public bool IsClientConnected
    {
        get => _isClientConnected;
        private set => _isClientConnected = value;
    }

    [SerializeField] [ReadOnly] private bool _isServerActive = false;
    public bool IsServerActive
    {
        get => _isServerActive;
        private set => _isServerActive = value;
    }

    [SerializeField] private float connectionForceTimeout = 10.0f;

    [SerializeField] [ReadOnly] private uint clientID = 0;
    [SerializeField] [ReadOnly] private ushort localPacketNumber = 0;
    private IPEndPoint serverEP = null;
    private ushort serverLatestPacket = 0;
    private float timeSinceLastServerMessage = 0.0f;

    [SerializeField] [ReadOnly] private bool playerOneConnected = false;
    private IPEndPoint playerOneEP = null;
    [SerializeField] [ReadOnly] private uint playerOneID = 0;
    private ushort playerOneLatestPacket = 0;
    private float timeSinceLastPlayerOneMessage = 0.0f;

    [SerializeField] [ReadOnly] private bool playerTwoConnected = false;
    private IPEndPoint playerTwoEP = null;
    [SerializeField] [ReadOnly] private uint playerTwoID = 0;
    private ushort playerTwoLatestPacket = 0;
    private float timeSinceLastPlayerTwoMessage = 0.0f;

    private void Start()
    {
        if (playerOnePaddle == null || playerTwoPaddle == null || pongBall == null || scoreboard == null)
        {
            LogError("Missing references! Game will NOT function correctly!");
        }

        if (Application.isBatchMode && !IsServerActive)
        {
            Application.targetFrameRate = 60;
            EnableServer(serverPort);
        }
    }

    private void OnDestroy()
    {
        if (IsClientActive) DisableClient();
        if (IsServerActive) DisableServer();
    }

    private void Update()
    {
        if (IsClientActive && IsClientConnected)
        {
            playerOnePaddle.GetComponent<MeshRenderer>().enabled = true;
            playerTwoPaddle.GetComponent<MeshRenderer>().enabled = true;
            pongBall.GetComponent<MeshRenderer>().enabled = true;
            scoreboard.GraphicsEnabled = true;
        }
        else
        {
            playerOnePaddle.GetComponent<MeshRenderer>().enabled = false;
            playerTwoPaddle.GetComponent<MeshRenderer>().enabled = false;
            pongBall.GetComponent<MeshRenderer>().enabled = false;
            scoreboard.GraphicsEnabled = false;
        }

        if (bufferObject == null) return;
        if (!IsClientActive && !IsServerActive) return;

        if (IsClientActive && IsClientConnected)
        {
            timeSinceLastServerMessage += Time.deltaTime;
            if (timeSinceLastServerMessage > connectionForceTimeout)
            {
                Log("Connection timed out. Disconnecting...");
                DisableClient();
                return;
            }
        }

        if (IsServerActive)
        {
            if (!playerOneConnected || !playerTwoConnected)
            {
                scoreboard.PlayerOneScore = 0;
                scoreboard.PlayerTwoScore = 0;
                pongBall.allowFixedUpdate = false;
                pongBall.ResetBall();
            }
            else
            {
                pongBall.allowFixedUpdate = true;
            }

            if (pongBall.Position.x < playerOnePaddle.transform.position.x - 5.0f)
            {
                scoreboard.PlayerOneScore++;
                pongBall.ResetBall();
            }
            if (pongBall.Position.x > playerTwoPaddle.transform.position.x + 5.0f)
            {
                scoreboard.PlayerTwoScore++;
                pongBall.ResetBall();
            }

            if (playerOneConnected)
            {
                timeSinceLastPlayerOneMessage += Time.deltaTime;
                if (timeSinceLastPlayerOneMessage > connectionForceTimeout)
                {
                    Log("Player one timed out.");

                    byte[] message = new byte[1];
                    message[0] = (byte)Connection.DISCONNECT;
                    accessPoint.SendMessage(message, playerOneEP);

                    playerOneEP = null;
                    playerOneID = 0;
                    playerOneConnected = false;
                }
            }

            if (playerTwoConnected)
            {
                timeSinceLastPlayerTwoMessage += Time.deltaTime;
                if (timeSinceLastPlayerTwoMessage > connectionForceTimeout)
                {
                    Log("Player two timed out.");

                    byte[] message = new byte[1];
                    message[0] = (byte)Connection.DISCONNECT;
                    accessPoint.SendMessage(message, playerTwoEP);

                    playerTwoEP = null;
                    playerTwoID = 0;
                    playerTwoConnected = false;
                }
            }
        }

        Packet[] packets = bufferObject.RetrieveAll();

        if (packets.Length > 0)
        {
            for (int i = 0; i < packets.Length; i++)
            {
                byte[] response;

                switch (packets[i].message[packets[i].bytesRead++])
                {
                    #region REQUEST

                    case (byte)Connection.REQUEST:
                        response = new byte[1 + sizeof(uint)];
                        if (IsServerActive)
                        {
                            // The local machine is a server.
                            // Check if there are available slots for players.

                            if (playerOneConnected && playerTwoConnected)
                            {
                                response[0] = (byte)Connection.REJECT;
                            }
                            else
                            {
                                uint playerID = (uint)Random.Range(uint.MinValue, uint.MaxValue);
                                if (!playerOneConnected)
                                {
                                    playerOneEP = packets[i].endPoint;
                                    playerOneID = playerID;
                                    playerOneLatestPacket = 0;
                                    byte[] id = System.BitConverter.GetBytes(playerID);
                                    System.Buffer.BlockCopy(id, 0, response, 1, id.Length);
                                    playerOneConnected = true;
                                    Log("Player one connected. (" + packets[i].endPoint.ToString() + ")");
                                }
                                else
                                {
                                    playerTwoEP = packets[i].endPoint;
                                    playerTwoID = playerID;
                                    playerTwoLatestPacket = 0;
                                    byte[] id = System.BitConverter.GetBytes(playerID);
                                    System.Buffer.BlockCopy(id, 0, response, 1, id.Length);
                                    playerTwoConnected = true;
                                    Log("Player two connected. (" + packets[i].endPoint.ToString() + ")");
                                }
                                response[0] = (byte)Connection.ACCEPT;
                            }
                        }
                        else
                        {
                            // The local machine is a client.
                            // Reject automatically.
                            response[0] = (byte)Connection.REJECT;
                        }

                        accessPoint.SendMessage(response, packets[i].endPoint);

                        break;

                    #endregion
                    #region ACCEPT

                    case (byte)Connection.ACCEPT:
                        if (IsClientActive && !IsClientConnected)
                        {
                            serverEP = packets[i].endPoint;
                            clientID = System.BitConverter.ToUInt32(packets[i].message, (int)packets[i].bytesRead);
                            packets[i].bytesRead += sizeof(uint);
                            IsClientConnected = true;
                            Log("Connection accepted from " + packets[i].endPoint.ToString() + ".");
                        }

                        break;

                    #endregion
                    #region REJECT

                    case (byte)Connection.REJECT:
                        if (IsClientActive)
                        {
                            DisableClient();
                        }
                        break;

                    #endregion
                    #region DISCONNECT

                    case (byte)Connection.DISCONNECT:
                        if (IsClientActive)
                        {
                            // Server forces client to disconnect.
                            if (packets[i].endPoint == serverEP) DisableClient();
                        }

                        if (IsServerActive)
                        {
                            // Client disconnects from server.
                            uint playerID = System.BitConverter.ToUInt32(packets[i].message, (int)packets[i].bytesRead);
                            packets[i].bytesRead += sizeof(uint);
                            if (playerID == playerOneID)
                            {
                                playerOneConnected = false;
                                playerOneID = 0;
                                playerOneEP = null;
                                Log("Player one disconnected. (" + packets[i].endPoint.ToString() + ")");
                            }
                            else if (playerID == playerTwoID)
                            {
                                playerTwoConnected = false;
                                playerTwoID = 0;
                                playerTwoEP = null;
                                Log("Player two disconnected. (" + packets[i].endPoint.ToString() + ")");
                            }
                        }

                        break;

                    #endregion
                    #region UPDATE

                    case (byte)Connection.UPDATE:
                        if (IsClientActive)
                        {
                            timeSinceLastServerMessage = 0.0f;

                            ushort packetNumber = System.BitConverter.ToUInt16(packets[i].message, (int)packets[i].bytesRead);
                            packets[i].bytesRead += sizeof(ushort);

                            if (packetNumber > serverLatestPacket)
                            {
                                float playerOnePosition = System.BitConverter.ToSingle(packets[i].message, (int)packets[i].bytesRead);
                                packets[i].bytesRead += sizeof(float);
                                float playerTwoPosition = System.BitConverter.ToSingle(packets[i].message, (int)packets[i].bytesRead);
                                packets[i].bytesRead += sizeof(float);

                                playerOnePaddle.SetPosition(playerOnePosition);
                                playerTwoPaddle.SetPosition(playerTwoPosition);

                                scoreboard.PlayerOneScore = packets[i].message[packets[i].bytesRead++];
                                scoreboard.PlayerTwoScore = packets[i].message[packets[i].bytesRead++];

                                Vector2 ballPosition = new Vector2();
                                ballPosition.x = System.BitConverter.ToSingle(packets[i].message, (int)packets[i].bytesRead);
                                packets[i].bytesRead += sizeof(float);
                                ballPosition.y = System.BitConverter.ToSingle(packets[i].message, (int)packets[i].bytesRead);
                                packets[i].bytesRead += sizeof(float);
                                pongBall.Position = ballPosition;
                            }
                            else
                            {
                                packets[i].bytesRead += sizeof(float) * 2;
                            }
                        }

                        if (IsServerActive)
                        {
                            uint playerID = System.BitConverter.ToUInt32(packets[i].message, (int)packets[i].bytesRead);
                            packets[i].bytesRead += sizeof(uint);
                            if (playerID == playerOneID)
                            {
                                ushort packetNumber = System.BitConverter.ToUInt16(packets[i].message, (int)packets[i].bytesRead);
                                if (packetNumber > playerOneLatestPacket)
                                {
                                    packets[i].bytesRead += sizeof(ushort);
                                    float position = System.BitConverter.ToSingle(packets[i].message, (int)packets[i].bytesRead);
                                    playerOnePaddle.SetPosition(position);
                                    packets[i].bytesRead += sizeof(float);
                                    playerOneLatestPacket = packetNumber;
                                }
                                else
                                {
                                    packets[i].bytesRead += sizeof(ushort) + sizeof(float);
                                }

                                timeSinceLastPlayerOneMessage = 0.0f;
                            }
                            else if (playerID == playerTwoID)
                            {
                                ushort packetNumber = System.BitConverter.ToUInt16(packets[i].message, (int)packets[i].bytesRead);
                                if (packetNumber > playerTwoLatestPacket)
                                {
                                    packets[i].bytesRead += sizeof(ushort);
                                    float position = System.BitConverter.ToSingle(packets[i].message, (int)packets[i].bytesRead);
                                    playerTwoPaddle.SetPosition(position);
                                    packets[i].bytesRead += sizeof(float);
                                    playerTwoLatestPacket = packetNumber;
                                }
                                else
                                {
                                    packets[i].bytesRead += sizeof(ushort) + sizeof(float);
                                }

                                timeSinceLastPlayerTwoMessage = 0.0f;
                            }
                        }

                        break;

                    #endregion
                    #region HEARTBEAT

                    case (byte)Connection.HEARTBEAT:
                        if (IsClientActive)
                        {
                            timeSinceLastServerMessage = 0.0f;
                        }

                        if (IsServerActive)
                        {
                            uint playerID = System.BitConverter.ToUInt32(packets[i].message, (int)packets[i].bytesRead);
                            if (playerID == playerOneID)
                            {
                                timeSinceLastPlayerOneMessage = 0.0f;
                            }
                            else if (playerID == playerTwoID)
                            {
                                timeSinceLastPlayerTwoMessage = 0.0f;
                            }
                        }

                        break;

                    #endregion
                    #region DEFAULT

                    default:
                        LogWarning("Unknown message type received!");
                        break;

                        #endregion
                }
            }
        }
    }

    private void LateUpdate()
    {
        if (IsClientActive && IsClientConnected)
        {
            if (Application.isBatchMode) return;
            if (localPlayerInput == null) return;

            byte[] updateMessage = new byte[1 + sizeof(uint) + sizeof(ushort) + sizeof(float)];
            updateMessage[0] = (byte)Connection.UPDATE;
            System.Buffer.BlockCopy(System.BitConverter.GetBytes(clientID), 0, updateMessage, 1, sizeof(uint));
            System.Buffer.BlockCopy(System.BitConverter.GetBytes(localPacketNumber++), 0, updateMessage, 1 + sizeof(uint), sizeof(ushort));
            System.Buffer.BlockCopy(System.BitConverter.GetBytes(localPlayerInput.Position.y), 0, updateMessage, 1 + sizeof(uint) + sizeof(ushort), sizeof(float));
            accessPoint.SendMessage(updateMessage, serverEP);
        }

        if (IsServerActive)
        {
            if (!playerOneConnected && !playerTwoConnected) return;

            byte[] updateMessage = new byte[1 + sizeof(ushort) + sizeof(float) * 2 + 2 + sizeof(float) * 2];
            int counter = 0;

            updateMessage[0] = (byte)Connection.UPDATE;
            counter += 1;

            System.Buffer.BlockCopy(System.BitConverter.GetBytes(localPacketNumber++), 0, updateMessage, counter, sizeof(ushort));
            counter += sizeof(ushort);

            System.Buffer.BlockCopy(System.BitConverter.GetBytes(playerOnePaddle.GetPosition()), 0, updateMessage, counter, sizeof(float));
            counter += sizeof(float);

            System.Buffer.BlockCopy(System.BitConverter.GetBytes(playerTwoPaddle.GetPosition()), 0, updateMessage, counter, sizeof(float));
            counter += sizeof(float);

            updateMessage[counter++] = scoreboard.PlayerOneScore;
            updateMessage[counter++] = scoreboard.PlayerTwoScore;

            System.Buffer.BlockCopy(System.BitConverter.GetBytes(pongBall.Position.x), 0, updateMessage, counter, sizeof(float));
            counter += sizeof(float);
            System.Buffer.BlockCopy(System.BitConverter.GetBytes(pongBall.Position.y), 0, updateMessage, counter, sizeof(float));
            counter += sizeof(float);

            if (playerOneConnected) accessPoint.SendMessage(updateMessage, playerOneEP);
            if (playerTwoConnected) accessPoint.SendMessage(updateMessage, playerTwoEP);
        }
    }

    private void ToggleClient()
    {
        if (Application.isEditor && !Application.isPlaying)
        {
            LogWarning("Editor needs to be in play mode!");
            return;
        }

        if (!IsClientActive) EnableClient();
        else DisableClient();
    }

    private void ToggleServer()
    {
        if (Application.isEditor && !Application.isPlaying)
        {
            LogWarning("Editor needs to be in play mode!");
            return;
        }

        if (!IsServerActive) EnableServer(serverPort);
        else DisableServer();
    }

    public Error EnableClient()
    {
        if (accessPoint == null)
        {
            LogError("Access point is missing!");
            return Error.MISSING_ACCESS_POINT;
        }

        if (IsClientActive)
        {
            LogWarning("Client is already enabled!");
            return Error.CLIENT_ALREADY_ENABLED;
        }

        if (IsServerActive)
        {
            LogError("Can't enable client while server is active!");
            return Error.SERVER_ALREADY_ENABLED;
        }

        timeSinceLastServerMessage = 0.0f;
        localPacketNumber = 0;
        IsClientActive = true;

        if (!accessPoint.IsActive)
        {
            if (!accessPoint.Listen((ushort)Random.Range(ushort.MinValue, ushort.MaxValue)))
            {
                LogError("Couldn't activate access point!");
                DisableClient();
                return Error.ACCESS_POINT_ERROR;
            }
        }

        if (!IPAddress.TryParse(connectToAddress, out IPAddress ip))
        {
            Debug.LogError("Couldn't parse IP address!");
            DisableClient();
            return Error.ACCESS_POINT_ERROR;
        }

        byte[] connectionRequest = new byte[1];
        connectionRequest[0] = (byte)Connection.REQUEST;
        accessPoint.SendMessage(connectionRequest, new IPEndPoint(ip, connectToPort));
        connectUI.SetActive(false);
        disconnectUI.SetActive(true);

        Log("Waiting for response...");

        return Error.WAITING_FOR_RESPONSE;
    }

    public Error DisableClient()
    {
        if (accessPoint == null)
        {
            LogError("Access point is missing!");
            return Error.MISSING_ACCESS_POINT;
        }

        if (!IsClientActive)
        {
            LogWarning("Client hasn't been enabled yet!");
            return Error.CLIENT_NOT_ENABLED;
        }

        if (IsClientConnected)
        {
            byte[] disconnectMessage = new byte[1 + sizeof(uint)];
            disconnectMessage[0] = (byte)Connection.DISCONNECT;
            System.Buffer.BlockCopy(System.BitConverter.GetBytes(clientID), 0, disconnectMessage, 1, sizeof(uint));
            accessPoint.SendMessage(disconnectMessage, serverEP);
            IsClientConnected = false;
        }

        accessPoint.Close();

        clientID = 0;
        IsClientActive = false;
        disconnectUI.SetActive(false);
        connectUI.SetActive(true);
        Log("Client disabled successfully.");
        return Error.OK;
    }

    public Error EnableServer(ushort portNumber)
    {
        if (accessPoint == null)
        {
            LogError("Access point is missing!");
            return Error.MISSING_ACCESS_POINT;
        }
        
        if (IsServerActive)
        {
            LogWarning("Server is already enabled!");
            return Error.SERVER_ALREADY_ENABLED;
        }

        if (IsClientActive)
        {
            LogError("Can't enable server while client is active!");
            return Error.CLIENT_ALREADY_ENABLED;
        }

        if (accessPoint.IsActive)
        {
            accessPoint.Close();
        }

        if (!accessPoint.Listen(serverPort))
        {
            LogError("Couldn't activate access point!");
            return Error.ACCESS_POINT_ERROR;
        }

        timeSinceLastPlayerOneMessage = 0.0f;
        timeSinceLastPlayerTwoMessage = 0.0f;
        localPacketNumber = 0;
        IsServerActive = true;
        Log("Server enabled successfully.\nIf you're running in batchmode, press CTRL + C to close the server.");
        return Error.OK;
    }

    public Error DisableServer()
    {
        if (!IsServerActive)
        {
            LogWarning("Server hasn't been enabled yet!");
            return Error.SERVER_NOT_ENABLED;
        }

        if (accessPoint == null)
        {
            LogError("Access point is missing!");
            return Error.MISSING_ACCESS_POINT;
        }

        if (IsClientActive) DisableClient();
        accessPoint.Close();

        IsServerActive = false;
        Log("Server disabled successfully.");
        return Error.OK;
    }
}
