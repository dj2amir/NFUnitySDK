﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.IO;
using UnityEngine;
using NFMsg;
using NFrame;
using Google.Protobuf;
using NFSDK;

namespace NFrame
{
    public class NFNetHandlerModule : NFIModule
    {
        public enum Event : int
        {
            SwapScene = 1,
            PlayerMove,
        };


        class ObjectDataBuff
        {
            public NFMsg.ObjectRecordList xRecordList;
            public NFMsg.ObjectPropertyList xPropertyList;
        };


        private NFIKernelModule mKernelModule;
        private NFIClassModule mClassModule;

        private NFHelpModule mHelpModule;
        private NFNetModule mNetModule;
        private NFNetEventModule mNetEventModule;
        private NFSceneModule mSceneModule;
        private NFLoginModule mLoginModule;
        private NFUIModule mUIModule;

        private NFNetHandlerModule mxNetListener = null;

        private Dictionary<NFGUID, ObjectDataBuff> mxObjectDataBuff = new Dictionary<NFGUID, ObjectDataBuff>();

        public delegate void ResultCodeDelegation(NFMsg.EGameEventCode eventCode, String data);
        private Dictionary<NFMsg.EGameEventCode, ResultCodeDelegation> mhtEventDelegation = new Dictionary<NFMsg.EGameEventCode, ResultCodeDelegation>();

        public NFNetHandlerModule(NFIPluginManager pluginManager)
        {
            mPluginManager = pluginManager;
        }

        public void AddMsgEventCallBack(NFMsg.EGameEventCode code, NFNetHandlerModule.ResultCodeDelegation netHandler)
        {
            if (!mhtEventDelegation.ContainsKey(code))
            {
                ResultCodeDelegation myDelegationHandler = new ResultCodeDelegation(netHandler);
                mhtEventDelegation.Add(code, myDelegationHandler);
            }
            else
            {
                ResultCodeDelegation myDelegationHandler = (ResultCodeDelegation)mhtEventDelegation[code];
                myDelegationHandler += new ResultCodeDelegation(netHandler);
            }
        }

        public void DoResultCodeDelegation(NFMsg.EGameEventCode code, String data)
        {
            if (mhtEventDelegation.ContainsKey(code))
            {
                ResultCodeDelegation myDelegationHandler = (ResultCodeDelegation)mhtEventDelegation[code];
                myDelegationHandler(code, "");
            }
        }

        // Use this for initialization
        public override void Awake()
        {
            mClassModule = mPluginManager.FindModule<NFIClassModule>();
            mKernelModule = mPluginManager.FindModule<NFIKernelModule>();

            mSceneModule = mPluginManager.FindModule<NFSceneModule>();
            mNetModule = mPluginManager.FindModule<NFNetModule>();
            mHelpModule = mPluginManager.FindModule<NFHelpModule>();
            mNetEventModule = mPluginManager.FindModule<NFNetEventModule>();
            mUIModule = mPluginManager.FindModule<NFUIModule>();

        }

        public override void Init()
        {

            mKernelModule.RegisterClassCallBack(NFrame.Player.ThisName, ClassEventHandler);
            mKernelModule.RegisterClassCallBack(NFrame.NPC.ThisName, ClassEventHandler);

            mNetModule.AddReceiveCallBack(NFMsg.EGameMsgID.EventResult, EGMI_EVENT_RESULT);

            mNetModule.AddReceiveCallBack(NFMsg.EGameMsgID.AckEnterGame, EGMI_ACK_ENTER_GAME);
            mNetModule.AddReceiveCallBack(NFMsg.EGameMsgID.AckSwapScene, EGMI_ACK_SWAP_SCENE);
            mNetModule.AddReceiveCallBack(NFMsg.EGameMsgID.AckEnterGameFinish, EGMI_ACK_ENTER_GAME_FINISH);


            mNetModule.AddReceiveCallBack(NFMsg.EGameMsgID.AckObjectEntry, EGMI_ACK_OBJECT_ENTRY);
            mNetModule.AddReceiveCallBack(NFMsg.EGameMsgID.AckObjectLeave, EGMI_ACK_OBJECT_LEAVE);
            mNetModule.AddReceiveCallBack(NFMsg.EGameMsgID.AckMove, EGMI_ACK_MOVE);
            mNetModule.AddReceiveCallBack(NFMsg.EGameMsgID.AckMoveImmune, EGMI_ACK_MOVE_IMMUNE);
            mNetModule.AddReceiveCallBack(NFMsg.EGameMsgID.AckStateSync, EGMI_ACK_STATE_SYNC);
            mNetModule.AddReceiveCallBack(NFMsg.EGameMsgID.AckPosSync, EGMI_ACK_POS_SYNC);

            mNetModule.AddReceiveCallBack(NFMsg.EGameMsgID.AckPropertyInt, EGMI_ACK_PROPERTY_INT);
            mNetModule.AddReceiveCallBack(NFMsg.EGameMsgID.AckPropertyFloat, EGMI_ACK_PROPERTY_FLOAT);
            mNetModule.AddReceiveCallBack(NFMsg.EGameMsgID.AckPropertyString, EGMI_ACK_PROPERTY_STRING);
            mNetModule.AddReceiveCallBack(NFMsg.EGameMsgID.AckPropertyObject, EGMI_ACK_PROPERTY_OBJECT);
            mNetModule.AddReceiveCallBack(NFMsg.EGameMsgID.AckPropertyVector2, EGMI_ACK_PROPERTY_VECTOR2);
            mNetModule.AddReceiveCallBack(NFMsg.EGameMsgID.AckPropertyVector3, EGMI_ACK_PROPERTY_VECTOR3);
            mNetModule.AddReceiveCallBack(NFMsg.EGameMsgID.AckPropertyClear, EGMI_ACK_PROPERTY_CLEAR);

            mNetModule.AddReceiveCallBack(NFMsg.EGameMsgID.AckRecordInt, EGMI_ACK_RECORD_INT);
            mNetModule.AddReceiveCallBack(NFMsg.EGameMsgID.AckRecordFloat, EGMI_ACK_RECORD_FLOAT);
            mNetModule.AddReceiveCallBack(NFMsg.EGameMsgID.AckRecordString, EGMI_ACK_RECORD_STRING);
            mNetModule.AddReceiveCallBack(NFMsg.EGameMsgID.AckRecordObject, EGMI_ACK_RECORD_OBJECT);
            mNetModule.AddReceiveCallBack(NFMsg.EGameMsgID.AckSwapRow, EGMI_ACK_SWAP_ROW);
            mNetModule.AddReceiveCallBack(NFMsg.EGameMsgID.AckAddRow, EGMI_ACK_ADD_ROW);
            mNetModule.AddReceiveCallBack(NFMsg.EGameMsgID.AckRemoveRow, EGMI_ACK_REMOVE_ROW);
            mNetModule.AddReceiveCallBack(NFMsg.EGameMsgID.AckRecordClear, EGMI_ACK_RECORD_CLEAR);

            mNetModule.AddReceiveCallBack(NFMsg.EGameMsgID.AckObjectRecordEntry, EGMI_ACK_OBJECT_RECORD_ENTRY);
            mNetModule.AddReceiveCallBack(NFMsg.EGameMsgID.AckObjectPropertyEntry, EGMI_ACK_OBJECT_PROPERTY_ENTRY);
            mNetModule.AddReceiveCallBack(NFMsg.EGameMsgID.AckDataFinished, EGMI_ACK_DATA_FINISHED);


            mNetModule.AddReceiveCallBack(NFMsg.EGameMsgID.AckSkillObjectx, EGMI_ACK_SKILL_OBJECTX);
            //mNetModule.AddReceiveCallBack(NFMsg.EGameMsgID.AckChat, EGMI_ACK_CHAT);

        }

