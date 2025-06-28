using System;

namespace ModelWeave.Core
{
    public interface IDeserializer<T>
    {
        T? Deserialize(string input);
    }
}
