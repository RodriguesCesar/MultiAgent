namespace MultiAgent.API.Models;

public class AgentContextModel
{
    /// <summary>
    /// Mensagem com a solicitação enviada para o orquestrador
    /// </summary>
    public string? UserRequest { get; set; }

    /// <summary>
    /// Resposta fornecida pelo agente de planejamento
    /// </summary>
    public string? Plan { get; set; }

    /// <summary>
    /// Resposta fornecida pelo agente de pesquisa
    /// </summary>
    public string? Research { get; set; }

    /// <summary>
    /// Resposta fornecdida pelo agente programador
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// Resposta forncedida pelo agente revisor
    /// </summary>
    public string? Review { get; set; }
}
