using Mirror;

public struct RedirectMessage : NetworkMessage
{
    public string ip;
    public int port;
    public string username;

    public RedirectMessage(string ip, int port, string username)
    {
        this.ip = ip;
        this.port = port;
        this.username = username;
    }
}