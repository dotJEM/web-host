using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

namespace Demo.Server
{
    [HubName("content")]
    public class DemoContentHub : Hub { }
}