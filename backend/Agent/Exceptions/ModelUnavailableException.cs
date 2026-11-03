namespace LinkVaultApi.Agent.Exceptions;

public class ModelUnavailableException : Exception
{
    public string ModelName { get; }

    public ModelUnavailableException(string message, string modelName = "meta-models/muse-spark-open", Exception? innerException = null)
        : base(message, innerException)
    {
        ModelName = modelName;
    }
}