        public override void AfterInit()
        {
        }

        public override void Execute()
        {
        }

        public override void BeforeShut()
        {
        }
        public override void Shut()
        {
        }

        private void EGMI_EVENT_RESULT(UInt16 id, MemoryStream stream)
        {
            NFMsg.MsgBase xMsg = NFMsg.MsgBase.Parser.ParseFrom(stream);

            //OnResultMsg
            NFMsg.AckEventResult xResultCode = NFMsg.AckEventResult.Parser.ParseFrom(xMsg.MsgData);
            NFMsg.EGameEventCode eEvent = xResultCode.EventCode;

            DoResultCodeDelegation(eEvent, "");
        }



        private void EGMI_ACK_ENTER_GAME(UInt16 id, MemoryStream stream)
        {

            NFMsg.MsgBase xMsg = NFMsg.MsgBase.Parser.ParseFrom(stream);

            NFMsg.AckEventResult xData = NFMsg.AckEventResult.Parser.ParseFrom(xMsg.MsgData);

            Debug.Log("EGMI_ACK_ENTER_GAME " + xData.EventObject.ToString());

            //mSceneModule.LoadScene((int)xData.event_code);
            //可以播放过图动画场景
        }

        private void EGMI_ACK_SWAP_SCENE(UInt16 id, MemoryStream stream)
        {
            NFMsg.MsgBase xMsg = NFMsg.MsgBase.Parser.ParseFrom(stream);

            NFMsg.ReqAckSwapScene xData = NFMsg.ReqAckSwapScene.Parser.ParseFrom(xMsg.MsgData);

            Debug.Log("SWAP_SCENE: " + xData.SceneId + " " + xData.X + "," + xData.Y + "," + xData.Z);

            /*
            NFMsg.AckMiningTitle xTileData = null;
            if (null != xData.Data && xData.Data.Length > 0)
            {
                xTileData = NFMsg.AckMiningTitle.Parser.ParseFrom(xData.Data);
            }
            */
            mSceneModule.LoadScene(xData.SceneId, xData.X, xData.Y, xData.Z, "");

            //重置主角坐标到出生点
        }

        private void EGMI_ACK_ENTER_GAME_FINISH(UInt16 id, MemoryStream stream)
        {
            NFMsg.MsgBase xMsg = NFMsg.MsgBase.Parser.ParseFrom(stream);

            NFMsg.ReqAckEnterGameSuccess xData = NFMsg.ReqAckEnterGameSuccess.Parser.ParseFrom(xMsg.MsgData);

            Debug.Log("Enter game finished: " + xData.ToString());

            // 去掉遮场景的ui
            //主角，等怪物enable，并且充值在相应的position
            //mSceneModule.LoadScene(xData.scene_id, xData.x, xData.y, xData.z);
        }


        private void EGMI_ACK_OBJECT_ENTRY(UInt16 id, MemoryStream stream)
        {
            NFMsg.MsgBase xMsg = NFMsg.MsgBase.Parser.ParseFrom(stream);

            NFMsg.AckPlayerEntryList xData = NFMsg.AckPlayerEntryList.Parser.ParseFrom(xMsg.MsgData);

            for (int i = 0; i < xData.ObjectList.Count; ++i)
            {
                NFMsg.PlayerEntryInfo xInfo = xData.ObjectList[i];

                NFVector3 vPos = new NFVector3(xInfo.X, xInfo.Y, xInfo.Z);

                NFDataList var = new NFDataList();
                var.AddString(NFrame.NPC.Position);
                var.AddVector3(vPos);

                NFGUID xObjectID = mHelpModule.PBToNF(xInfo.ObjectGuid);
                string strClassName = xInfo.ClassId.ToStringUtf8();
                string strConfigID = xInfo.ConfigId.ToStringUtf8();

                Debug.Log("new Object enter: " + strClassName + xObjectID.ToString() + " " + xInfo.X + " " + xInfo.Y + " " + xInfo.Z);

                ObjectDataBuff xDataBuff = new ObjectDataBuff();
                mxObjectDataBuff.Add(xObjectID, xDataBuff);
                /*
                NFIObject xGO = mKernelModule.CreateObject(xObjectID, xInfo.scene_id, 0, strClassName, strConfigID, var);
                if (null == xGO)
                {
                    Debug.LogError("ID conflict: " + xObjectID.ToString() + "  ConfigID: " + strClassName);
                    continue;
                }
                */
            }
        }

        private void EGMI_ACK_OBJECT_LEAVE(UInt16 id, MemoryStream stream)
        {
            NFMsg.MsgBase xMsg = NFMsg.MsgBase.Parser.ParseFrom(stream);

            NFMsg.AckPlayerLeaveList xData = NFMsg.AckPlayerLeaveList.Parser.ParseFrom(xMsg.MsgData);

            for (int i = 0; i < xData.ObjectList.Count; ++i)
            {
                mKernelModule.DestroyObject(mHelpModule.PBToNF(xData.ObjectList[i]));
            }
        }

