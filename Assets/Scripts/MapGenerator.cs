using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Assertions;

public class MapGenerator : MonoBehaviour
{
    //Room Object
    //struct

    [SerializeField]
    GameObject[] RoomPrefabs;
    [SerializeField]
    GameObject SpawnRoomPrefab;
    [SerializeField]
    Material EndRoomMaterial;

    [SerializeField]
    Material[] RoomMaterials;

    [SerializeField]
    float[] RoomMaterialProabilities;

    RoomConfig[] RoomPrefabConfigs = new RoomConfig[25]; //Update size if # of prefab rooms > size value

    [SerializeField]
    [Range(1, 20)]
    private int mHeight, mWidth;

    [SerializeField]
    private int doorChance = 50;

    private int tileSize = 10;

    private GameObject[,] mMapContents = new GameObject[20, 20];
    private RoomConfig[,] mMapConfig = new RoomConfig[20, 20];

    [SerializeField]
    private GameObject playerAvatar;

    bool contentChanged = false; //Dirty flag for update cycle

    Vector3 mPreviousPlayerPosition = new Vector3();

    void Start()
    {
        int index = 0;
        foreach (GameObject prefab in RoomPrefabs)
        {
            GameObject obj = Instantiate(prefab, new Vector3(0, 0, 0), Quaternion.identity);
            RoomPrefabConfigs[index] = (RoomConfig)obj.GetComponent(typeof(RoomConfig));
            RoomPrefabConfigs[index].mRoomType = index;
            Destroy(obj);
            index++;
        }

        //Fill map with game objects to track components
        GameObject mapTileHolder = new GameObject("MapTiles");
        for (int i = 0; i < mHeight; i++)
        {
            for (int j = 0; j < mHeight; j++)
            {
                string objName = i + "_" + j;
                mMapContents[i, j] = new GameObject(objName);
                mMapContents[i, j].transform.SetParent(mapTileHolder.transform);
            }
        }

        Vector3 spawnTileLocation = GenerateRandomRoomLocation(true);
        GameObject spawnTile = Instantiate(SpawnRoomPrefab, tileSize * spawnTileLocation, Quaternion.identity);
        spawnTile.transform.SetParent(mMapContents[(int)spawnTileLocation.x, (int)spawnTileLocation.z].transform);
        mMapConfig[(int)spawnTileLocation.x, (int)spawnTileLocation.z] = (RoomConfig)spawnTile.GetComponent(typeof(RoomConfig));
        mMapConfig[(int)spawnTileLocation.x, (int)spawnTileLocation.z].mIsConstructed = true;

        playerAvatar.transform.position = new Vector3(tileSize * spawnTileLocation.x, 3, tileSize * spawnTileLocation.z);
        mPreviousPlayerPosition = FindPlayerPosition();

        CriticalPathGenerator(spawnTileLocation);

        contentChanged = true;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 newPlayerPosition = FindPlayerPosition();
        if(newPlayerPosition != mPreviousPlayerPosition)
        {
            mPreviousPlayerPosition = newPlayerPosition;
            contentChanged = true;
        }

        if(contentChanged)
        {
            AssessMapState(newPlayerPosition);
            if(contentChanged)
            {
                CreateNewTiles(newPlayerPosition);
                contentChanged = false;
            }
        }
    }

    private void AssessMapState(Vector3 playerPosition)
    {
        for (int i = 0; i <= 4; i++)
        {
            for (int j = 0; j <= 4; j++)
            {
                if (i != 2 || j != 2) //Don't assess current tile; Even upon spawn, the current tile is accounted for
                {
                    int x = (int)playerPosition.x + (i - 2);
                    int z = (int)playerPosition.z + (j - 2);

                    if(x > -1 && z > -1 && x < mWidth && z < mHeight)
                    {
                        if(mMapConfig[x, z] is null)
                        {
                            AddPrefabData(x, z);
                            contentChanged = true;
                        }
                    }
                }
            }
        }
    }

    private void AddPrefabData(int x, int z)
    {
        RoomConfig roomType = GetRoomType(x, z);

        roomType.mRoomLocation = new Vector3(x, 0, z);

        //Set Room Color
        if(!roomType.mNoMaterial)
        {
            var probability = Random.Range(0, 100);
            float totalColorProb = SumArray(RoomMaterialProabilities);

            int index = 0;
            float sum = 0f;
            foreach(float colorProb in RoomMaterialProabilities)
            {
                sum += colorProb;
                if(probability < (sum/totalColorProb) * 100)
                {
                    roomType.mMaterial = RoomMaterials[index];
                    break;
                }
                index++;
            }
        }

        mMapConfig[x, z] = roomType;
    }

    private RoomConfig GetRoomType(int x, int z)
    {
        int[] doors = GetDoorRequirements(x, z);
        return _GetRoomType(x, z, doors);
    }

