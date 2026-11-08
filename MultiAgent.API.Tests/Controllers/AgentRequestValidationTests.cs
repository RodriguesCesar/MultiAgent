using System.ComponentModel.DataAnnotations;
using System.Reflection;
using MultiAgent.API.Controllers;
using Xunit;

namespace MultiAgent.API.Tests.Controllers;

public class AgentRequestValidationTests
{
    [Fact]
    public void AgentRequest_ConstructorParameters_HaveValidationAttributes()
    {
        var constructor = typeof(AgentRequest).GetConstructors(BindingFlags.Public | BindingFlags.Instance).Single();
        var messageParameter = constructor.GetParameters().Single(x => x.Name == "Message");

        Assert.Contains(messageParameter.GetCustomAttributes<RequiredAttribute>(), _ => true);
        Assert.Contains(messageParameter.GetCustomAttributes<MinLengthAttribute>(), x => x.Length == 5);
        Assert.Contains(messageParameter.GetCustomAttributes<MaxLengthAttribute>(), x => x.Length == 4000);
    }
}
