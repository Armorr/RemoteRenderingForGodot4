using Godot;
using System;

namespace Godot.RemoteRendering
{

    internal static class SignalingEventEmitter
    {
        public static void InitSignalingEventEmitter(SignalingManager manager)
        {
            manager.onConnect += OnConnect;
            manager.onCreatedConnection += OnCreatedConnection;
            manager.onDeletedConnection += OnDeletedConnection;
            manager.onDisconnect += OnDisconnect;
            manager.onGotAnswer += OnGotAnswer;
            manager.onGotOffer += OnGotOffer;
            manager.onStart += OnStart;
        }

        private static void OnConnect(string connectionId)
        {
            
        }

        private static void OnCreatedConnection(string connectionId)
        {

        }

        private static void OnDeletedConnection(string connectionId)
        {

        }

        private static void OnDisconnect(string connectionId)
        {

        }

        private static void OnGotAnswer(string connectionId, string sdp)
        {

        }

        private static void OnGotOffer(string connectionId, string sdp)
        {

        }

        private static void OnStart()
        {

        }


    }


}