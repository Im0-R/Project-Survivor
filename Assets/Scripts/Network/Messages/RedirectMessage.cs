using Mirror;

public struct RedirectMessage : NetworkMessage
{
    public string ip;
    public int port;

    public RedirectMessage(string ip, int port)
    {
        this.ip = ip;
        this.port = port;
    }
}