        private void EGMI_ACK_OBJECT_RECORD_ENTRY(UInt16 id, MemoryStream stream)
        {
            NFMsg.MsgBase xMsg = NFMsg.MsgBase.Parser.ParseFrom(stream);

            NFMsg.MultiObjectRecordList xData = NFMsg.MultiObjectRecordList.Parser.ParseFrom(xMsg.MsgData);

            for (int i = 0; i < xData.MultiPlayerRecord.Count; i++)
            {
                NFMsg.ObjectRecordList xObjectRecordList = xData.MultiPlayerRecord[i];
                NFGUID xObjectID = mHelpModule.PBToNF(xObjectRecordList.PlayerId);

                //Debug.Log ("new record enter Object: " + xObjectID.ToString () );

                ObjectDataBuff xDataBuff;
                if (mxObjectDataBuff.TryGetValue(xObjectID, out xDataBuff))
                {
                    xDataBuff.xRecordList = xObjectRecordList;
                    if (xObjectID.IsNull())
                    {
                        AttachObjectData(xObjectID);
                    }
                }
            }
        }

        private void EGMI_ACK_OBJECT_PROPERTY_ENTRY(UInt16 id, MemoryStream stream)
        {
            NFMsg.MsgBase xMsg = NFMsg.MsgBase.Parser.ParseFrom(stream);

            NFMsg.MultiObjectPropertyList xData = NFMsg.MultiObjectPropertyList.Parser.ParseFrom(xMsg.MsgData);

            for (int i = 0; i < xData.MultiPlayerProperty.Count; i++)
            {
                NFMsg.ObjectPropertyList xPropertyData = xData.MultiPlayerProperty[i];
                NFGUID xObjectID = mHelpModule.PBToNF(xPropertyData.PlayerId);

                ObjectDataBuff xDataBuff;
                if (mxObjectDataBuff.TryGetValue(xObjectID, out xDataBuff))
                {

                    xDataBuff.xPropertyList = xPropertyData;
                    if (xObjectID.IsNull())
                    {
                        AttachObjectData(xObjectID);
                    }
                }
                else
                {
                    xDataBuff = new ObjectDataBuff();
                    xDataBuff.xPropertyList = xPropertyData;
                    mxObjectDataBuff[xObjectID] = xDataBuff;
                    AttachObjectData(xObjectID);
                }
            }
        }

        private void AttachObjectData(NFGUID self)
        {
            //Debug.Log ("AttachObjectData : " + self.ToString () );

            ObjectDataBuff xDataBuff;
            if (mxObjectDataBuff.TryGetValue(self, out xDataBuff))
            {
                ////////////////record
                if (xDataBuff.xRecordList != null)
                {
                    for (int j = 0; j < xDataBuff.xRecordList.RecordList.Count; j++)
                    {
                        NFMsg.ObjectRecordBase xObjectRecordBase = xDataBuff.xRecordList.RecordList[j];
                        string srRecordName = xObjectRecordBase.RecordName.ToStringUtf8();

                        for (int k = 0; k < xObjectRecordBase.RowStruct.Count; ++k)
                        {
                            NFMsg.RecordAddRowStruct xAddRowStruct = xObjectRecordBase.RowStruct[k];

                            ADD_ROW(self, xObjectRecordBase.RecordName.ToStringUtf8(), xAddRowStruct);
                        }
                    }

                    xDataBuff.xRecordList = null;
                }
                ////////////////property
                if (xDataBuff.xPropertyList != null)
                {
                    /// 
                    NFIObject go = mKernelModule.GetObject(mHelpModule.PBToNF(xDataBuff.xPropertyList.PlayerId));
                    NFIPropertyManager xPropertyManager = go.GetPropertyManager();

                    for (int j = 0; j < xDataBuff.xPropertyList.PropertyIntList.Count; j++)
                    {
                        string strPropertyName = xDataBuff.xPropertyList.PropertyIntList[j].PropertyName.ToStringUtf8();
                        NFIProperty xProperty = xPropertyManager.GetProperty(strPropertyName);
                        if (null == xProperty)
                        {
                            NFDataList.TData var = new NFDataList.TData(NFDataList.VARIANT_TYPE.VTYPE_INT);
                            xProperty = xPropertyManager.AddProperty(strPropertyName, var);
                        }

                        //string className = mKernelModule.QueryPropertyString(self, NFrame.IObject.ClassName);
                        //Debug.LogError (self.ToString() + " " + className + " " + strPropertyName + " : " + xDataBuff.xPropertyList.property_int_list[j].Data);

                        xProperty.SetInt(xDataBuff.xPropertyList.PropertyIntList[j].Data);
                    }

                    for (int j = 0; j < xDataBuff.xPropertyList.PropertyFloatList.Count; j++)
                    {
                        string strPropertyName = xDataBuff.xPropertyList.PropertyFloatList[j].PropertyName.ToStringUtf8();
                        NFIProperty xProperty = xPropertyManager.GetProperty(strPropertyName);
                        if (null == xProperty)
                        {

                            NFDataList.TData var = new NFDataList.TData(NFDataList.VARIANT_TYPE.VTYPE_FLOAT);
                            xProperty = xPropertyManager.AddProperty(strPropertyName, var);
                        }

                        xProperty.SetFloat(xDataBuff.xPropertyList.PropertyFloatList[j].Data);
                    }

                    for (int j = 0; j < xDataBuff.xPropertyList.PropertyStringList.Count; j++)
                    {
                        string strPropertyName = xDataBuff.xPropertyList.PropertyStringList[j].PropertyName.ToStringUtf8();
                        NFIProperty xProperty = xPropertyManager.GetProperty(strPropertyName);
                        if (null == xProperty)
                        {
                            NFDataList.TData var = new NFDataList.TData(NFDataList.VARIANT_TYPE.VTYPE_STRING);
                            xProperty = xPropertyManager.AddProperty(strPropertyName, var);
                        }

                        //string className = mKernelModule.QueryPropertyString(self, NFrame.IObject.ClassName);
                        //Debug.LogError(self.ToString() + " " + className + " " + strPropertyName + " : " + xDataBuff.xPropertyList.property_string_list[j].Data.ToStringUtf8());

                        xProperty.SetString(xDataBuff.xPropertyList.PropertyStringList[j].Data.ToStringUtf8());
                    }

                    for (int j = 0; j < xDataBuff.xPropertyList.PropertyObjectList.Count; j++)
                    {
                        string strPropertyName = xDataBuff.xPropertyList.PropertyObjectList[j].PropertyName.ToStringUtf8();
                        NFIProperty xProperty = xPropertyManager.GetProperty(strPropertyName);
                        if (null == xProperty)
                        {
                            NFDataList.TData var = new NFDataList.TData(NFDataList.VARIANT_TYPE.VTYPE_OBJECT);
                            xProperty = xPropertyManager.AddProperty(strPropertyName, var);
                        }

                        xProperty.SetObject(mHelpModule.PBToNF(xDataBuff.xPropertyList.PropertyObjectList[j].Data));
                    }

                    for (int j = 0; j < xDataBuff.xPropertyList.PropertyVector2List.Count; j++)
                    {
                        string strPropertyName = xDataBuff.xPropertyList.PropertyVector2List[j].PropertyName.ToStringUtf8();
                        NFIProperty xProperty = xPropertyManager.GetProperty(strPropertyName);
                        if (null == xProperty)
                        {
                            NFDataList.TData var = new NFDataList.TData(NFDataList.VARIANT_TYPE.VTYPE_VECTOR2);

                            xProperty = xPropertyManager.AddProperty(strPropertyName, var);
                        }

                        xProperty.SetVector2(mHelpModule.PBToNF(xDataBuff.xPropertyList.PropertyVector2List[j].Data));
                    }

                    for (int j = 0; j < xDataBuff.xPropertyList.PropertyVector3List.Count; j++)
                    {
                        string strPropertyName = xDataBuff.xPropertyList.PropertyVector3List[j].PropertyName.ToStringUtf8();
                        NFIProperty xProperty = xPropertyManager.GetProperty(strPropertyName);
                        if (null == xProperty)
                        {
                            NFDataList.TData var = new NFDataList.TData(NFDataList.VARIANT_TYPE.VTYPE_VECTOR3);

                            xProperty = xPropertyManager.AddProperty(strPropertyName, var);
                        }

                        xProperty.SetVector3(mHelpModule.PBToNF(xDataBuff.xPropertyList.PropertyVector3List[j].Data));
                    }


                    xDataBuff.xPropertyList = null;
                }
            }
        }

