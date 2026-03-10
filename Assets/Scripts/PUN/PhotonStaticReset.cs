#if UNITY_EDITOR
using UnityEditor;
using Photon.Pun;
using System.Diagnostics;
using System;

[InitializeOnLoad]
public static class PhotonPlayModeCleanup
{
    static PhotonPlayModeCleanup()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            Console.WriteLine("Disconnecting Photon Network...");
            PhotonNetwork.Disconnect();
        }
    }
}
#endif