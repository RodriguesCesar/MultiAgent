using Microsoft.AspNetCore.Mvc.ViewEngines;
using MultiAgent.API.Agents;
using MultiAgent.API.Models;
using System.Numerics;

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

    public async Task<AgentContextModel> Execute(string message)
    {
        //Capturando a mensagem
        var model = new AgentContextModel { UserRequest = message };

        //Gerando o plano de ação
        model.Plan = await _plannerAgent.CreatePlan(model);

        //Gerando a pesquisa
        model.Research = await _researchAgent.Research(model);

        //Gerando o código
        model.Code = await _codeAgent.GenerateCode(model);

        //Revisar o código
        model.Review = await _reviewAgent.Review(model);

        //Retornar o resultado
        return model;
    }

}