        private void ClassEventHandler(NFGUID self, int nContainerID, int nGroupID, NFIObject.CLASS_EVENT_TYPE eType, string strClassName, string strConfigIndex)
        {
            switch (eType)
            {
                case NFIObject.CLASS_EVENT_TYPE.OBJECT_CREATE:
                    break;
                case NFIObject.CLASS_EVENT_TYPE.OBJECT_LOADDATA:
                    AttachObjectData(self);
                    break;
                case NFIObject.CLASS_EVENT_TYPE.OBJECT_CREATE_FINISH:
                    mxObjectDataBuff.Remove(self);
                    break;
                case NFIObject.CLASS_EVENT_TYPE.OBJECT_DESTROY:
                    break;
                default:
                    break;
            }
        }

        private void EGMI_ACK_DATA_FINISHED(UInt16 id, MemoryStream stream)
        {
            NFMsg.MsgBase xMsg = NFMsg.MsgBase.Parser.ParseFrom(stream);

            NFMsg.AckPlayerEntryList xData = NFMsg.AckPlayerEntryList.Parser.ParseFrom(xMsg.MsgData);

            for (int i = 0; i < xData.ObjectList.Count; ++i)
            {
                NFMsg.PlayerEntryInfo xInfo = xData.ObjectList[i];

                NFVector3 vPos = new NFVector3(xInfo.X, xInfo.Y, xInfo.Z);

                NFDataList var = new NFDataList();
                var.AddString("Position");
                var.AddVector3(vPos);

                NFGUID xObjectID = mHelpModule.PBToNF(xInfo.ObjectGuid);
                string strClassName = xInfo.ClassId.ToStringUtf8();
                string strConfigID = xInfo.ConfigId.ToStringUtf8();

                Debug.Log("create Object: " + strClassName + " " + xObjectID.ToString() + " " + strConfigID + " (" + xInfo.X + "," + xInfo.Y + "," + xInfo.Z + ")");

                ObjectDataBuff xDataBuff;
                if (mxObjectDataBuff.TryGetValue(xObjectID, out xDataBuff))
                {
                    NFIObject xGO = mKernelModule.CreateObject(xObjectID, xInfo.SceneId, 0, strClassName, strConfigID, var);
                    if (null == xGO)
                    {
                        Debug.LogError("ID conflict: " + xObjectID.ToString() + "  ConfigID: " + strClassName);
                        continue;
                    }
                }
            }
        }
        /////////////////////////////////////////////////////////////////////
        private void EGMI_ACK_PROPERTY_INT(UInt16 id, MemoryStream stream)
        {
            NFMsg.MsgBase xMsg = NFMsg.MsgBase.Parser.ParseFrom(stream);

            NFMsg.ObjectPropertyInt xData = NFMsg.ObjectPropertyInt.Parser.ParseFrom(xMsg.MsgData);

            NFIObject go = mKernelModule.GetObject(mHelpModule.PBToNF(xData.PlayerId));
            if (go == null)
            {
                return;
            }

            NFIPropertyManager propertyManager = go.GetPropertyManager();

            for (int i = 0; i < xData.PropertyList.Count; i++)
            {
                string name = xData.PropertyList[i].PropertyName.ToStringUtf8();
                Int64 data = xData.PropertyList[i].Data;

                NFIProperty property = propertyManager.GetProperty(name);
                if (null == property)
                {
                    NFDataList.TData var = new NFDataList.TData(NFDataList.VARIANT_TYPE.VTYPE_INT);
                    property = propertyManager.AddProperty(name, var);
                }

                property.SetInt(data);
            }
        }

        private void EGMI_ACK_PROPERTY_FLOAT(UInt16 id, MemoryStream stream)
        {
            NFMsg.MsgBase xMsg = NFMsg.MsgBase.Parser.ParseFrom(stream);

            NFMsg.ObjectPropertyFloat xData = NFMsg.ObjectPropertyFloat.Parser.ParseFrom(xMsg.MsgData);

            NFIObject go = mKernelModule.GetObject(mHelpModule.PBToNF(xData.PlayerId));
            if (go == null)
            {
                return;
            }

            NFIPropertyManager propertyManager = go.GetPropertyManager();
            for (int i = 0; i < xData.PropertyList.Count; i++)
            {
                string name = xData.PropertyList[i].PropertyName.ToStringUtf8();
                float data = xData.PropertyList[i].Data;

                NFIProperty property = propertyManager.GetProperty(name);
                if (null == property)
                {
                    NFDataList.TData var = new NFDataList.TData(NFDataList.VARIANT_TYPE.VTYPE_FLOAT);
                    property = propertyManager.AddProperty(name, var);
                }

                property.SetFloat(data);
            }
        }

