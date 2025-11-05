using UnityEngine;

public class BuildInfo : MonoBehaviour
{
   public static string Version = "Build " + System.DateTime.Now.ToString("yyyyMMddHHmm");
}
