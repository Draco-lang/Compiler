namespace Draco.SourceGeneration.Lsp.Metamodel;

/// <summary>
/// Indicates in which direction a message is sent in the protocol.
/// </summary>
internal enum MessageDirection
{
    ClientToServer,
    ServerToClient,
    Both,
}