    private RoomConfig _GetRoomType(int x, int z, int[] doors)
    {
        RoomConfig[] possibleRooms = new RoomConfig[25]; //Increase if number of room types increases above 25
        int possibleRoomIndex = 0;
        foreach(RoomConfig room in RoomPrefabConfigs)
        {
            if(room is null)
            {
                continue;
            }
            bool validRoom = true;
            for(int i = 0; i < 4; i++)
            {
                if(doors[i] != room.mDoorLocations[i])
                {
                    validRoom = false;
                }
            }

            if(validRoom)
            {
                possibleRooms[possibleRoomIndex] = room;
                possibleRoomIndex++;
            }
        }

        if(possibleRoomIndex == 0)
        {
            return RoomPrefabConfigs[0];
        }

        RoomConfig returnRoom = new RoomConfig(possibleRooms[0]); //DO PROBABILITY SELECTION YOU DUMMASS
        return returnRoom;
    }

    private int[] GetDoorRequirements(int x, int z)
    {
        // -1: No data; 0: No door; 1: Door;
        int[] doors = { -1, -1, -1, -1};

        if (z == mHeight -1)
        {
            doors[0] = 0;
        }
        else
        {
            RoomConfig neighborRoom = mMapConfig[x, z + 1];
            if(!(neighborRoom is null))
            {
                doors[0] = mMapConfig[x, z + 1].mDoorLocations[2];
            }
            else
            {
                doors[0] = RandomDoorSelect() ? 1 : 0;
            }
        }

        if (x == mWidth - 1)
        {
            doors[1] = 0;
        }
        else
        {
            RoomConfig neighborRoom = mMapConfig[x + 1, z];
            if(!(neighborRoom is null))
            {
                doors[1] = mMapConfig[x + 1, z].mDoorLocations[3];
            }
            else
            {
                doors[1] = RandomDoorSelect() ? 1 : 0;
            }
        }

        if (z == 0)
        {
            doors[2] = 0;
        }
        else
        {
            RoomConfig neighborRoom = mMapConfig[x, z - 1];
            if(!(neighborRoom is null))
            {
                doors[2] = mMapConfig[x, z - 1].mDoorLocations[0];
            }
            else
            {
                doors[2] = RandomDoorSelect() ? 1 : 0;
            }
        }

        if (x == 0)
        {
            doors[3] = 0;
        }
        else
        {
            RoomConfig neighborRoom = mMapConfig[x - 1, z];
            if(!(neighborRoom is null))
            {
                doors[3] = mMapConfig[x - 1, z].mDoorLocations[1];
            }
            else
            {
                doors[3] = RandomDoorSelect() ? 1 : 0;
            }
        }
        return doors;
    }

    private bool RandomDoorSelect()
    {
        return Random.Range(0, 100) > doorChance;
    }

    private void CreateNewTiles(Vector3 playerPosition)
    {
        for (int i = 0; i <= 4; i++)
        {
            for (int j = 0; j <= 4; j++)
            {
                int x = (int)playerPosition.x + (i - 2);
                int z = (int)playerPosition.z + (j - 2);

                if (x > -1 && z > -1 && x < mWidth && z < mHeight)
                {
                    RoomConfig config = mMapConfig[x, z];
                    if (!(config is null) && config.mRoomType != -1 && !config.mIsConstructed)
                    {
                        GameObject spawnTile = Instantiate(RoomPrefabs[config.mRoomType], tileSize * config.mRoomLocation, Quaternion.identity);
                        spawnTile.transform.SetParent(mMapContents[(int)config.mRoomLocation.x, (int)config.mRoomLocation.z].transform);

                        if (config.mNumOfDoors > 0)
                        {
                            GameObject floorObject = spawnTile.transform.Find("Floor").gameObject;
                            if (floorObject)
                            {
                                Renderer rend = floorObject.GetComponent<Renderer>();
                                if (rend)
                                {
                                    rend.material = config.mMaterial;
                                }
                            }
                        }

                        config.mIsConstructed = true;
                    }
                }
            }
        }
    }

    private Vector3 FindPlayerPosition()
    {
        Vector3 pos =playerAvatar.transform.position;

        pos.x = Mathf.Round((playerAvatar.transform.position.x) / 10f);
        pos.z = Mathf.Round((playerAvatar.transform.position.z) / 10f);
        pos.y = 0.0f;

        return pos;
    }

    private Vector3 GenerateRandomRoomLocation(bool notOnWalls)
    {
        float width = notOnWalls ? Random.Range(1, mWidth - 2) : Random.Range(0, mWidth - 1);
        float height = notOnWalls ? Random.Range(1, mHeight - 2) : Random.Range(0, mHeight - 1);
        Vector3 location = new Vector3(width, 0, height);
        return location;
    }