        private void EGMI_ACK_PROPERTY_STRING(UInt16 id, MemoryStream stream)
        {
            NFMsg.MsgBase xMsg = NFMsg.MsgBase.Parser.ParseFrom(stream);

            NFMsg.ObjectPropertyString xData = NFMsg.ObjectPropertyString.Parser.ParseFrom(xMsg.MsgData);

            NFIObject go = mKernelModule.GetObject(mHelpModule.PBToNF(xData.PlayerId));
            if (go == null)
            {
                Debug.LogError("error id" + xData.PlayerId);
                return;
            }

            NFIPropertyManager propertyManager = go.GetPropertyManager();

            for (int i = 0; i < xData.PropertyList.Count; i++)
            {
                string name = xData.PropertyList[i].PropertyName.ToStringUtf8();
                string data = xData.PropertyList[i].Data.ToStringUtf8();

                NFIProperty property = propertyManager.GetProperty(name);
                if (null == property)
                {
                    NFDataList.TData var = new NFDataList.TData(NFDataList.VARIANT_TYPE.VTYPE_STRING);
                    property = propertyManager.AddProperty(name, var);
                }

                property.SetString(data);
            }
        }

        private void EGMI_ACK_PROPERTY_OBJECT(UInt16 id, MemoryStream stream)
        {
            NFMsg.MsgBase xMsg = NFMsg.MsgBase.Parser.ParseFrom(stream);

            NFMsg.ObjectPropertyObject xData = NFMsg.ObjectPropertyObject.Parser.ParseFrom(xMsg.MsgData);

            NFIObject go = mKernelModule.GetObject(mHelpModule.PBToNF(xData.PlayerId));
            if (go == null)
            {
                Debug.LogError("error id" + xData.PlayerId);
                return;
            }

            NFIPropertyManager propertyManager = go.GetPropertyManager();
            for (int i = 0; i < xData.PropertyList.Count; i++)
            {
                string name = xData.PropertyList[i].PropertyName.ToStringUtf8();
                NFMsg.Ident data = xData.PropertyList[i].Data;

                NFIProperty property = propertyManager.GetProperty(name);
                if (null == property)
                {
                    NFDataList.TData var = new NFDataList.TData(NFDataList.VARIANT_TYPE.VTYPE_OBJECT);
                    property = propertyManager.AddProperty(name, var);
                }

                property.SetObject(mHelpModule.PBToNF(data));
            }
        }

        private void EGMI_ACK_PROPERTY_VECTOR2(UInt16 id, MemoryStream stream)
        {
            NFMsg.MsgBase xMsg = NFMsg.MsgBase.Parser.ParseFrom(stream);

            NFMsg.ObjectPropertyVector2 xData = NFMsg.ObjectPropertyVector2.Parser.ParseFrom(xMsg.MsgData);

            NFIObject go = mKernelModule.GetObject(mHelpModule.PBToNF(xData.PlayerId));
            if (go == null)
            {
                Debug.LogError("error id" + xData.PlayerId);
                return;
            }

            NFIPropertyManager propertyManager = go.GetPropertyManager();

            for (int i = 0; i < xData.PropertyList.Count; i++)
            {
                string name = xData.PropertyList[i].PropertyName.ToStringUtf8();
                NFMsg.Vector2 data = xData.PropertyList[i].Data;

                NFIProperty property = propertyManager.GetProperty(name);
                if (null == property)
                {
                    NFDataList.TData var = new NFDataList.TData(NFDataList.VARIANT_TYPE.VTYPE_VECTOR2);
                    property = propertyManager.AddProperty(name, var);
                }

                property.SetVector2(mHelpModule.PBToNF(data));
            }
        }

        private void EGMI_ACK_PROPERTY_VECTOR3(UInt16 id, MemoryStream stream)
        {
            NFMsg.MsgBase xMsg = NFMsg.MsgBase.Parser.ParseFrom(stream);

            NFMsg.ObjectPropertyVector3 xData = NFMsg.ObjectPropertyVector3.Parser.ParseFrom(xMsg.MsgData);

            NFIObject go = mKernelModule.GetObject(mHelpModule.PBToNF(xData.PlayerId));
            if (go == null)
            {
                Debug.LogError("error id" + xData.PlayerId);
                return;
            }

            NFIPropertyManager propertyManager = go.GetPropertyManager();
            for (int i = 0; i < xData.PropertyList.Count; i++)
            {
                string name = xData.PropertyList[i].PropertyName.ToStringUtf8();
                NFMsg.Vector3 data = xData.PropertyList[i].Data;

                NFIProperty property = propertyManager.GetProperty(name);
                if (null == property)
                {
                    NFDataList.TData var = new NFDataList.TData(NFDataList.VARIANT_TYPE.VTYPE_VECTOR3);
                    property = propertyManager.AddProperty(name, var);
                }

                property.SetVector3(mHelpModule.PBToNF(data));
            }
        }

        private void EGMI_ACK_PROPERTY_CLEAR(UInt16 id, MemoryStream stream)
        {
            NFMsg.MsgBase xMsg = NFMsg.MsgBase.Parser.ParseFrom(stream);

            NFMsg.ObjectPropertyVector3 xData = NFMsg.ObjectPropertyVector3.Parser.ParseFrom(xMsg.MsgData);

            NFIObject go = mKernelModule.GetObject(mHelpModule.PBToNF(xData.PlayerId));
            if (go == null)
            {
                Debug.LogError("error id" + xData.PlayerId);
                return;
            }

            NFIPropertyManager propertyManager = go.GetPropertyManager();
            //propertyManager.

        }

        private void EGMI_ACK_RECORD_INT(UInt16 id, MemoryStream stream)
        {
            NFMsg.MsgBase xMsg = NFMsg.MsgBase.Parser.ParseFrom(stream);

            NFMsg.ObjectRecordInt xData = NFMsg.ObjectRecordInt.Parser.ParseFrom(xMsg.MsgData);

            NFIObject go = mKernelModule.GetObject(mHelpModule.PBToNF(xData.PlayerId));
            if (go == null)
            {
                Debug.LogError("error id" + xData.PlayerId);
                return;
            }

            NFIRecordManager recordManager = go.GetRecordManager();
            NFIRecord record = recordManager.GetRecord(xData.RecordName.ToStringUtf8());

            for (int i = 0; i < xData.PropertyList.Count; i++)
            {
                record.SetInt(xData.PropertyList[i].Row, xData.PropertyList[i].Col, (int)xData.PropertyList[i].Data);
            }
        }

        private void EGMI_ACK_RECORD_FLOAT(UInt16 id, MemoryStream stream)
        {
            NFMsg.MsgBase xMsg = NFMsg.MsgBase.Parser.ParseFrom(stream);

            NFMsg.ObjectRecordFloat xData = NFMsg.ObjectRecordFloat.Parser.ParseFrom(xMsg.MsgData);

            NFIObject go = mKernelModule.GetObject(mHelpModule.PBToNF(xData.PlayerId));
            if (go == null)
            {
                Debug.LogError("error id" + xData.PlayerId);
                return;
            }

            NFIRecordManager recordManager = go.GetRecordManager();
            NFIRecord record = recordManager.GetRecord(xData.RecordName.ToStringUtf8());

            for (int i = 0; i < xData.PropertyList.Count; i++)
            {
                record.SetFloat(xData.PropertyList[i].Row, xData.PropertyList[i].Col, (float)xData.PropertyList[i].Data);
            }
        }

