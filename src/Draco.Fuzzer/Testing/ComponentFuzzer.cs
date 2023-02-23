namespace Draco.Fuzzer.Testing;

internal abstract class ComponentFuzzer
{
    public virtual void StartTesting(int numEpoch, int numMutations)
    {
        // If number of epochs is -1 we run forever
        for (int i = 0; i < numEpoch || numEpoch == -1; i++)
        {
            this.RunEpoch();
            for (int j = 0; j < numMutations; j++)
            {
                this.RunMutation();
            }
        }
        Helper.PrintResult();
    }

    public abstract void RunEpoch();
    public abstract void RunMutation();
}
