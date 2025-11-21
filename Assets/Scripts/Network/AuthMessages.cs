using Mirror;

namespace AuthMessages
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