        private void EGMI_ACK_RECORD_STRING(UInt16 id, MemoryStream stream)
        {
            NFMsg.MsgBase xMsg = NFMsg.MsgBase.Parser.ParseFrom(stream);

            NFMsg.ObjectRecordString xData = NFMsg.ObjectRecordString.Parser.ParseFrom(xMsg.MsgData);

            NFIObject go = mKernelModule.GetObject(mHelpModule.PBToNF(xData.PlayerId));
            NFIRecordManager recordManager = go.GetRecordManager();
            NFIRecord record = recordManager.GetRecord(xData.RecordName.ToStringUtf8());

            for (int i = 0; i < xData.PropertyList.Count; i++)
            {
                record.SetString(xData.PropertyList[i].Row, xData.PropertyList[i].Col, xData.PropertyList[i].Data.ToStringUtf8());
            }
        }

        private void EGMI_ACK_RECORD_OBJECT(UInt16 id, MemoryStream stream)
        {
            NFMsg.MsgBase xMsg = NFMsg.MsgBase.Parser.ParseFrom(stream);

            NFMsg.ObjectRecordObject xData = NFMsg.ObjectRecordObject.Parser.ParseFrom(xMsg.MsgData);

            NFIObject go = mKernelModule.GetObject(mHelpModule.PBToNF(xData.PlayerId));
            if (go == null)
            {
                Debug.LogError("error id" + xData.PlayerId);
                return;
            }

            NFIRecordManager recordManager = go.GetRecordManager();
            NFIRecord record = recordManager.GetRecord(xData.RecordName.ToStringUtf8());

            for (int i = 0; i < xData.PropertyList.Count; i++)
            {
                record.SetObject(xData.PropertyList[i].Row, xData.PropertyList[i].Col, mHelpModule.PBToNF(xData.PropertyList[i].Data));
            }
        }

        private void EGMI_ACK_SWAP_ROW(UInt16 id, MemoryStream stream)
        {
            NFMsg.MsgBase xMsg = NFMsg.MsgBase.Parser.ParseFrom(stream);

            NFMsg.ObjectRecordSwap xData = NFMsg.ObjectRecordSwap.Parser.ParseFrom(xMsg.MsgData);

            NFIObject go = mKernelModule.GetObject(mHelpModule.PBToNF(xData.PlayerId));
            if (go == null)
            {
                Debug.LogError("error id" + xData.PlayerId);
                return;
            }

            NFIRecordManager recordManager = go.GetRecordManager();
            NFIRecord record = recordManager.GetRecord(xData.OriginRecordName.ToStringUtf8());

            record.SwapRow(xData.RowOrigin, xData.RowTarget);

        }

        private void ADD_ROW(NFGUID self, string strRecordName, NFMsg.RecordAddRowStruct xAddStruct)
        {
            NFIObject go = mKernelModule.GetObject(self);
            if (go == null)
            {
                Debug.LogError("error id" + self);
                return;
            }

            NFIRecordManager xRecordManager = go.GetRecordManager();

            Hashtable recordVecDesc = new Hashtable();
            Hashtable recordVecData = new Hashtable();

            for (int k = 0; k < xAddStruct.RecordIntList.Count; ++k)
            {
                NFMsg.RecordInt addIntStruct = (NFMsg.RecordInt)xAddStruct.RecordIntList[k];

                if (addIntStruct.Col >= 0)
                {
                    recordVecDesc[addIntStruct.Col] = NFDataList.VARIANT_TYPE.VTYPE_INT;
                    recordVecData[addIntStruct.Col] = addIntStruct.Data;
                }
            }

            for (int k = 0; k < xAddStruct.RecordFloatList.Count; ++k)
            {
                NFMsg.RecordFloat addFloatStruct = (NFMsg.RecordFloat)xAddStruct.RecordFloatList[k];

                if (addFloatStruct.Col >= 0)
                {
                    recordVecDesc[addFloatStruct.Col] = NFDataList.VARIANT_TYPE.VTYPE_FLOAT;
                    recordVecData[addFloatStruct.Col] = addFloatStruct.Data;

                }
            }

            for (int k = 0; k < xAddStruct.RecordStringList.Count; ++k)
            {
                NFMsg.RecordString addStringStruct = (NFMsg.RecordString)xAddStruct.RecordStringList[k];

                if (addStringStruct.Col >= 0)
                {
                    recordVecDesc[addStringStruct.Col] = NFDataList.VARIANT_TYPE.VTYPE_STRING;
                    if (addStringStruct.Data != null)
                    {
                        recordVecData[addStringStruct.Col] = addStringStruct.Data.ToStringUtf8();
                    }
                    else
                    {
                        recordVecData[addStringStruct.Col] = "";
                    }
                 }
             }

             for (int k = 0; k<xAddStruct.RecordObjectList.Count; ++k)
             {
                 NFMsg.RecordObject addObjectStruct = (NFMsg.RecordObject)xAddStruct.RecordObjectList[k];

                 if (addObjectStruct.Col >= 0)
                 {
                     recordVecDesc[addObjectStruct.Col] = NFDataList.VARIANT_TYPE.VTYPE_OBJECT;
                     recordVecData[addObjectStruct.Col] = mHelpModule.PBToNF(addObjectStruct.Data);

                 }
             }

             for (int k = 0; k<xAddStruct.RecordVector2List.Count; ++k)
             {
                 NFMsg.RecordVector2 addObjectStruct = (NFMsg.RecordVector2)xAddStruct.RecordVector2List[k];

                 if (addObjectStruct.Col >= 0)
                 {
                     recordVecDesc[addObjectStruct.Col] = NFDataList.VARIANT_TYPE.VTYPE_VECTOR2;
                     recordVecData[addObjectStruct.Col] = mHelpModule.PBToNF(addObjectStruct.Data);

                 }
             }

             for (int k = 0; k<xAddStruct.RecordVector3List.Count; ++k)
             {
                 NFMsg.RecordVector3 addObjectStruct = (NFMsg.RecordVector3)xAddStruct.RecordVector3List[k];

                 if (addObjectStruct.Col >= 0)
                 {
                     recordVecDesc[addObjectStruct.Col] = NFDataList.VARIANT_TYPE.VTYPE_VECTOR3;
                     recordVecData[addObjectStruct.Col] = mHelpModule.PBToNF(addObjectStruct.Data);

                 }
             }

             NFDataList varListDesc = new NFDataList();
             NFDataList varListData = new NFDataList();
             for (int m = 0; m<recordVecDesc.Count; m++)
             {
                 if (recordVecDesc.ContainsKey(m) && recordVecData.ContainsKey(m))
                 {
                     NFDataList.VARIANT_TYPE nType = (NFDataList.VARIANT_TYPE)recordVecDesc[m];
                     switch (nType)
                     {
                         case NFDataList.VARIANT_TYPE.VTYPE_INT:
                         {
                             varListDesc.AddInt(0);
                             varListData.AddInt((Int64) recordVecData[m]);
                         }

                         break;
                         case NFDataList.VARIANT_TYPE.VTYPE_FLOAT:
                         {
                             varListDesc.AddFloat(0.0f);
                             varListData.AddFloat((float) recordVecData[m]);
                         }
                         break;
                         case NFDataList.VARIANT_TYPE.VTYPE_STRING:
                         {
                             varListDesc.AddString("");
                             varListData.AddString((string) recordVecData[m]);
                         }
                         break;
                         case NFDataList.VARIANT_TYPE.VTYPE_OBJECT:
                         {
                             varListDesc.AddObject(new NFGUID());
                             varListData.AddObject((NFGUID) recordVecData[m]);
                         }
                         break;
                         case NFDataList.VARIANT_TYPE.VTYPE_VECTOR2:
                             {
                                 varListDesc.AddVector2(new NFVector2());
                                 varListData.AddVector2((NFVector2) recordVecData[m]);
                             }
                             break;
                         case NFDataList.VARIANT_TYPE.VTYPE_VECTOR3:
                             {
                                 varListDesc.AddVector3(new NFVector3());
                                 varListData.AddVector3((NFVector3) recordVecData[m]);
                             }
                             break;
                         default:
                         break;

                     }
                 }
                 else
                 {
                     //����
                     //Debug.LogException(i);
                 }
             }

             NFIRecord xRecord = xRecordManager.GetRecord(strRecordName);
             if (null == xRecord)
             {
                 string strClassName = mKernelModule.QueryPropertyString(self, NFrame.IObject.ClassName);
                 NFIClass xLogicClass = mClassModule.GetElement(strClassName);
                 NFIRecord xStaticRecord = xLogicClass.GetRecordManager().GetRecord(strRecordName);

                 xRecord = xRecordManager.AddRecord(strRecordName, 512, varListDesc, xStaticRecord.GetTagData());
             }

             xRecord.AddRow(xAddStruct.Row, varListData);
         }

