
using UnityEngine;
using UnityEngine.UI;

public class ConnectionButtonManager : MonoBehaviour
{
    [SerializeField] private PongMaster master = null;
    [SerializeField] private Text IPAddressText = null;
    [SerializeField] private Text portText = null;

    public void EnableClient()
    {
        if (master == null) return;

        master.connectToAddress = IPAddressText.text;
        if (!ushort.TryParse(portText.text, out ushort port)) return;
        master.connectToPort = port;
        master.EnableClient();
    }

    public void DisableClient()
    {
        master.DisableClient();
    }
}
