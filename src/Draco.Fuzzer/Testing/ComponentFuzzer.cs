namespace Draco.Fuzzer.Testing;

internal abstract class ComponentFuzzer
{
    public virtual void StartTesting(int numEpoch, int numMutations)
    {
        for (int i = 0; i < numEpoch; i++)
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