        private void EGMI_ACK_ADD_ROW(UInt16 id, MemoryStream stream)
        {
            NFMsg.MsgBase xMsg = NFMsg.MsgBase.Parser.ParseFrom(stream);

            NFMsg.ObjectRecordAddRow xData = NFMsg.ObjectRecordAddRow.Parser.ParseFrom(xMsg.MsgData);

            NFIObject go = mKernelModule.GetObject(mHelpModule.PBToNF(xData.PlayerId));
            if (go == null)
            {
                Debug.LogError("error id" + xData.PlayerId);
                return;
            }

            NFIRecordManager recordManager = go.GetRecordManager();

            for (int i = 0; i < xData.RowData.Count; i++)
            {
                ADD_ROW(mHelpModule.PBToNF(xData.PlayerId), xData.RecordName.ToStringUtf8(), xData.RowData[i]);
            }
        }

        private void EGMI_ACK_REMOVE_ROW(UInt16 id, MemoryStream stream)
        {
            NFMsg.MsgBase xMsg = NFMsg.MsgBase.Parser.ParseFrom(stream);

            NFMsg.ObjectRecordRemove xData = NFMsg.ObjectRecordRemove.Parser.ParseFrom(xMsg.MsgData);

            NFIObject go = mKernelModule.GetObject(mHelpModule.PBToNF(xData.PlayerId));
            if (go == null)
            {
                Debug.LogError("error id" + xData.PlayerId);
                return;
            }
            NFIRecordManager recordManager = go.GetRecordManager();
            NFIRecord record = recordManager.GetRecord(xData.RecordName.ToStringUtf8());

            for (int i = 0; i < xData.RemoveRow.Count; i++)
            {
                record.Remove(xData.RemoveRow[i]);
            }
        }

        private void EGMI_ACK_RECORD_CLEAR(UInt16 id, MemoryStream stream)
        {
            NFMsg.MsgBase xMsg = NFMsg.MsgBase.Parser.ParseFrom(stream);

            NFMsg.MultiObjectRecordList xData = NFMsg.MultiObjectRecordList.Parser.ParseFrom(xMsg.MsgData);
            for (int i = 0; i < xData.MultiPlayerRecord.Count; ++i)
            {
                NFMsg.ObjectRecordList objectRecordList = xData.MultiPlayerRecord[i];
                for (int j = 0; j < objectRecordList.RecordList.Count; ++j)
                {
                    NFIObject go = mKernelModule.GetObject(mHelpModule.PBToNF(objectRecordList.PlayerId));
                    if (go == null)
                    {
                        Debug.LogError("error id" + objectRecordList.PlayerId);
                        return;
                    }

                    NFMsg.ObjectRecordBase objectRecordBase = objectRecordList.RecordList[j];
                    string recordName = objectRecordBase.RecordName.ToStringUtf8();
                    NFIRecordManager recordManager = go.GetRecordManager();

                    if (recordManager != null)
                    {
                        NFIRecord record = recordManager.GetRecord(recordName);
                        if (record != null)
                        {
                            record.Clear();
                        }
                    }
                }
            }
        }

        //////////////////////////////////
        /// 
        private void EGMI_ACK_MOVE(UInt16 id, MemoryStream stream)
        {
            NFMsg.MsgBase xMsg = NFMsg.MsgBase.Parser.ParseFrom(stream);

            NFMsg.ReqAckPlayerMove xData = NFMsg.ReqAckPlayerMove.Parser.ParseFrom(xMsg.MsgData);

            if (xData.TargetPos.Count <= 0)
            {
                return;
            }

            float fSpeed = mKernelModule.QueryPropertyInt(mHelpModule.PBToNF(xData.Mover), NFrame.NPC.MOVE_SPEED) / 100.0f;

            mSceneModule.MoveTo(mHelpModule.PBToNF(xData.Mover), new UnityEngine.Vector3(xData.TargetPos[0].X, xData.TargetPos[0].Y, xData.TargetPos[0].Z), fSpeed, true);
        }

