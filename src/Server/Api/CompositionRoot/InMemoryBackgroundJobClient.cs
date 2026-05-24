using Hangfire;
using Hangfire.Common;
using Hangfire.States;

namespace Api.CompositionRoot;

internal sealed class InMemoryBackgroundJobClient : IBackgroundJobClient
{
    public string Create(Job job, IState state) => Guid.NewGuid().ToString("N");

    public bool ChangeState(string jobId, IState state, string expectedState) => true;
}
