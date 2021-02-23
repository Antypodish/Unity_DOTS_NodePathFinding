#if UNITY_EDITOR
using UnityEditor;
 
class PlayModeStopButton
{
    [MenuItem("Edit/Stop", priority = 160)]
    static void DoMenuItem()
    {
        EditorApplication.isPlaying = false;
    }
}
#endif