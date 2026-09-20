using UnityEngine;

public class AcornWallet : MonoBehaviour
{
    [SerializeField] private int acorns;

    public int Acorns => acorns;

    public void Collect(int amount)
    {
        acorns += Mathf.Max(0, amount);
    }

    public bool TrySpend(int amount)
    {
        if (amount < 0 || acorns < amount)
            return false;

        acorns -= amount;
        return true;
    }
}
