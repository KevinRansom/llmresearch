using System.Threading.Tasks;

namespace ModelWeave.Core;

public interface IPromptRunner
{
    Task<string> RunAsync(PromptRequest request);
}
