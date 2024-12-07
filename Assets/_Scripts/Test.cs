using PurrNet;
using UnityEngine;
using Channel = PurrNet.Transports.Channel;

public class Test : NetworkBehaviour
{
    private SyncList<int> testList = new();

    protected override void OnInitializeModules()
    {
        base.OnInitializeModules();
    }

    [ServerRpc()]
    private static void TestMethod()
    {
        
    }
}
