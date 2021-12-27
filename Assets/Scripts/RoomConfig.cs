using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class RoomConfig : MonoBehaviour
{
    [SerializeField]
    public bool mIsConstructed = false;
    [SerializeField]
    public int mNumOfDoors = 1;
    [SerializeField]
    public int[] mDoorLocations = new int[4];
    [SerializeField]
    public bool mSpecialRoom = false;
    [SerializeField]
    public bool mNoMaterial = false;
    [SerializeField]
    public Material mMaterial;

    public int mRoomType = -1;
    public Vector3 mRoomLocation = new Vector3();
    


    public RoomConfig()
    {
    }

    public RoomConfig(int roomType)
    {
        mRoomType = roomType; 
    }

    public RoomConfig(RoomConfig cloneConfig)
    {
        mNumOfDoors = cloneConfig.mNumOfDoors;
        mDoorLocations = cloneConfig.mDoorLocations;
        mSpecialRoom = cloneConfig.mSpecialRoom;
        mNoMaterial = cloneConfig.mNoMaterial;
        mRoomType = cloneConfig.mRoomType;
    }
}
