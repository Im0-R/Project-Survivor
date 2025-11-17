using Mirror;
using UnityEngine;

public class AuthMessages : MonoBehaviour
{
    public struct RegisterMessage : NetworkMessage
    {
        public string username;
        public string password;
    }

    public struct LoginMessage : NetworkMessage
    {
        public string username;
        public string password;
    }

    public struct AuthResponseMessage : NetworkMessage
    {
        public bool success;
        public string message;
    }
}
