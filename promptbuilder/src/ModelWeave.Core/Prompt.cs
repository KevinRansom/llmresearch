namespace ModelWeave.Core;

public static class Prompt
{
    public static Prompt<T> WithSignature<T>(string instruction)
        => new Prompt<T>(instruction);
}

public readonly struct Prompt<T>
{
    public string Instruction { get; }

    public Prompt(string instruction)
        => Instruction = instruction;
}
