using Microsoft.Agents.AI;

namespace ISOAuditAgent.API.Agents.FindingsClassification.Agents;

/// <summary>
/// Wrapper tipado del <see cref="AIAgent"/> de Microsoft.Agents.AI específico
/// para este módulo, para que el contenedor DI pueda registrarlo sin que
/// colisione con otros AIAgent que puedan existir en el host.
/// </summary>
public sealed class FindingsClassificationAgent
{
    public AIAgent Agent { get; }

    public FindingsClassificationAgent(AIAgent agent)
    {
        Agent = agent ?? throw new ArgumentNullException(nameof(agent));
    }
}
