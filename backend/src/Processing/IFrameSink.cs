namespace Processing;

using Processing.Models;

public interface IFrameSink
{
    void Add(Reading reading);
}
