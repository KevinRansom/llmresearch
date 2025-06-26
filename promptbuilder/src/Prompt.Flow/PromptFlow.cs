using System;
using System.Threading.Tasks;
using Prompt.Core;

namespace Prompt.Flow;

public class PromptFlow<T>
{
    private string _prompt = "";
    private Func<string, T?> _parser = _ => default!;
    private IPromptRunner _runner;
    private int _retryCount = 0;

    public PromptFlow(IPromptRunner runner) => _runner = runner;

    public PromptFlow<T> WithPrompt(string prompt)
    {
        _prompt = prompt;
        return this;
    }

    public PromptFlow<T> Expecting(Func<string, T?> parser)
    {
        _parser = parser;
        return this;
    }

    public PromptFlow<T> WithRetry(int count)
    {
        _retryCount = count;
        return this;
    }

    public async Task<T?> RunAsync()
    {
        for (int i = 0; i <= _retryCount; i++)
        {
            var result = await _runner.RunAsync(new PromptRequest(_prompt));
            var parsed = _parser(result);
            if (parsed != null)
                return parsed;
        }
        return default;
    }
}
