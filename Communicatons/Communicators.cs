public abstract class Communicator
{
    public enum Type
    {
        Socket,
        Queue
    }
    public abstract Task<List<Dictionary<string, object>>> GetMessage();
}