    private void CriticalPathGenerator(Vector3 spawnPosition)
    {
        Vector3 goalPos = GenerateRandomRoomLocation(false);

        int[,] maze = new int[10, 10];
        maze[(int)spawnPosition.x, (int)spawnPosition.z] = -1;
        maze[(int)goalPos.x, (int)goalPos.z] = -2;

        //Debug.Log("Start: " + spawnPosition);
        //Debug.Log("End: " + goalPos);

        bool succeeded = BuildPathConnection(maze, goalPos, spawnPosition, 0);

        for (int i = 0; i < 10; i++)
        {
            string printString = "";
            for (int j = 0; j < 10; j++)
            {
                printString += maze[j, i] + " ";
            }
            //Debug.Log(printString);
        }

                Assert.IsTrue(succeeded);

        for (int i = 0; i < 10; i++)
        {
            for (int j = 0; j < 10; j++)
            {
                if(maze[i,j] == 0 || maze[i, j] == -1)
                {
                    continue;
                }

                int[] doors = new int[4];
                Vector3[] directions = { Vector3.forward, Vector3.right, Vector3.back, Vector3.left };
                int index = 0;
                foreach (Vector3 direction in directions)
                {
                    int x = i + (int)direction.x;
                    int z = j + (int)direction.z;
                    bool notInBounds = x < 0 || x > mWidth - 1 || z < 0 || z > mHeight - 1;
                    if (notInBounds)
                    {
                        doors[index] = 0;
                        index++;
                        continue;
                    }

                    if(maze[x, z] == 0)
                    {
                        if(i != (int)goalPos.x || j != (int)goalPos.z) //Don't add doors to the end point
                        {
                            doors[index] = RandomDoorSelect() ? 1 : 0;
                        }
                    }
                    else
                    {
                        if(maze[i, j] == 1 && maze[x, z] == -1)
                        {
                            doors[index] = 1;
                        }
                        
                        if ((maze[i,j] - 1 == maze[x,z]) || (maze[i, j] + 1 == maze[x, z]))
                        {
                            doors[index] = 1;
                        }
                    }
                    index++;
                }

                RoomConfig roomType = _GetRoomType(i, j, doors);
                Assert.AreNotEqual(roomType.mRoomType, -1);

                roomType.mRoomLocation = new Vector3(i, 0, j);

                //Set Room Color
                if (!roomType.mNoMaterial)
                {
                    var probability = Random.Range(0, 100);
                    float totalColorProb = SumArray(RoomMaterialProabilities);

                    int colourCount = 0;
                    float sum = 0f;
                    if(i == (int)goalPos.x && j == (int)goalPos.z)
                    {
                        roomType.mMaterial = EndRoomMaterial;
                    }
                    else
                    {
                        foreach (float colorProb in RoomMaterialProabilities)
                        {
                            sum += colorProb;
                            if (probability < (sum / totalColorProb) * 100)
                            {
                                roomType.mMaterial = RoomMaterials[colourCount];
                                break;
                            }
                            colourCount++;
                        }
                    }
                }

                mMapConfig[i, j] = roomType;
            }
        }
        
        //Debug.Log("PathCompleted");
    }

    private bool BuildPathConnection(int[,] maze, Vector3 goalPos, Vector3 currentTile, int tileCount)
    {
        Vector3[] directions = { Vector3.forward, Vector3.right, Vector3.back, Vector3.left };
        bool success = false;

        //Randomize directions
        directions = ShuffleVectorArray(directions);

        foreach (Vector3 direction in directions)
        {
            Vector3 newVector = currentTile + direction;
            
            bool notInBounds = newVector.x < 0 || newVector.x > mWidth - 1 || newVector.z < 0 || newVector.z > mHeight - 1;
            if(notInBounds)
            {
                continue;
            }

            switch (maze[(int)newVector.x, (int)newVector.z])
            {
                case -2:
                    //SUCCESS!
                    success = true;
                    break;
                case 0:
                    //Valid new tile
                    tileCount++;
                    maze[(int)newVector.x, (int)newVector.z] = tileCount;
                    bool validPath = BuildPathConnection(maze, goalPos, newVector, tileCount);

                    if(validPath)
                    {
                        if(maze[(int)goalPos.x, (int)goalPos.z] == -2)
                        {
                            maze[(int)goalPos.x, (int)goalPos.z] = ++tileCount;
                        }
                        success = true;
                    }
                    else
                    {
                        maze[(int)newVector.x, (int)newVector.z] = 0;
                        tileCount--;
                    }
                    break;
                case -1:
                case 1:
                default:
                    //invalid
                    break;
            }

            if(success)
            {
                break;
            }
        }

        return success;
    }

    private Vector3[] ShuffleVectorArray(Vector3[] array)
    {
        for(int i = 0; i < array.Length; i++)
        {
            int rnd = Random.Range(i, array.Length);
            Vector3 holder = array[rnd];
            array[rnd] = array[i];
            array[i] = holder;
        }

        return array;
    }

    private float SumArray(float[] array)
        {
            float sum = 0.0f;
            foreach(float value in array)
            {
                sum += value;
            }
            return sum;
        }
    }