        private void EGMI_ACK_MOVE_IMMUNE(UInt16 id, MemoryStream stream)
        {
            NFMsg.MsgBase xMsg = NFMsg.MsgBase.Parser.ParseFrom(stream);

            NFMsg.ReqAckPlayerMove xData = NFMsg.ReqAckPlayerMove.Parser.ParseFrom(xMsg.MsgData);

            if (xData.TargetPos.Count <= 0)
            {
                return;
            }

            NFGUID xMover = mHelpModule.PBToNF(xData.Mover);
            if (xMover.IsNull())
            {
                return;
            }

            GameObject xGameObject = mSceneModule.GetObject(xMover);
            if (!xGameObject)
            {
                return;
            }

            NFHeroSync xHeroSync = xGameObject.GetComponent<NFHeroSync>();
            if (!xHeroSync)
            {
                return;
            }

            NFMsg.Vector3 vector = xData.TargetPos[0];

            //TODO
            //GC----------
            UnityEngine.Vector3 v = new UnityEngine.Vector3();
            v.x = vector.X;
            v.y = vector.Y;
            v.z = vector.Z;

            //xHeroSync.AddSyncData(v);

            //float fSpeed = mKernelModule.QueryPropertyInt(xMover, NFrame.NPC.MOVE_SPEED) / 100.0f;
            //fSpeed *= 1.5f;
            //mSceneModule.MoveImmuneBySpeed(PBToNF(xData.mover), new Vector3(xData.target_pos[0].x, xData.target_pos[0].y, xData.target_pos[0].z), fSpeed, true);

        }

        private void EGMI_ACK_STATE_SYNC(UInt16 id, MemoryStream stream)
        {
            NFMsg.MsgBase xMsg = NFMsg.MsgBase.Parser.ParseFrom(stream);

            NFMsg.ReqAckPlayerMove xData = NFMsg.ReqAckPlayerMove.Parser.ParseFrom(xMsg.MsgData);

            if (xData.TargetPos.Count <= 0)
            {
                return;
            }

            NFGUID xGUID = mHelpModule.PBToNF(xData.Mover);
            NFIObject xObject = mKernelModule.GetObject(xGUID);
            if (xObject == null)
            {
                return;
            }
            if (xObject != null)
            {

                GameObject go = mSceneModule.GetObject(xGUID);
                if (go != null)
                {
                    NFAnimaStateMachine xStateMachineMng = go.GetComponent<NFAnimaStateMachine>();
                    if (xStateMachineMng != null)
                    {
                        NFAnimaStateType eNewState = (NFAnimaStateType)(xData.MoveType);
                        float fSpeed = xData.Speed;
                        int nTime = xData.Time;

                        UnityEngine.Vector3 vNowPos = new UnityEngine.Vector3();
                        UnityEngine.Vector3 vTargetPos = new UnityEngine.Vector3();
                        UnityEngine.Vector3 vMoveDirection = new UnityEngine.Vector3();

                        if (xData.SourcePos != null && xData.SourcePos.Count > 0)
                        {
                            vNowPos.x = xData.SourcePos[0].X;
                            vNowPos.y = xData.SourcePos[0].Y;
                            vNowPos.z = xData.SourcePos[0].Z;
                        }
                        if (xData.TargetPos != null && xData.TargetPos.Count > 0)
                        {
                            vTargetPos.x = xData.TargetPos[0].X;
                            vTargetPos.y = xData.TargetPos[0].Y;
                            vTargetPos.z = xData.TargetPos[0].Z;
                        }
                        if (xData.MoveDirection != null && xData.MoveDirection.Count > 0)
                        {
                            vMoveDirection.x = xData.MoveDirection[0].X;
                            vMoveDirection.y = xData.MoveDirection[0].Y;
                            vMoveDirection.z = xData.MoveDirection[0].Z;
                        }

                        xStateMachineMng.InputStateData(eNewState, (NFAnimaStateType)xData.LastState, fSpeed, nTime, vNowPos, vTargetPos, vMoveDirection);
                    }
                }
            }
        }

        private void EGMI_ACK_POS_SYNC(UInt16 id, MemoryStream stream)
        {
            NFMsg.MsgBase xMsg = NFMsg.MsgBase.Parser.ParseFrom(stream);

            NFMsg.ReqAckPlayerPosSync xData = NFMsg.ReqAckPlayerPosSync.Parser.ParseFrom(xMsg.MsgData);

            NFGUID xMover = mHelpModule.PBToNF(xData.Mover);
            if (xMover.IsNull())
            {
                return;
            }

            GameObject xGameObject = mSceneModule.GetObject(xMover);
            if (!xGameObject)
            {
                return;
            }

            NFHeroSync xHeroSync = xGameObject.GetComponent<NFHeroSync>();
            if (!xHeroSync)
            {
                return;
            }

            xHeroSync.AddSyncData(xData);
        }

        private void EGMI_ACK_SKILL_OBJECTX(UInt16 id, MemoryStream stream)
        {
            NFMsg.MsgBase xMsg = NFMsg.MsgBase.Parser.ParseFrom(stream);

            NFMsg.ReqAckUseSkill xData = NFMsg.ReqAckUseSkill.Parser.ParseFrom(xMsg.MsgData);

            NFGUID xUser = mHelpModule.PBToNF(xData.User);
            GameObject xGameObject = mSceneModule.GetObject(xUser);
            if (xGameObject)
            {
                //NFHeroSkill xHeroSkill = xGameObject.GetComponent<NFHeroSkill>();
                //xHeroSkill.AckSkill(xUser, xData.use_index, xData.skill_id.ToStringUtf8(), xData.effect_data.ToList<NFMsg.EffectData>());
            }
        }
        /*
        private void EGMI_ACK_CHAT(UInt16 id, MemoryStream stream)
        {
            NFMsg.MsgBase xMsg = NFMsg.MsgBase.Parser.ParseFrom(stream);

            NFMsg.ReqAckPlayerChat xData = NFMsg.ReqAckPlayerChat.Parser.ParseFrom(xMsg.MsgData);
            NFUIChat uiChat = mUIModule.GetUI<NFUIChat>();
            if (uiChat == null)
            {
                uiChat = mUIModule.ShowUI<NFUIChat>();
            }

            NFGUID playerID = mHelpModule.PBToNF(xData.PlayerId);
            if (playerID.IsNull())
            {
                return;
            }

            uiChat.AddMessage(playerID, xData.player_name.ToStringUtf8(), xData.chat_channel, xData.chat_type, xData.chat_info.ToStringUtf8());
        }
        */
    }
}