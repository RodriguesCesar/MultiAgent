using MultiAgent.API.Agents;
using MultiAgent.API.Models;

namespace MultiAgent.API.Services;

public class AgentService
{

    #region Agents
    private readonly PlannerAgent _plannerAgent;
    private readonly ResearchAgent _researchAgent;
    private readonly CodeAgent _codeAgent;
    private readonly ReviewAgent _reviewAgent;
    #endregion

    public AgentService(PlannerAgent plannerAgent, ResearchAgent researchAgent, CodeAgent codeAgent, ReviewAgent reviewAgent)
    {
        _plannerAgent = plannerAgent;
        _researchAgent = researchAgent;
        _codeAgent = codeAgent;
        _reviewAgent = reviewAgent;
    }

    public async Task<AgentContextModel> Execute(string message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("A mensagem é obrigatória.", nameof(message));

        var model = new AgentContextModel { UserRequest = message };

        model.Plan = await _plannerAgent.CreatePlan(model, cancellationToken);

        model.Research = await _researchAgent.Research(model, cancellationToken);

        model.Code = await _codeAgent.GenerateCode(model, cancellationToken);

        model.Review = await _reviewAgent.Review(model, cancellationToken);

        return model;
    }
}
