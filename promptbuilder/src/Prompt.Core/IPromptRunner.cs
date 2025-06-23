using System.Threading.Tasks;

namespace Prompt.Core;

public interface IPromptRunner
{
    Task<string> RunAsync(PromptRequest request);
}
